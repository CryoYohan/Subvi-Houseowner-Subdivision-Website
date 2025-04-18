let currentTab = 'Complaint';
let currentFeedbackId = null;
let isSearchMode = false;

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

function formatDate(dateString) {
    const date = new Date(dateString);
    const options = {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
        hour12: true,
    };
    return date.toLocaleString('en-US', options);
}

$('.inner-tab-btn').click(function () {
    $('.inner-tab-btn').removeClass('active');
    $(this).addClass('active');
    currentTab = $(this).data('type');
    currentFeedbackId = null;

    $('#feedbackTypeLabel').text(`${currentTab} Feedback`);
    $('#feedbackConvList').html('');
    $('#conversationSection').html('');
    $('#searchToggle').removeClass('d-none');
    $('#viewDetailsBtn').addClass('d-none');
    $('#messageInputSection').addClass('d-none');

    // Reset search mode when switching tabs
    isSearchMode = false;
    $('#searchInputContainer').hide();
    $('#searchInput').val('');
    $('#searchToggle').removeClass('fa-times').addClass('fa-search');

    // Hide/show input based on tab
    if (currentTab === 'Complaint') {
        
        $('#messageInputSection').show();
    } else {
        $('#messageInputSection').hide();
    }

    // Feedback header label
    if (!currentFeedbackId) {
        $('#feedbackMessage').removeClass('d-none');
    } else {
        $('#feedbackMessage').addClass('d-none');
    }

    // Determine API endpoint
    let apiUrl = `/home/getfeedbacklist?type=${currentTab}`;
    if (currentTab === 'Resolved') {
        apiUrl = `/home/getresolvedfeedback`;
    }

    // Fetch feedback
    $.get(apiUrl, function (data) {
        $('#feedbackConvList').append(`<h5 class="font-semibold text-center p-2 border-bottom">${currentTab}</h5>`);

        if (data.length === 0) {
            $('#searchToggle').addClass('d-none');

            $('#feedbackConvList').append(`
                <div class="text-center text-muted p-4">
                    <i class="fas fa-inbox fa-2x mb-2"></i>
                    <div>No ${currentTab} feedback found.</div>
                </div>
            `);
            return;
        }

        data.forEach(item => {
            let statusBadge = '';

            if (currentTab === 'Complaint' || currentTab === 'Resolved') {
                if (item.complaintStatus === 'Resolved') {
                    statusBadge = '<span class="badge bg-success">Resolved</span>';
                } else if (item.complaintStatus === 'Ongoing') {
                    statusBadge = '<span class="badge bg-yellow-500 p-2 text-1xl">Ongoing</span>';
                } else if (item.complaintStatus === 'Pending') {
                    statusBadge = '<span class="badge bg-secondary">Pending</span>';
                }
            }

            if (currentTab === 'Resolved' && item.complaintStatus === 'Resolved') {
                statusBadge = '<span class="badge bg-success">Resolved</span>';
            }

            let isSelected = item.feedbackId === currentFeedbackId ? 'selected-feedback bg-light' : '';

            $('#feedbackConvList').append(`
                <div class="p-2 border-bottom feedback-item bg-white hover-bg cursor-pointer ${isSelected}"
                    style="cursor: pointer;"
                    data-id="${item.feedbackId}"
                    data-type="${currentTab}"
                    data-status="${item.complaintStatus}">

                    <small class="text-muted">${formatDate(item.dateSubmitted)}</small>
                    <div class="feedback-description">
                        ${item.description.length > 55 ? item.description.substring(0, 55) + '...' : item.description}
                    </div>
                    <div class="text-right">${statusBadge}</div>
                </div>
            `);
        });

        // Feedback item click to highlight
        $('.feedback-item').click(function () {
            $('.feedback-item').removeClass('selected-feedback bg-light');
            $(this).addClass('selected-feedback bg-light');
            currentFeedbackId = $(this).data('id');
        });
    });
});

// Search toggle logic
$('#searchToggle').click(function () {
    isSearchMode = !isSearchMode;

    if (isSearchMode) {
        $('#searchInputContainer').show();
        $(this).removeClass('fa-search').addClass('fa-times');
        $('#searchInput').focus();
    } else {
        $('#searchInputContainer').hide();
        $(this).removeClass('fa-times').addClass('fa-search');
        $('#searchInput').val('');
        $('.inner-tab-btn.active').click();
    }
});

// Live filtering
$('#searchInput').on('input', function () {
    const keyword = $(this).val().toLowerCase();

    $('#feedbackConvList .feedback-item').each(function () {
        const text = $(this).find('.feedback-description').text().toLowerCase();
        $(this).toggle(text.includes(keyword));
    });
});

$(document).on('click', '.feedback-item', function () {
    $('.feedback-item').removeClass('bg-lightblue');
    $(this).addClass('bg-lightblue'); // Apply background highlight

    currentFeedbackId = $(this).data('id');
    const type = $(this).data('type');
    const status = $(this).data('status');
    const currentUserId = $('#currentUserId').val();
    currentFeedbackId = $(this).data('id');

    $('#conversationSection').hide().html('');

    if (currentFeedbackId) {
        $('#feedbackMessage').addClass('d-none');
    } else {
        $('#feedbackMessage').removeClass('d-none');
    }

    // Show search if clicking a complaint or resolved item
    if ((type === 'Complaint' || type === 'Resolved') && currentFeedbackId) {
        $('#messageInputSection').removeClass('d-none');
    } else {
        $('#messageInputSection').addClass('d-none');
    }

    if (type === 'Complaint' || type === 'Resolved') {
        $('#conversationSection').show();

        $.get(`/home/getconversation?feedbackId=${currentFeedbackId}`, function (messages) {
            $('#viewDetailsBtn').removeClass('d-none');
            $('#conversationSection').html(''); // Clear old messages

            for (let i = 0; i < messages.length; i++) {
                const msg = messages[i];
                const isOwn = msg.userId == currentUserId;
                const nextMsg = messages[i + 1];
                const isLastFromSender = !nextMsg || nextMsg.userId !== msg.userId;
                const getRandomAvatar = () => {
                const randomIndex = Math.floor(Math.random() * 10) + 1; // 1 to 10
                    return `/images/avatars/avatar${randomIndex}.png`;
                };

                const profileImg = msg.profileImage && msg.profileImage.trim() !== "" ? msg.profileImage : getRandomAvatar();

                let msgHtml = '';

                if (isOwn) {
                    msgHtml = `
                               <div class="d-flex justify-content-end animate__animated animate__fadeInUp">
                                   <div class="bg-primary text-white p-2 mb-1 rounded-3" style="max-width: 350px;">${msg.message}</div>
                               </div>
                           `;
                } else {
                    if (isLastFromSender) {
                        msgHtml = `
                            <div class="d-flex align-items-start animate__animated animate__fadeInUp">
                                <img src="${profileImg}" class="rounded-circle me-2" width="40" height="40" />
                                <div>
                                    <div class="fw-bold">${msg.fullName}</div>
                                    <div class="bg-light p-2 mb-1 rounded-3" style="max-width: 350px">${msg.message}</div>
                                </div>
                            </div>
                        `;
                    } else {
                        msgHtml = `
                                   <div class="d-flex align-items-start animate__animated animate__fadeInUp" style="margin-left: 48px;">
                                       <div class="bg-light p-2 mb-1 rounded-3" style="max-width: 350px">${msg.message}</div>
                                   </div>
                               `;
                    }
                }

                $('#conversationSection').append(msgHtml);
            }

            if (type === 'Resolved') {
                $('#conversationSection').append(`
                           <div class="text-center text-success mt-3 fw-bold">The Complaint is Resolved</div>
                       `);
                $('#messageInputSection').hide();
            } else {
                $('#messageInputSection').show();
            }

            // Ensure scroll is at the bottom after rendering
            setTimeout(() => {
                const container = $('#conversationSection');
                container.scrollTop(container[0].scrollHeight);
            }, 100);
        });
    }
});

// Send message
function sendMessage() {
    const message = $('#messageInput').val();
    if (!message.trim()) return;

    $.ajax({
        url: '/home/sendmessage',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ feedbackId: currentFeedbackId, message }),
        success: function () {
            $('#messageInput').val('');
            // Re-fetch messages
            $('.feedback-item[data-id="' + currentFeedbackId + '"]').click();
        }
    });
}

$('#sendMessage').click(() => sendMessage());

$('#messageInput').keydown(function (e) {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

if (currentFeedbackId) {
    $('#viewDetailsBtn').removeClass('d-none');  // Show the button
} else {
    $('#viewDetailsBtn').addClass('d-none');  // Hide the button
}

// Handle the 'View' button click
$('#viewDetailsBtn').click(function () {
    $.get(`/home/getfeedbackdetails?feedbackId=${currentFeedbackId}`, function (data) {
        const statusBadge = getStatusBadge(data.complaintStatus);

        $('#modalBody').html(`
                   <div class="card shadow-sm mb-3">
                       <div class="card-body">
                           <h5 class="card-title text-center mb-3">Feedback Details</h5>
                           <div class="d-flex justify-content-between align-items-center mb-2">
                               <div><strong>Feedback ID:</strong> ${data.feedbackId}</div>
                               <div><strong>Date Submitted:</strong> ${formatDate(data.dateSubmitted)}</div>
                           </div>
                           <div class="mb-3">
                               <strong>Type:</strong> <span class="badge bg-primary">${data.feedbackType}</span>
                           </div>
                           <div class="mb-3">
                               <strong>Description:</strong>
                               <p class="text-base font-semibold">${data.description}</p>
                           </div>
                           <div class="mb-3">
                               <strong>Status:</strong> ${statusBadge}
                           </div>
                       </div>
                   </div>
               `);
    });
});

// Function to generate a styled badge based on the complaint status
function getStatusBadge(status) {
    let baseClass = 'badge text-base px-3 py-2';
    switch (status) {
        case 'Resolved':
            return `<span class="${baseClass} bg-success">Resolved</span>`;
        case 'Ongoing':
            return `<span class="${baseClass} bg-warning text-dark">Ongoing</span>`;
        case 'Pending':
            return `<span class="${baseClass} bg-secondary">Pending</span>`;
        default:
            return `<span class="${baseClass} bg-info">Unknown</span>`;
    }
}

// Auto-load Complaint tab on start
$(function () {
    $('.inner-tab-btn[data-type="Complaint"]').click();
});