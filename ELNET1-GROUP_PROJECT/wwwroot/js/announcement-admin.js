function toggleAddForm() {
    const form = document.getElementById('addAnnouncementForm');
    form.classList.toggle('hidden');
}

function confirmDelete(id) {
    if (confirm("Are you sure you want to delete this announcement?")) {
        const form = document.createElement('form');
        form.method = 'post';
        form.action = '/Announcement/DeleteAnnouncement';
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'id';
        input.value = id;
        form.appendChild(input);
        document.body.appendChild(form);
        form.submit();
    }
}