document.addEventListener("DOMContentLoaded", function () {
    $(document).ajaxStart(function () {
        $("#loading-screen").fadeIn(); // Show loader
    });

    $(document).ajaxStop(function () {
        $("#loading-screen").fadeOut(); // Hide loader
    });
});
