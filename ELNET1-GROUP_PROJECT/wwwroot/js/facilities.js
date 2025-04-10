
document.addEventListener('DOMContentLoaded', function () {
    const modal = document.getElementById('reservationModal');
    const closeModalButton = document.getElementById('closeModal');
    const facilityNameInput = document.getElementById('facilityName');

    document.querySelectorAll('.facility-card').forEach(facility => {
        facility.addEventListener('click', function () {

            this.classList.add('clicked');


            this.addEventListener('animationend', () => {
                this.classList.remove('clicked');
            }, { once: true });


            const facilityName = this.querySelector('h2').textContent;
            facilityNameInput.value = facilityName;
            modal.classList.remove('hidden');
        });
    });

    closeModalButton.addEventListener('click', function () {
        modal.classList.add('hidden');
    });

    document.getElementById('reservationForm').addEventListener('submit', function (event) {
        event.preventDefault();
        // Handle form submission here
        alert('Reservation submitted for ' + facilityNameInput.value);
        modal.classList.add('hidden');
    });
});

// Facilities Modal Handling
function showFacilityModal(imageSrc, title, description, staff, hours) {
    const modal = document.getElementById('facilityModal');
    const overlay = document.getElementById('modalOverlay');
    const modalImage = document.getElementById('modalImage');
    const modalTitle = document.getElementById('modalTitle');
    const modalDescription = document.getElementById('modalDescription');
    const modalStaff = document.getElementById('modalStaff');


    // Set modal content
    modalImage.src = imageSrc;
    modalTitle.textContent = title;
    modalDescription.textContent = description;
    modalStaff.innerHTML = staff;


    // Show modal
    modal.classList.remove('hidden');
    setTimeout(() => {
        overlay.classList.remove('opacity-0');
        modal.querySelector('.modal-content').classList.remove('scale-95', 'opacity-0');
    }, 10);
}

function closeFacilityModal() {
    const modal = document.getElementById('facilityModal');
    const overlay = document.getElementById('modalOverlay');

    overlay.classList.add('opacity-0');
    modal.querySelector('.modal-content').classList.add('scale-95', 'opacity-0');

    setTimeout(() => {
        modal.classList.add('hidden');
    }, 300);
}

// Facility Reservation Handling
function confirmReservation(facilityName) {
    // Close the facility modal first
    closeFacilityModal();

    Swal.fire({
        title: 'Confirm Reservation',
        html: `Are you sure you want to reserve <b>${facilityName}</b>?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#2563eb',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Yes, reserve it!',
        allowOutsideClick: false
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Reserved!',
                text: 'Your facility reservation has been submitted.',
                icon: 'success',
                confirmButtonColor: '#2563eb'
            });
        }
    });
}
// Calendar Initialization
document.addEventListener('DOMContentLoaded', function () {
    // Modal Close Handlers
    document.getElementById('modalOverlay').addEventListener('click', closeFacilityModal);
    document.querySelector('[data-modal-close]').addEventListener('click', closeFacilityModal);

    // Facility Card Click Handlers
    document.querySelectorAll('[data-facility]').forEach(button => {
        button.addEventListener('click', () => {
            const card = button.closest('.facility-card');
            const imageSrc = card.querySelector('img').src;
            const title = card.querySelector('h3').textContent;
            const description = card.querySelector('p').textContent;

            // These would typically come from data attributes
            const staff = '<li>John Doe - Maintenance</li><li>Jane Smith - Cleaning</li>';

            showFacilityModal(imageSrc, title, description, staff);
        });
    });

    // Reserve Button Handlers
    document.querySelectorAll('[data-reserve]').forEach(button => {
        button.addEventListener('click', () => {
            const facilityName = button.closest('.facility-card').querySelector('h3').textContent;
            confirmReservation(facilityName);
        });
    });
});

function showCalendarModal() {
    const modal = document.getElementById('calendarModal');
    const overlay = document.getElementById('calendarOverlay');

    modal.classList.remove('hidden');
    setTimeout(() => {
        overlay.classList.remove('opacity-0');
        modal.querySelector('.modal-content').classList.remove('scale-95', 'opacity-0');
    }, 10);

    renderCalendar(currentDate.getMonth(), currentDate.getFullYear());
}

function closeCalendarModal() {
    const modal = document.getElementById('calendarModal');
    const overlay = document.getElementById('calendarOverlay');

    overlay.classList.add('opacity-0');
    modal.querySelector('.modal-content').classList.add('scale-95', 'opacity-0');

    setTimeout(() => {
        modal.classList.add('hidden');
        selectedDate = null;
    }, 300);
}

function updateCalendarHeader(month, year) {
    document.getElementById('currentMonthYear').textContent =
        `${monthNames[month]} ${year}`;
}

function handleDateSelection(date) {
    selectedDate = date;
    document.getElementById('selectedDate').textContent =
        date.toLocaleDateString('en-US', {
            weekday: 'long',
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    document.getElementById('confirmReservationBtn').disabled = false;
}

// Modified Reservation Flow
function confirmReservation(facilityName) {

    closeFacilityModal();

    Swal.fire({
        title: 'Reserve Facility',
        html: `Choose a date for your <b>${facilityName}</b> reservation`,
        icon: 'info',
        showCancelButton: true,
        confirmButtonColor: '#2563eb',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Select Date',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            closeFacilityModal();
            showCalendarModal();
        }
    });
}

// Calendar Cell Styling
function getCalendarCellStyle(date, isToday, isSelected) {
    const baseStyle = 'text-center py-2 rounded-lg cursor-pointer transition-all';
    const dayStyle = date.getMonth() === currentDate.getMonth()
        ? 'text-gray-800 hover:bg-blue-100'
        : 'text-gray-400 hover:bg-gray-100';
    const todayStyle = isToday ? 'border-2 border-blue-400' : '';
    const selectedStyle = isSelected ? 'bg-blue-500 text-white hover:bg-blue-600' : '';

    return `${baseStyle} ${dayStyle} ${todayStyle} ${selectedStyle}`;
}

