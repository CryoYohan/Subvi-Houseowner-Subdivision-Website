let currentRating = 0;
let currentfilterstatus = '';
function loadFeedbacks() {
    const feedbackType = document.getElementById("filterType").value;
    currentfilterstatus = feedbackType;
    fetch(`/Home/GetFeedbacks?feedbackType=${feedbackType}`)
        .then(res => res.json())
        .then(data => {
            const list = document.getElementById("feedbackCreationList");
            list.innerHTML = "";

            if (data.length === 0) {
                list.innerHTML = `<p class="text-gray-500 font-semibold text-base text-center">No ${currentfilterstatus} feedback yet.</p>`;
                return;
            }

            data.forEach(feedback => {
                const statusClass = feedback.complaintStatus === 'Resolved' ? 'bg-green-600' :
                    feedback.complaintStatus === 'Pending' ? 'bg-red-600' :
                        feedback.complaintStatus === 'Ongoing' ? 'bg-gray-500' : 'hidden';

                const item = document.createElement('div');
                item.className = 'p-4 border rounded-lg shadow-sm hover:shadow-md transition cursor-pointer bg-white hover:bg-blue-50';
                item.innerHTML = `
                    <div class="flex justify-between items-center">
                        <h3 class="text-lg font-semibold">${feedback.feedbackType}</h3>
                        ${feedback.feedbackType === 'Complaint' ? `
                            <p class="text-sm px-3 py-1 rounded-md ${statusClass} text-white">
                                ${feedback.complaintStatus}
                            </p>
                        ` : ''}
                    </div>
                    <p class="text-gray-700 mt-2">${feedback.description}</p>
                    <p class="text-sm text-gray-500 mt-1">${new Date(feedback.dateSubmitted).toLocaleString()}</p>
                `;

                // On click, open modal with feedback details
                item.addEventListener('click', () => {
                    document.getElementById('modalFeedbackType').textContent = feedback.feedbackType;
                    document.getElementById('modalDescription').textContent = feedback.description;
                    document.getElementById('modalDate').textContent = new Date(feedback.dateSubmitted).toLocaleString();

                    const statusElem = document.getElementById('modalStatusContainer');
                    const starElem = document.getElementById('modalRatingContainer');

                    if (feedback.feedbackType === 'Complaint') {
                        statusElem.style.display = 'block';
                        starElem.style.display = 'none';

                        const statusText = feedback.complaintStatus ?? 'N/A';
                        let badgeClass = 'bg-secondary';

                        if (statusText === 'Pending') badgeClass = 'bg-danger';
                        else if (statusText === 'Ongoing') badgeClass = 'bg-warning text-dark';
                        else if (statusText === 'Resolved') badgeClass = 'bg-success';

                        document.getElementById('modalStatus').textContent = statusText;
                        document.getElementById('modalStatus').className = `badge fs-6 ${badgeClass}`;
                    } else if (feedback.feedbackType === 'Compliment') {
                        statusElem.style.display = 'none';
                        starElem.style.display = 'block';
                        const rating = feedback.rating || 0;
                        const starIcons = Array.from({ length: 5 }, (_, i) =>
                            `<i class="bi ${i < rating ? 'bi-star-fill text-yellow-500' : 'bi-star text-gray-400'} fs-5"></i>`
                        ).join('');
                        document.getElementById('modalRating').innerHTML = starIcons;
                    } else {
                        // Suggestion
                        statusElem.style.display = 'none';
                        starElem.style.display = 'none';
                    }

                    const modal = new bootstrap.Modal(document.getElementById('feedbackViewModal'));
                    modal.show();
                });

                list.appendChild(item);
            });
        })
        .catch(error => {
            console.error(error);
            showToast("Failed to load feedbacks", "error");
        });
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
    const description = document.getElementById("feedbackDescription").value.trim();
    const descriptionError = document.getElementById("descriptionError");

    let valid = true;

    // Validate description
    if (!description) {
        descriptionError.classList.remove("hidden");
        valid = false;
    } else {
        descriptionError.classList.add("hidden");
    }

    // Validate rating if type is Compliment
    if (feedbackType === "Compliment" && currentRating === 0) {
        document.getElementById("ratingError").classList.remove("hidden");
        valid = false;
    } else {
        document.getElementById("ratingError").classList.add("hidden");
    }

    if (!valid) return;

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

$(function () {
    loadFeedbacks();
});