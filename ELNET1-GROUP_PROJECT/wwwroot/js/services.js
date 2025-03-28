// Service staff assignments (you can fetch this from your backend)
const serviceStaff = {
    "Clean-up Service": ["John Doe", "Maria Garcia", "Robert Chen"],
    "Maintenance Services": ["David Wilson", "Sarah Johnson", "Michael Brown"],
    "Security Services": ["James Smith", "Emma Davis", "William Taylor"]
};

// Enhanced View Service Modal with staff listing
function showServiceModal(imagePath, serviceName, serviceDescription) {
    const staffList = serviceStaff[serviceName].map(staff =>
        `<li class="py-1 flex items-center">
            <span class="w-2 h-2 bg-blue-500 rounded-full mr-2"></span>
            ${staff}
        </li>`
    ).join('');

    Swal.fire({
        title: serviceName,
        html: `
            <div class="text-left">
                <img src="${imagePath}" alt="${serviceName}" class="w-full h-48 object-cover rounded-lg mb-4">
                <p class="text-gray-600 mb-4">${serviceDescription}</p>
                
                <div class="bg-blue-50 p-4 rounded-lg mb-6">
                    <h4 class="font-semibold text-blue-800 mb-2">Assigned Staff:</h4>
                    <ul class="list-none pl-2 text-gray-700">
                        ${staffList}
                    </ul>
                </div>
                
                <button onclick="Swal.close();confirmRequest('${serviceName}')" 
                        class="w-full bg-[#60A5FA] hover:bg-blue-400 text-white font-semibold py-3 px-6 rounded-lg transition-colors duration-200">
                    REQUEST THIS SERVICE
                </button>
            </div>
        `,
        showConfirmButton: false,
        background: '#ffffff',
        width: '650px',
        customClass: {
            popup: 'rounded-xl',
            title: 'text-[#1E3A8A] text-xl font-bold mb-2'
        }
    });
}

// Enhanced Request Confirmation Modal with staff selection
function confirmRequest(serviceName) {
    const staffOptions = serviceStaff[serviceName].map(staff =>
        `<option value="${staff}">${staff}</option>`
    ).join('');

    Swal.fire({
        title: `Request ${serviceName}?`,
        html: `
            <div class="text-left">
                <p class="mb-4">You're requesting <b>${serviceName}</b>. Please complete the details below:</p>
                
                <div class="mb-4">
                    <label for="requestNotes" class="block text-sm font-medium text-gray-700 mb-1">Additional Notes</label>
                    <textarea id="requestNotes" class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500" rows="3" placeholder="Special instructions..."></textarea>
                </div>
            </div>
        `,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#60A5FA',
        cancelButtonColor: '#6B7280',
        confirmButtonText: 'Submit Request',
        cancelButtonText: 'Cancel',
        background: '#ffffff',
        focusConfirm: false,
        preConfirm: () => {
            return {
                preferredStaff: document.getElementById('preferredStaff').value,
                notes: document.getElementById('requestNotes').value
            }
        },
        customClass: {
            popup: 'rounded-xl',
            title: 'text-[#1E3A8A] text-xl font-bold mb-2'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const { preferredStaff, notes } = result.value;

            // Simulate API call
            setTimeout(() => {
                Swal.fire({
                    title: 'Request Submitted!',
                    html: `
                        <div class="text-left">
                            <p>Your <b>${serviceName}</b> request has been received.</p>
                            ${preferredStaff ? `<p class="mt-2">Preferred Staff: ${preferredStaff}</p>` : ''}
                            ${notes ? `<p class="mt-2">Notes: ${notes}</p>` : ''}
                        </div>
                    `,
                    icon: 'success',
                    confirmButtonColor: '#60A5FA'
                });
            }, 800);
        }
    });
}