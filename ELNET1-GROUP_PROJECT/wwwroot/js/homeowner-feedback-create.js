let currentRating = 0;
let currentfilterstatus = '';

$('#tabFeedback').click(function () {
    $('#tabFeedback').addClass('bg-blue-600 text-white').removeClass('bg-gray-200 text-gray-700');
    $('#tabComplaint').removeClass('bg-blue-600 text-white').addClass('bg-gray-200 text-gray-700');
    $('#panelFeedback').show();
    $('#panelComplaint').hide();
});

$('#tabComplaint').click(function () {
    $('#tabComplaint').addClass('bg-blue-600 text-white').removeClass('bg-gray-200 text-gray-700');
    $('#tabFeedback').removeClass('bg-blue-600 text-white').addClass('bg-gray-200 text-gray-700');
    $('#panelFeedback').hide();
    $('#panelComplaint').show();
});

function loadFeedbacks() {
    const feedbackType = document.getElementById("filterType").value;
    currentfilterstatus = feedbackType;
    fetch(`/Home/GetFeedbacks?feedbackType=${feedbackType}`)
        .then(res => res.json())
        .then(data => {
            const list = document.getElementById("feedbackCreationList");
            list.innerHTML = "";
            if (data.length === 0) {
                list.innerHTML = '<p class="text-gray-500 font-semibold text-base text-center">No ${currentfilterstatus} feedback yet.</p>';
                return;
            }

            data.forEach(feedback => {
                list.innerHTML += `
                            <div class="p-4 border rounded-lg shadow-sm hover:shadow-md transition hover:blue-600" style="background-color:white">
                                <div class="flex justify-between items-center">
                                    <h3 class="text-lg font-semibold">${feedback.feedbackType}</h3>
                                    ${feedback.feedbackType == 'Complaint' ? `
                                          <p class="text-sm px-3 py-1 rounded-md ${feedback.complaintStatus === 'RESOLVED'
                            ? 'bg-green-600 text-white'
                            : feedback.complaintStatus === 'PENDING'
                                ? 'bg-red-600 text-white'
                                : feedback.complaintStatus === 'ONGOING'
                                    ? 'bg-gray-500 text-white'
                                    : 'hidden'
                        }">
                                            ${feedback.complaintStatus}
                                          </p>
                                        ` : ''}
                                </div>
                                <p class="text-gray-700 mt-2">${feedback.description}</p>
                                <p class="text-sm text-gray-500 mt-1">${new Date(feedback.dateSubmitted).toLocaleString()}</p>
                            </div>
                        `;
            });
        })
        .catch(error => console.error(error));
}

function openFeedbackModal() {
    document.getElementById("feedbackModal").classList.remove("hidden");
    toggleRating();  // Ensures rating visibility is correct when opening
}

function closeFeedbackModal() {
    document.getElementById("feedbackModal").classList.add("hidden");
    document.getElementById("feedbackType").value = "Compliment";
    document.getElementById("feedbackDescription").value = "";
    document.getElementById("charCount").textContent = "0 / 255";
    document.getElementById("charError").classList.add("hidden");
    currentRating = 0;
    toggleRating();
}

function toggleRating() {
    const type = document.getElementById("feedbackType").value;
    document.getElementById("ratingSection").classList.toggle("hidden", type !== "Compliment");
}

function setRating(rating) {
    currentRating = rating;
    const stars = document.querySelectorAll("#ratingSection span");
    stars.forEach((star, index) => {
        star.classList.toggle("text-yellow-400", index < rating);
    });
}

function updateCharCount() {
    const textarea = document.getElementById("feedbackDescription");
    const charCount = document.getElementById("charCount");
    const charError = document.getElementById("charError");
    const length = textarea.value.length;

    charCount.textContent = `${length} / 255`;

    if (length >= 255) {
        charError.classList.remove("hidden");
    } else {
        charError.classList.add("hidden");
    }
}

function submitFeedback() {
    const feedbackType = document.getElementById("feedbackType").value;
    const description = document.getElementById("feedbackDescription").value;
    if (!description.trim()) {
        alert("Please enter a description.");
        return;
    }

    fetch('/Home/AddFeedback', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ feedbackType, description, rating: currentRating })
    })
        .then(() => {
            closeFeedbackModal();
            loadFeedbacks();
        })
        .catch(error => console.error(error));
}

document.addEventListener('DOMContentLoaded', loadFeedbacks);