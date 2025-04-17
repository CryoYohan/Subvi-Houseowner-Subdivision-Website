// Enhanced View Service Modal 
function showServiceModal(imagePath, serviceName, serviceDescription) {
    Swal.fire({
        title: serviceName,
        html: `
            <div class="text-left">
                <img src="${imagePath}" alt="${serviceName}" class="w-full h-48 object-cover rounded-lg mb-4">
                <p class="text-gray-600 mb-4">${serviceDescription}</p>
                
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

// Enhanced Request Confirmation Modal
function confirmRequest(serviceName) {
    Swal.fire({
        title: `Request ${serviceName}?`,
        html: `
            <div class="text-left">
                <p class="mb-4">You're requesting <b>${serviceName}</b>. Please complete the details below:</p>
                
                <div class="mb-4">
                    <label for="requestNotes" class="block text-sm font-medium text-gray-700 mb-1">Description</label>
                    <textarea id="requestNotes" class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500" rows="3" placeholder="Provide the Description..."></textarea>
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
            const notes = document.getElementById('requestNotes').value.trim();
            if (!notes) {
                Swal.showValidationMessage("Please enter description for the Service request.");
            }
            return { notes };
        },
        customClass: {
            popup: 'rounded-xl',
            title: 'text-[#1E3A8A] text-xl font-bold mb-2'
        }
    }).then(async (result) => {
        if (result.isConfirmed) {
            const { notes } = result.value;

            try {
                const response = await fetch('/Home/SubmitServiceRequest', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        serviceName: serviceName,
                        notes: notes
                    })
                });

                const result = await response.json();

                if (response.status === 409) {
                    Swal.fire({
                        title: 'Duplicate Request',
                        text: result.message,
                        icon: 'warning',
                        confirmButtonColor: '#F59E0B'
                    });
                    return;
                }

                if (!response.ok) {
                    throw new Error("Request failed");
                }

                fetchServiceRequests();
                Swal.fire({
                    title: 'Request Submitted!',
                    html: `
            <div class="text-left">
                <p>Your <b>${serviceName}</b> request has been received.</p>
                ${notes ? `<p class="mt-2">Description: ${notes}</p>` : ''}
            </div>
        `,
                    icon: 'success',
                    confirmButtonColor: '#60A5FA'
                });

            } catch (error) {
                console.error("Submission error:", error);
                Swal.fire({
                    title: 'Submission Failed',
                    text: 'There was an error submitting your request. Please try again later.',
                    icon: 'error',
                    confirmButtonColor: '#EF4444'
                });
            }
        }
    });
}

// Function to fetch pending service requests and populate the table
async function fetchServiceRequests() {
    const loadingSpinner = document.getElementById('loadingSpinner');
    const noDataMessage = document.getElementById('noDataMessage');
    const tableHead = document.querySelector('#serviceRequestTable thead');
    const tableBody = document.getElementById('serviceRequestBody');

    try {
        loadingSpinner.classList.remove('hidden');
        noDataMessage.classList.add('hidden');
        tableBody.innerHTML = '';
        tableHead.innerHTML = '';

        // Fetch from the backend
        const response = await fetch('/Home/GetPendingServiceRequests');
        const serviceRequests = await response.json();

        loadingSpinner.classList.add('hidden');

        if (serviceRequests.length === 0) {
            noDataMessage.classList.remove('hidden');
            return;
        }

        // Dynamically build table headers for pending status
        tableHead.innerHTML = `
    <tr class="text-sm text-gray-500 text-center">
        <th class="py-2">ID</th>
        <th class="py-2">Req Type</th>
        <th class="py-2">Description</th>
        <th class="py-2">Date Submitted</th>
        <th class="py-2">Status</th>
    </tr>
`;

        // Loop and generate rows
        serviceRequests.forEach(request => {
            const row = document.createElement('tr');
            row.classList.add('text-center'); // ⬅️ Center align all columns in the row

            const reqidCell = document.createElement('td');
            reqidCell.classList.add('py-2');
            reqidCell.textContent = request.serviceRequestId;
            row.appendChild(reqidCell);

            const reqTypeCell = document.createElement('td');
            reqTypeCell.classList.add('py-2');
            reqTypeCell.textContent = request.reqType;
            row.appendChild(reqTypeCell);

            const descriptionCell = document.createElement('td');
            descriptionCell.classList.add('py-2');
            descriptionCell.textContent = request.description;
            row.appendChild(descriptionCell);

            const dateRequestedCell = document.createElement('td');
            dateRequestedCell.classList.add('py-2');
            dateRequestedCell.textContent = new Date(request.dateSubmitted).toLocaleDateString();
            row.appendChild(dateRequestedCell);

            const statusCell = document.createElement('td');
            statusCell.classList.add('py-2');
            const statusSpan = document.createElement('span');
            statusSpan.classList.add('px-2', 'py-1', 'rounded-full');
            if (request.status === 'Pending') {
                statusSpan.classList.add('bg-yellow-100');
                statusSpan.textContent = 'Pending';
            }
            statusCell.appendChild(statusSpan);
            row.appendChild(statusCell);

            tableBody.appendChild(row);
        });
    } catch (error) {
        console.error('Error fetching service requests:', error);
        loadingSpinner.classList.add('hidden');
        noDataMessage.classList.remove('hidden');
    }
}

document.addEventListener("DOMContentLoaded", () => {
    fetchServiceRequests();
    const statusFilter = document.getElementById("statusFilter");
    const searchInput = document.getElementById("searchInput");
    const tableHead = document.getElementById("servicesTableHead");
    const tableBody = document.getElementById("servicesTableBody");
    const loading = document.getElementById("loadingAnimation");
    const fullscreenBtn = document.getElementById("fullscreenToggle");
    const container = document.getElementById("servicesHistoryContainer");

    function renderServices(services, status) {
        tableBody.innerHTML = ""; // Clear rows
        tableHead.innerHTML = ""; // Clear headers

        // Set dynamic column based on status
        let dynamicHeader = status === "Rejected"
            ? "Rejected Reason"
            : "Schedule Date";

        // Create headers
        tableHead.innerHTML = `
        <tr class="text-sm text-gray-500">
            <th class="text-center py-2">Id</th>
            <th class="text-center py-2">Req Type</th>
            <th class="text-center py-2">Date Submitted</th>
            <th class="text-center py-2">Status</th>
            <th class="text-center py-2">${dynamicHeader}</th>
        </tr>
    `;

        // No records
        if (services.length === 0) {
            tableBody.innerHTML = `
            <tr>
                <td colspan="5" class="text-center text-gray-500 py-4">No service records found.</td>
            </tr>`;
            return;
        }

        // Create rows
        services.forEach(s => {
            let dynamicCell = status === "Rejected"
                ? ((s.rejectedReason?.length > 20 ? s.rejectedReason.slice(0, 20) + "..." : s.rejectedReason) || "N/A")
                : (s.scheduleDate || "N/A");

            // Choose badge color based on status
            let statusColor = "";
            switch (s.status) {
                case "Pending":
                    statusColor = "bg-yellow-100 text-yellow-700";
                    break;
                case "Scheduled":
                    statusColor = "bg-blue-100 text-blue-700";
                    break;
                case "Ongoing":
                    statusColor = "bg-blue-100 text-blue-700";
                    break;
                case "Rejected":
                    statusColor = "bg-red-100 text-red-700";
                    break;
                case "Completed":
                    statusColor = "bg-green-100 text-green-700";
                    break;
                default:
                    statusColor = "bg-gray-100 text-gray-600";
            }

            const row = document.createElement("tr");
            row.innerHTML = `
            <td class="py-2 text-center">${s.serviceRequestId}</td>
            <td class="py-2 text-center">${s.requestType}</td>
            <td class="py-2 text-center">${s.dateSubmitted}</td>
            <td class="py-2 text-center">
                <span class="px-2 py-1 rounded-full text-sm font-medium ${statusColor}">
                    ${s.status}
                </span>
            </td>
            <td class="py-2 text-center">${dynamicCell}</td>
        `;
            tableBody.appendChild(row);
        });
    }

    async function fetchServices(status) {
        loading.classList.remove("hidden");
        tableBody.innerHTML = "";
        statusMessage.textContent = ""; // Clear any previous message

        try {
            const response = await fetch(`/Home/ServiceRequests?status=${encodeURIComponent(status)}`);
            if (!response.ok) throw new Error("Failed to fetch service requests");

            const data = await response.json();

            // Filter the data based on search input
            const filtered = data.filter(item => {
                // Assuming the "service" field exists, adjust this based on the actual field
                const serviceType = item.reqType ? item.reqType.toLowerCase() : "";
                const searchQuery = searchInput.value.toLowerCase();
                return serviceType.includes(searchQuery);  // Match with search query
            });

            if (filtered.length === 0) {
                statusMessage.textContent = "No service requests found.";
            } else {
                renderServices(filtered, status); // Pass filtered results to render
            }
        } catch (error) {
            console.error("Error fetching service requests:", error);
            statusMessage.textContent = "Something went wrong while fetching data.";
        } finally {
            loading.classList.add("hidden");
        }
    }

    // Handle search
    searchInput.addEventListener("input", () => {
        fetchServices(statusFilter.value); 
    });

    // Handle dropdown change
    statusFilter.addEventListener("change", () => {
        fetchServices(statusFilter.value);
    });

    // Handle fullscreen toggle
    let isFullscreen = false;
    fullscreenBtn.addEventListener("click", () => {
        container.classList.toggle("fixed");
        container.classList.toggle("top-0");
        container.classList.toggle("left-0");
        container.classList.toggle("w-full");
        container.classList.toggle("h-screen");
        container.classList.toggle("z-50");
        container.classList.toggle("bg-white");
        isFullscreen = !isFullscreen;
    });

    // Load initial data
    fetchServices("Scheduled");
});
