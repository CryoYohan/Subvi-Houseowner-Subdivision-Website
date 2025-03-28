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