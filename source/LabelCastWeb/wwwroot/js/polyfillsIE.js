﻿// --------------------------------------------------------------------------------------------------------
//   Polyfills for IE
// --------------------------------------------------------------------------------------------------------


// Document.querySelectorAll method (for IE-6)
//
// Note that this does not properly work for attribute queries (such as 'input[type="text"]')
//
if (!document.querySelectorAll) {
    document.querySelectorAll = function (selectors) {
        var style = document.createElement('style'), elements = [], element;
        document.documentElement.firstChild.appendChild(style);
        document._qsa = [];

        style.styleSheet.cssText = selectors + '{x-qsa:expression(document._qsa && document._qsa.push(this))}';
        window.scrollBy(0, 0);
        style.parentNode.removeChild(style);

        while (document._qsa.length) {
            element = document._qsa.shift();
            element.style.removeAttribute('x-qsa');
            elements.push(element);
        }
        document._qsa = null;
        return elements;
    };
}

// Document.querySelector method (for IE-6)

if (!document.querySelector) {
    document.querySelector = function (selectors) {
        var elements = document.querySelectorAll(selectors);
        return (elements.length) ? elements[0] : null;
    };
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
    try {
        String.prototype.trim = function () {
            return this.replace(/^\s+|\s+$/g, '');
        };
    }
    catch (e) {
        alert('error in polyfill')
    }
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

