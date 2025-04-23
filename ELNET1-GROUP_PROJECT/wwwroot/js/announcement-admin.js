function validateForm(formId, titleId, descId) {
    const title = document.getElementById(titleId).value.trim();
    const description = document.getElementById(descId).value.trim();

    if (title === "" || description === "") {
        Swal.fire({
            icon: 'error',
            title: 'Oops...',
            text: 'Please fill out all fields!',
        });
        return false;
    } else {
        showLoading("Saving announcement...");
        document.getElementById(formId).submit();
        return true;
    }
}

function toggleAddForm() {
    const form = document.getElementById('addAnnouncementForm');
    form.classList.toggle('hidden');
}

// Open Edit Modal
function openAnnouncementEditModal(announcement) {
    console.log(announcement)
    if (typeof announcement === 'string') {
        announcement = JSON.parse(announcement);
    }

    // Populate the form fields safely
    document.getElementById('editAnnouncementId').value = announcement.announcementId;
    document.getElementById('editTitle').value = announcement.title;
    document.getElementById('editDescription').value = announcement.description;

    // Show the modal
    const modal = document.getElementById('editAnnouncementModal');
    modal.classList.remove('hidden');
    setTimeout(() => modal.children[0].classList.add('scale-100'), 50);
}

// Close Modal (For Both Add and Edit)
function closeAnnouncementModal(modalId) {
    const modal = document.getElementById(modalId);
    modal.children[0].classList.remove('scale-100');
    setTimeout(() => modal.classList.add('hidden'), 300);
}

function confirmDelete(id) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            const form = document.createElement('form');
            form.method = 'post';
            form.action = '/Admin/DeleteAnnouncement';
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'id';
            input.value = id;
            form.appendChild(input);
            document.body.appendChild(form);
            form.submit();
        }
    });
}

function toggleDescription(descId, toggleId, shortText, fullText) {
    const descEl = document.getElementById(descId);
    const toggleEl = document.getElementById(toggleId);
    const isExpanded = toggleEl.textContent.trim() === "See less";

    descEl.textContent = isExpanded ? shortText : fullText;
    toggleEl.textContent = isExpanded ? "See more..." : "See less";
}