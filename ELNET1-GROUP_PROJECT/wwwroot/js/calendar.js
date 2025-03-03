
let currentDate = new Date();
let scheduleData = {};
let selectedDate;
const scheduleList = document.getElementById("schedule-list");
const calendarDays = document.getElementById("calendar-days");
const currentMonthText = document.getElementById("current-month");

async function fetchScheduleData() {
    try {
        const response = await fetch("/api/calendar/schedules");
        const Data = await response.json();
        console.log(Data)

        if (Data) {
            scheduleData = Data;
        } else {
            scheduleData = {
                "2025-03-02": ["Meeting with Admin", "Project Discussion"],
                "2025-03-10": ["Meeting with Admin", "Project Discussion"],
                "2025-03-15": ["Staff Training", "System Maintenance"],
                "2025-03-20": ["Budget Review"]
            };
        }
        renderCalendar();
    } catch (error) {
        console.error("Failed to fetch schedule data:", error);
    }
}

fetchScheduleData();

function renderCalendar() {
    calendarDays.innerHTML = "";
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const today = new Date();

    const todayStr = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
    if (!selectedDate) selectedDate = todayStr;

    currentMonthText.textContent = currentDate.toLocaleString('default', {
        month: 'long',
        year: 'numeric'
    });

    // Create a grid container
    const gridContainer = document.createElement('div');
    gridContainer.className = "grid grid-cols-7 grid-rows-6 gap-2";

    // Add empty cells before the first day
    for (let i = 0; i < firstDay; i++) {
        const emptyCell = document.createElement('div');
        emptyCell.className = "h-22 min-w-24 p-2 opacity-0";
        gridContainer.appendChild(emptyCell);
    }

    // Add days
    for (let day = 1; day <= daysInMonth; day++) {
        const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
        const schedule = scheduleData[dateStr] || { events: [], reservations: 0, reservationDateTime: [] };
        const isToday = dateStr === todayStr;
        const isSelected = dateStr === selectedDate;

        // Create day cell
        const dayCell = document.createElement('div');
        dayCell.className = `h-22 min-w-24 p-2 border rounded-lg relative cursor-pointer transition-colors 
            ${isSelected ? 'calendar-selected-bg' : 'hover:bg-blue-200'}`;
        dayCell.onclick = () => showSchedule(dateStr);

        // Date number
        const dateSpan = document.createElement('span');
        dateSpan.className = "font-medium relative z-10";
        dateSpan.innerHTML = isToday
            ? `<span class="bg-blue-800 text-white rounded-full w-6 h-6 flex items-center justify-center">${day}</span>`
            : day;

        // Event dot indicator (shows if there are events or reservations)
        const eventIndicator = document.createElement('span');
        eventIndicator.className = "w-2 h-2 bg-blue-400 rounded-full";
        eventIndicator.style.visibility = (schedule.events.length > 0 || schedule.reservations > 0) ? "visible" : "hidden";

        // Append top section (date & indicator)
        const topSection = document.createElement('div');
        topSection.className = "flex justify-between items-start";
        topSection.appendChild(dateSpan);
        topSection.appendChild(eventIndicator);
        dayCell.appendChild(topSection);

        // Display Events
        if (schedule.events.length > 0) {
            const eventsDiv = document.createElement('div');
            eventsDiv.className = "mt-1 text-left text-sm truncate";
            eventsDiv.innerHTML = schedule.events.map(event => `<div class="text-blue-900"><strong>• ${event}</strong></div>`).join('');
            dayCell.appendChild(eventsDiv);
        }

        // Display Reservations
        if (schedule.reservations > 0) {
            const reservationsDiv = document.createElement('div');
            reservationsDiv.className = "mt-1 text-left text-sm truncate text-blue-700";
            reservationsDiv.innerHTML = `<div>📌<strong>Reservations: </strong> ${schedule.reservations} </div>`;
            dayCell.appendChild(reservationsDiv);
        }

        gridContainer.appendChild(dayCell);
    }

    // Fill remaining cells
    const totalCells = firstDay + daysInMonth;
    const remainingCells = totalCells % 7 === 0 ? 0 : 7 - (totalCells % 7);
    for (let i = 0; i < remainingCells; i++) {
        const emptyCell = document.createElement('div');
        emptyCell.className = "h-22 min-w-24 p-2 opacity-0";
        gridContainer.appendChild(emptyCell);
    }

    // Append the grid container to the calendar
    calendarDays.appendChild(gridContainer);
}

function showSchedule(date) {
    selectedDate = date;
    scheduleList.innerHTML = "";
    const data = scheduleData[date] || { events: [], reservationDateTime: [] };

    let content = "";

    // 🟦 Events Section
    if (data.events.length > 0) {
        content += `
            <div class="text-center font-bold calendar-event-text-title-color">📅 EVENTS</div>
            <div class="p-3 calendar-event-list-bg rounded-lg shadow-md">
                <ul class="mt-1 space-y-1">
                    ${data.events.map(event => `
                        <li class="flex calendar-event-text-list-color items-center font-semibold">• ${event}</li>
                    `).join('')}
                </ul>
            </div>`;
    }

    if (data.events.length > 0 && data.reservationDateTime.length > 0) {
        content += `<div class="border-t my-3"></div>`;
    }

    if (data.reservationDateTime.length > 0) {
        content += `
            <div class="text-center font-bold calendar-reservation-text-list-color">⏰ RESERVATIONS: ${data.reservationDateTime.length}</div>
            <div class="p-3 calendar-reservation-list-bg rounded-lg shadow-md">
                <ul class="mt-1 space-y-1">
                    ${data.reservationDateTime.map(time => {
                        const formattedTime = new Date(`1970-01-01T${time}`).toLocaleTimeString([], {
                            hour: 'numeric',
                            minute: '2-digit',
                            hour12: true
                        });

                        return `
                    <li class="calendar-reservation-text-list-color flex font-semibold items-center">
                        🕒 ${formattedTime}
                    </li>`;
                    }).join('')}
                </ul>
            </div>`;
    }

    // No Events or Reservations
    if (data.events.length === 0 && data.reservationDateTime.length === 0) {
        content = `<div class="p-3 text-gray-600 bg-gray-100 rounded-lg text-center">No events</div>`;
    }

    scheduleList.innerHTML = content;
    renderCalendar();
}

selectedDate = `${currentDate.getFullYear()}-${String(currentDate.getMonth() + 1).padStart(2, '0')}-${String(currentDate.getDate()).padStart(2, '0')}`;
showSchedule(selectedDate);

document.getElementById("prev-month").addEventListener("click", () => {
    currentDate.setMonth(currentDate.getMonth() - 1);
    renderCalendar();
});

document.getElementById("next-month").addEventListener("click", () => {
    currentDate.setMonth(currentDate.getMonth() + 1);
    renderCalendar();
});

renderCalendar();