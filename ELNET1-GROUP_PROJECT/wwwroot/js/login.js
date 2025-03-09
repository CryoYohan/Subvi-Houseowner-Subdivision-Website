// Open the login modal
function openLoginModal() {
    document.getElementById("login-modal").classList.remove("hidden");
}

// Close the login modal
function closeLoginModal() {
    document.getElementById("login-modal").classList.add("hidden");
}

// Handle login submission
document.getElementById("login-form").addEventListener("submit", async function (event) {
    event.preventDefault();

    const loginData = {
        email: document.getElementById("login-email").value,
        password: document.getElementById("login-password").value
    };

    const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(loginData)
    });

    if (response.ok) {
        const data = await response.json();
        document.getElementById("login-form").reset();
    //    alert("Login successful!");

        // Redirect to Dashboard
        window.location.href = data.redirectUrl;
    } else {
        alert("Login failed. Invalid credentials.");
    }
});


// Handle Google login directly
async function handleGoogleLogin(response) {
    const user = jwtDecode(response.credential);

    const checkUser = await fetch("/api/auth/check-google-user", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: user.email })
    });

    const result = await checkUser.json();

    if (result.exists) {
        // User exists, login and show profile
        alert("Login successful!");
        displayUserProfile(user);
    } else {
        // Show modal for first name & last name if user is new
        document.getElementById("googleModal").style.display = "block";
        document.getElementById("googleEmail").value = user.email;
    }
}

async function logout() {
    await fetch("/api/auth/logout", {
        method: "POST",
        credentials: "include",
    });

    alert("Logged out!");
    window.location.href = "/login.html";
}