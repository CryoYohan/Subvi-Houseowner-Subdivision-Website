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
        fetch(`/api/notifications/staff/${userId}`),
        fetch(`/api/notifications/staff/unread-count/${userId}`)
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
            const unreadClass = !n.isRead ? 'bg-blue-50 border-l-4 border-blue-500 shadow-sm' : '';

            const notifHtml = `
                <div class="dropdown-item notif-entry ${unreadClass} 
                    hover:bg-blue-100 transition-colors rounded-md p-2 mb-1"
                    data-id="${n.notificationId}" data-link="${n.link || ''}" style="cursor: pointer;">
                    
                    <div class="flex justify-between items-center mb-1">
                        <div class="text-sm text-gray-900 font-medium">${n.title}</div>
                        ${!n.isRead ? '<span class="w-2 h-2 bg-blue-500 rounded-full animate-pulse"></span>' : ''}
                    </div>
                    <div class="text-sm text-gray-700">
                        ${n.message.length > 65 ? n.message.slice(0, 65) + '...' : n.message}
                    </div>
                    <div class="d-flex justify-between items-center mt-1 text-xs text-gray-500">
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

                await fetch(`/api/notifications/staff/mark-read/${userId}/${id}`, { method: "PUT" });
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
    await fetch(`/api/notifications/staff/mark-all-read/${userId}`, { method: "PUT" });
    await loadNotifications();
});

// SignalR for real-time updates
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

// When a staff member connects, add them to the 'staff' group
connection.start().then(() => {
    connection.invoke("AddToStaffGroup").catch(console.error);
    loadNotifications();
}).catch(console.error);

connection.on("ReceiveNotification", function (notif) {
    loadNotifications();
    toastr.info(`${notif.title}: ${notif.message}`, "🔔 New Notification");
});
