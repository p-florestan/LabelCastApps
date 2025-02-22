using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LabelCastWeb;
using LabelCastWeb.Models;
using Newtonsoft.Json;
using System.Net;
using LabelCast;

namespace LabelCastWeb.Controllers
{
    [Route("labels")]
    public class AppController : Controller
    {
        #region Constructors

        public AppController()
        {
        }

        #endregion


        #region Public API - Main Page

        /// <summary>
        /// Splash screen. This gets saved profile for that client from browser local storage
        /// and then redirects to main entry page.
        /// </summary>
        [HttpGet("main")]
        public IActionResult ShowSplashScreen()
        {
            return View("Splash");
        }


        /// <summary>
        /// Display main label entry home page (for modern regular browsers).
        /// Requires minimum Firefox 52 / Chrome 
        /// </summary>
        [HttpGet("entry")]
        public IActionResult GetLabelEntryPage(String p, String n)
        {
            Logger.Write(Level.Debug, "GetLabelEntryPage - profile = " + p + ", printer = " + n);
            try
            {
                App.Config.ReadConfiguration();

                Profile activeProfile = GetActiveProfile(p);
                Printer activePrinter = GetActivePrinter(n);
                var labelProcessor = new LabelProcessor(activeProfile, activePrinter);

                var entryData = new LabelStructure
                {
                    EntryTable = labelProcessor.GetFieldTable(),
                    LabelDescriptor = labelProcessor.LabelDescriptor,
                    ProfileList = App.Config.ProfileList.Select(p => p.Abbreviation).ToList(),
                    ActiveProfile = activeProfile.Abbreviation,
                    PrinterList = PrinterStore.Printers.Select(p => p.Name).ToList(),
                    ActivePrinter = activePrinter.Name
                };

                return View("LabelEntry", entryData);
            }
            catch (Exception ex)
            {
                Logger.Write(Level.Debug, "Error: " + ex.Message + "\r\nStack Trace " + ex.StackTrace);
                return View("Error", "Error: " + ex.Message);
            }
        }


        // ---------- Pages for IE6+8 browsers -------------------------------------------------


        /// <summary>
        /// Splash screens for IE6-8. This gets saved profile for that client from browser local storage
        /// or cookies and then redirects to main entry page.
        /// </summary>
        [HttpGet("main/{browser}")]
        public IActionResult ShowSplashScreenIE(String browser)
        {
            if (browser.Trim().ToLower() == "ie8")
                return View("SplashIE8");
            else if (browser.Trim().ToLower() == "ie6")
                return View("SplashIE6");
            else return View("Error", "Page does not exist.");
        }


        /// <summary>
        /// Display main label entry home pages - versions for IE6+8 old browsers
        /// </summary>
        [HttpGet("entry/{browser}")]
        public IActionResult GetLabelEntryPageIE(String p, String n, String browser)
        {
            Logger.Write(Level.Debug, "GetLabelEntryPageIE - profile = " + p + ", printer = " + n);
            try
            {
                String viewName = "";
                if (browser.Trim().ToLower() == "ie8")
                    viewName = "LabelEntryIE8";
                else if (browser.Trim().ToLower() == "ie6")
                    viewName = "LabelEntryIE6";
                else
                    throw new ArgumentException("Page '" + browser + "' does not exist.");

                App.Config.ReadConfiguration();

                Profile activeProfile = GetActiveProfile(p);
                Printer activePrinter = GetActivePrinter(n);
                var labelProcessor = new LabelProcessor(activeProfile, activePrinter);

                var entryData = new LabelStructure
                {
                    EntryTable = labelProcessor.GetFieldTable(),
                    LabelDescriptor = labelProcessor.LabelDescriptor,
                    ProfileList = App.Config.ProfileList.Select(p => p.Abbreviation).ToList(),
                    ActiveProfile = activeProfile.Abbreviation,
                    PrinterList = PrinterStore.Printers.Select(p => p.Name).ToList(),
                    ActivePrinter = activePrinter.Name
                };

                return View(viewName, entryData);
            }
            catch (Exception ex)
            {
                Logger.Write(Level.Debug, "Error: " + ex.Message + "\r\nStack Trace " + ex.StackTrace);
                return View("Error", "Error: " + ex.Message);
            }
        }

        #endregion

        #region Public API - Update Methods

        /// <summary>
        /// Edit a single row in the entry table. This will be called when the user has moved from
        /// one table row to another. Queries database as appropriate.<br/>
        /// Accepts JSON formatted LabelDescriptor object and returns same object with updated values
        /// if applicable.
        /// </summary>
        [HttpPost("variables")]
        public IActionResult EditValues([FromBody] String requestData, [FromQuery] String profile)
        {
            LabelDescriptor? descriptor = new LabelDescriptor();
            try
            {
                descriptor = JsonConvert.DeserializeObject<LabelDescriptor>(requestData);
                if (descriptor == null)
                    throw new ArgumentException("Invalid label descriptor received from HTML client: " + requestData);

                Profile? activeProfile = App.Config.ProfileList.FirstOrDefault(p => p.Abbreviation == profile);
                var labelProcessor = new LabelProcessor(activeProfile, null);  // printer param not required here
                descriptor = labelProcessor.EditFieldValueWeb(descriptor);

                return StatusCode(200, descriptor.ToJson());
            }
            catch (DataException ex)
            {
                // HTTP 422 Unprocessable Entity
                return GetDataErrorResult(422, descriptor, ex);
            }
            catch (ArgumentException ex)
            {
                // Invalid data submitted - HTTP 406 Not Acceptable
                return GetErrorResult(406, descriptor, ex);
            }
            catch (Exception ex)
            {
                // Any other error - HTTP 500 Internal Server Error
                return GetErrorResult(500, descriptor, ex);
            }
        }


        /// <summary>
        /// Check over the label data submitted. If verified as complete, this prints the label.
        /// </summary>
        [HttpPost("label")]
        public IActionResult CheckFinalLabelData([FromBody] String requestData, [FromQuery] String profile, [FromQuery] String printer)
        {
            LabelDescriptor? descResult = new LabelDescriptor();
            String errorMsg = "";
            try
            {
                LabelDescriptor? descriptor = JsonConvert.DeserializeObject<LabelDescriptor>(requestData);
                if (descriptor == null)
                    throw new ArgumentException("Invalid label descriptor received from HTML client: " + requestData);

                Profile? activeProfile = App.Config.ProfileList.FirstOrDefault(p => p.Abbreviation == profile);
                Printer? activePrinter = PrinterStore.Printers.FirstOrDefault(p => p.Name == printer);
                var labelProcessor = new LabelProcessor(activeProfile, activePrinter);
                (descResult, errorMsg) = labelProcessor.FinalizeAndPrintWeb(descriptor);

                // There are 4 possible outcomes:

                //   (1)  Other fields aren't filled out  --> show error message
                if (!String.IsNullOrWhiteSpace(errorMsg))
                    return GetResult(406, descResult, errorMsg);     // HTTP 406 - Not Acceptable

                //   (2)  DataQuery shown as FAILED       --> show error message
                else if (descResult.DataQueryStatus == DbQueryStatus.Failed)
                    return GetDataInvalidResult(406, descResult);    // HTTP 406 - Not Acceptable

                //   (3)  DataQuery not complete          --> client must call this method again
                else if (descResult.DataQueryStatus == DbQueryStatus.Pending)
                    return GetResult(200, descResult, "");

                //   (4)  Query complete, all filled out  --> we print the label, show success to client
                else if (descResult.DataQueryStatus == DbQueryStatus.Success)
                    return GetResult(200, descResult, "");

                else
                    throw new ApplicationException("Internal Error while finalizing label: DataQueryStatus " + descResult.DataQueryStatus.ToString());
            }
            catch (Exception ex)
            {
                return GetErrorResult(500, descResult, ex);
            }
        }


        /// <summary>
        /// Obtains a list of options for database query results when the user has entered
        /// database search criteria with wildcards.
        /// </summary>
        [HttpGet("search")]
        public IActionResult SearchOptionValues(String query, String profile)
        {
            try
            {
                Profile? activeProfile = App.Config.ProfileList.FirstOrDefault(p => p.Abbreviation == profile);
                var labelProcessor = new LabelProcessor(activeProfile, null); // printer param not required here

                var queryVars = JsonConvert.DeserializeObject<Dictionary<string, string>>(query);
                var optionList = labelProcessor.DbWildcardQueryWeb(queryVars);

                return StatusCode(200, JsonConvert.SerializeObject(optionList));
            }
            catch (DataException ex)
            {
                // HTTP 422 Unprocessable Entity
                return StatusCode(422, ex.Message);
            }
            catch (Exception ex)
            {
                // Any other error - HTTP 500 Internal Server Error
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region Public API - Label Requests

        /// <summary>
        /// External API method - submit label data for printing (with all data fields 
        /// filled out).<br/>
        /// The label data can be in XML or JSON format and must contain fields to select
        /// profile and/or printer to use, as configured in the profile definitions.
        /// </summary>
        [HttpPost("printqueue")]
        public IActionResult SubmitLabel([FromBody] String request)
        {
            try
            {
                var req = new RequestHandler(request,
                                             App.Config.ConfigDir,
                                             App.Config.ProfileList,
                                             PrinterStore.Printers);

                req.Descriptor.DataQueryStatus = DbQueryStatus.Success;

                LabelProcessor proc = new LabelProcessor(req.RequestProfile, req.RequestPrinter);

                (var descriptor, var msg) = proc.FinalizeAndPrintWeb(req.Descriptor);
                if (!String.IsNullOrWhiteSpace(msg))
                    throw new ApplicationException(msg);

                return StatusCode(200, "Label printed.");
            }
            catch (DataException ex)
            {
                // HTTP 422 Unprocessable Entity
                return StatusCode(422, ex.Message);
            }
            catch (ArgumentException ex)
            {
                // Invalid data submitted - HTTP 406 Not Acceptable
                return StatusCode(406, ex.Message);
            }
            catch (Exception ex)
            {
                // Any other error - HTTP 500 Internal Server Error
                return StatusCode(500, ex.Message);
            }
        }



        /// <summary>
        /// External API method - submit label data, requiring a database query to obtain the final
        /// label fields, and then sending the label to printer.<br/>
        /// The label data can be in XML or JSON format and must contain fields to select
        /// profile and/or printer to use, as configured in the profile definitions.
        /// </summary>
        [HttpPost("dataqueue")]
        public IActionResult SubmitDataQueryLabel([FromBody] String request)
        {
            try
            {
                // Request handler fills out all fields, except DbResultFields
                var req = new RequestHandler(request,
                                             App.Config.ConfigDir,
                                             App.Config.ProfileList,
                                             PrinterStore.Printers,
                                             requireDbQuery: true);

                LabelProcessor proc = new LabelProcessor(req.RequestProfile, req.RequestPrinter);

                // Trigger db-query by setting edit-field to LastSearchField
                req.Descriptor.CurrentEditField = req.Descriptor.LastSearchField;
                var descriptor = proc.EditFieldValueWeb(req.Descriptor);

                (descriptor, var msg) = proc.FinalizeAndPrintWeb(descriptor);
                if (!String.IsNullOrWhiteSpace(msg))
                    throw new ApplicationException(msg);

                return StatusCode(200, "Label printed.");
            }
            catch (DataException ex)
            {
                // HTTP 422 Unprocessable Entity
                return StatusCode(422, ex.Message);
            }
            catch (ArgumentException ex)
            {
                // Invalid data submitted - HTTP 406 Not Acceptable
                return StatusCode(406, ex.Message);
            }
            catch (Exception ex)
            {
                // Any other error - HTTP 500 Internal Server Error
                return StatusCode(500, ex.Message);
            }
        }


        /// <summary>
        /// External API method - submit label data from a simple HTML form, 
        /// submitted in standard www-form-urlencoded format.<br/>
        /// (This is primarily intended for supporting old IE6 browsers,
        /// and therefore, upon success, it returns the view LabelEntryIE6.)
        /// </summary>
        [HttpPost("formqueue")]
        public async Task<IActionResult> SubmitLabelHtmlFormIE6()
        {
            try
            {
                Dictionary<String, String> fieldList = new Dictionary<String, String>();

                // Read the entire request body as a single string
                using (var reader = new StreamReader(Request.Body))
                {
                    String formData = await reader.ReadToEndAsync();
                    fieldList = formData.Split('&')
                                        .Select(x => x.Split('='))
                                        .ToDictionary(x => x[0], x => x[1]);
                }

                Logger.Write(Level.Debug, "FormQueue request: " + JsonConvert.SerializeObject(fieldList, Formatting.Indented));

                Profile profile = FindProfile(fieldList);
                Printer printer = FindPrinter(fieldList);
                LabelProcessor proc = new LabelProcessor(profile, printer);

                var desc = new LabelDescriptor(profile);
                desc.LabelCount = GetLabelCount(fieldList);
                foreach (String key in fieldList.Keys.Except(new List<String> { "profile", "printer" }))
                {
                    desc.EditFieldValue(key, fieldList[key]);
                }

                // Database query needed?
                if (desc.DbQueryFields.Count > 0)
                {
                    // Trigger db-query by setting edit-field to LastSearchField
                    desc.CurrentEditField = desc.LastSearchField;
                    desc = proc.EditFieldValueWeb(desc);
                }

                // Print label
                (desc, var msg) = proc.FinalizeAndPrintWeb(desc);
                if (!String.IsNullOrWhiteSpace(msg))
                    throw new ApplicationException(msg);

                // Return entire LabelEntryIE6 view with success message (no AJAX)
                var entryData = new LabelStructure
                {
                    EntryTable = proc.GetFieldTable(),
                    LabelDescriptor = proc.LabelDescriptor,
                    ProfileList = App.Config.ProfileList.Select(p => p.Abbreviation).ToList(),
                    ActiveProfile = profile.Abbreviation,
                    PrinterList = PrinterStore.Printers.Select(p => p.Name).ToList(),
                    ActivePrinter = printer.Name,
                    PageMessage = "Label printed."
                };

                return View("LabelEntryIE6", entryData);
            }
            catch (DataException ex)
            {
                // HTTP 422 Unprocessable Entity
                return StatusCode(422, ex.Message);
            }
            catch (ArgumentException ex)
            {
                // Invalid data submitted - HTTP 406 Not Acceptable
                return StatusCode(406, ex.Message);
            }
            catch (Exception ex)
            {
                // Any other error - HTTP 500 Internal Server Error
                return StatusCode(500, ex.Message);
            }
        }

        #endregion
        

        #region Internal Methods - Error Handlers

        /// <summary>
        /// Obtains IActionResult for a database-query related error, when an exception occured,
        /// with the specified HTTP Response code.
        /// </summary>
        private IActionResult GetDataErrorResult(int httpCode, LabelDescriptor? descriptor, Exception ex)
        {
            descriptor ??= new LabelDescriptor();
            descriptor.DataQueryStatus = DbQueryStatus.Failed;
            descriptor.DataQueryStatusText = ex.Message;
            LogErrors(ex);
            return StatusCode(httpCode, descriptor.ToJson());
        }

        /// <summary>
        /// Obtains IActionResult for general errors, when an exception was thrown,
        /// with the specified HTTP Response code.
        /// </summary>
        private IActionResult GetErrorResult(int httpCode, LabelDescriptor? descriptor, Exception ex)
        {
            descriptor ??= new LabelDescriptor();
            descriptor.ErrorMessage = "Error - " + ex.Message;
            LogErrors(ex);
            return StatusCode(httpCode, descriptor.ToJson());
        }

        /// <summary>
        /// Obtains IActionResult for returning data when no exception was thrown.
        /// </summary>
        private IActionResult GetDataInvalidResult(int httpCode, LabelDescriptor? descriptor)
        {
            descriptor ??= new LabelDescriptor();
            return StatusCode(httpCode, descriptor.ToJson());
        }


        /// <summary>
        /// Obtains IActionResult to return descriptor with a message, when no exception occured.
        /// This can also be an HTTP 200 OK result.
        /// </summary>
        private IActionResult GetResult(int httpCode, LabelDescriptor? descriptor, String message)
        {
            descriptor ??= new LabelDescriptor();
            descriptor.ErrorMessage = message;
            return StatusCode(httpCode, descriptor.ToJson());
        }

        private void LogErrors(Exception ex)
        {
            String trace = $"Stack Trace\r\n{ex.StackTrace}";
            Logger.Write(Level.Debug, ex.Message + "\r\n" + trace);
        }

        #endregion

        #region Internal Methods - Profiles and Printers

        private Profile GetActiveProfile(String profile)
        {
            Profile? activeProfile = null;
            if (String.IsNullOrWhiteSpace(profile))
            {
                activeProfile = App.Config.ActiveProfile;
                if (activeProfile == null)
                    throw new ArgumentException("Invalid configuration - no active configuration profile found.");
            }
            else
            {
                activeProfile = App.Config.ProfileList.FirstOrDefault(p => p.Abbreviation.ToLower().Trim() == profile.ToLower().Trim());
                if (activeProfile == null)
                    activeProfile = App.Config.ProfileList.FirstOrDefault();
                if (activeProfile == null)
                    throw new ArgumentException("There are no profiles configured. Configure at least one profile to use this app.");
            }
            return activeProfile;
        }

        private Printer GetActivePrinter(String printer)
        {
            Printer? activePrinter = null;
            if (String.IsNullOrWhiteSpace(printer))
            {
                activePrinter = App.Config?.ActivePrinter;
                if (activePrinter == null)
                    throw new ArgumentException("Invalid configuration - no active printer found.");
            }
            else
            {
                activePrinter = PrinterStore.Printers.FirstOrDefault(p => p.Name.ToLower().Trim() == printer.ToLower().Trim());
                if (activePrinter == null)                    
                    activePrinter = PrinterStore.Printers.FirstOrDefault(p => p.Name.ToLower().Trim() == App.Config?.ActiveProfile?.DefaultPrinter?.ToLower().Trim());
                if (activePrinter == null)
                    activePrinter = PrinterStore.Printers.FirstOrDefault();
                if (activePrinter == null)
                    throw new ArgumentException("There are no printers configured at all. At least one printer must be configured before using this app.");
            }
            return activePrinter;
        }
        
        #endregion

        #region Internal Methods - Form Data

        private Profile FindProfile(Dictionary<String, String> fieldList)
        {
            if (!fieldList.ContainsKey("profile"))
                throw new ArgumentException("Cannot determine label profile.");
            var profile = App.Config.ProfileList.Find(p => p.Abbreviation == fieldList["profile"]);
            if (profile == null)
                throw new ArgumentException("Profile '" + fieldList["profile"] + "' not found in list of profiles.");
            //
            return profile;
        }


        private Printer FindPrinter(Dictionary<String, String> fieldList)
        {
            if (!fieldList.ContainsKey("printer"))
                throw new ArgumentException("Cannot determine label printer.");
            var printer = PrinterStore.GetPrinterByName(fieldList["printer"]);
            if (printer == null)
                throw new ArgumentException("Printer '" + fieldList["printer"] + "' not found in list of printers.");
            //
            return printer;
        }


        int GetLabelCount(Dictionary<String, String> fieldList)
        {
            if (!fieldList.ContainsKey("labelcount"))
                throw new ArgumentException("Cannot determine quantity of labels to print ('labelcount' field missing).");
            //
            return Convert.ToInt32(fieldList["labelcount"]);
        }

        #endregion
    }

}
