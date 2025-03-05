
// Includes "polyfillsIE.js"

// --------------------------------------------------------------------------------------------------------
// Variables
// --------------------------------------------------------------------------------------------------------

var NoDebug = true;

mActiveProfile = '';
mActivePrinter = '';

mCurrentInput = null;

// Input elements
var i = 0;
mInputList = [];
while (true) {
    var el = document.getElementById('col' + i);
    if (el) {
        mInputList.push(el);
        i++;
    }
    else
        break;
}

// Number of input elements
var mInputRowCount = mInputList.length;
debugPrint('mInputRowCount = ' + mInputRowCount);

// Top input element
var mFirstInput = document.getElementById('col0');

// List of "dbResult" elements - these are hidden form fields which are
// in the dbResultField list but are not dbQuery fields and not EditFields
var dbResultList = document.querySelectorAll('input.dbResult');

// Whether navigating through input fields triggers updates
var mNavActive = true;

var printButton = document.getElementById('btnPrint');
var clearButton = document.getElementById('btnClear');



// --------------------------------------------------------------------------------------------------------
// Utility Functions
// --------------------------------------------------------------------------------------------------------


// SetCookie function
//  name:  The name of the cookie.
//  value: The value to store in the cookie.
//  days:  Number of days the cookie should be valid. If not provided,
//         the cookie will be a session cookie (deleted when the browser is closed).
function setCookie(name, value, days) {
    try {
        var expires = "";
        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));  // Convert days to milliseconds
            expires = "; expires=" + 'x'; // date.toLocaleString();
        }
        // Set the cookie with the name, value, expiration, and optional path ("/" means the cookie is accessible throughout the site)
        document.cookie = name + "=" + escape(value) + expires + "; path=/";
    }
    catch (e) {
        alert('Error in script (labelentryie.js): ' + e.message);
    }
}

// Read existing cookies
function getCookie(name) {
    var nameEq = name + "=";
    var cookies = document.cookie.split(';');
    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i].trim();
        if (cookie.substring(0, nameEq.length) == nameEq) {
            return unescape(cookie.substring(nameEq.length));
        }
    }
    return null;  // Return null if the cookie is not found
}



function isNumber(str) {
    if (typeof str != "string" || str.replace(/^\s+|\s+$/g, "") == "")
        return false;
    return !isNaN(str) && !isNaN(parseFloat(str));
}

function focusAndSelect(inputElement) {
    inputElement.focus();
    setTimeout(function () {
        inputElement.select();
    }, 10);
}


// Diagnostics
function debugPrint(text) {
    if (!NoDebug) {
        var debugDiv = document.getElementById('debugMsg');
        debugDiv.innerHTML += text + '<br>';
    }
}


function SetFormFieldValue(field, value) {
    var fieldElement = document.getElementById(field);
    if (fieldElement) {
        fieldElement.value = value;
    }
}


function SubmitForm() {
    var form = document.getElementById('labelEntryForm');
    form.submit();
}


function GetFormFieldValue(fieldName) {
    var element = document.getElementById(fieldName)
    if (element)
        return element.value;
    else
        return undefined;
}


// --------------------------------------------------------------------------------------------------------
// General Event Handlers
// --------------------------------------------------------------------------------------------------------

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


function printButtonClick() {
    SetFormFieldValue('CurrentEditField', 'Number of Labels');
    SubmitForm();
}


function clearButtonClick() {
    clearInputFields();
}

// Clear all input elements and focus on top cell
function clearInputFields() {

    // Clear DbQuery fields and EditFields
    for (var n = 0; n < mInputList.length; n++) {
        mInputList[n].value = '';
    }
    mCurrentInput = 'col0';
    mNextInput = '';

    // Clear other DbResult fields
    for (var n = 0; n < dbResultList.length; n++) {
        dbResultList[n].value = '';
    }

    // Clear special hidden fields

    document.querySelector('.entryTable input[name="Number of Labels"]').value = 1;

    document.getElementById('CurrentEditField').value = '';
    document.getElementById('DataQueryStatus').value = 0;
    document.getElementById('PageEditIndex').value = 0;

    var msgLabel = document.getElementById('msgLabel');
    if (msgLabel)
        msgLabel.innerText = '';

    mNavActive = false;
    mFirstInput.focus();
    mNavActive = true;
}




// --------------------------------------------------------------------------------------------------------
// Input Event Handlers
// --------------------------------------------------------------------------------------------------------


// This advances the focus to the next inputbox below upon pressing ENTER key
function inputKeyDown(event) {
    var event = event || window.event;  // Handle cross-browser compatibility
    if (event.keyCode == 13) {

        // Do not submit form (prevent default behavior)
        if (event.preventDefault) {
            event.preventDefault();  // Modern browsers
            event.stopPropagation();
        } else {
            event.returnValue = false;  // IE6 and earlier
            event.cancelBubble = true;
        }

        var idx = 0;
        var element = event.srcElement;
        if (element && element.id) {
            idx = parseInt(element.id.substring(3), 10);
        }

        // numeric query? then position after dbQuery fields
        var fieldName = event.srcElement.getAttribute('name');
        var fieldValue = event.srcElement.value;

        debugPrint('fieldName = ' + fieldName + ', fieldValue = ' + fieldValue);
        debugPrint('mInputRowCount = ' + mInputRowCount);

        //if (fieldName == GetFormFieldValue('FirstSearchField') && isNumber(fieldValue)) {
        //    debugPrint('--> numeric query');
        //    var nextInput = document.querySelector('#col' + GetFormFieldValue('FirstEditFieldIndex').toString());
        //}

        if (idx < mInputRowCount - 1) {
            var nextInput = document.getElementById('col' + (idx + 1).toString());
            if (nextInput)
                nextInput.focus();
            else 
                debugPrint('nextInput field col' + + (idx + 1).toString() + ' not found');
        }

        else {
            // Pressing ENTER on last input prints the label.
            // The last input is always 'Number of Labels'.
            debugPrint(' ENTER KEY ON LABEL COUNT');
            // when testing delay form submit:
            // setTimeout(function () { printButtonClick(); }, 3000);
            printButtonClick();            
        }

        return false;
    }
}


function inputLostFocus() {
    var event = window.event;
    if (mNavActive) {
        mCurrentInput = event.srcElement;
        SetFormFieldValue('CurrentEditField', mCurrentInput.getAttribute('name'))
        debugPrint('Lost focus: ' + event.srcElement.getAttribute('name'));
    }
}


// When a new input element gets focus, we send the form to the server if the earlier focused field 
// was FirstSearchField or LastSearchField
function inputGotFocus() {
    var event = window.event;
    event.srcElement.select();
    debugPrint('Got focus: ' + event.srcElement.getAttribute('name'));

    mNextInput = event.srcElement.id;

    if (mCurrentInput && mNextInput !== mCurrentInput) {

        // numeric query? then position after dbQuery fields
        var inputName = mCurrentInput.getAttribute('name');
        var inputValue = mCurrentInput.value;

        debugPrint('GetFormFieldValue = ' + GetFormFieldValue('FirstSearchField'));

        if (inputName == GetFormFieldValue('FirstSearchField') && isNumber(inputValue)) {
            // Numeric db query
            SetFormFieldValue('DataQueryStatus', '1')   // pending
            SubmitForm();
        }

        else if (inputName == GetFormFieldValue('LastSearchField')) {
            // Regular db query
            SetFormFieldValue('DataQueryStatus', '1')   // pending
            SubmitForm();
        }

        mCurrentInput = mNextInput;
    }
}





// --------------------------------------------------------------------------------------------------------
// Main Script
// --------------------------------------------------------------------------------------------------------


mActiveProfile = getCookie('ActiveProfile');
mActivePrinter = getCookie('ActivePrinter');

editIndex = GetFormFieldValue('PageEditIndex');
focusInput = document.getElementById('col' + editIndex.toString());
focusInput.focus();

