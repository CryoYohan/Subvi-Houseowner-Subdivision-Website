let deleteId = '';
let selectedStatus = '';
let homeowners = [];
let selectedHomeownerId = null;
let vehicleToActivate = null;

// Expose to global scope
window.initCarPreview();
window.updateCarModel();
window.onWindowResize();
window.debounce();
window.setupInputListeners();

// Handle status selection and change the button colors
function filterByStatus(status) {
    // Set the selected status
    selectedStatus = status;

    // Update the button styles
    if (status === 'Active') {
        document.getElementById('activeButton').classList.remove('bg-blue-400');
        document.getElementById('activeButton').classList.add('bg-blue-700');
        document.getElementById('inactiveButton').classList.remove('bg-blue-700');
        document.getElementById('inactiveButton').classList.add('bg-blue-400');
    } else {
        document.getElementById('inactiveButton').classList.remove('bg-blue-400');
        document.getElementById('inactiveButton').classList.add('bg-blue-700');
        document.getElementById('activeButton').classList.remove('bg-blue-700');
        document.getElementById('activeButton').classList.add('bg-blue-400');
    }

    fetchVehicles(status)
}

function filterTable() {
    let input = document.getElementById('searchInput').value.toLowerCase();  // Get the search input value
    let tableBody = document.getElementById('vehicleTableBody');
    let tableRows = tableBody.getElementsByTagName('tr');
    let noDataMessage = document.getElementById('noDataMessage');
    let isAnyRowVisible = false;

    if (noDataMessage) {
        noDataMessage.remove();
    }

    for (let row of tableRows) {
        if (row.cells.length < 5) continue;

        let plateNumber = row.cells[0].innerText.toLowerCase();
        let type = row.cells[1].innerText.toLowerCase();
        let status = row.cells[4].innerText.toLowerCase(); // Column 5 is the status
        let color = row.cells[2].innerText.toLowerCase();
        let vehicleBrand = row.cells[3].innerText.toLowerCase();
        let isSearchMatch = plateNumber.includes(input) || type.includes(input) || status.includes(input) || color.includes(input) || vehicleBrand.includes(input);

        if (isSearchMatch) {
            row.style.display = '';
            isAnyRowVisible = true;
        } else {
            row.style.display = 'none';
        }
    }

    if (!isAnyRowVisible) {
        let noDataMessage = document.createElement('tr');
        noDataMessage.id = 'noDataMessage';
        noDataMessage.innerHTML = '<td colspan="6" class="text-center py-4 text-gray-500">No Data Found</td>';
        tableBody.appendChild(noDataMessage);
    }
}

async function fetchVehicles(status = "Active") {
    try {
        const response = await fetch(`/staff/vehicle/registration/data/status/${status}`);

        // Check if the response is ok (status 200)
        if (!response.ok) {
            throw new Error('Failed to fetch vehicle data');
        }

        const data = await response.json();
        renderVehicleTable(data, status); // Pass the fetched data to renderVehicleTable
    } catch (error) {
        console.error("Error fetching vehicle data:", error);
    }
}

// Function to render the table
function renderVehicleTable(data, status) {
    const tableBody = document.getElementById('vehicleTableBody');
    tableBody.innerHTML = "";

    if (data.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="6" class="text-center p-4" style="background-color: white">No ${status} Data Found.</td></tr>`;
    } else {
        data.forEach(vehicle => {
            function formatVehicleType(type) {
                return type.toLowerCase() === 'suv' ? type.toUpperCase() : type.charAt(0).toUpperCase() + type.slice(1);
            }

            let actions = `
                <button onclick="editVehicle(${vehicle.vehicleId})" class="text-blue-600 text-base font-semibold">Edit</button>
            `;

            if (vehicle.status === 'Active') {
                actions += `
                    <button onclick="confirmDelete(${vehicle.vehicleId})" class="text-red-500 text-base font-semibold">Delete</button>
                `;
            } else if (vehicle.status === 'Inactive') {
                actions += `
                    <button onclick="openActivateConfirm(${vehicle.vehicleId})" class="text-green-600 text-base font-semibold">Activate</button>
                `;
            }

            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="p-2 text-center" style="background-color: white">${vehicle.plateNumber}</td>
                <td class="p-2 text-center" style="background-color: white">${formatVehicleType(vehicle.type)}</td>
                <td class="p-2 text-center" style="background-color: white">${vehicle.color}</td>
                <td class="p-2 text-center" style="background-color: white">${vehicle.vehicleBrand}</td>
                <td class="p-2 text-center" style="background-color: white">
                    <span class="status-badge ${vehicle.status === 'Active' ? 'bg-green-500' : vehicle.status === 'Inactive' ? 'bg-red-500' : 'bg-gray-500'} text-white">
                        ${vehicle.status}
                    </span>
                </td>
                <td class="p-2 text-center space-x-2" style="background-color: white">
                    ${actions}
                </td>
            `;

            tableBody.appendChild(row);
        });
    }
}

// Load homeowners on modal open
async function loadHomeowners() {
    try {
        const response = await fetch('/staff/gethomeowners');
        homeowners = await response.json();
        renderHomeowners(homeowners);
    } catch (error) {
        console.error('Error fetching homeowners:', error);
    }
}

// Render homeowners list
function renderHomeowners(data) {
    const list = document.getElementById('homeownerList');
    list.innerHTML = data.length ? data.map(user => `
            <div onclick="selectHomeowner(${user.userId}, '${user.firstName} ${user.lastName}')"
                class="px-4 py-2 cursor-pointer ${selectedHomeownerId === user.userId ? 'bg-blue-100' : 'hover:bg-gray-50'
        } rounded-md">
                <span class="font-medium">${user.firstName} ${user.lastName}</span>
                <span class="text-gray-500 text-sm block">${user.email}</span>
            </div>
        `).join('') : '<div class="text-gray-500 p-4">No homeowners found</div>';
    list.classList.remove('hidden');
}

// Filter homeowners on input
function filterHomeowners() {
    const query = document.getElementById('homeownerSearch').value.toLowerCase();
    const filtered = homeowners.filter(user => {
        const fullName = `${user.firstName} ${user.lastName}`.toLowerCase();
        return user.firstName.toLowerCase().includes(query) ||
            user.lastName.toLowerCase().includes(query) ||
            fullName.includes(query);
    });
    renderHomeowners(filtered);
}

// Select a homeowner
function selectHomeowner(userId, fullName) {
    selectedHomeownerId = userId; // Update selected
    document.getElementById('selectedUserId').value = userId;
    document.getElementById('selectedHomeownerName').innerText = fullName;
    document.getElementById('homeownerSearch').value = '';
    renderHomeowners(homeowners); // Re-render to highlight
}

function showAddModal() {
    $('#vehicleForm')[0].reset();
    document.getElementById('selectedUserId').value = '';
    document.getElementById('selectedHomeownerName').value = '';
    $('#modalTitle').text('Add Vehicle');
    $('#vehicleId').val('');
    $('#vehicleModal').removeClass('hidden').addClass('flex');
    loadHomeowners();
    initCarPreview();
    // Add input listeners
    ['type', 'color', 'plateNumber'].forEach(id => {
        document.getElementById(id).addEventListener('input',
            debounce(updateCarModel, 300)
        );
    });
}

function editVehicle(id) {
    $.get(`/staff/VehicleRegistration/${id}`, function (data) {
        loadHomeowners();
        document.getElementById('selectedUserId').value = data.userId;
        document.getElementById('selectedHomeownerName').textContent = data.homeownerName;
        selectedHomeownerId = data.userId;
        renderHomeowners(data);
        currentCarData = data;

        // Populate form fields
        $('#modalTitle').text('Edit Vehicle');
        $('#vehicleId').val(data.vehicleId);
        $('#plateNumber').val(data.plateNumber);
        $('#type').val(data.type.toLowerCase());
        $('#color').val(data.color);
        $('#vehicleBrand').val(data.vehicleBrand);

        if (carPreviewInitialized) {
            cleanupCarPreview();
        }

        // Initialize preview after short delay to ensure DOM updates
        setTimeout(() => {
            if (!carPreviewInitialized) {
                initCarPreview();
                ['type', 'color', 'plateNumber'].forEach(id => {
                    document.getElementById(id).addEventListener('input',
                        debounce(updateCarModel, 300)
                    );
                });
                carPreviewInitialized = true;
            }
            updateCarModel();
        }, 50);

        // Show modal
        $('#vehicleModal').removeClass('hidden').addClass('flex');
    });
}

function saveVehicle() {
    const vehicleID = $('#vehicleId').val();
    const userId = document.getElementById('selectedUserId').value;

    if (!userId) {
        Swal.fire('Error', 'Please select a homeowner.', 'error');
        return;
    }

    const addvehicle = {
        plateNumber: $('#plateNumber').val(),
        type: $('#type').val(),
        color: $('#color').val(),
        vehicleBrand: $('#vehicleBrand').val(),
        userId: userId
    };

    const editvehicle = {
        vehicleId: vehicleID,
        plateNumber: $('#plateNumber').val(),
        type: $('#type').val(),
        color: $('#color').val(),
        vehicleBrand: $('#vehicleBrand').val(),
        userId: userId
    };

    const url = vehicleID ? `/staff/VehicleRegistration/${vehicleID}` : '/staff/VehicleRegistration';
    const method = vehicleID ? 'PUT' : 'POST';
    const data = vehicleID ? editvehicle : addvehicle;

    $.ajax({
        url: url,
        type: method,
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function () {
            document.getElementById('homeownerSearch').value = '';
            selectedHomeownerId = null;
            document.getElementById('selectedHomeownerName').textContent = null;
            Swal.fire({
                icon: 'success',
                title: vehicleID ? 'Vehicle Updated Successfully!' : 'Vehicle Added Successfully!',
                text: 'The vehicle has been successfully saved.',
                confirmButtonText: 'OK',
                confirmButtonColor: '#3085d6',
            }).then(() => {
                location.reload();
            });
        },
        error: function (xhr, status, error) {
            if (xhr.status === 409) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Duplicate Vehicle',
                    text: xhr.responseText || 'A similar vehicle information already registered in the system.',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#f59e0b'
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Something Went Wrong!',
                    text: 'Operation Failed.',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#d33',
                });
            }
        }
    });
}

function confirmDelete(id) {
    deleteId = id; // This will assign the value properly
    $('#deleteConfirm').removeClass('hidden').addClass('flex');
}

function deleteVehicleConfirmed() {
    if (!deleteId) return; // Ensure deleteId is valid

    // Proceed with the deletion and show a success message after deletion
    $.ajax({
        url: `/staff/vehicleregistration/${deleteId}/deactivate`,
        type: 'PUT',
        success: function () {
            Swal.fire({
                icon: 'success',
                title: 'Deactivated Success!',
                text: 'The vehicle has been deactivated successfully.',
                confirmButtonText: 'OK'
            }).then(() => {
                location.reload(); // Reload page after deletion
            });
        },
        error: function () {
            // Show an error message if something goes wrong
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: 'There was an issue deleting the vehicle.',
                confirmButtonText: 'OK'
            });
        }
    });
}

function closeModal() {
    $('#vehicleModal').addClass('hidden');
    document.getElementById('homeownerSearch').value = '';
    selectedHomeownerId = null;
    document.getElementById('selectedHomeownerName').textContent = null;
    carPreviewInitialized = false;
    $('#vehicleModal').addClass('hidden');
}

function closeDeleteConfirm() {
    $('#deleteConfirm').addClass('hidden');
}

//Activate Vehicle Registration functionality
function openActivateConfirm(vehicleId) {
    vehicleToActivate = vehicleId;
    document.getElementById('activateConfirm').classList.remove('hidden');
}

function closeActivateConfirm() {
    vehicleToActivate = null;
    document.getElementById('activateConfirm').classList.add('hidden');
}

async function activateVehicleConfirmed() {
    if (!vehicleToActivate) return;

    try {
        const response = await fetch(`/staff/vehicle/activate/${vehicleToActivate}`, {
            method: 'POST'
        });

        if (response.ok) {
            closeActivateConfirm();

            Swal.fire({
                icon: 'success',
                title: 'Activated',
                text: 'Vehicle has been successfully reactivated!',
                confirmButtonColor: '#3085d6'
            }).then(() => {
                location.reload();
            });

        } else {
            Swal.fire({
                icon: 'error',
                title: 'Failed',
                text: 'Failed to activate vehicle.',
                confirmButtonColor: '#d33'
            });
        }
    } catch (err) {
        console.error('Error:', err);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'An unexpected error occurred.',
            confirmButtonColor: '#d33'
        });
    }
}

document.addEventListener("DOMContentLoaded", () => {
    filterByStatus('Active');
});