
if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/g, '');
    }
}


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

var initProfile = getCookie('ActiveProfile');

var initPrinter = getCookie('ActivePrinter');
if (!initPrinter)
    initPrinter = '';

if (initProfile) {
    location.href = '/labels/entry/ie6?p=' + initProfile + '&n=' + initPrinter;
}
else {
    location.href ='/labels/entry/ie6';
}

