const sentences = [
    "Experience a safe and well-managed community designed for your convenience and comfort.",
    "Join us and enjoy top-notch amenities and a vibrant neighborhood.",
    "Your dream home awaits in a peaceful and secure environment."
];
let index = 0;
let textElement = document.getElementById("changing-text");

function typeText(text, i, callback) {
    if (i < text.length) {
        textElement.textContent += text.charAt(i);
        setTimeout(() => typeText(text, i + 1, callback), 50);
    } else {
        setTimeout(callback, 2000); // Wait before backspacing
    }
}

function deleteText(i, callback) {
    if (i >= 0) {
        textElement.textContent = textElement.textContent.slice(0, i);
        setTimeout(() => deleteText(i - 1, callback), 30);
    } else {
        callback();
    }
}

function updateText() {
    let currentSentence = sentences[index];
    typeText(currentSentence, 0, () => {
        deleteText(currentSentence.length, () => {
            index = (index + 1) % sentences.length;
            setTimeout(updateText, 900);
        });
    });
}

updateText();