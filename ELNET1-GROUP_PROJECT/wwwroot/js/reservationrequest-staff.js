// Toggle panels
const reservationBtn = document.getElementById("reservationBtn");
const facilityBtn = document.getElementById("facilityBtn");
const reservationPanel = document.getElementById("reservationPanel");
const facilityPanel = document.getElementById("facilityPanel");

//Reservation Panel
reservationBtn.addEventListener("click", () => {
    reservationBtn.classList.replace("bg-gray-200", "bg-blue-900");
    reservationBtn.classList.replace("text-blue-900", "text-white");
    facilityBtn.classList.replace("bg-blue-900", "bg-gray-200");
    facilityBtn.classList.replace("text-white", "text-blue-900");
    reservationPanel.classList.remove("hidden");
    facilityPanel.classList.add("hidden");
});

//Facility Panel
facilityBtn.addEventListener("click", () => {
    facilityBtn.classList.replace("bg-gray-200", "bg-blue-900");
    facilityBtn.classList.replace("text-blue-900", "text-white");
    reservationBtn.classList.replace("bg-blue-900", "bg-gray-200");
    reservationBtn.classList.replace("text-white", "text-blue-900");
    reservationPanel.classList.add("hidden");
    facilityPanel.classList.remove("hidden");
});

document.addEventListener("DOMContentLoaded", () => {
    const reservationTable = document.getElementById("reservationTable");
    const statusFilter = document.getElementById("statusFilter");
    const reserveconfirmationModal = document.getElementById("reserveconfirmationModal");
    const confirmBtn = document.getElementById("confirmBtn");
    const cancelBtn = document.getElementById("cancelBtn");

    let selectedReservationId = null;
    let selectedAction = null;

    // Fetch reservations from backend
    async function fetchReservations(status = "Pending") {
        try {
            const response = await fetch(`/staff/Reservations?status=${status}`);
            const data = await response.json();
            renderReservations(data);
        } catch (error) {
            console.error("Error fetching reservations:", error);
        }
    }

    // Render reservations in table
    function renderReservations(reservations) {
        reservationTable.innerHTML = "";

        if (reservations.length === 0) {
            reservationTable.innerHTML = `
            <tr>
              <td colspan="6" class="text-center p-4 text-gray-500">
                No ${statusFilter.value.charAt(0).toUpperCase() + statusFilter.value.slice(1)} Requests
              </td>
            </tr>`;
            return;
        }

        reservations.forEach((r) => {
            const row = document.createElement("tr");
            row.className = "hover:bg-gray-100 text-base transition";
            row.innerHTML = `
            <td class="p-3 text-center">${r.id}</td>
            <td class="p-3 text-center">${r.facilityName}</td>
            <td class="p-3 text-center">${r.requestedBy}</td>
            <td class="p-3 text-center">${r.schedDate}</td>
            <td class="p-3 text-center">${r.startTime} - ${r.endTime}</td>
            <td class="p-3 text-center">
              <span class="px-2 py-1 text-xs font-semibold rounded ${r.status === "Pending"
                    ? "bg-yellow-500 text-white"
                    : r.status === "Approved"
                        ? "bg-green-500 text-white"
                        : "bg-red-500 text-white"
                }">
                ${r.status.toUpperCase()}
              </span>
            </td>
            <td class="p-3 text-center space-x-2">
              ${r.status === "Pending"
                    ? `
                <button class="approveBtn px-4 py-1 text-sm bg-green-600 text-white rounded hover:bg-green-700 transition" data-id="${r.id}">Approve</button>
                <button class="declineBtn px-4 py-1 text-sm bg-red-600 text-white rounded hover:bg-red-700 transition" data-id="${r.id}">Decline</button>
                `
                    : `<span class="text-gray-500 text-sm">No Actions</span>`
                }
            </td>
          `;
            reservationTable.appendChild(row);
        });

        attachEventListeners();
    }

    // Attach action listeners
    function attachEventListeners() {
        document.querySelectorAll(".approveBtn").forEach((btn) =>
            btn.addEventListener("click", () => openModal(btn.dataset.id, "Approve"))
        );

        document.querySelectorAll(".declineBtn").forEach((btn) =>
            btn.addEventListener("click", () => openModal(btn.dataset.id, "Declined"))
        );
    }

    // Open confirmation modal
    function openModal(reservationId, action) {
        selectedReservationId = reservationId;
        selectedAction = action;
        const modalMessage = document.getElementById("modalMessage");
        modalMessage.textContent = `Are you sure you want to ${action.toLowerCase()} this reservation?`;
        reserveconfirmationModal.classList.remove("hidden");
    }

    // Handle modal buttons
    cancelBtn.addEventListener("click", () => reserveconfirmationModal.classList.add("hidden"));

    confirmBtn.addEventListener("click", async () => {
        if (selectedReservationId && selectedAction) {
            try {
                const response = await fetch(`/staff/reservations/${selectedReservationId}`, {
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        status: selectedAction === "Approve" ? "Approved" : "Declined"
                    }),
                });

                if (!response.ok) throw new Error("Failed to update reservation");

                reserveconfirmationModal.classList.add("hidden");
                fetchReservations(statusFilter.value);

                showToast(`The Reservation Request was ${selectedAction === "Approve" ? "Approved" : "Declined"} successfully.`);
            } catch (error) {
                console.error("Error updating reservation:", error);
                showToast("Failed to update reservation. Try again later.", 'red');
            }
        }
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

    // Filter reservations
    statusFilter.addEventListener("change", () => fetchReservations(statusFilter.value));

    // Initial fetch
    fetchReservations();
});