window.requestPermission = () => {
    return new Promise((resolve, reject) => {
        Notification.requestPermission((permission) => {
            resolve(permission);
        });
    });
}

window.currentPermission = () => {
    return Notification.permission;
};

window.isSupported = () => {
    if ("Notification" in window)
        return true;
    return false;
}

window.createNotification = (title, options) => {
    var notification = new Notification(title, options);

    notification.onclick = function (event) {
        event.preventDefault(); // prevent the browser from focusing the Notification's tab
        window.open(options.uri, '_blank');
    }
    return notification;
}