document.addEventListener("DOMContentLoaded", async function () {
    try {
        const response = await fetch("/AdminDashboard/dashboard-data");

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const text = await response.text();

        if (!text) {
            throw new Error("Empty response from server");
        }

        const data = JSON.parse(text); 

        if (!data || Object.keys(data).length === 0) {
            throw new Error("No valid data found in response");
        }

        document.getElementById("facilityCount").textContent = data.facilityCount || 0;
        document.getElementById("totalUsers").textContent = data.totalUsers || 0;
        document.getElementById("adminCount").textContent = data.adminCount || 0;
        document.getElementById("staffCount").textContent = data.staffCount || 0;
        document.getElementById("homeownerCount").textContent = data.homeownerCount || 0;

        document.getElementById("totalReservations").textContent = data.totalReservations || 0;
        document.getElementById("pendingReservations").textContent = data.pendingReservations || 0;
        document.getElementById("approvedReservations").textContent = data.approvedReservations || 0;
        document.getElementById("declinedReservations").textContent = data.declinedReservations || 0;

        document.getElementById("totalRequests").textContent = data.totalRequests || 0;
        document.getElementById("pendingRequests").textContent = data.pendingRequests || 0;
        document.getElementById("scheduledRequests").textContent = data.scheduledRequests || 0;
        document.getElementById("ongoingRequests").textContent = data.ongoingRequests || 0;
        document.getElementById("completedRequests").textContent = data.completedRequests || 0;
        document.getElementById("cancelledRequests").textContent = data.cancelledRequests || 0;
        document.getElementById("declinedRequests").textContent = data.declinedRequests || 0;
    } catch (error) {
        console.error("Error loading dashboard data:", error);
    }

    const weekDaysContainer = document.getElementById("weekDays");
    const prevWeekBtn = document.getElementById("prevWeek");
    const nextWeekBtn = document.getElementById("nextWeek");
    const monthDisplay = document.getElementById("month");
    const yearDisplay = document.getElementById("year");

    let today = new Date();
    let currentDate = new Date(today); // Clone the current date

    function renderWeek() {
        weekDaysContainer.innerHTML = "";

        // Update month and year based on the center date (currentDate)
        monthDisplay.textContent = currentDate.toLocaleString("en-PH", { month: "long" });
        yearDisplay.textContent = currentDate.getFullYear();

        for (let i = -3; i <= 3; i++) {
            let day = new Date(currentDate);
            day.setDate(currentDate.getDate() + i);

            let dayElement = document.createElement("div");
            dayElement.classList.add(
                "p-3", "rounded-lg", "text-center", "w-13", "cursor-pointer",
                "transition-all", "text-gray-600", "shadow-md"
            );

            // Highlight today's date with blue color
            if (
                day.getDate() === today.getDate() &&
                day.getMonth() === today.getMonth() &&
                day.getFullYear() === today.getFullYear()
            ) {
                dayElement.classList.add("bg-blue-600", "text-white", "font-bold");
            } else {
                dayElement.classList.add("bg-white");
            }

            dayElement.innerHTML = `
            <div class="text-sm">${day.toLocaleString("en-US", { weekday: "short" })}</div>
            <div class="text-lg">${day.getDate()}</div>
        `;

            weekDaysContainer.appendChild(dayElement);
        }
    }

    // Event listeners for previous and next week buttons
    prevWeekBtn.addEventListener("click", () => {
        currentDate.setDate(currentDate.getDate() - 7);
        renderWeek();
    });

    nextWeekBtn.addEventListener("click", () => {
        currentDate.setDate(currentDate.getDate() + 7);
        renderWeek();
    });

    // Initial render
    renderWeek();
});
    function toggleDescription(descId, toggleId, shortText, fullText) {
        const descEl = document.getElementById(descId);
        const toggleEl = document.getElementById(toggleId);
        const isExpanded = toggleEl.textContent.trim() === "See less";

        descEl.textContent = isExpanded ? shortText : fullText;
        toggleEl.textContent = isExpanded ? "See more..." : "See less";
    }