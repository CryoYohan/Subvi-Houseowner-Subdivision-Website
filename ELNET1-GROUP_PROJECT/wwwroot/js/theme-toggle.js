document.addEventListener("DOMContentLoaded", function () {
    const themeToggle = document.getElementById("theme-toggle");
    const themeIcon = document.getElementById("theme-icon");
    const htmlElement = document.documentElement;
    const currentTheme = localStorage.getItem("theme") || "light";

    function updateTheme(theme) {
        htmlElement.setAttribute("data-theme", theme);
        localStorage.setItem("theme", theme);
        themeIcon.classList.remove("fa-moon", "fa-sun");
        themeIcon.classList.add(theme === "dark" ? "fa-sun" : "fa-moon");
        themeIcon.style.transform = "rotate(360deg)";
        setTimeout(() => (themeIcon.style.transform = ""), 200);

        // Update active item background color
        document.querySelectorAll(".active-item").forEach(item => {
            item.style.backgroundColor = theme === "dark" ? "#1d4ed8" : "#e5e7eb";
        });
    }

    updateTheme(currentTheme);

    themeToggle.addEventListener("click", () => {
        const newTheme = htmlElement.getAttribute("data-theme") === "light" ? "dark" : "light";
        updateTheme(newTheme);
    });
});

