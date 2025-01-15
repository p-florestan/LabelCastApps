
var initProfile = localStorage.getItem('LabelCast.ActiveProfile');

var initPrinter = localStorage.getItem('LabelCast.ActivePrinter');
if (!initPrinter)
    initPrinter = '';

if (initProfile) {
    console.log('switching to labelentry');
    window.location.assign('/labels/entry?p=' + initProfile + '&n=' + initPrinter);
}
else {
    window.location.assign('/labels/entry');
}


// Detect if we are running IE8 - 10
function getIEVersion() {
    var ua = navigator.userAgent;

    if (ua.indexOf('MSIE ') > -1) {
        // IE 10 or below
        var version = ua.match(/MSIE (\d+)/)[1];
        return parseInt(version, 10);
    } else if (ua.indexOf('Trident/') > -1) {
        // IE 11 (uses the "Trident" engine)
        var version = ua.match(/rv:(\d+)/)[1];
        return 11; // IE 11
    }
    return null;  // Not IE
}



