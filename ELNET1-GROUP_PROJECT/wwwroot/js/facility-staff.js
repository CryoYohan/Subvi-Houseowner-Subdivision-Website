let currentEditingId = null;
let facilities = [];
let selectedFacilityId = null;
let selectedFacilityStatus = "Active";
const facilityModal = document.getElementById('addFacilityModal');
const facilityBootstrapModal = new bootstrap.Modal(facilityModal);

// Fetch facilities data
function fetchFacilitiesByStatus(status = selectedFacilityStatus) {
    selectedFacilityStatus = status;
    fetch(`/staff/by-status/${status}`)
        .then(res => res.json())
        .then(data => {
            facilities = data;
            displayFacilities(facilities);
        });
}

//populate to display facility data card
function displayFacilities(facilitiesToDisplay) {
    const container = document.getElementById("facilityContainer");
    container.innerHTML = ""; // Clear the container

    // Check if there are no facilities to display
    if (facilitiesToDisplay.length === 0) {
        container.innerHTML = `
                    <div class="flex justify-center items-center w-full h-full text-gray-500 text-lg">
                        <i class="fas fa-info-circle mr-2"></i>
                         No ${selectedFacilityStatus} facilities available.
                    </div>
                `;
        return;
    }

    // Display the facilities if data is available
    facilitiesToDisplay.forEach(facility => {
        console.log(facility)
        container.innerHTML += `
                    <div class="border rounded-lg p-4 shadow hover:shadow-lg transition w-full sm:w-1/2 lg:w-1/3 xl:w-1/4 m-2 relative group bg-white">
                        <img src="${facility.image}" alt="${facility.facilityName}"
                             onerror="this.onerror=null; this.src='/images/facilityimage/default-facility.jpg';"
                             class="w-full h-40 object-cover rounded">
                        <h2 class="text-lg font-semibold mt-2">${facility.facilityName}</h2>
                        <p class="text-gray-600 text-sm">${facility.description}</p>
                        <p class="text-blue-700 text-sm mt-1 font-medium">Time: ${facility.availableTime}</p>

                        <!-- Hover buttons below description -->
                        <div class="flex justify-center space-x-2 mt-3">
                            <button class="bg-blue-600 hover:bg-blue-700 text-white text-sm p-2 rounded-lg"
                                    onclick="viewFacility(${facility.facilityId})">
                                <i class="fas fa-eye"></i>
                            </button>
                            <button class="bg-yellow-500 hover:bg-yellow-600 text-white text-sm p-2 rounded-lg"
                                    onclick="editFacility(${facility.facilityId})">
                                <i class="fas fa-edit"></i>
                            </button>
                            ${facility.status === 'Active' ?
                `<button class="bg-red-600 hover:bg-red-700 text-white text-sm p-2 rounded-lg"
                                        onclick="confirmDelete(${facility.facilityId})">
                                    <i class="fas fa-trash-alt"></i>
                                </button>` :
                `<button class="bg-green-600 hover:bg-green-700 text-white text-sm p-2 rounded-lg"
                                        onclick="confirmActivate(${facility.facilityId})">
                                    <i class="fas fa-check-circle"></i> Activate
                                </button>`
            }
                        </div>
                    </div>
                `;
    });
}

// Search functionality
function searchFacilities() {
    const searchTerm = document.getElementById('facilitySearch').value.toLowerCase();
    const filteredFacilities = facilities.filter(facility => {
        return facility.facilityName.toLowerCase().includes(searchTerm) ||
            facility.description.toLowerCase().includes(searchTerm);
    });
    displayFacilities(filteredFacilities);
}

// View Facility details
function viewFacility(facilityId) {
    const facility = facilities.find(facility => facility.facilityId === facilityId);
    document.getElementById('facilityViewName').textContent = facility.facilityName;
    document.getElementById('facilityViewDescription').textContent = facility.description;
    document.getElementById('facilityViewAvailableTime').textContent = `Time: ${facility.availableTime}`;
    document.getElementById('facilityViewImage').src = facility.image;

    // Open modal
    const viewModal = new bootstrap.Modal(document.getElementById('viewFacilityModal'));
    viewModal.show();
}

// Generate time options from 6:00 AM to 11:00 PM with 30 min gap
const startTimeSelect = document.getElementById("startTime");
const endTimeSelect = document.getElementById("endTime");
const validTimes = [];

for (let h = 6; h <= 23; h++) {
    for (let m of [0, 30]) {
        let suffix = h >= 12 ? "PM" : "AM";
        let hour = h % 12 === 0 ? 12 : h % 12;
        let time = `${("0" + hour).slice(-2)}:${m === 0 ? "00" : "30"} ${suffix}`;
        validTimes.push(time);
    }
}

// Populate start and end time dropdowns
startTimeSelect.innerHTML = "";
endTimeSelect.innerHTML = "";

validTimes.forEach(time => {
    startTimeSelect.innerHTML += `<option value="${time}">${time}</option>`;
    endTimeSelect.innerHTML += `<option value="${time}">${time}</option>`;
});

// Description character count
const descInput = document.getElementById("facilityDesc");
const descCount = document.getElementById("descCount");
descInput.addEventListener("input", () => {
    descCount.textContent = descInput.value.length;
});

// Image preview
const imageInput = document.getElementById("facilityImage");
const imagePreview = document.getElementById("imagePreview");

imageInput.addEventListener("change", e => {
    const file = e.target.files[0];
    if (file) {
        imagePreview.src = URL.createObjectURL(file);
        imagePreview.classList.remove("hidden");
    }
});

// Edit button logic
function editFacility(id) {
    fetch(`/staff/get-facility/${id}`)
        .then(res => res.json())
        .then(data => {
            currentEditingId = id;
            const [startTime, endTime] = data.availableTime.split(" - ");
            document.getElementById("facilityName").value = data.facilityName;
            document.getElementById("facilityDesc").value = data.description;
            document.getElementById("startTime").value = startTime.trim();
            document.getElementById("endTime").value = endTime.trim();

            const preview = document.getElementById("imagePreview");
            const imageUrl = `/images/facilityimage/${data.facilityName.replace(/\s+/g, '_')}.jpg`;

            // Set the preview image and fallback to default icon on error
            preview.src = imageUrl;
            preview.onerror = function () {
                this.onerror = null; // prevent infinite loop
                this.src = "/images/facilityimage/default-facility.jpg"; // fallback icon path
            };
            preview.classList.remove("hidden");

            // Clear previous file input
            document.getElementById("facilityImage").value = "";

            // Show modal with updated title and button text
            const modal = new bootstrap.Modal(document.getElementById("addFacilityModal"));
            document.getElementById("addFacilityModalLabel").textContent = "Edit Facility";
            document.getElementById("saveFacilityBtn").textContent = "Update Facility";
            modal.show();
        });
}

// Open modal for "Add New Facility"
function openAddFacilityModal() {
    currentEditingId = null;
    document.getElementById("addFacilityModalLabel").textContent = "Add New Facility";
    document.getElementById("saveFacilityBtn").textContent = "Save Facility";
    document.getElementById("facilityName").value = "";
    document.getElementById("facilityDesc").value = "";
    document.getElementById("startTime").value = "";
    document.getElementById("endTime").value = "";
    document.getElementById("imagePreview").classList.add("hidden");

    const modal = new bootstrap.Modal(document.getElementById("addFacilityModal"));
    modal.show();
}

// Reset form when closing modal or canceling
document.getElementById("cancelBtn").addEventListener("click", resetForm);

// Reset form function
function resetForm() {
    document.getElementById("facilityName").value = "";
    document.getElementById("facilityDesc").value = "";
    document.getElementById("startTime").value = "";
    document.getElementById("endTime").value = "";
    document.getElementById("facilityImage").value = "";
    document.getElementById("imagePreview").classList.add("hidden");
    document.getElementById("facilityNameError").classList.add("hidden");
    document.getElementById("facilityError").classList.add("hidden");
    document.getElementById("timeError").classList.add("hidden");
}

// On Save (edit or new), show confirmation modal
document.getElementById("saveFacilityBtn").addEventListener("click", () => {
    const name = document.getElementById("facilityName").value.trim();
    const desc = document.getElementById("facilityDesc").value.trim();
    const start = document.getElementById("startTime").value;
    const end = document.getElementById("endTime").value;
    const startIndex = validTimes.indexOf(start);
    const endIndex = validTimes.indexOf(end);

    if (!name) {
        document.getElementById("facilityNameError").classList.remove("hidden");
        return;
    } else {
        document.getElementById("facilityNameError").classList.add("hidden");
    }

    if (!desc) {
        document.getElementById("facilityDescError").classList.remove("hidden");
        return;
    } else {
        document.getElementById("facilityDescError").classList.add("hidden");
    }

    if (startIndex >= endIndex) {
        document.getElementById("timeError").classList.remove("hidden");
        return;
    } else {
        document.getElementById("timeError").classList.add("hidden");
    }
    openConfirmationModal();
});

// Open confirmation modal
function openConfirmationModal() {
    const modal = document.getElementById("AddEditConfirmationModal");
    modal.classList.add("show");  // Show the modal
}

// Close confirmation modal
function closeConfirmationModal() {
    const modal = document.getElementById("AddEditConfirmationModal");
    modal.classList.remove("show");  // Hide the modal
}

// Handle the confirmation button in the modal
document.getElementById("confirmBtnModal").addEventListener("click", () => {
    const name = document.getElementById("facilityName").value.trim();
    const desc = document.getElementById("facilityDesc").value.trim();
    const start = document.getElementById("startTime").value;
    const end = document.getElementById("endTime").value;
    const imageInput = document.getElementById("facilityImage");
    const image = imageInput.files.length > 0 ? imageInput.files[0] : null;  // Check if file is selected
    const imageName = name.replace(/\s+/g, '_');

    const availableTime = `${start} - ${end}`;
    const formData = new FormData();
    formData.append("facilityName", name);
    formData.append("description", desc);
    formData.append("availableTime", availableTime);

    if (image) {
        formData.append("image", image, `${imageName}.jpg`);
    }

    let url = "/staff/add-facility";
    let method = "POST";

    if (currentEditingId) {
        formData.append("facilityId", currentEditingId);
        url = `/staff/update-facility/${currentEditingId}`;
    }

    fetch(url, {
        method: method,
        body: formData
    }).then(async (res) => {
        const data = await res.json().catch(() => ({})); // Safely handle cases where no JSON is returned

        if (res.ok) {
            currentEditingId = null;
            resetForm();

            // Close the form modal after success
            const addFacilityModal = bootstrap.Modal.getInstance(document.getElementById("addFacilityModal"));
            addFacilityModal.hide();

            // Show success modal
            const successModal = new bootstrap.Modal(document.getElementById("successModal"));
            successModal.show();
            showToast("Facility has been saved successfully.");
            fetchFacilitiesByStatus(); // Refresh display
        } else {
            const errorMessageContainer = document.getElementById("facilityError");
            errorMessageContainer.textContent = data.message || "An unexpected error occurred.";
            errorMessageContainer.classList.remove("hidden");
            showToast("There was an issue saving the facility. Please try again later.", "red");
        }
    }).catch(error => {
        console.error('Error:', error);
        showToast("An error occurred while processing your request. Please try again later.", "red");
    });

    // Close the confirmation modal after action
    closeConfirmationModal(); // Close confirmation modal
});

// Close confirmation modal on cancel
document.getElementById("cancelBtnModal").addEventListener("click", () => {
    closeConfirmationModal(); // Close the modal without doing anything
});

// Function to show the Activate Confirmation Modal
function confirmActivate(facilityId) {
    selectedFacilityId = facilityId;  // Store the selected facility ID

    // Show the modal using Bootstrap's Modal API
    const confirmationModal = new bootstrap.Modal(document.getElementById('ActivateconfirmationModal'));
    confirmationModal.show();
}

// Handle the Confirm button click to activate the facility
document.getElementById("confirmActivateBtnModal").addEventListener("click", () => {
    fetch(`/staff/activate-facility/${selectedFacilityId}`, {
        method: 'POST'
    })
        .then(res => {
            if (res.ok) {
                // Close the modal after confirming activation
                const confirmationModal = bootstrap.Modal.getInstance(document.getElementById('ActivateconfirmationModal'));
                confirmationModal.hide();  // Close the modal
                showToast("Facility has been activated successfuly.");
                fetchFacilitiesByStatus(); // Refresh the facilities display
            }
        })
        .catch(err => {
            console.error('Error:', err);
            showToast("There was an issue activating the facility.", "red");
        });
});

// Confirm delete or to set it to Inactive action
function confirmDelete(facilityId) {
    selectedFacilityId = facilityId;

    // Show the Bootstrap modal
    const confirmationModal = new bootstrap.Modal(document.getElementById('DeactivateconfirmationModal'));
    confirmationModal.show();
}

// Handle the confirmation button in the modal
document.getElementById("confirmDeactivateBtnModal").addEventListener("click", () => {
    // Proceed with making the facility inactive
    fetch(`/staff/inactive-facility/${selectedFacilityId}`, {
        method: 'POST',
    }).then(res => {
        if (res.ok) {
            // Close the modal after the action
            const confirmationModal = bootstrap.Modal.getInstance(document.getElementById('DeactivateconfirmationModal'));
            confirmationModal.hide();

            // Optionally, refresh the list of facilities or show a success message
            showToast("Facility has been successfully deactivated.");
            fetchFacilitiesByStatus(); // Refresh the facility list
        } else {
            showToast("There was an issue deactivating the facility.","red");
        }
    });
});

function showToast(message, color = 'green') {
    const toast = document.createElement('div');
    toast.className = `fixed top-4 right-4 text-white px-6 py-3 rounded-lg flex items-center gap-2 shadow-lg transform translate-y-20 opacity-0 transition-all z-50`;
    toast.style.backgroundColor = color;
    toast.innerHTML = `<i class="fas fa-check-circle"></i> ${message}`;
    document.body.appendChild(toast);

    setTimeout(() => {
        toast.classList.remove('translate-y-20', 'opacity-0');
        setTimeout(() => {
            toast.classList.add('translate-y-20', 'opacity-0');
            setTimeout(() => toast.remove(), 500);
        }, 4000);
    }, 50);
}

// Initial load
document.addEventListener("DOMContentLoaded", () => {
    fetchFacilitiesByStatus(selectedFacilityStatus);
});