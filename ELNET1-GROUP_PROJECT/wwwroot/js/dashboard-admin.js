document.addEventListener("DOMContentLoaded", async function () {
    try {
        const response = await fetch("/dashboard-data");

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const text = await response.text(); // Read as text first
        console.log("Raw Response:", text);

        if (!text) {
            throw new Error("Empty response from server");
        }

        const data = JSON.parse(text); // Convert text to JSON
        console.log("Parsed JSON:", data);

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
        document.getElementById("approvedRequests").textContent = data.approvedRequests || 0;
        document.getElementById("declinedRequests").textContent = data.declinedRequests || 0;
    } catch (error) {
        console.error("Error loading dashboard data:", error);
    }

    const weekDaysContainer = document.getElementById("weekDays");
    const prevWeekBtn = document.getElementById("prevWeek");
    const nextWeekBtn = document.getElementById("nextWeek");
    const monthDisplay = document.getElementById("month");
    const YearDisplay = document.getElementById("year");

    let today = new Date();
    let currentDate = new Date(today); // Clone the current date

    function renderWeek() {
        weekDaysContainer.innerHTML = "";

        // Update month and year
        monthDisplay.textContent = currentDate.toLocaleString("en-US", { month: "long" });
        YearDisplay.textContent = currentDate.getFullYear();

        for (let i = -3; i <= 3; i++) {
            let day = new Date(currentDate);
            day.setDate(today.getDate() + i);

            let dayElement = document.createElement("div");
            dayElement.classList.add(
                "p-3", "rounded-lg", "text-center", "w-14", "cursor-pointer",
                "transition-all", "text-gray-600", "shadow-md"
            );

            if (i === 0) {
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

    prevWeekBtn.addEventListener("click", () => {
        today.setDate(today.getDate() - 7);
        renderWeek();
    });

    nextWeekBtn.addEventListener("click", () => {
        today.setDate(today.getDate() + 7);
        renderWeek();
    });

    renderWeek();
});
