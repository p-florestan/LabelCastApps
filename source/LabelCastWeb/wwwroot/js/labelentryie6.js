// Variables

mActiveProfile = '';
mActivePrinter = '';

// Function Definitions go prior to actual script
// (no function hosting in IE6)

// String.trim() is not supported in IE6
if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/g, '');
    }
}

// SetCookie function
//  name:  The name of the cookie.
//  value: The value to store in the cookie.
//  days:  Number of days the cookie should be valid.If not provided,
//         the cookie will be a session cookie(deleted when the browser is closed).
function setCookie(name, value, days) {
    var expires = "";
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));  // Convert days to milliseconds
        expires = "; expires=" + date.toUTCString();
    }
    // Set the cookie with the name, value, expiration, and optional path ("/" means the cookie is accessible throughout the site)
    document.cookie = name + "=" + escape(value) + expires + "; path=/";
}

// Read existing cookies
function getCookie(name) {
    var nameEq = name + "=";
    var cookies = document.cookie.split(';');
    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i].trim();
        if (cookie.substring(0, nameEq.length) === nameEq) {
            return unescape(cookie.substring(nameEq.length));
        }
    }
    return null;  // Return null if the cookie is not found
}

function switchprofile() {
    var event = window.event;
    mActiveProfile = event.srcElement.value;
    setCookie('ActiveProfile', mActiveProfile, 365)
    location.href = '/labels/entry/ie6?p=' + mActiveProfile + '&n=' + mActivePrinter;
}

function switchprinter() {
    var event = window.event;
    mActivePrinter = event.srcElement.value;
    setCookie('ActivePrinter', mActivePrinter, 365)
    location.href = '/labels/entry/ie6?p=' + mActiveProfile + '&n=' + mActivePrinter;
}



// Main Script 

mActiveProfile = getCookie('ActiveProfile');
mActivePrinter = getCookie('ActivePrinter');

