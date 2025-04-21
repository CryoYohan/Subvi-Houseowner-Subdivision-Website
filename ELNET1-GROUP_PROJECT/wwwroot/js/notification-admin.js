const userId = getCookie("Id");
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
        fetch(`/api/notifications/admin/${userId}`),
        fetch(`/api/notifications/admin/unread-count/${userId}`)
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
                <div class="dropdown-item notif-entry ${isUnread ? 'bg-blue-50 border-start border-3 border-primary shadow-sm' : ''}" 
                    data-id="${n.notificationId}" data-link="${n.link || ''}" style="cursor: pointer;">
                    
                    <div class="d-flex justify-content-between align-items-start mb-1">
                        <strong class="text-dark">${n.title}</strong>
                        ${isUnread ? '<span class="badge bg-primary rounded-circle p-1 ms-2" style="width: 10px; height: 10px;"></span>' : ''}
                    </div>
                    
                    <div><small class="text-secondary">${n.message.length > 65 ? n.message.slice(0, 65) + '...' : n.message}</small></div>
                    
                    <div class="d-flex justify-content-between align-items-center mt-1">
                        <small class="text-muted">${new Date(n.dateCreated).toLocaleString()}</small>
                        ${hasLink ? '<span class="text-primary small">View</span>' : ''}
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

                await fetch(`/api/notifications/admin/mark-read/${userId}/${id}`, { method: "PUT" });
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
    await fetch(`/api/notifications/admin/mark-all-read/${userId}`, { method: "PUT" });
    await loadNotifications();
});

// SignalR for real-time updates
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

// When a admin member connects, add them to the 'admin' group
connection.start().then(() => {
    connection.invoke("AddToAdminGroup").catch(console.error);
    loadNotifications();
}).catch(console.error);

connection.on("ReceiveNotification", function (notif) {
    loadNotifications();
    toastr.info(`${notif.title}: ${notif.message}`, "🔔 New Notification");
});
