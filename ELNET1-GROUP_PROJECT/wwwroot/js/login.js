// Open the login modal
function openLoginModal() {
    document.getElementById("login-modal").classList.remove("hidden");
}

// Close the login modal
function closeLoginModal() {
    document.getElementById("login-modal").classList.add("hidden");
    document.getElementById("login-form").reset(); // Reset form fields
    document.getElementById("login-message-error").classList.add("hidden");
    document.getElementById("login-message-error").textContent = '';
    document.getElementById("login-message-success").classList.add("hidden");
    document.getElementById("login-message-success").textContent = '';
}

const passwordInput = document.getElementById('login-password');
const togglePassword = document.getElementById('toggle-password');
const toggleIcon = document.getElementById('toggle-icon');

togglePassword.addEventListener('click', () => {
    const isPasswordHidden = passwordInput.type === 'password';
    passwordInput.type = isPasswordHidden ? 'text' : 'password';
    toggleIcon.classList.toggle('fa-eye');
    toggleIcon.classList.toggle('fa-eye-slash');
});

// Handle login submission
document.getElementById("login-form").addEventListener("submit", async function (event) {
    event.preventDefault();

    const loginData = {
        email: document.getElementById("login-email").value,
        password: document.getElementById("login-password").value
    };

    const errormessageElem = document.getElementById("login-message-error");
    const successmessageElem = document.getElementById("login-message-success");
    errormessageElem.textContent = "";
    successmessageElem.textContent = "";

    try {
        const response = await fetch("/api/auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(loginData)
        });

        if (response.ok) {

            const data = await response.json();
            document.getElementById("login-form").reset();
            window.location.href = data.redirectUrl;
            successmessageElem.textContent = "Login Successful. Redirecting.."
            successmessageElem.classList.remove("hidden");
        } else {
            const errorData = await response.json(); 
            errormessageElem.textContent = errorData.message || "Invalid Credentials. Please check your entered email or password.";
            errormessageElem.classList.remove("hidden");
        }
    } catch (err) {
        errormessageElem.textContent = "Something went wrong. Please try again.";
        errormessageElem.classList.remove("hidden");
        console.error(err);
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