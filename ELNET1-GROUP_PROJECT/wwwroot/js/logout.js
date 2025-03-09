function openLogoutPopup() {
    document.getElementById("logoutPopup").classList.remove("hidden");
}

function closeLogoutPopup() {
    document.getElementById("logoutPopup").classList.add("hidden");
}

function logout() {
    fetch('/api/auth/logout', { method: 'POST' })
        .then(response => response.json())
        .then(data => {
            closeLogoutPopup();

            let messageBox = document.getElementById("logoutMessage");
            messageBox.classList.remove("hidden");

            setTimeout(() => {
                messageBox.classList.add("hidden");
                window.location.href = data.redirectUrl;
            }, 2500);
        })
        .catch(error => console.error('Logout failed:', error));
}