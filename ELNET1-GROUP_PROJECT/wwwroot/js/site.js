// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

/*
let expiryTime;
const countdownWarningMinutes = 3;
const refreshInterval = 1000;

// Fetch expiry from headers once on page load
async function refreshSession() {
    try {
        const response = await fetch(window.location.href, { method: 'HEAD' });
        const expiryHeader = response.headers.get("Session-Expiry");
        if (expiryHeader) {
            expiryTime = new Date(expiryHeader).getTime();
        }
    } catch (error) {
        console.error("Failed to refresh session:", error);
    }
}

// Wrap window.fetch to refresh session on every API call
const originalFetch = window.fetch;
window.fetch = async (...args) => {
    const response = await originalFetch(...args);
    refreshSession();  // Refresh session only when API calls happen
    return response;
};

// Countdown logic
function startSessionCountdown() {
    setInterval(() => {
        if (!expiryTime) return;

        const now = new Date().getTime();
        const timeLeft = expiryTime - now;

        if (timeLeft <= 0) {
            autoLogout();
            return;
        }

        if (timeLeft <= countdownWarningMinutes * 60 * 1000) {
            showSessionWarning(Math.ceil(timeLeft / 1000));
        }
    }, refreshInterval);
}

function showSessionWarning(secondsLeft) {
    const minutes = Math.floor(secondsLeft / 60);
    const seconds = secondsLeft % 60;

    if (!document.getElementById("session-warning")) {
        Swal.fire({
            title: 'Session Expiring Soon',
            html: `<p>Your session will expire in <strong id="countdown"></strong>.</p>`,
            icon: 'warning',
            showCancelButton: false,
            showConfirmButton: false,
            allowOutsideClick: false,
            backdrop: true
        });
    }
    document.getElementById("countdown").textContent = `${minutes}m ${seconds}s`;
}

function autoLogout() {
    Swal.fire({
        icon: 'error',
        title: 'Session Expired',
        text: 'You have been logged out due to inactivity.',
        timer: 5000,
        showConfirmButton: false
    }).then(() => window.location.href = '/home');
}

// Start countdown and session refresh on page load
document.addEventListener('DOMContentLoaded', async () => {
    await refreshSession();
    startSessionCountdown();
});
*/

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
    const userMenuBtn = document.getElementById("user-menu-btn");
    const userDropdown = document.getElementById("user-dropdown");

    userMenuBtn.addEventListener("click", function () {
        userDropdown.classList.toggle("hidden");
    });

    // Close dropdown when clicking outside
    document.addEventListener("click", function (event) {
        if (!userMenuBtn.contains(event.target) && !userDropdown.contains(event.target)) {
            userDropdown.classList.add("hidden");
        }
    });
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
