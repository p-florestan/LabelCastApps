
var initProfile = localStorage.getItem('LabelCast.ActiveProfile');

var initPrinter = localStorage.getItem('LabelCast.ActivePrinter');
if (!initPrinter)
    initPrinter = '';

if (initProfile) {
    window.location.assign('/labels/entry/ie8?p=' + initProfile + '&n=' + initPrinter);
}
else {
    window.location.assign('/labels/entry/ie8');
}

