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

    // Toggle 'View More Details' button visibility
    const viewMoreBtn = document.getElementById("viewMoreDetailsBtn");
    if (user.role === "Homeowner" || user.role === "Staff") {
        viewMoreBtn.classList.remove("hidden");
    } else {
        viewMoreBtn.classList.add("hidden");
    }

    document.getElementById("userInfoBox").classList.remove("hidden");
    document.getElementById("userTableContainer").style.maxHeight = "170px";
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
    document.getElementById('editRole').value = user.role;
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