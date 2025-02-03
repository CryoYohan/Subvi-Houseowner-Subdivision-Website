function toggleChat() {
    const chat = document.getElementById("chat-container");

    if (chat.classList.contains("open")) {
        chat.classList.remove("open"); // Slide out effect
        setTimeout(() => {
            chat.style.display = "none"; // Hide after animation
        }, 400); // Match transition time
    } else {
        chat.style.display = "flex"; // Show before sliding in
        setTimeout(() => {
            chat.classList.add("open"); // Slide in effect
        }, 10); // Tiny delay to ensure display change happens first
    }

    // If no session ID exists, create one
    if (!sessionStorage.getItem('sessionId')) {
        const sessionId = generateUniqueId();
        sessionStorage.setItem('sessionId', sessionId);
    }
}

function generateUniqueId() {
    return 'session-' + Math.random().toString(36).substr(2, 9); // Generates a unique session ID
}

let lastUserMessage = ""; // Store the last message
let currentMessage = ""; // For new message coming from sender

function sendMessage() {
    const input = document.getElementById("chat-input");
    const message = input.value.trim();
    currentMessage = message;

    if (message) { 
        displayMessage("You", message);
        displayTypingIndicator();
        getBotResponse(message);
    }

    

    input.value = "";  // Clear input field
}

function displayMessage(sender, message) {
    const chatBody = document.getElementById("chat-body");
    const msgDiv = document.createElement("div");
    msgDiv.classList.add("chat-message");

    if (sender === "Bot") {
        msgDiv.classList.add("bot-message");
        typeWriterEffect(msgDiv, message);
    } else {
        msgDiv.classList.add("user-message");
        msgDiv.textContent = message; // User message appears instantly
    }

    chatBody.appendChild(msgDiv);
    chatBody.scrollTop = chatBody.scrollHeight;
}

// Typewriter effect for bot messages
function typeWriterEffect(element, text, speed = 50) {
    let i = 0;
    function type() {
        if (i < text.length) {
            element.textContent += text.charAt(i);
            i++;
            setTimeout(type, speed);
        }
    }
    type();
}

function displayTypingIndicator() {
    const chatBody = document.getElementById("chat-body");
    const typingDiv = document.createElement("div");
    typingDiv.id = "typing-indicator";
    typingDiv.textContent = "Bot is typing...";
    chatBody.appendChild(typingDiv);
    chatBody.scrollTop = chatBody.scrollHeight;
}

function removeTypingIndicator() {
    const typingIndicator = document.getElementById("typing-indicator");
    if (typingIndicator) {
        typingIndicator.remove();
    }
}

function getBotResponse(userMessage) {
    if (currentMessage !== lastUserMessage) {
        lastUserMessage = currentMessage;  
        fetch("/api/chatbot", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ message: userMessage, sessionId: sessionStorage.getItem('sessionId') })
        })
            .then(response => response.json())
            .then(data => {
                console.log(data)
                removeTypingIndicator();
                displayMessage("Bot", data.response); // Display the bot's response
                lastUserMessage = "";
            })
            .catch(error => {
                displayMessage("Bot", "Sorry, something went wrong.");
                lastUserMessage = "";
                console.log("Please check if the bot service is running.")
                console.error("Error:", error);
            });
    }
        
}

function handleKeyPress(event) {
    if (event.key === "Enter") sendMessage();
}
