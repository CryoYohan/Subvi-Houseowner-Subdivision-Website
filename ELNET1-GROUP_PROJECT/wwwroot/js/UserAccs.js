let selectedUser = null;

function filterByRole() {
    const selectedRole = document.getElementById("roleFilter").value;
    const rows = document.querySelectorAll("#userTableBody tr");
    let visibleCount = 0;

    rows.forEach(row => {
        const role = row.getAttribute("data-role");
        const status = row.getAttribute("data-status");  // Assuming you store the user status in a "data-status" attribute.
        console.log(status)

        // Check for "Inactive" status
        if (selectedRole === "All" || role === selectedRole || (selectedRole === "Inactive" && status === "INACTIVE")) {
            row.style.display = "";
            visibleCount++;
        } else {
            row.style.display = "none";
        }
    });

    // Update the user count text
    const userCountText = `${selectedRole === "Inactive" ? "Inactive" : selectedRole}: ${visibleCount} User${visibleCount !== 1 ? "s" : ""}`;
    document.getElementById("userCount").textContent = userCountText;
}

function searchUsers() {
    const input = document.getElementById("searchInput").value.toLowerCase();
    const rows = document.querySelectorAll("#userTableBody tr");

    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        if (text.includes(input)) {
            row.style.display = "";
        } else {
            row.style.display = "none";
        }
    });
}

function openUserInfoBox(user) {
    selectedUser = user;

    document.getElementById("userInfoId").textContent = `ID: ${user.id}`;
    document.getElementById("userInfoName").textContent = `Name: ${user.firstname} ${user.lastname}`;
    document.getElementById("userInfoRole").textContent = `Role: ${user.role}`;
    document.getElementById("userInfoAddress").textContent = `Address: ${user.address}`;
    document.getElementById("userInfoPhone").textContent = `Phone: ${user.phoneNumber}`;
    document.getElementById("userInfoEmail").textContent = `Email: ${user.email}`;

    const statusBadge = document.getElementById("userStatusBadge");
    if (user.status === "ACTIVE") {
        statusBadge.textContent = "Active";
        statusBadge.classList.remove("bg-red-500");
        statusBadge.classList.add("bg-green-500");
    } else {
        statusBadge.textContent = "Inactive";
        statusBadge.classList.remove("bg-green-500");
        statusBadge.classList.add("bg-red-500");
    }

    // Toggle View More Details button
    const viewMoreBtn = document.getElementById("viewMoreDetailsBtn");
    if (user.role === "Homeowner" || user.role === "Staff") {
        viewMoreBtn.classList.remove("hidden");
    } else {
        viewMoreBtn.classList.add("hidden");
    }

    // Toggle Assign as Staff button
    const assignBtn = document.getElementById("assignAsStaffBtn");
    if (user.role === "Homeowner") {
        assignBtn.classList.remove("hidden");
    } else {
        assignBtn.classList.add("hidden");
    }

    document.getElementById("userInfoBox").classList.remove("hidden");
    document.getElementById("userTableContainer").style.maxHeight = "170px";
}

async function confirmAssignAsStaff() {
    const user = selectedUser;
    const result = await Swal.fire({
        title: 'Confirm Assignment',
        html: `Do you really want to set Homeowner <b>ID ${user.id} - ${user.firstname} ${user.lastname}</b> to be employed as Staff?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Yes, Assign',
        cancelButtonText: 'Cancel',
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33'
    });

    if (result.isConfirmed) {
        try {
            const response = await fetch(`/admin/promotetostaffuser`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(user.id)
            });

            if (response.ok) {
                Swal.fire({
                    icon: 'success',
                    title: 'Promoted!',
                    text: `${user.firstname} ${user.lastname} has been assigned as Staff.`,
                    timer: 1500,
                    showConfirmButton: false
                }).then(() => location.reload());
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Failed',
                    text: 'An error occurred while assigning the user as Staff.'
                });
            }
        } catch (err) {
            console.error(err);
            Swal.fire({
                icon: 'error',
                title: 'Failed',
                text: 'Unable to connect to the server.'
            });
        }
    }
}

function closeUserInfoBox() {
    // Hide info box and restore table height
    document.getElementById("userInfoBox").classList.add("hidden");
    document.getElementById("userTableContainer").style.maxHeight = "520px";
}

async function openMoreDetailsModal() {
    const modal = document.getElementById("moreDetailsModal");
    modal.classList.remove("hidden");

    const personId = selectedUser.personId;

    const res = await fetch(`/admin/getuserfulldetails?personId=${personId}`);
    const data = await res.json();

    // Section 1 - Lot Info
    const personalInfo = data.personalInfo;
    console.log(personalInfo)
    document.getElementById("personalInfoSection").innerHTML = `
        <p><strong>Person Name:</strong> ${personalInfo.firstname} ${personalInfo.lastname}</p>
    `;

    // Section 2 - Lot Info
    const lot = data.lot;
    document.getElementById("lotInfoSection").innerHTML = `
        <p><strong>Block:</strong> ${lot.blockNumber}</p>
        <p><strong>Lot:</strong> ${lot.lotNumber}</p>
        <p><strong>Size:</strong> ${lot.sizeSqm} sqm</p>
        <p><strong>Price:</strong> ₱${lot.price}</p>
        <p><strong>Status:</strong> ${lot.status}</p>
        <p><strong>Description:</strong> ${lot.description || "N/A"}</p>
    `;

    // Section 3 - Application
    const app = data.application;
    document.getElementById("applicationDetailsSection").innerHTML = `
        <p><strong>Date Applied:</strong> ${app.dateApplied}</p>
        <p><strong>Remarks:</strong> ${app.remarks || "N/A"}</p>
    `;

    // Section 4 - Documents
    const docsContainer = document.getElementById("documentsSection");
    docsContainer.innerHTML = '';
    data.documents.forEach(doc => {
        const isImage = /\.(jpg|jpeg|png|gif|webp)$/i.test(doc.filName);
        const div = document.createElement('div');
        div.className = "bg-gray-100 p-2 rounded shadow hover:shadow-md cursor-pointer text-center";

        if (isImage) {
            div.innerHTML = `<img src="${doc.filePath}" class="rounded object-cover h-24 w-full mb-2" alt="${doc.fileName}">
                             <p class="text-sm">${doc.fileName}</p>`;
            div.onclick = () => openImageModal(doc.filePath);
        } else {
            div.innerHTML = `<i class="fas fa-file-alt text-3xl text-blue-600 mb-1"></i>
                             <p class="text-sm">${doc.fileName}</p>`;
            div.onclick = () => window.open(doc.filePath, '_blank');
        }

        docsContainer.appendChild(div);
    });
}

function closeMoreDetailsModal() {
    document.getElementById("moreDetailsModal").classList.add("hidden");
    document.getElementById("personalInfoSection").innerHTML = '';
    document.getElementById("lotInfoSection").innerHTML = '';
    document.getElementById("applicationDetailsSection").innerHTML = '';
    document.getElementById("documentsSection").innerHTML = '';
}

// For adding staff
const modal = document.getElementById('addStaffModal');
const searchInput = document.getElementById('searchInput');
const userList = document.getElementById('userList');
const errorMsg = document.getElementById('modalErrorMsg');
let allUsers = [];
let isSearchBound = false;

function openAddStaffModal() {
    modal.classList.remove('hidden');
    searchInput.value = '';
    errorMsg.classList.add('hidden');
    userList.innerHTML = '';

    loadUsers().then(() => {
        renderUserList(allUsers); // render on load
    });
}

async function loadUsers() {
    const response = await fetch('/admin/gethomeowners');
    allUsers = await response.json();
    console.log(allUsers);  // Log to check if the data is loaded
}

// Function to filter users based on the input value
function filterUsers(query) {
    const keyword = query.toLowerCase(); // Use the passed query
    const filtered = allUsers.filter(u =>
        `${u.firstname} ${u.lastname}`.toLowerCase().includes(keyword)
    );

    // Render filtered users
    renderUserList(filtered);
}

// Render the user list in the modal
function renderUserList(users) {
    const userList = document.getElementById('userList');
    userList.innerHTML = '';  // Clear previous list

    users.forEach(u => {
        const name = `${capitalize(u.firstname)} ${capitalize(u.lastname)}`;
        const item = document.createElement('div');
        item.className = 'flex items-center gap-2';
        item.innerHTML = `
            <input type="checkbox" value="${u.userId}" class="user-checkbox">
            <label class="text-sm">${u.userId} - ${name}</label>
        `;
        userList.appendChild(item);
    });
}

// Capitalize the first letter of the name
function capitalize(text) {
    return text.charAt(0).toUpperCase() + text.slice(1).toLowerCase();
}

// Close the modal and reset everything
function closeAddStaffModal() {
    const modal = document.getElementById('addStaffModal');
    const searchInput = document.getElementById('searchInput');
    const errorMsg = document.getElementById('modalErrorMsg');
    const userList = document.getElementById('userList');

    modal.classList.add('hidden');
    searchInput.value = '';
    userList.innerHTML = '';
    errorMsg.classList.add('hidden');
}

async function saveStaffAssignment() {
    const selectedCheckboxes = Array.from(document.querySelectorAll('.user-checkbox:checked'));
    const selected = selectedCheckboxes.map(cb => cb.value);

    if (selected.length === 0) {
        errorMsg.classList.remove('hidden');
        return;
    }

    // Get user details for confirmation
    const selectedUsers = allUsers.filter(u => selected.includes(u.userId.toString()));
    const userListHtml = selectedUsers.map(u =>
        `<li><strong>ID ${u.userId}</strong> - ${capitalize(u.firstname)} ${capitalize(u.lastname)}</li>`
    ).join('');

    const { isConfirmed } = await Swal.fire({
        title: 'Confirm Staff Promotion',
        html: `
            <p class="mb-2">Do you want to add as Staff the following users?</p>
            <ul class="text-left ml-4 list-disc">${userListHtml}</ul>
        `,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Yes, Add as Staff',
        cancelButtonText: 'Cancel',
        customClass: {
            confirmButton: 'bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700',
            cancelButton: 'px-4 py-2 rounded border'
        }
    });

    if (!isConfirmed) return;

    try {
        const response = await fetch('/admin/promotetostaff', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(selected)
        });

        if (response.ok) {
            closeAddStaffModal();
            Swal.fire({
                title: 'Success!',
                text: 'Selected users were promoted to Staff.',
                icon: 'success',
                timer: 4000,
                showConfirmButton: false
            }).then(() => {
                location.reload();
            });
        } else {
            const errorText = await response.text();
            Swal.fire({
                title: 'Failed!',
                text: 'Something went wrong while assigning staff. Please try again later.',
                icon: 'error',
                confirmButtonText: 'Okay'
            });
        }
    } catch (err) {
        Swal.fire({
            title: 'Error!',
            text: 'Unable to complete the request. Please try again later.',
            icon: 'error',
            confirmButtonText: 'Close'
        });
    }
}

function openEditModalFromBox() {
    openEditModal(selectedUser);
    closeUserInfoBox();
}

function deleteUserFromBox() {
    deleteUser(selectedUser.id)
}

function openEditModal(user, event) {
    if (event) {
        event.stopPropagation();
    }

    // Parse the JSON string into a JavaScript object (if needed)
    if (typeof user === 'string') {
        user = JSON.parse(user);
    }

    // Populate the form fields
    document.getElementById('editUserId').value = user.id;
    document.getElementById('editFirstName').value = user.firstname;
    document.getElementById('editLastName').value = user.lastname;
    document.getElementById('editAddress').value = user.address;
    document.getElementById('editPhoneNumber').value = user.phoneNumber;
    document.getElementById('editEmail').value = user.email;

    // Show the modal
    document.getElementById('editUserModal').classList.remove('hidden');
}

function closeEditModal() {
    document.getElementById('editUserModal').classList.add('hidden');
}

// Handle form submission
document.getElementById('editUserForm').addEventListener('submit', function (event) {
    event.preventDefault(); // Prevent default form submission

    // Submit the form via AJAX or let it submit normally
    this.submit();
});

function deleteUser(userId, event) {
    if (event) {
        event.stopPropagation();
    }

    Swal.fire({
        title: 'Are you sure you want to Delete with ID No. ' + userId + '?',
        text: "By Removing user you just set it to Inactive!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, Remove it!'
    }).then((result) => {
        if (result.isConfirmed) {
            // Send a DELETE request to the server
            fetch(`/Admin/DeleteUser/${userId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                },
            })
                .then(response => {
                    if (response.ok) {
                        closeUserInfoBox();
                        Swal.fire({
                            title: 'Remove!',
                            text: 'The user has been remove.',
                            icon: 'success',
                            timer: 5000, // Close after 5 seconds
                            timerProgressBar: true,
                            didClose: () => {
                                // Redirect after the 5-second delay
                                window.location.href = '/Admin/HomeownerStaffAccounts';
                            }
                        });
                    } else {
                        Swal.fire('Error', 'Failed to delete the user.', 'error');
                    }
                })
                .catch(error => {
                    Swal.fire('Error', 'An error occurred while deleting the user.', 'error');
                });
        }
    });
}

function activateUser(userId, event) {
    if (event) {
        event.stopPropagation();
    }

    Swal.fire({
        title: 'Activate this user?',
        text: `User ID ${userId} will be reactivated.`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#28a745',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, activate!'
    }).then((result) => {
        if (result.isConfirmed) {
            fetch(`/Admin/ActivateUser/${userId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
            })
                .then(response => {
                    if (response.ok) {
                        Swal.fire({
                            title: 'Activated!',
                            text: 'The user has been reactivated.',
                            icon: 'success',
                            timer: 5000,
                            timerProgressBar: true,
                            didClose: () => {
                                window.location.href = '/Admin/HomeownerStaffAccounts';
                            }
                        });
                    } else {
                        Swal.fire('Error', 'Failed to activate the user.', 'error');
                    }
                })
                .catch(error => {
                    Swal.fire('Error', 'An error occurred while activating the user.', 'error');
                });
        }
    });
}