
    document.addEventListener('DOMContentLoaded', function() {
        const modal = document.getElementById('reservationModal');
        const closeModalButton = document.getElementById('closeModal');
        const facilityNameInput = document.getElementById('facilityName');

        document.querySelectorAll('.facility-card').forEach(facility => {
            facility.addEventListener('click', function() {
    
                this.classList.add('clicked');

             
                this.addEventListener('animationend', () => {
                    this.classList.remove('clicked');
                }, { once: true });

             
                const facilityName = this.querySelector('h2').textContent;
                facilityNameInput.value = facilityName;
                modal.classList.remove('hidden');
            });
        });

        closeModalButton.addEventListener('click', function() {
            modal.classList.add('hidden');
        });

        document.getElementById('reservationForm').addEventListener('submit', function(event) {
            event.preventDefault();
            // Handle form submission here
            alert('Reservation submitted for ' + facilityNameInput.value);
            modal.classList.add('hidden');
        });
    });
