// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

let expiryTime;
const countdownWarningMinutes = 3;
const refreshInterval = 1000; // 1 second
let countdownShown = false; // Prevent multiple warnings

// Fetch expiry from headers once on page load
async function fetchSessionExpiry() {
    try {
        const response = await fetch(window.location.href, { method: 'HEAD' });
        const expiryHeader = response.headers.get("Session-Expiry");
        if (expiryHeader) {
            expiryTime = new Date(expiryHeader).getTime();
        }
    } catch (error) {
        console.error("Failed to fetch session expiry:", error);
    }
}

// Countdown logic
function startSessionCountdown() {
    fetchSessionExpiry(); // Get the initial expiry time

    const countdown = setInterval(() => {
        if (!expiryTime) return;

        const now = new Date().getTime();
        const timeLeft = expiryTime - now;

        // Auto logout when session expires
        if (timeLeft <= 0) {
            clearInterval(countdown);
            autoLogout();
            return;
        }

        // Show warning before expiry (only once)
        if (timeLeft <= countdownWarningMinutes * 60 * 1000 && !countdownShown) {
            countdownShown = true;
            showSessionWarning(Math.ceil(timeLeft / 1000), () => {
                countdownShown = false; // Reset when user stays logged in
            });
        }
    }, refreshInterval);
}

// Show session expiration warning
function showSessionWarning(secondsLeft, onRefresh) {
    const minutes = Math.floor(secondsLeft / 60);
    const seconds = secondsLeft % 60;

    Swal.fire({
        title: 'You still there?',
        html: `<p>Your session is about to expire in <strong id="countdown">${minutes}m ${seconds}s</strong>.</p>`,
        icon: 'warning',
        showCancelButton: true,
        cancelButtonText: 'Logout',
        confirmButtonText: 'Stay Logged In',
        allowOutsideClick: false,
        backdrop: true
    }).then((result) => {
        if (result.isConfirmed) {
            refreshSession(onRefresh);  // Reset session expiry
        } else if (result.dismiss === Swal.DismissReason.cancel) {
            logoutUser();
        }
    });

    // Countdown Timer Update
    const countdownInterval = setInterval(() => {
        secondsLeft--;
        const updatedMinutes = Math.floor(secondsLeft / 60);
        const updatedSeconds = secondsLeft % 60;
        const countdownElement = document.getElementById("countdown");

        if (countdownElement) {
            countdownElement.textContent = `${updatedMinutes}m ${updatedSeconds}s`;
        }

        if (secondsLeft <= 0) {
            clearInterval(countdownInterval);
            logoutUser();
        }
    }, 1000);
}

// Refresh session expiry time
function refreshSession(onRefresh) {
    fetch('/api/auth/refresh-session', { method: 'POST' })
        .then(response => {
            if (!response.ok) throw new Error('Session refresh failed');
            console.log('Session refreshed');
            fetchSessionExpiry();  // Fetch new expiry after refresh
            if (onRefresh) onRefresh();
        })
        .catch(error => console.error(error));
}

// Auto logout when session expires
async function logoutUser() {
    try {
        const response = await fetch("/api/auth/logout", {
            method: "POST",
            credentials: "include",
        });

        if (response.ok) {
            await Swal.fire({
                icon: 'success',
                title: 'Logged Out',
                text: 'You have been successfully logged out!',
                timer: 2000,
                showConfirmButton: false
            });

            window.location.href = "/home";
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Failed to log out. Please try again.',
            });
        }
    } catch (error) {
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'An unexpected error occurred. Please try again.',
        });
    }
}

function autoLogout() {
    Swal.fire({
        icon: 'error',
        title: 'Session Expired',
        text: 'You have been logged out due to inactivity.',
        timer: 5000,
        showConfirmButton: false
    }).then(() => window.location.href = '/home');  // Redirect to home page
}

// Start countdown when page loads
document.addEventListener('DOMContentLoaded', startSessionCountdown);

document.addEventListener("DOMContentLoaded", function () {
    $(document).ajaxStart(function () {
        $("#loading-screen").fadeIn(); // Show loader
    });

    $(document).ajaxStop(function () {
        $("#loading-screen").fadeOut(); // Hide loader
    });
});

//This is the part where the user profile menu the open and close functionality
document.addEventListener("DOMContentLoaded", function () {
    // Dropdown Toggles
    const setupDropdown = (buttonId, dropdownId) => {
        const btn = document.getElementById(buttonId);
        const dropdown = document.getElementById(dropdownId);

        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            dropdown.classList.toggle('hidden');
            dropdown.classList.toggle('active');
        });

        document.addEventListener('click', (e) => {
            if (!dropdown.contains(e.target) && e.target !== btn) {
                dropdown.classList.add('hidden');
                dropdown.classList.remove('active');
            }
        });
    };

    setupDropdown('profile-btn', 'profile-dropdown');
});

const currentPath = window.location.pathname;

// Function to highlight the active link with animation
function setActiveNavLink() {
    document.querySelectorAll(".nav-item").forEach(link => {
        if (link.getAttribute("href") === currentPath) {
            link.classList.add("active");
        } else {
            link.classList.remove("active");
        }
    });
}

setActiveNavLink();

document.addEventListener("DOMContentLoaded", function () {
    var mapContainer = document.getElementById("map");
    if (!mapContainer) {
        console.error("Map container not found!");
        return;
    }

    var map = L.map("map").setView([10.2966, 123.8993], 18); 
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: "&copy; OpenStreetMap contributors",
    }).addTo(map);

    L.marker([10.2966, 123.8993]).addTo(map)
        .bindPopup("<b>Subvi Office</b><br>123 Subvi Street, Cebu City")
        .openPopup();

});
