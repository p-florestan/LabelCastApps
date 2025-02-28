
// Includes "polyfillsIE.js"

// --------------------------------------------------------------------------------------------------------
// Variables
// --------------------------------------------------------------------------------------------------------

var NoDebug = true;

mActiveProfile = '';
mActivePrinter = '';

mCurrentInput = null;

// Input elements
var mInputList = document.querySelectorAll('input[type="text"]');

// Number of input elements
var mInputRowCount = mInputList.length;

// Top input element
var mFirstInput = document.querySelector('#col0');

// List of "dbResult" elements - these are hidden form fields which are
// in the dbResultField list but are not dbQuery fields and not EditFields
var dbResultList = document.querySelectorAll('input.dbResult');

// Whether navigating through input fields triggers updates
var mNavActive = true;

var printButton = document.querySelector('#btnPrint');
var clearButton = document.querySelector('#btnClear');



// --------------------------------------------------------------------------------------------------------
// Utility Functions
// --------------------------------------------------------------------------------------------------------


// SetCookie function
//  name:  The name of the cookie.
//  value: The value to store in the cookie.
//  days:  Number of days the cookie should be valid. If not provided,
//         the cookie will be a session cookie (deleted when the browser is closed).
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
    for (let n = 0; n < mInputList.length; n++) {
        mInputList[n].value = '';
    }
    mCurrentInput = 'col0';
    mNextInput = '';

    // Clear other DbResult fields
    for (let n = 0; n < dbResultList.length; n++) {
        dbResult[n].value = '';
    }

    // Clear special hidden fields

    document.querySelector('.entryTable input[name="Number of Labels"]').value = 1;

    document.querySelector('input[name="CurrentEditField"]').value = '';
    document.querySelector('input[name="DataQueryStatus"]').value = 0;
    document.querySelector('input[name="PageEditIndex"]').value = 0;

    mNavActive = false;
    mFirstInput.focus();
    mNavActive = true;
}




// --------------------------------------------------------------------------------------------------------
// Input Event Handlers
// --------------------------------------------------------------------------------------------------------


// This advances the focus to the next inputbox below upon pressing ENTER key
function inputKeyDown() {
    var event = window.event;
    if (event.keyCode == 13) {
        event.preventDefault(); // do not submit form
        var idx = parseInt(event.srcElement.id.substring(3), 10);

        // numeric query? then position after dbQuery fields
        var fieldName = event.srcElement.getAttribute('name');
        var fieldValue = event.srcElement.value;
        if (fieldName == GetFormFieldValue('FirstSearchField') && isNumber(fieldValue)) {
            var nextInput = document.querySelector('#col' + GetFormFieldValue('FirstEditFieldIndex').toString());
        }

        if (idx < mInputRowCount - 1) {
            var nextInput = document.querySelector('#col' + (idx + 1).toString());
            nextInput.focus();
        }

        else {
            // Pressing ENTER on last input prints the label.
            // The last input is always 'Number of Labels'.
            debugPrint(' ENTER KEY ON LABEL COUNT');
            printButtonClick();
        }
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
focusInput = document.querySelector('#col' + editIndex.toString());
focusInput.focus();



