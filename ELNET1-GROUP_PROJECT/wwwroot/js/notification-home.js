﻿const userId = getCookie("Id");
const notifDropdown = document.getElementById("notifDropdown");
const notifCount = document.getElementById("notifCount");
const notifListContainer = document.getElementById("notifListContainer");
const markAllReadBtn = document.getElementById("markAllReadBtn");

function getCookie(name) {
    const match = document.cookie.match(new RegExp("(^| )" + name + "=([^;]+)"));
    return match ? decodeURIComponent(match[2]) : null;
}

// Prevent dropdown from closing when clicking inside dropdown menu
document.querySelectorAll(".dropdown-menu").forEach(menu => {
    menu.addEventListener("click", function (e) {
        e.stopPropagation();
    });
});

async function loadNotifications() {
    const [notifRes, countRes] = await Promise.all([
        fetch(`/api/notifications/${userId}`),
        fetch(`/api/notifications/unread-count/${userId}`)
    ]);

    const notifications = await notifRes.json();
    const countData = await countRes.json();

    // Update badge
    if (countData.count > 0) {
        notifCount.classList.remove("d-none");
        notifCount.textContent = countData.count;
        markAllReadBtn.disabled = false;
    } else {
        notifCount.classList.add("d-none");
        markAllReadBtn.disabled = true;
    }

    notifListContainer.innerHTML = "";

    if (notifications.length > 0) {
        notifications.forEach(n => {
            const hasLink = n.link && n.link.trim() !== "";
            const isUnread = !n.isRead;

            const notifHtml = `
                <div class="dropdown-item notif-entry ${isUnread ? 'bg-blue-50 border-l-4 border-blue-600 shadow-sm' : ''} 
                    hover:bg-blue-100 transition-colors rounded-md p-2 mb-1" 
                    data-id="${n.notificationId}" data-link="${n.link || ''}" style="cursor: pointer;">
                    
                    <div class="flex justify-between items-start">
                        <div class="text-sm text-gray-900 ${isUnread ? 'font-semibold' : 'font-medium'}">
                            ${n.title}
                        </div>
                        ${isUnread ? '<span class="ms-2 mt-1 inline-block w-2 h-2 bg-blue-600 rounded-full"></span>' : ''}
                    </div>

                    <div class="text-sm text-gray-700">
                        ${n.message.length > 65 ? n.message.slice(0, 65) + '...' : n.message}
                    </div>

                    <div class="d-flex justify-content-between align-items-center mt-1 text-xs text-gray-500">
                        <span>${new Date(n.dateCreated).toLocaleString()}</span>
                        ${hasLink ? '<span class="text-blue-600 hover:underline">View</span>' : ''}
                    </div>
                </div>
            `;
            notifListContainer.innerHTML += notifHtml;
        });

        // Click whole notification
        document.querySelectorAll(".notif-entry").forEach(item => {
            item.addEventListener("click", async function () {
                const id = this.getAttribute("data-id");
                const link = this.getAttribute("data-link");

                if (!link) return;

                await fetch(`/api/notifications/mark-read/${id}`, { method: "PUT" });
                await loadNotifications();
                window.location.href = link;
            });
        });
    } else {
        notifListContainer.innerHTML = `<li class="dropdown-item text-center text-muted">No notifications</li>`;
    }
}

// Auto-close 3-dot dropdown when clicked outside or selected
document.addEventListener("click", function (e) {
    const dotMenu = document.querySelector(".dropdown.dropstart");
    const dotMenuBtn = dotMenu.querySelector("[data-bs-toggle='dropdown']");
    const dropdown = dotMenu.querySelector(".dropdown-menu");

    // Check if click is outside
    if (!dotMenu.contains(e.target)) {
        bootstrap.Dropdown.getOrCreateInstance(dotMenuBtn).hide();
    }
});

// Mark all as read
markAllReadBtn.addEventListener("click", async () => {
    await fetch(`/api/notifications/mark-all-read/${userId}`, { method: "PUT" });
    await loadNotifications();
});

// SignalR for real-time updates
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.on("ReceiveNotification", function (notif) {
    // Only show notifications for the specific homeowner (by userId)
    if (notif.userId && notif.userId === currentUserId) {
        loadNotifications(); // Update notifications on UI
        toastr.info(`${notif.title}: ${notif.message}`, "🔔 New Notification");
    }
});

connection.start().then(loadNotifications).catch(console.error);
