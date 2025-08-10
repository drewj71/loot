// Make sure Plaid Link JS library is loaded before this script
window.openPlaidLink = function (linkToken, dotnetHelper) {
    if (typeof Plaid === 'undefined') {
        alert("Plaid script not loaded.");
        return;
    }

    var handler = Plaid.create({
        token: linkToken,
        onSuccess: function (public_token, metadata) {
            dotnetHelper.invokeMethodAsync('OnPlaidSuccess', public_token);
        },
        onExit: function (err, metadata) {
            if (err) {
                console.error('Plaid Link exited with error:', err);
                alert('Error connecting bank: ' + err.display_message);
            }
        },
        onEvent: function (eventName, metadata) {
            console.log('Plaid event:', eventName, metadata);
        }
    });
    handler.open();
};
