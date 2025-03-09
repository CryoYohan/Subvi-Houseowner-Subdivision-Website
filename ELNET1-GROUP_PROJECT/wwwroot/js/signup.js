// Open the modal
function openSignupModal() {
    document.getElementById("signup-modal").classList.remove("hidden");
}

// Close the modal
function closeSignupModal() {
    document.getElementById("signup-modal").classList.add("hidden");
}

// Handle form submission
document.getElementById("signup-form").addEventListener("submit", async function (event) {
    event.preventDefault();

    const userData = {
        firstname: document.getElementById("firstname").value,
        lastname: document.getElementById("lastname").value,
        email: document.getElementById("email").value,
        password: document.getElementById("password").value,
        role: "Homeowner",
        address: document.getElementById("address").value,
        phoneNumber: document.getElementById("phonenumber").value
    };

    const response = await fetch("/api/auth/signup", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(userData)
    });

    if (response.ok) {
        document.getElementById("signup-form").reset();
        alert("Sign up successful!");
        closeSignupModal();
    } else {
        alert("Sign up failed. Email might already be in use.");
    }
});

async function handleGoogleSignIn(response) {
    const user = jwtDecode(response.credential);

    const checkUser = await fetch("/api/auth/check-google-user", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: user.email })
    });

    const result = await checkUser.json();

    if (result.exists) {
        alert("Login successful!");
        // Redirect user or handle login session
    } else {
        // Show modal for first name & last name
        document.getElementById("googleModal").style.display = "block";
        document.getElementById("googleEmail").value = user.email;
    }
}// Handle Google Sign In
async function handleGoogleSignIn(response) {
    const user = jwtDecode(response.credential);

    const checkUser = await fetch("/api/auth/check-google-user", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: user.email })
    });

    const result = await checkUser.json();

    if (result.exists) {
        // User exists in the database, proceed to login and show profile
        alert("Login successful!");
        displayUserProfile(user);
    } else {
        // User does not exist, show modal to fill first and last name
        document.getElementById("googleModal").style.display = "block";
        document.getElementById("googleEmail").value = user.email;
    }
}

// Handle Submit after filling first and last name
async function submitGoogleSignup() {
    const firstname = document.getElementById("googleFirstName").value;
    const lastname = document.getElementById("googleLastName").value;
    const email = document.getElementById("googleEmail").value;

    const res = await fetch("/api/auth/google-signup", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ firstname, lastname, email })
    });

    if (res.ok) {
        // User signed up successfully, log them in and show profile
        const user = await res.json();
        displayUserProfile(user);
        document.getElementById("googleModal").style.display = "none";
    } else {
        alert("Failed to sign up with Google.");
    }
}

// Display user profile after successful login/signup
function displayUserProfile(user) {
    // Store user info in session/local storage (optional)
    localStorage.setItem("user", JSON.stringify(user));

    // Show the user's profile
    document.getElementById('user-name').innerText = `Welcome, ${user.name}!`;
    document.getElementById('user-email').innerText = `Email: ${user.email}`;
    document.getElementById('user-profile-picture').src = user.profilePicture; // Assuming the API returns a profile picture URL

    // Hide login or signup modals
    closeLoginModal();
    closeSignupModal();
}