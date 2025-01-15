
// Javascript code for IE8 browser
//
// Differences addressed:
//   Polyfill for JSON.parse/stringify    (json2.js)
//   attachEvent instead of addEventHandler
//   prefix event names with 'on'
//   IE does not fire DOMContentLoaded event. Just put the script after all HTML
//   Replace === with ==
//   classList prop does not exist
//   does not support Array.from() and Array.indexOf()

// Also CSS must change
//   no 'ch' unit, does not understand 'header' tag, no CSS variables


var dbNOQUERY = 0;
var dbPENDING = 1;
var dbSUCCESS = 2;
var dbFAILED = 3;
var dbStatus = ['NoQuery', 'Pending', 'Success', 'Failed'];

var NoDebug = true;


// --------------------------------------------------------------------------------------------------------
//   Polyfills for IE
// --------------------------------------------------------------------------------------------------------


if (!document.querySelectorAll)
    document.querySelectorAll = function (selector) {
        var head = document.documentElement.firstChild;
        var styleTag = document.createElement("STYLE");
        head.appendChild(styleTag);
        document.__qsResult = [];

        styleTag.styleSheet.cssText = selector + "{x:expression(document.__qsResult.push(this))}";
        window.scrollBy(0, 0);
        head.removeChild(styleTag);

        var result = [];
        for (var i in document.__qsResult)
            result.push(document.__qsResult[i]);
        return result;
    }


if (!document.querySelector)
    document.querySelector = function (selector) {
        var head = document.documentElement.firstChild;
        var styleTag = document.createElement("STYLE");
        head.appendChild(styleTag);
        document.__qsResult = [];

        styleTag.styleSheet.cssText = selector + "{x:expression(document.__qsResult.push(this))}";
        window.scrollBy(0, 0);
        head.removeChild(styleTag);

        // Return first result only               
        return document.__qsResult[0] || null;
    }


// Obtain all "own" properties of an object
function getKeys(obj) {
    var keys = [];
    for (var key in obj) {
        if (obj.hasOwnProperty(key)) {
            keys.push(key);
        }
    }
    return keys;
}

// IE8 does not support trim() function
if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/g, '');
    };
}

// IE does not support classList array property.
// We need to use 'className' property instead which takes 
// a space-separated list of classes. 
function removeClass(htmlElement, cssClass) {

    cssClass = cssClass.trim();
    htmlElement.className = htmlElement.className.trim();
    var cssLen = cssClass.length;
    var classLen = htmlElement.className.length;

    if (htmlElement.className == cssClass) {
        htmlElement.className = '';
        //console.log('case 1: className = css, result = "' + htmlElement.className + '"');
    }
    else if (htmlElement.className.substr(classLen - cssLen - 1) == ' ' + cssClass) {
        htmlElement.className = htmlElement.className.substr(0, classLen - cssLen - 1);
        //console.log('case 2: begins with css, result = "' + htmlElement.className + '"');
    }
    else if (htmlElement.className.substr(0, cssLen + 1) == cssClass + ' ') {
        htmlElement.className = htmlElement.className.substr(cssLen + 1);
        //console.log('case 3: ends with css, result = "' + htmlElement.className + '"');
    }
    else {
        var idx = htmlElement.className.indexOf(' ' + cssClass + ' ');
        if (idx >= 0) {
            htmlElement.className = htmlElement.className.substr(0, idx + 1) + htmlElement.className.substr(idx + 1 + cssLen + 1);
            //console.log('case 4: in the middle, result = "' + htmlElement.className + '"');
        }
        //else 
            //console.log('case 5: not present, result = "' + htmlElement.className + '"');
    }
}

/*
//
// Tests for the above "removeClass" function
//
function removeClassTest() {
    var b = document.querySelector('body');

    console.log('-- Test Case 1 -- (equal)');
    b.className = 'selected';
    console.log('className before: ' + b.className);
    removeClass(b, 'selected');

    console.log('-- Test Case 2 -- (begins with)');
    b.className = 'selected bold fancy';
    console.log('className before: ' + b.className);
    removeClass(b, 'selected');

    console.log('-- Test Case 3 -- (ends with)');
    b.className = 'bold fancy selected';
    console.log('className before: ' + b.className);
    removeClass(b, 'selected');

    console.log('-- Test Case 4 -- (in the middle)');
    b.className = 'bold fancy selected clean muddled';
    console.log('className before: ' + b.className);
    removeClass(b, 'selected');

    console.log('-- Test Case 5 -- (not present)');
    b.className = 'bold fancy clean';
    console.log('className before: ' + b.className);
    removeClass(b, 'selected');
}
*/

// --------------------------------------------------------------------------------------------------------
//   JS for Label Entry
// --------------------------------------------------------------------------------------------------------


(function () {

    var mMessageLabel = document.querySelector('#msgLabel');
    var mErrorLabel = document.querySelector('#errorLabel');
    var debugMsg = document.querySelector('#debugMsg');
    var debugStatus = document.querySelector('#debugStatus');

    var printButton = document.querySelector('#btnPrint');
    var profileSelect = document.querySelector('select[name="profile"]');
    var printerSelect = document.querySelector('select[name="printer"]');

    // Abbrev of currently active profile
    var mActiveProfile = '';

    // Input elements
    var mInputList = document.querySelectorAll('.entryTable input');

    // Number of input elements
    var mInputRowCount = mInputList.length;

    // Top input element
    var mFirstInput = document.querySelector('#col0');

    // Currently focused input element
    var mCurrentInput = 'col0';
    var mCurrentFocus = mInputList[0];

    // input element for label count (always the last one)
    var mLabelCountInput = 'col' + (mInputRowCount - 1).toString();

    // Newly focused input element
    var mNextInput = 'col0';

    // Timer to wait for database query result
    var dbTimerIdx = 60;

    // Whether navigating through input fields triggers updates
    var mNavActive = true;


    // Label descriptor global data object.
    // See C# LabelDescriptor server class for exact definition.
    // This holds all state for the label entry processing. Like any usual web application,
    // all state has to be kept on the client, and server is stateless.
    // (Defined in earlier script in LabelEntry.cshtml view)
    //
    // var mDescriptor = {}
    //

    initProfileSelect();
    initPrinterSelect();
    mFirstInput.focus();

    debugStatusShow();



    // --------------------------------------------------------------------------------------------------------
    //   Event Handlers
    // --------------------------------------------------------------------------------------------------------

    for (var n = 0; n < mInputList.length; n++) {
        mInputList[n].attachEvent('onkeydown', inputKeyDown);
        mInputList[n].attachEvent('onfocusout', inputLostFocus);
        mInputList[n].attachEvent('onfocusin', inputGotFocus);
    }
    printButton.attachEvent('onclick', printButtonClick);
    profileSelect.attachEvent('onchange', activeProfileChange);
    printerSelect.attachEvent('onchange', activePrinterChange);


    function inputKeyDown() {
        // console.log('KeyDown event');
        var event = window.event;
        // console.log(event.keyCode);
        if (event.keyCode == 13) {
            var idx = Number(event.srcElement.id.substring(3));
            if (idx < mInputRowCount - 1) {
                var nextInput = document.querySelector('#col' + (idx + 1).toString());
                nextInput.focus();
            }
            else {
                // Pressing ENTER on last input prints the label.
                // The last input is always 'Number of Labels'.
                debugPrint(' ENTER KEY ON LABEL COUNT');
                printButton.click();
            }
        }
    }

    function inputLostFocus(event) {
        // console.log('LostFocus event');
        var event = window.event;
        if (mNavActive) {
            mCurrentInput = event.srcElement.id;
        }
    }

    // When a new input element gets focus, we update the value of the element 
    // focused before that, and send the data to server.
    function inputGotFocus(event) {
        // console.log('GotFocus event');
        var event = window.event;
        mCurrentFocus = event.srcElement;
        event.srcElement.select();
        clearMessages();

        mNextInput = event.srcElement.id;

        if (mNextInput !== mCurrentInput) {
            sendEditRequest(mCurrentInput);
            mCurrentInput = mNextInput;
        }
    }

    function printButtonClick(event) {
        // console.log('printBtn click event');

        // edit field which lost focus:
        sendEditRequest(mCurrentInput);

        if (mDescriptor.DataQueryStatus == dbPENDING) {
            dbTimerIdx = 60;
            debugPrint(' - repeated print req -');
            sendRepeatedPrintRequest();
        }
        else {
            debugPrint(' - single print req -');
            sendPrintRequest();
        }
    }

    // Set a different active profile if user changes dropdown
    function activeProfileChange(event) {
        mActiveProfile = event.srcElement.value;
        localStorage.setItem("LabelCast.ActiveProfile", mActiveProfile);
        window.location.assign('/labels/entry/ie8?p=' + mActiveProfile + '&n=' + mActivePrinter);
    }


    // Set a different active printer if user changes dropdown
    function activePrinterChange(event) {
        mActivePrinter = event.srcElement.value;
        localStorage.setItem("LabelCast.ActivePrinter", mActivePrinter);
        // Not strictly necessary to refresh page, but cleaner:
        window.location.assign('/labels/entry/ie8?p=' + mActiveProfile + '&n=' + mActivePrinter);
    }





    // --------------------------------------------------------------------------------------------------------
    //  Descriptor State Update - State Management
    // --------------------------------------------------------------------------------------------------------

    // All state is kept on the client, and is stored in the "mDescriptor" variable
    // which represents an instance of the LabelDescriptor class.


    // Update global data structure 'mDescriptor' with values from the <input> fields.
    // This is done before sending a request to server.
    // If the currently edited field is the same as LastSearchField and any of the dbQueryField
    // values contain a wildcard sign (percent symbol, %), this method returns TRUE.
    function updateDescriptor(currentField) {

        var wildCardsPresent = false;
        mDescriptor.CurrentEditField = currentField;

        // Determine if this is a database-query
        if (currentField == mDescriptor.LastSearchField) {
            debugPrint('Edit results in a database query');
            mDescriptor.DataQueryStatus = dbPENDING;
        }

        for (var n = 0; n < mInputList.length; n++) {

            var fieldName = mInputList[n].getAttribute('name');

            if (mDescriptor.DbQueryFields.hasOwnProperty(fieldName)) {
                mDescriptor.DbQueryFields[fieldName] = mInputList[n].value;
                if (mInputList[n].value.search('%') >= 0)
                    wildCardsPresent = true;
            }
            else if (mDescriptor.EditableFields.hasOwnProperty(fieldName)) {
                mDescriptor.EditableFields[fieldName] = mInputList[n].value;
            }
            else if (fieldName == 'Number of Labels') {
                mDescriptor.LabelCount = mInputList[n].value;
                if (isNaN(mDescriptor.LabelCount) || (!mDescriptor.LabelCount) || mDescriptor.LabelCount == 0) {
                    mDescriptor.LabelCount = 1;
                    mInputList[n].value = 1;
                }
            }
            else {
                // console.log('Configuration error: field name "' + fieldName + '" not found in LabelDescriptor.');
            }
        }

        return (wildCardsPresent && mDescriptor.DataQueryStatus == dbPENDING);
    }

    // Update the values in mDescrptor from server response data.
    // This only updates database query results and ReadyToPrint prop - we cannot just replace
    // mDescriptor with the response because server responses may return out of order
    function updateResultFields(responseText, currentField) {
        var respData = JSON.parse(responseText);

        // Determine if this is a database-query
        // Only if so, we update db query status and result fields
        if (currentField == respData.LastSearchField) {

            mDescriptor.DataQueryStatus = respData.DataQueryStatus;
            mDescriptor.DataQueryStatusText = respData.DataQueryStatusText;

            if (mDescriptor.DataQueryStatus == dbSUCCESS) {
                var keys = getKeys(mDescriptor.DbResultFields);
                for (var n = 0; n < keys.length; n++) {
                    mDescriptor.DbResultFields[keys[n]] = respData.DbResultFields[keys[n]];
                }
            }
            else if (mDescriptor.DataQueryStatus == dbFAILED) {
                debugPrint('DbQuery failure. Clear dbTimer');
                dbTimerIdx = 0;
            }
        }

        // update currently edited field
        if (mDescriptor.EditableFields.hasOwnProperty(currentField)) {
            mDescriptor.EditableFields[currentField] = respData.EditableFields[currentField];
        }

        mDescriptor.ErrorMessage = respData.ErrorMessage;
        mDescriptor.ReadyToPrint = respData.ReadyToPrint;

        if (mDescriptor.ReadyToPrint) {
            mMessageLabel.innerText = 'Printing label';
            debugPrint('Label ready to print');
            dbTimerIdx = 0;

            setTimeout(function () {
                clearInputFields();
            }, 2000);
        }

        debugStatusShow();
    }


    // Reset all values in mDescriptor (ready for next input)
    function clearDescriptor() {

        keys = getKeys(mDescriptor.DbQueryFields);
        for (var n = 0; n < keys.length; n++) {
            mDescriptor.DbQueryFields[keys[n]] = '';
        }

        keys = getKeys(mDescriptor.DbResultFields);
        for (var n = 0; n < keys.length; n++) {
            mDescriptor.DbResultFields[keys[n]] = '';
        }

        keys = getKeys(mDescriptor.EditableFields);
        for (var n = 0; n < keys.length; n++) {
            mDescriptor.EditableFields[keys[n]] = '';
        }

        mDescriptor.CurrentEditField = '';
        mDescriptor.DataQueryStatus = 0;
        mDescriptor.LabelCount = 1;
        mDescriptor.ReadyToPrint = false;

        debugStatusShow();
    }



    // --------------------------------------------------------------------------------------------------------
    //  User Interface Handlers
    // --------------------------------------------------------------------------------------------------------

    // Select remembered profile
    function initProfileSelect() {
        var initProfile = localStorage.getItem('LabelCast.ActiveProfile');
        if (initProfile) {
            profileSelect.value = initProfile;
            mActiveProfile = initProfile;
        }
        else {
            profileSelect.SelectedIndex = 0;
            mActiveProfile = profileSelect.value;
        }
    }


    // Select remembered printer
    function initPrinterSelect() {
        var initPrinter = localStorage.getItem('LabelCast.ActivePrinter');
        if (initPrinter) {
            printerSelect.value = initPrinter;
            mActivePrinter = initPrinter;
        }
        else {
            printerSelect.SelectedIndex = 0;
            mActivePrinter = printerSelect.value;
        }
    }


    // Clear all input elements and focus on top cell
    function clearInputFields() {
        clearMessages();
        clearDescriptor();

        for (var n = 0; n < mInputList.length; n++) {
            mInputList[n].value = '';
        }
        mCurrentInput = 'col0';
        mNextInput = '';

        document.querySelector('.entryTable input[name="Number of Labels"]').value = 1;

        mNavActive = false;
        mFirstInput.focus();
        mNavActive = true;
    }


    function clearMessages() {
        mMessageLabel.innerText = '';
        mErrorLabel.innerText = '';
    }




    // --------------------------------------------------------------------------------------------------------
    //   Overlay for displaying options (wildcard search)
    // --------------------------------------------------------------------------------------------------------

    function showOverlay() {
        var overlay = document.querySelector('#overlay');
        overlay.innerHTML = '<br/><p>Searching ...</p>';
        overlay.style.display = 'block';
    }


    function hideOverlay() {
        var overlay = document.querySelector('#overlay');
        overlay.style.display = 'none';
        setTimeout(function () {
            mCurrentFocus.focus();
        }, 50);
    }


    function createOverlay(options) {

        var displayCol = mDescriptor.DisplayField;

        // Create inner HTML
        var html = '<ul id="listbox" class="listbox" tabindex="0">';
        for (var j = 0; j < options.length; j++) {
            html += '<li class="listbox-item" tabindex="-1">' + options[j][displayCol] + '</li>';
        }
        html += '</ul>'
        document.querySelector('#overlay').innerHTML = html;

        // Add interactivity

        var listbox = document.querySelector('#listbox');
        var items = listbox.querySelectorAll('.listbox-item');

        listbox.focus();
        items[0].focus();

        for (var n = 0; n < items.length; n++) {
            items[n].attachEvent('onclick', function () {
                var event = window.event;
                for (var k = 0; k < items.length; k++) {
                    removeClass(items[k], 'selected');
                }
                event.srcElement.className += ' selected';
                selectOption(event.srcElement.innerText, options, displayCol);
            });

            items[n].attachEvent('onkeydown', function () {
                var event = window.event;
                if (event.keyCode == 13 || event.keyCode == 20) {
                    event.returnValue = false;
                    for (var i = 0; i < items.length; i++) {
                        removeClass(items[i], 'selected');
                    }
                    event.srcElement.className += ' selected';
                    selectOption(event.srcElement.innerText, options, displayCol);
                }
            });
        }

        listbox.attachEvent('onkeydown', function (e) {

            // IE8 does not understand Array.from, Array.indexOf,
            // var index = Array.from(items).indexOf(document.activeElement);

            var index = 0;
            for (var i = 0; i < items.length; i++) {
                if (items[i] == document.activeElement) {
                    index = i;
                    break;
                }                
            }
            
            if (e.keyCode == 40) {
                e.returnValue = false;
                index = (index + 1) % items.length;
                items[index].focus();
            }
            else if (e.key == 38) {
                e.returnValue = false;
                index = (index - 1 + items.length) % items.length;
                items[index].focus();
            }
            else if (e.keyCode == 27) {
                e.returnValue = false;
                hideOverlay();
            }
        });
    }


    function selectOption(item, options, displayColumn) {
        var selectedData = {};
        for (var n = 0; n < options.length; n++) {
            if (options[n][displayColumn] == item) {
                selectedData = options[n];
                break;
            }
        }

        keys = getKeys(mDescriptor.DbResultFields);
        for (var n = 0; n < keys.length; n++) {
            mDescriptor.DbResultFields[keys[n]] = selectedData[keys[n]];
        }

        mDescriptor.DataQueryStatus = dbSUCCESS;
        hideOverlay();
    }



    // --------------------------------------------------------------------------------------------------------
    //   Sending Requests to Server
    // --------------------------------------------------------------------------------------------------------


    // Send data for a single editing-value to server 
    function sendEditRequest(inputIndex) {

        var inputBox = document.querySelector('#' + inputIndex);
        if (inputBox) {
            var currentField = inputBox.getAttribute('name');

            if (updateDescriptor(currentField)) {
                // User has entered wildcards (% signs) - display options
                debugPrint('Client: Searching options for "' + currentField + "' = " + inputBox.value);
                setTimeout(showOverlay, 100);
                sendGetRequest('/labels/search?query=' + encodeURIComponent(JSON.stringify(mDescriptor.DbQueryFields)) + '&profile=' + mActiveProfile);
            }
            else {
                // No wildcards
                debugPrint('Client: Edit "' + currentField + "' = " + inputBox.value + ', DataQueryStatus = ' + dbStatus[mDescriptor.DataQueryStatus]);
                sendPostRequest('/labels/variables?profile=' + mActiveProfile, mDescriptor, currentField);
            }
        }
        else {
            debugPrint('Cannot send server request - inputField "' + inputIndex + ' not found');
        }
    }


    // Request label be printed
    function sendPrintRequest() {
        debugPrint('Client: PrintRequest. Current DbQueryStatus = ' + dbStatus[mDescriptor.DataQueryStatus]);
        sendPostRequest('/labels/label?profile=' + mActiveProfile + '&printer=' + mActivePrinter, mDescriptor, '');
    }


    // Repeatedly send requests for label to be printed (every 2 sec)
    function sendRepeatedPrintRequest() {
        if (dbTimerIdx > 0) {
            sendPrintRequest();

            dbTimerIdx--;
            if (dbTimerIdx == 0) {
                // Zero reached by decrement (not by clearing timer)
                mErrorLabel.innerText = 'Timeout expired - no response from server. Restart app.';
            }

            setTimeout(sendRepeatedPrintRequest, 500);
        }
    }





    // --------------------------------------------------------------------------------------------------------
    //   xmlHttpRequest
    // --------------------------------------------------------------------------------------------------------

    // POST request for field value editing 
    // To support older browsers, we use XMLHttpRequest rather than the Fetch API
    function sendPostRequest(url, data, currentField) {
        var xhr;

        // This is supposed to support old browsers.
        // Check if browser supports XMLHttpRequest, otherwise fall back to ActiveXObject for IE6
        if (window.XMLHttpRequest) {
            // Modern browsers (IE7+, Firefox, Chrome, etc.)
            xhr = new XMLHttpRequest();
        } else if (window.ActiveXObject) {
            // IE6 and earlier
            xhr = new ActiveXObject("Microsoft.XMLHTTP");
        } else {
            alert("Your browser does not support XMLHttpRequest.");
            return;
        }

        // Open a new POST request to the specified URL
        xhr.open("POST", url, true);

        // Set content type for POST data to "text/plain", because we do not know 
        // the structure(dynamic keyvalue pairs depending on profile)
        xhr.setRequestHeader("Content-Type", "text/plain");

        // Define a callback function to handle the response
        xhr.onreadystatechange = function () {
            if (xhr.readyState == 4) {
                // 4 means the request is complete

                if (xhr.status == 200) {
                    // HTTP OK
                    updateResultFields(xhr.responseText, currentField);    
                    debugPrint('Server: Response "' + currentField + '"' + ', DataQueryStatus = ' + dbStatus[mDescriptor.DataQueryStatus]);

                } else if (xhr.status == 422) {
                    // Database Error
                    updateResultFields(xhr.responseText, currentField);    
                    mErrorLabel.innerText = mDescriptor.DataQueryStatusText;
                    debugPrint('Server: Database Error "' + currentField + '" - ' + mDescriptor.DataQueryStatusText + ' (DataQueryStatus = ' + dbStatus[mDescriptor.DataQueryStatus] + ')');
                    dbTimerIdx = 0;

                } else {
                    // Other type of error 
                    // console.error("Error: " + xhr.status + " - " + xhr.statusText);
                    updateResultFields(xhr.responseText, currentField);    
                    mErrorLabel.innerText = mDescriptor.ErrorMessage;
                    debugPrint('Server: Other Error - ' + mDescriptor.ErrorMessage);
                    dbTimerIdx = 0;
                }

                if (mDescriptor.ReadyToPrint) {
                    debugPrint('Server response: label is printing');
                    clearMessages();
                    mMessageLabel.innerText = 'Printing label';
                    dbTimerIdx = 0;
                }
                    
            }
        };

        // Send as JSON string
        xhr.send(JSON.stringify(data));
    }





    // GET request to obtain option lists
    // To support older browsers, we use XMLHttpRequest rather than the Fetch API
    function sendGetRequest(url) {
        var xhr;

        // Check if browser supports XMLHttpRequest, otherwise fall back to ActiveXObject for IE6
        if (window.XMLHttpRequest) {
            // Modern browsers (IE7+, Firefox, Chrome, etc.)
            xhr = new XMLHttpRequest();
        } else if (window.ActiveXObject) {
            // IE6 and earlier
            xhr = new ActiveXObject("Microsoft.XMLHTTP");
        } else {
            console.error("Your browser does not support XMLHttpRequest.");
            return;
        }

        // Open a new GET request to the specified URL
        xhr.open("GET", url, true);

        // Define a callback function to handle the response
        xhr.onreadystatechange = function () {
            if (xhr.readyState == 4) {
                // 4 means the request is complete

                if (xhr.status == 200) {
                    // HTTP OK
                    var options = JSON.parse(xhr.responseText);
                    createOverlay(options);
                }
                else if (xhr.status == 422) {
                    // Database Error
                    mErrorLabel.innerText = 'Database Error\r\n' + xhr.responseText;
                }
                else {
                    // Other type of error 
                    mErrorLabel.innerText = 'Other error \r\n' + xhr.responseText;
                }
            }
        };

        xhr.send();
    }




    // --------------------------------------------------------------------------------------------------------
    //   Diagnostics 
    // --------------------------------------------------------------------------------------------------------
    function debugPrint(text) {
        if (NoDebug)
            return;
        //
        debugMsg.innerText += text + '\r\n';
    }

    function debugStatusShow() {
        if (NoDebug)
            return;
        //

        text = '';

        keys = getKeys(mDescriptor.DbQueryFields);
        for (n = 0; n < keys.length; n++) {
            text += keys[n] + ':     ' + mDescriptor.DbQueryFields[keys[n]] + '\r\n';
        }
        text += '\r\n';

        keys = getKeys(mDescriptor.DbResultFields);
        for (n = 0; n < keys.length; n++) {
            text += keys[n] + ':     ' + mDescriptor.DbResultFields[keys[n]] + '\r\n';
        }
        text += '\r\n';

        keys = getKeys(mDescriptor.EditableFields);
        for (n = 0; n < keys.length; n++) {
            text += keys[n] + ':     ' + mDescriptor.EditableFields[keys[n]] + '\r\n';
        }
        text += '\r\n';

        text += 'CurrentEditField:     ' + mDescriptor.CurrentEditField + '\r\n';
        text += 'LastSearchField:      ' + mDescriptor.LastSearchField + '\r\n';
        text += 'DataQueryStatus:      ' + dbStatus[mDescriptor.DataQueryStatus] + '\r\n';
        text += 'DataQueryStatusText:  ' + mDescriptor.DataQueryStatusText + '\r\n';
        text += 'LabelCount:           ' + mDescriptor.LabelCount + '\r\n';
        text += 'Error Message:        ' + mDescriptor.ErrorMessage + '\r\n';
        text += 'ReadyToPrint:         ' + mDescriptor.ReadyToPrint + '\r\n';

        debugStatus.innerText = text;
    }





    
})();








