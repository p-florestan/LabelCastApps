using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LabelCastWeb.Models;
using Newtonsoft.Json;
using System.Web;
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
            Logger.Write(Level.Debug, "Splash screen for " + browser);
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
                var desc = labelProcessor.LabelDescriptor;

                var entryData = new LabelStructure
                {
                    EntryTable = labelProcessor.GetFieldTable(),
                    LabelDescriptor = desc,
                    ProfileList = App.Config.ProfileList.Select(p => p.Abbreviation).ToList(),
                    ActiveProfile = activeProfile.Abbreviation,
                    PrinterList = PrinterStore.Printers.Select(p => p.Name).ToList(),
                    ActivePrinter = activePrinter.Name,
                    PageEditIndex = 0,
                    PageResultFields = desc.DbResultFields.Except(desc.DbQueryFields)
                                                          .Except(desc.EditableFields)
                                                          .ToDictionary(),
                    PageMessage = "",
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

        #region Public API - Label Requests API

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

        #endregion

        #region Public API - Form Request Methods (IE6)
                
        // These controller methods work like a classic ASP site without Ajax - every request
        // results in a full page view.

        /// <summary>
        /// External API method - submit label data from a simple HTML form, submitted in standard 
        /// www-form-urlencoded format, like classic ASP or PHP.<br/>
        /// Data exchange between client and server occurs through form fields, not by asynchronously exchanging
        /// JSON objects like the regular web client. This is primarily intended for supporting old IE6 browsers, 
        /// and returns HTML (view LabelEntryIE6.cshtml).
        /// </summary>
        [HttpPost("formqueue")]
        public async Task<IActionResult> SubmitLabelHtmlFormClassic()
        {
            LabelProcessor proc;
            DataTable entryTable;
            Dictionary<String, String> fieldList = new Dictionary<String, String>();

            try
            {
                fieldList = await ConvertRequestBody();
                proc = InstantiateLabelProcessor(fieldList);
                entryTable = proc.GetFieldTable();
            }
            catch (Exception ex)
            {
                // Early return - basic error due to invalid HTML form data
                return View("SplashIE6", "Invalid input form data: " + ex.Message);
            }

            try 
            { 
                Logger.Write(Level.Debug, "FormQueue request: " + JsonConvert.SerializeObject(fieldList, Formatting.Indented));

                LabelDescriptor desc = proc.LabelDescriptor;
                desc = UpdateDescriptorFromFieldList(desc, fieldList);

                bool isDbQuery = false;
                String pageMsg = "";
                if (desc.CurrentEditField == "Number of Labels")
                {
                    // Print label
                    entryTable = UpdateEntryTableFromFieldList(entryTable, fieldList);
                    (desc, var msg) = proc.FinalizeAndPrintWeb(desc);               
                    if (!String.IsNullOrWhiteSpace(msg))
                        throw new ApplicationException(msg);
                    // reset values
                    entryTable = proc.GetFieldTable();
                    isDbQuery = false;
                }
                else
                {
                    // Process field value / query database
                    desc = proc.EditFieldValueWeb(desc);
                    entryTable = UpdateEntryTableFromDbQuery(entryTable, proc.ActiveProfile, desc.DbResultFields);
                    isDbQuery = true;
                }

                var entryData = PrepareLabelStructure(proc, entryTable, pageMsg, isDbQuery);
                return View("LabelEntryIE6", entryData);

            }
            catch (DataException ex)
            {
                // Database error
                var entryData = PrepareLabelStructure(proc, entryTable, ex.Message, true, true);
                return View("LabelEntryIE6", entryData);
            }
            catch (ArgumentException ex)
            {
                // Invalid data submitted
                var entryData = PrepareLabelStructure(proc, entryTable, ex.Message, true, true);
                return View("LabelEntryIE6", entryData);
            }
            catch (Exception ex)
            {
                // Any other error - HTTP 500 Internal Server Error
                var entryData = PrepareLabelStructure(proc, entryTable, ex.Message, true, true);
                return View("LabelEntryIE6", entryData);
            }
        }


        /// <summary>
        /// Update entry table values (UI field table) from HTML form data.
        /// This is mainly needed when an exception is thrown, so that the field values are
        /// shown in the UI along with the error message.
        /// </summary>
        private DataTable UpdateEntryTableFromFieldList(DataTable entryTable, Dictionary<string, string> fieldList)
        {
            // Update any field which you can find in 'entryTable'

            foreach (String searchField in fieldList.Keys)
            {
                int idx = LocateMatchingRow(entryTable, searchField);
                if (idx >= 0)
                    entryTable.Rows[idx]["Value"] = fieldList[searchField];
            }

            return entryTable;
        }



        /// <summary>
        /// Fill out values in LabelDescriptor from the 'fieldList' obtained through the HTML form.
        /// </summary>
        /// <param name="desc">Current LabelDescriptor</param>
        /// <param name="fieldList">Key-value pairs representing field values from HTML form</param>
        /// <returns></returns>
        private LabelDescriptor UpdateDescriptorFromFieldList(LabelDescriptor desc, Dictionary<string, string> fieldList)
        {
            desc.LabelCount = GetLabelCount(fieldList);

            foreach (String key in fieldList.Keys.Except(new List<String> { "profile", "printer" }))
            {
                desc.EditFieldValue(key, HttpUtility.UrlDecode(fieldList[key]));
            }
            desc.CurrentEditField = fieldList["CurrentEditField"];
            desc.DataQueryStatus = (DbQueryStatus)Convert.ToInt32(fieldList["DataQueryStatus"]);

            Logger.Write(Level.Debug, "Descriptor filled out from form fields:\r\n" + JsonConvert.SerializeObject(desc, Formatting.Indented));

            return desc;

        }



        /// <summary>
        /// Convert web request body (www-url-formencoded) to dictionary object.
        /// </summary>
        private async Task<Dictionary<String, String>> ConvertRequestBody()
        {
            Dictionary<String, String> fieldList = new Dictionary<String, String>();
            // Read the entire request body as a single string
            using (var reader = new StreamReader(Request.Body))
            {
                String formData = await reader.ReadToEndAsync();
                fieldList = formData.Split('&')
                                    .Select(x => x.Split('='))
                                     // replace html-encoded "+" signs back to spaces:
                                    .ToDictionary(x => x[0].Replace("+", " "), 
                                                  x => x[1].Replace("+", " "));
            }

            return fieldList;
        }


        /// <summary>
        /// Instantiate label processor using profile and printer information in 'fieldList'
        /// </summary>
        private LabelProcessor InstantiateLabelProcessor(Dictionary<string, string> fieldList)
        {
            Profile profile = FindProfile(fieldList);
            Printer printer = FindPrinter(fieldList);
            LabelProcessor proc = new LabelProcessor(profile, printer);
            return proc;
        }



        /// <summary>
        /// Update the field-table DataTable objects with a column-value pair dictionary
        /// from the database query.
        /// </summary>
        /// <param name="fieldTable">DataTable showing input data to user</param>
        /// <param name="profile">Active profile</param>
        /// <param name="dataValues">Database query result data</param>
        private DataTable UpdateEntryTableFromDbQuery(DataTable fieldTable, Profile? profile, Dictionary<String, String> dataValues)
        {
            if (profile == null)
                return fieldTable;
            if (dataValues == null || dataValues.Count == 0)
                return fieldTable;

            // The sequence in FieldTable is: first DbQueryFields, then EditFields

            // Update dbQuery fields where possible
            foreach (String searchField in profile.SearchFields)
            {
                if (dataValues.ContainsKey(searchField))
                {
                    int idx = LocateMatchingRow(fieldTable, searchField);
                    if (idx >= 0)
                        fieldTable.Rows[idx]["Value"] = dataValues[searchField];
                }
            }

            // Also update EditFields where possible (only update those EditFields which also
            // exist in DbResultFields and thus were returned by database query)
            foreach (String editField in profile.EditableFields)
            {
                if (dataValues.ContainsKey(editField))
                {
                    int idx = LocateMatchingRow(fieldTable, editField);
                    if (idx >= 0)
                        fieldTable.Rows[idx]["Value"] = dataValues[editField];
                }
            }

            return fieldTable;
        }



        /// <summary>
        /// Prepare label structure data for HTML view.
        /// </summary>
        /// <param name="proc"></param>
        /// <param name="entryTable"></param>
        /// <param name="pageMsg"></param>
        /// <param name="isDbQuery"></param>
        /// <returns></returns>
        private LabelStructure PrepareLabelStructure(LabelProcessor proc,
                             DataTable entryTable,
                             String pageMsg,
                             bool isDbQuery,
                             bool focusTopCell = false)
        {
            int pageEditIndex = 0;
            var desc = proc.LabelDescriptor;
            Profile? profile = proc.ActiveProfile;
            Printer? printer = proc.ActivePrinter;

            if (isDbQuery)
            {
                if (focusTopCell)
                    pageEditIndex = 0;
                else 
                    pageEditIndex = desc.FirstEditFieldIndex;
            }
            else
            {
                proc = new LabelProcessor(profile, printer);
                desc = proc.LabelDescriptor;
                pageMsg = "Label printed.";
            }

            // Fill out data for view
            var entryData = new LabelStructure
            {
                EntryTable = entryTable,
                LabelDescriptor = desc,
                ProfileList = App.Config.ProfileList.Select(p => p.Abbreviation).ToList(),
                ActiveProfile = proc.ActiveProfile?.Abbreviation ?? "",
                PrinterList = PrinterStore.Printers.Select(p => p.Name).ToList(),
                ActivePrinter = proc.ActivePrinter?.Name ?? "",
                PageMessage = pageMsg,
                PageResultFields = desc.DbResultFields.Except(desc.DbQueryFields)
                                                      .Except(desc.EditableFields)
                                                      .ToDictionary(),
                PageEditIndex = pageEditIndex
            };

            return entryData;
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


        private int GetLabelCount(Dictionary<String, String> fieldList)
        {
            if (!fieldList.ContainsKey("Number of Labels"))
                throw new ArgumentException("Cannot determine quantity of labels to print ('Number of Labels' field missing).");
            //
            return Convert.ToInt32(fieldList["Number of Labels"]);
        }



        /// <summary>
        /// Locate row index of matching field in fieldTable, if any.
        /// </summary>
        private int LocateMatchingRow(DataTable fieldTable, string fieldName)
        {
            // Columns of fieldTable: Variable, Value
            if (!fieldTable.Columns.Contains("Variable") || !fieldTable.Columns.Contains("Value"))
                throw new ApplicationException("Internal Error: App FieldTable has invalid structure - must have columns 'Variable' and 'Value'.");

            int idx = -1;
            for (int n = 0; n < fieldTable.Rows.Count; n++)
            {
                if (fieldTable.Rows[n]["Variable"].ToString() == fieldName)
                {
                    idx = n;
                    break;
                }
            }
            return idx;
        }

        #endregion
    }

}
