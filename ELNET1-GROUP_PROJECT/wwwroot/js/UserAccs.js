let selectedUser = null;

function filterByRole() {
    const selectedRole = document.getElementById("roleFilter").value;
    const rows = document.querySelectorAll("#userTableBody tr");
    let visibleCount = 0;

    rows.forEach(row => {
        const role = row.getAttribute("data-role");

        if (selectedRole === "All" || role === selectedRole) {
            row.style.display = "";
            visibleCount++;
        } else {
            row.style.display = "none";
        }
    });

    // Update the user count text
    const userCountText = `${selectedRole}: ${visibleCount} User${visibleCount !== 1 ? "s" : ""}`;
    document.getElementById("userCount").textContent = userCountText;
}

function openUserInfoBox(user) {
    selectedUser = user;
    document.getElementById("userInfoId").textContent = `ID: ${user.id}`;
    document.getElementById("userInfoName").textContent = `Name: ${user.firstname} ${user.lastname}`;
    document.getElementById("userInfoRole").textContent = `Role: ${user.role}`;
    document.getElementById("userInfoAddress").textContent = `Address: ${user.address}`;
    document.getElementById("userInfoPhone").textContent = `Phone: ${user.phoneNumber}`;
    document.getElementById("userInfoEmail").textContent = `Email: ${user.email}`;

    // Show info box and shrink table height
    document.getElementById("userInfoBox").classList.remove("hidden");
    document.getElementById("userTableContainer").style.maxHeight = "190px";
}

function closeUserInfoBox() {
    // Hide info box and restore table height
    document.getElementById("userInfoBox").classList.add("hidden");
    document.getElementById("userTableContainer").style.maxHeight = "545px";
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
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
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
                        Swal.fire('Deleted!', 'The user has been deleted.', 'success');
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