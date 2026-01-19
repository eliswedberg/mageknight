// Notification handling for Mage Knight Online

// Request notification permission on load
if ('Notification' in window) {
    if (Notification.permission === 'default') {
        // We'll request permission when user interacts with the page
        document.addEventListener('click', function requestPermission() {
            Notification.requestPermission();
            document.removeEventListener('click', requestPermission);
        }, { once: true });
    }
}

// Show a browser notification
window.showNotification = function (title, message) {
    if (!('Notification' in window)) {
        console.log('Browser does not support notifications');
        return;
    }

    if (Notification.permission === 'granted') {
        createNotification(title, message);
    } else if (Notification.permission !== 'denied') {
        Notification.requestPermission().then(permission => {
            if (permission === 'granted') {
                createNotification(title, message);
            }
        });
    }
};

function createNotification(title, message) {
    const notification = new Notification(title, {
        body: message,
        icon: '/images/deed_icon.png',
        badge: '/images/deed_icon.png',
        tag: 'mage-knight-turn', // Prevents duplicate notifications
        requireInteraction: false,
        silent: false
    });

    // Auto-close after 5 seconds
    setTimeout(() => notification.close(), 5000);

    // Focus window when clicked
    notification.onclick = function () {
        window.focus();
        notification.close();
    };
}

// Play a notification sound (optional)
window.playNotificationSound = function () {
    try {
        const audio = new Audio('/sounds/notification.mp3');
        audio.volume = 0.5;
        audio.play().catch(() => {
            // Ignore if autoplay is blocked
        });
    } catch (e) {
        // Ignore errors
    }
};

// Check if notifications are supported and permitted
window.checkNotificationPermission = function () {
    if (!('Notification' in window)) {
        return 'unsupported';
    }
    return Notification.permission;
};

// Request notification permission
window.requestNotificationPermission = async function () {
    if (!('Notification' in window)) {
        return 'unsupported';
    }
    
    const permission = await Notification.requestPermission();
    return permission;
};
