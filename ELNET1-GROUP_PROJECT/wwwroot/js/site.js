// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

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
