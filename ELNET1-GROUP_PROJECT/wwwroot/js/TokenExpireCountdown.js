let expiryTime;
const countdownWarningMinutes = 3;
const refreshInterval = 1000; // Check every second

function startSessionCountdown() {
    fetch(window.location.href, { method: 'HEAD' })
        .then(response => {
            const expiryHeader = response.headers.get("Session-Expiry");
            if (expiryHeader) {
                expiryTime = new Date(expiryHeader).getTime();
            }
        });

    setInterval(() => {
        if (!expiryTime) return;

        const now = new Date().getTime();
        const timeLeft = expiryTime - now;

        if (timeLeft <= 0) {
            autoLogout();
            return;
        }

        // Show warning when 3 minutes left
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
            title: 'You still there?',
            html: `<p>Your session is about to expire in <strong id="countdown"></strong>.</p>`,
            icon: 'warning',
            showCancelButton: true,
            cancelButtonText: 'Logout',
            confirmButtonText: 'Stay Logged In',
            allowOutsideClick: false,
            backdrop: true
        }).then((result) => {
            if (result.isConfirmed) {
                refreshSession();  // Refresh the session if "Stay Logged In" is clicked
            } else if (result.dismiss === Swal.DismissReason.cancel) {
                logoutUser();  // Logout if "Logout" is clicked
            }
        });
    }
    document.getElementById("countdown").textContent = `${minutes}m ${seconds}s`;
}

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