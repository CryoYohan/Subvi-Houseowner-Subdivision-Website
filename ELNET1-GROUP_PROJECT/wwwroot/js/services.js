document.querySelectorAll("[data-service-view]").forEach(button => {
    button.addEventListener("click", () => {
        document.getElementById("modal-service-title").textContent = button.getAttribute("data-service-name");
        document.getElementById("modal-service-desc").textContent = button.getAttribute("data-service-desc");
        document.getElementById("modal-service-img").src = button.getAttribute("data-service-img");

        document.getElementById("service-modal").classList.remove("hidden");
    });
});

document.getElementById("close-modal").addEventListener("click", () => {
    document.getElementById("service-modal").classList.add("hidden");
});