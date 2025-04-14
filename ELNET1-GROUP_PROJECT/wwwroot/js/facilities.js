const facilityModal = document.getElementById('facilityModal');
const facilityBootstrapModal = new bootstrap.Modal(facilityModal); // Add this line

const reservationModal = new bootstrap.Modal(document.getElementById('reservationModal'));

facilityModal.addEventListener('show.bs.modal', function (event) {
    const button = event.relatedTarget;

    const facility = button.getAttribute('data-facility');
    const image = button.getAttribute('data-image');
    const description = button.getAttribute('data-description');
    const time = button.getAttribute('data-time');

    facilityModal.querySelector('#modalTitle').textContent = facility;
    facilityModal.querySelector('#modalImage').src = image;
    facilityModal.querySelector('#modalImage').alt = facility;
    facilityModal.querySelector('#modalDescription').textContent = description;
    facilityModal.querySelector('#modalTime').textContent = time;

    // Set time range in the hidden span
    const [startTime, endTime] = time.split('-').map(t => t.trim());
    const facilityTimeEl = document.getElementById('facilityTime');
    facilityTimeEl.setAttribute('data-start', startTime);
    facilityTimeEl.setAttribute('data-end', endTime);
});

document.addEventListener('DOMContentLoaded', function () {
    const facilityModal = document.getElementById('facilityModal');
    const reserveBtn = document.getElementById('reserve-facility-btn');

    //Fetch data for pending status schedule
    function fetchPendingFacilities() {
        const spinner = document.getElementById('PendingloadingSpinner');
        const tableBody = document.getElementById('pendingFacilitiesTableBody');

        // Show the loading spinner and clear previous data
        spinner.style.display = 'block';
        tableBody.innerHTML = '';

        // Fetch the pending facilities data from API
        fetch('/Home/GetPendingFacilities')
            .then(response => response.json())
            .then(data => {
                // Hide spinner after fetching data
                spinner.style.display = 'none';

                if (data.length === 0) {
                    tableBody.innerHTML = `<tr><td colspan="6" class="text-center text-muted">No pending facilities.</td></tr>`;
                } else {
                    data.forEach(item => {
                        const row = document.createElement('tr');
                        row.classList.add('fade-in'); // Add fade-in effect for row

                        // Create a badge for the status
                        let statusBadge = '';
                        statusBadge = `<span class="badge bg-warning text-dark">${item.status}</span>`;

                        row.innerHTML = `
                        <td class="text-center">${item.reservationId}</td>
                        <td class="text-center">${item.facilityName}</td>
                        <td class="text-center">${item.dateRequested}</td>
                        <td class="text-center">${item.startTime}</td>
                        <td class="text-center">${item.endTime}</td>
                        <td class="text-center">${statusBadge}</td>
                    `;

                        // Append the newly created row to the table body
                        tableBody.appendChild(row);
                    });
                }
            })
            .catch(error => {
                spinner.style.display = 'none';
                tableBody.innerHTML = `<tr><td colspan="6" class="text-center text-danger">Error fetching data. Please try again later.</td></tr>`;
                console.error('Error fetching pending facilities:', error);
            });
    }
    fetchPendingFacilities();

    // For Filtering in Approved/Declined table data
    const statusFilter = document.getElementById("statusFilter");
    const pendingtableBody = document.getElementById("reservationHistoryTableBody");
    const loadingSpinner = document.getElementById("ApprovedloadingSpinner");

    //Fetching Approved/Declined Data
    function fetchReservationData(status) {
        const pendingtableBody = document.getElementById('reservationHistoryTableBody');
        const loadingSpinner = document.getElementById('ApprovedloadingSpinner');

        pendingtableBody.innerHTML = '';
        loadingSpinner.style.display = "block"; // Show loading spinner

        fetch(`/Home/GetFilteredReservations?status=${status}`)
            .then(response => response.json())
            .then(data => {
                loadingSpinner.style.display = "none"; // Hide loading

                if (data.length === 0) {
                    pendingtableBody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center text-muted">No reservation records found for <strong>${status}</strong>.</td>
                </tr>`;
                    return;
                }

                data.forEach(item => {
                    const row = document.createElement("tr");

                    // Correct way to check status and display badge
                    const statusBadge = item.status === "Approved"
                        ? '<span class="badge bg-success px-3 py-2">Approved</span>'
                        : '<span class="badge bg-danger px-3 py-2">Declined</span>';

                    // Append content to the row
                    row.innerHTML = `
                    <td>${item.reservationId}</td>
                    <td>${item.facilityName}</td>
                    <td>${item.dateRequested}</td>
                    <td>${item.startTime}</td>
                    <td>${item.endTime}</td>
                    <td>${statusBadge}</td>
                `;

                    // Append row to the table body
                    pendingtableBody.appendChild(row);
                });
            })
            .catch(error => {
                loadingSpinner.style.display = "none";
                console.error("Error fetching data:", error);
                pendingtableBody.innerHTML = `
            <tr>
                <td colspan="6" class="text-danger text-center">Something went wrong while loading data.</td>
            </tr>`;
            });
    }
    fetchReservationData(statusFilter.value);

    // On filter change
    statusFilter.addEventListener("change", function () {
        fetchReservationData(this.value);
    });

    function truncate(text, length) {
        return text.length > length ? text.substring(0, length) + "..." : text;
    }

    reserveBtn.addEventListener('click', function () {
        // Get data from the facility modal
        const title = facilityModal.querySelector('#modalTitle')?.textContent || '';
        const time = facilityModal.querySelector('#modalTime')?.textContent || '';
        const description = facilityModal.querySelector('#modalDescription')?.textContent || '';
        const image = facilityModal.querySelector('#modalImage')?.src || '';

        // Populate reservation modal with the selected data
        document.getElementById('reservationTitle').textContent = title;
        document.getElementById('reservationDescription').textContent = description;
        document.getElementById('reservationImage').src = image;

        // Populate the Available Time
        const availableTime = facilityModal.querySelector('#facilityTime');
        const startTime = availableTime.getAttribute('data-start'); // "08:00"
        const endTime = availableTime.getAttribute('data-end');   // "18:00"

        // Show the available time in the reservation modal
        document.getElementById('reservationAvailableTime').textContent = `Available Time: ${startTime} - ${endTime}`;

        function convertToMinutes12Hour(timeStr) {
            const [time, modifier] = timeStr.split(" ");
            let [hours, minutes] = time.split(":").map(Number);

            if (modifier === "PM" && hours !== 12) hours += 12;
            if (modifier === "AM" && hours === 12) hours = 0;

            return hours * 60 + minutes;
        }

        function formatTo12Hour(minutes) {
            const hours24 = Math.floor(minutes / 60);
            const minutesPart = minutes % 60;
            const period = hours24 >= 12 ? "PM" : "AM";
            const hours12 = hours24 % 12 || 12;

            return `${hours12}:${minutesPart.toString().padStart(2, "0")} ${period}`;
        }

        function generateTimeSlots(start, end, addExtraMinute = false) {
            const slots = [];
            const adjustedEnd = addExtraMinute ? end + 1 : end;

            for (let time = start; time < adjustedEnd; time += 30) {
                slots.push(formatTo12Hour(time));
            }

            return slots;
        }

        const startTimeMin = convertToMinutes12Hour(startTime);
        const endTimeMin = convertToMinutes12Hour(endTime);

        // Generate separate time slots
        const startSlots = generateTimeSlots(startTimeMin, endTimeMin);
        const endSlots = generateTimeSlots(startTimeMin + 30, endTimeMin, true); // +1 minute for end

        // Populate the Start Time dropdown
        const startSelect = document.getElementById('startTimeSlot');
        startSelect.innerHTML = '';
        startSlots.forEach(slot => {
            const option = document.createElement('option');
            option.value = slot;
            option.textContent = slot;
            startSelect.appendChild(option);
        });

        // Populate the End Time dropdown
        const endSelect = document.getElementById('endTimeSlot');
        endSelect.innerHTML = '';
        endSlots.forEach(slot => {
            const option = document.createElement('option');
            option.value = slot;
            option.textContent = slot;
            endSelect.appendChild(option);
        });
        const today = new Date();
        const todayCell = [...document.querySelectorAll('#calendarTable td')].find(td => td.textContent == today.getDate());
        if (todayCell) todayCell.classList.add('selected');

        // Show the reservation modal
        reservationModal.show();
    });

    // Calendar functionality
    let currentMonth = new Date().getMonth(); // Current month (0 - 11)
    let currentYear = new Date().getFullYear();

    function generateCalendar() {
        const currentMonthYear = document.getElementById('currentMonthYear');
        currentMonthYear.textContent = `${new Date(currentYear, currentMonth).toLocaleString('default', { month: 'long' })} ${currentYear}`;

        const firstDay = new Date(currentYear, currentMonth, 1).getDay();
        const lastDate = new Date(currentYear, currentMonth + 1, 0).getDate();
        const calendarTable = document.getElementById('calendarTable').getElementsByTagName('tbody')[0];

        calendarTable.innerHTML = ''; // Clear existing dates

        let row = document.createElement('tr');
        let day = 1;

        for (let i = 0; i < 6; i++) { // 6 rows max
            while (day <= lastDate && (i > 0 || day > firstDay)) {
                const cell = document.createElement('td');
                cell.textContent = day;
                cell.style.cursor = 'pointer';
                cell.addEventListener('click', function () {
                    // Highlight selected date
                    const allCells = calendarTable.getElementsByTagName('td');
                    Array.from(allCells).forEach(cell => cell.classList.remove('selected'));
                    cell.classList.add('selected');
                });
                row.appendChild(cell);
                day++;
            }
            calendarTable.appendChild(row);
            row = document.createElement('tr');
        }
    }

    document.getElementById('prevMonth').addEventListener('click', function () {
        currentMonth--;
        if (currentMonth < 0) {
            currentMonth = 11;
            currentYear--;
        }
        generateCalendar();
    });

    document.getElementById('nextMonth').addEventListener('click', function () {
        currentMonth++;
        if (currentMonth > 11) {
            currentMonth = 0;
            currentYear++;
        }
        generateCalendar();
    });

    generateCalendar(); // Initial calendar setup

    let reserveFacilityBtn = document.getElementById('confirmReservationBtn');

    reserveFacilityBtn.addEventListener('click', async function () {
        const selectedDateEl = document.querySelector('#calendarTable .selected');
        const startTime = document.getElementById('startTimeSlot').value;
        const endTime = document.getElementById('endTimeSlot').value;

        if (!selectedDateEl || !startTime || !endTime) {
            alert('Please select a date and both start and end time!');
            return;
        }

        const selectedDay = selectedDateEl.textContent;
        const selectedDate = `${currentYear}-${String(currentMonth + 1).padStart(2, '0')}-${String(selectedDay).padStart(2, '0')}`;

        // Validate time logic
        const start = new Date(`1970-01-01T${startTime}`);
        const end = new Date(`1970-01-01T${endTime}`);
        if (start >= end) {
            alert("Start time must be before end time.");
            return;
        }

        const facilityName = document.getElementById('reservationTitle').textContent;

        // Check if there's an existing reservation
        const checkResponse = await fetch(`/Home/CheckReservationConflict?facilityName=${facilityName}&selectedDate=${selectedDate}&startTime=${startTime}&endTime=${endTime}`);
        const checkResult = await checkResponse.json();

        if (!checkResult.success) {
            alert(checkResult.message);
            return;
        }

        // Proceed to insert reservation
        const res = await fetch(`/Home/AddReservation`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ facilityName, selectedDate, startTime, endTime })
        });

        const result = await res.json();
        if (result.success) {
            alert("Reservation successfully added!");
            fetchPendingFacilities();
            facilityBootstrapModal.hide();
            reservationModal.hide();
        } else {
            alert("Failed to add reservation.");
        }
    });
});