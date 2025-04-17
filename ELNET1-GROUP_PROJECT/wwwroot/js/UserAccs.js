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

    // Set status badge text and color based on user status
    const statusBadge = document.getElementById("userStatusBadge");
    if (user.status === "ACTIVE") {
        statusBadge.textContent = "Active";
        statusBadge.classList.remove("bg-red-500");
        statusBadge.classList.add("bg-green-500");
    } else if (user.status === "INACTIVE") {
        statusBadge.textContent = "Inactive";
        statusBadge.classList.remove("bg-green-500");
        statusBadge.classList.add("bg-red-500");
    }

    // Show info box and shrink table height
    document.getElementById("userInfoBox").classList.remove("hidden");
    document.getElementById("userTableContainer").style.maxHeight = "170px";
}

function closeUserInfoBox() {
    // Hide info box and restore table height
    document.getElementById("userInfoBox").classList.add("hidden");
    document.getElementById("userTableContainer").style.maxHeight = "520px";
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