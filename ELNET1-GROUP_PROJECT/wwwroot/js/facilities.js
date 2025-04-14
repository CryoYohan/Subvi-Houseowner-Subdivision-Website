const facilityModal = document.getElementById('facilityModal');
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

    // Set cleaner list
    const cleanerList = facilityModal.querySelector('#modalCleaners');
    cleanerList.innerHTML = '';
    cleaner.split(',').forEach(name => {
        const li = document.createElement('li');
        li.textContent = name.trim();
        cleanerList.appendChild(li);
    });
});

document.addEventListener('DOMContentLoaded', function () {
    const facilityModal = document.getElementById('facilityModal');
    const reservationModal = new bootstrap.Modal(document.getElementById('reservationModal'));
    const reserveBtn = document.getElementById('reserve-facility-btn');

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

        // Show the reservation modal
        reservationModal.show();
    });

    // Helper function to convert time (HH:MM) to minutes
    function convertToMinutes(time) {
        const [hours, minutes] = time.split(':').map(num => parseInt(num, 10));
        return hours * 60 + minutes;
    }

    async function loadReservedSlots(facilityName, selectedDate) {
        const response = await fetch(`/YourController/GetReservedTimeSlots?facilityName=${encodeURIComponent(facilityName)}&selectedDate=${selectedDate}`);
        const reservations = await response.json();

        const disabledMinutes = [];

        // Convert all reservation ranges to minute ranges
        reservations.forEach(({ StartTime, EndTime }) => {
            const startMin = convertToMinutes12Hour(StartTime);
            const endMin = convertToMinutes12Hour(EndTime);
            for (let min = startMin; min < endMin; min += 30) {
                disabledMinutes.push(min);
            }
        });

        // Generate slots
        const startTimeMin = convertToMinutes12Hour(startTime);
        const endTimeMin = convertToMinutes12Hour(endTime);
        const startSlots = generateTimeSlots(startTimeMin, endTimeMin);
        const endSlots = generateTimeSlots(startTimeMin + 30, endTimeMin, true);

        // Populate dropdowns
        const startSelect = document.getElementById('startTimeSlot');
        const endSelect = document.getElementById('endTimeSlot');
        startSelect.innerHTML = '';
        endSelect.innerHTML = '';

        startSlots.forEach(slot => {
            const minutes = convertToMinutes12Hour(slot);
            const option = document.createElement('option');
            option.value = slot;
            option.textContent = slot;
            if (disabledMinutes.includes(minutes)) option.disabled = true;
            startSelect.appendChild(option);
        });

        endSlots.forEach(slot => {
            const minutes = convertToMinutes12Hour(slot);
            const option = document.createElement('option');
            option.value = slot;
            option.textContent = slot;
            if (disabledMinutes.includes(minutes)) option.disabled = true;
            endSelect.appendChild(option);
        });
    }


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
                    console.log(`Selected Date: ${day}/${currentMonth + 1}/${currentYear}`);
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


    // NOT DONE YET DO NOT TOUCH
    document.getElementById('calendarTable').addEventListener('click', (e) => {
        if (e.target.classList.contains('date-cell')) {
            const selectedDate = e.target.dataset.date; // assume you store the date on cell
            const facilityTitle = document.getElementById('reservationTitle').textContent;
            loadReservedSlots(facilityTitle, selectedDate);
        }
    });

    // Reserve Facility button action
    reserveFacilityBtn.addEventListener('click', function () {
        const selectedDate = document.querySelector('#calendarTable .selected');
        const selectedTimeSlot = document.getElementById('timeSlot').value;

        if (!selectedDate) {
            alert('Please select a date!');
            return;
        }

        if (!selectedTimeSlot) {
            alert('Please select a time slot!');
            return;
        }

        const reservationData = {
            facilityId: document.getElementById('reservationFacilityId').value,
            title: document.getElementById('reservationTitle').textContent,
            date: selectedDate.textContent,
            timeSlot: selectedTimeSlot,
        };

        console.log('Reservation Data:', reservationData);
        // Perform API call or reservation logic here...
        alert(`Reservation for ${reservationData.title} on ${reservationData.date} at ${reservationData.timeSlot}`);
    });
});