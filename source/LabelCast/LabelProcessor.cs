using System;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using Newtonsoft.Json;

namespace LabelCast
{
    public class LabelProcessor
    {
        #region Fields 

        /// <summary>
        /// Active profile (selected as active on LabelPrint tab of the UI)
        /// </summary>
        private Profile mProfile = new Profile();

        /// <summary>
        /// Label data - contains both database query and label fields
        /// </summary>
        private LabelDescriptor mDescriptor;

        /// <summary>
        /// Printer to be used when printing. A profile defines a default printer,
        /// but users can specify another.
        /// </summary>
        private Printer? mPrinter;

        /// <summary>
        /// String which holds the text content of the ZPL label template file
        /// configured in mProfile.
        /// </summary>
        private String mZPLTemplate = "";

        /// <summary>
        /// Timer to check whether the database query has completed
        /// </summary>
        private System.Timers.Timer DbQueryTimer;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new LabelProcessor instance based on the supplied active profile and printer.<br/>
        /// Throws an exception if the specified profile is null.
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public LabelProcessor(Profile? activeProfile, Printer? activePrinter)
        {
            if (activeProfile == null)
            {
                Logger.Write(Level.Debug, "LabelProcessor Error - activeProfile parameter cannot be NULL.");
                throw new ApplicationException("LabelProcessor Error: no active profile defined.");
            }

            // ActivePrinter might be null (not yet defined on a new profile, for example).
            // In that case, it will throw an exception only when we try to print

            Logger.Write(Level.Debug, "LabelProcessor instantiation for profile '" + activeProfile.Abbreviation + "'");

            mProfile = activeProfile;
            mPrinter = activePrinter;
            mDescriptor = new LabelDescriptor(activeProfile);

            DbQueryTimer = new System.Timers.Timer(100);
            DbQueryTimer.Elapsed += DbQueryTimer_Elapsed;
            DbQueryTimer.AutoReset = false;
            DbQueryTimer.Enabled = false;

            // diagnostics
            Logger.Write(Level.Debug, "At the end of constructor for LabelProcessor: ");
            if (mPrinter == null)
                Logger.Write(Level.Debug, "No printer assigned (active printer is NULL)");
            else
                Logger.Write(Level.Debug, "Printer Name: '" + mPrinter.Name + "', IP Address: " + mPrinter.IPAddress + ", port " + mPrinter.Port);
            // end
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// LabelDescriptor associated with the active profile used to instantiate
        /// this LabelProcessor.
        /// </summary>
        public LabelDescriptor LabelDescriptor
        {
            get { return mDescriptor; }
        }

        /// <summary>
        /// Currently active profile
        /// </summary>
        public Profile? ActiveProfile
        {
            get { return mProfile; }
        }

        /// <summary>
        /// Printer associated with this LabelProcessor instance.
        /// (Profiles define a default printer, but users can modify.)
        /// </summary>
        public Printer? ActivePrinter
        {
            get { return mPrinter; }
        }

        #endregion


        #region Public API - Common

        /// <summary>
        /// Generate a DataTable representation of the fields which must be 
        /// present in the form to fill out (search fields and editable fields),
        /// with empty values.
        /// </summary>
        public DataTable GetFieldTable()
        {
            DataTable fieldTable = new DataTable();

            fieldTable.Columns.Add("Variable", typeof(System.String));
            fieldTable.Columns.Add("Value", typeof(System.String));

            foreach (String searchField in mProfile.SearchFields)
            {
                DataRow row = fieldTable.NewRow();
                row[0] = searchField;
                row[1] = "";
                fieldTable.Rows.Add(row);
            }

            foreach (String editField in mProfile.EditableFields)
            {
                DataRow row = fieldTable.NewRow();
                row[0] = editField;
                row[1] = "";
                fieldTable.Rows.Add(row);
            }

            // Label Quantity Row - Always Present

            DataRow nbrRow = fieldTable.NewRow();
            nbrRow[0] = "Number of Labels";
            nbrRow[1] = "1";
            fieldTable.Rows.Add(nbrRow);

            return fieldTable;
        }


        /// <summary>
        /// Initialize database. This does not do much in terms of "initialization", 
        /// but rather simply tests we can connect to the database configured in the profile.
        /// </summary>
        public void InitializeDatabase()
        {
            if (mProfile.DatabaseType == DBType.None)
                return;

            TestDatabaseConnection(mProfile);
        }

        public void ClearLabelDescriptor()
        {
            LabelDescriptor.ClearValues();
        }

        #endregion

        #region Public API - Desktop App Only

        /// <summary>
        /// Process an edited value.<br/>
        /// Intended for Windows desktop app usage.<br/>
        /// his method should be called every time a user has entered a value into 
        /// one of the editable fields.
        /// </summary>
        public void EditFieldValue(string varName, string value)
        {
            Logger.Write(Level.Notice, "LabelProcessor EditFieldValue API: field '" + varName + "', value: " + value);

            if (mDescriptor.DbQueryFields.ContainsKey(varName))
            {
                mDescriptor.DbQueryFields[varName] = value;
                if (varName == mDescriptor.FirstSearchField && NumericQueryExists() && value.IsInteger())
                {
                    // Alternate numeric code query
                    Logger.Write(Level.Debug, "Alternate numeric code query (on first search variable)");
                    QueryDatabaseAsync(mProfile.SqlQueryNumeric, UseOnlyFirstQueryField: true);
                }
                else if (varName == mDescriptor.LastSearchField)
                {
                    // Regular database query by item
                    Logger.Write(Level.Debug, "Reqular database query (on last search variable)");
                    QueryDatabaseAsync(mProfile.SqlQuery);
                }
            }
            else if (mDescriptor.EditableFields.ContainsKey(varName))
            {
                mDescriptor.EditableFields[varName] = value;
            }

            else if (varName == "Number of Labels")
            {
                if (Int32.TryParse(value, out int lblCount))
                {
                    mDescriptor.LabelCount = lblCount;
                    Logger.Write(Level.Debug, " - label data entry complete.");
                    WaitForDataEntryComplete();
                }
                else
                    TriggerLabelMessageEvent("Number of labels must be a number");
            }

            else
                TriggerLabelMessageEvent("Configuration Error: User entry field '" + varName + "' not found.");
        }


        /// <summary>
        /// Search in database for a list of options when the user query contains wildcards
        /// using SQLSearchQuery.<br/>
        /// Intended for Windows desktop app usage.<br/>
        /// This is a synchronous method, returning the full data for each option.
        /// </summary>
        public List<Dictionary<String, String>> DbWildcardQueryDesktop(Dictionary<String, String>? queryVars)
        {
            Logger.Write(Level.Notice, "DbWildcardQueryDesktop start.");
            Logger.Write(Level.Debug, "Profile SearchSQLQuery = " + mProfile.SearchSqlQuery);

            var dbResult = new List<Dictionary<string, string>>();
            try
            {
                Logger.Write(Level.Debug, " - Query criteria: \r\n" + JsonConvert.SerializeObject(queryVars, Formatting.Indented));
                if (queryVars == null)
                    throw new ArgumentException("Query criteria empty.");
                if (String.IsNullOrWhiteSpace(mProfile.SearchSqlQuery))
                    throw new ArgumentException("Cannot search - no database search SQL query defined.");


                var db = CreateDb(mProfile.DatabaseType);
                dbResult = db.QueryListData(mProfile.SearchSqlQuery, queryVars, mDescriptor.DbResultFields);

                Logger.Write(Level.Notice, "Database Query Success.");
            }
            catch (Exception ex)
            {
                Logger.Write(Level.Debug, "Error - " + ex.Message);
                throw;
            }

            return dbResult;
        }

        #endregion

        #region Public API - Web App Only

        /// <summary>
        /// Web App Only Method.<br/>
        /// Process a single variable-value pair in a stateless manner, i.e. from an HTML page.<br/>
        /// Because the web demands that the client keep all state, the HTML client sends the entire
        /// label entry data every time, indicating which variable is currently edited.<br/>
        /// * The supplied LabelData is returned unchanged, unless updated by database-query.<br/>
        /// * Sets ReadyToPrint = true on last edit variable, if db query already complete.
        /// </summary>
        public LabelDescriptor EditFieldValueWeb(LabelDescriptor descriptor)
        {
            String editVar = descriptor.CurrentEditField;
            Logger.Write(Level.Notice, "LabelProcessor EditFieldValueWeb API: field '" + editVar + "'");
            Logger.Write(Level.Debug, " - Descriptor received from client:\r\n" + JsonConvert.SerializeObject(descriptor, Formatting.Indented));

            if (editVar == null)
                throw new ArgumentException(" - Error editing value: current edit-field name is empty.");

            mDescriptor = descriptor;
            String msgResult = "";

            if (mDescriptor.DbQueryFields.ContainsKey(editVar))
            {
                String editValue = mDescriptor.DbQueryFields[editVar];
                if (editVar == mDescriptor.FirstSearchField && NumericQueryExists() && editValue.IsInteger())
                {
                    // Alternate numeric code query
                    mDescriptor.IsNumericCodeQuery = true;
                    Logger.Write(Level.Debug, "Alternate numeric code query (on first search variable)");
                    try
                    {
                        // must use only first query criteria field!
                        msgResult = QueryDatabaseWeb(mProfile.SqlQueryNumeric, UseOnlyFirstQueryField:true);
                    }
                    catch (Exception ex)
                    {
                        mDescriptor.DataQueryStatus = DbQueryStatus.Failed;
                        mDescriptor.DataQueryStatusText = ex.Message;
                        Logger.Write(Level.Notice, " - Alternate Numeric Code Query Error: " + ex.Message + "\r\n" + ex.StackTrace);
                        throw new DataException(ex.Message);
                    }
                }
                else if (editVar == mDescriptor.LastSearchField)
                {
                    // Regular database query by item
                    mDescriptor.IsNumericCodeQuery = false;
                    Logger.Write(Level.Debug, "Reqular database query (on last search variable)");
                    try
                    {
                        msgResult = QueryDatabaseWeb(mProfile.SqlQuery);
                    }
                    catch (Exception ex)
                    {
                        mDescriptor.DataQueryStatus = DbQueryStatus.Failed;
                        mDescriptor.DataQueryStatusText = ex.Message;
                        Logger.Write(Level.Notice, " - Database Query Error: " + ex.Message + "\r\n" + ex.StackTrace);
                        throw new DataException(ex.Message);
                    }
                }
            }

            else if (mDescriptor.EditableFields.ContainsKey(editVar))
            {
                // Return request-label-data unchanged
            }

            else if (editVar == "Number of Labels")
            {
                if (mDescriptor.LabelCount == 0)
                    throw new ArgumentException("Number of labels cannot be 0.");
            }

            else
            {
                msgResult = "Configuration Error: User entry field '" + editVar + "' not found.";
            }

            if (!String.IsNullOrEmpty(msgResult))
                throw new ApplicationException(msgResult);

            Logger.Write(Level.Debug, "End of EditFieldValueWeb");

            return mDescriptor;
        }



        /// <summary>
        /// Web App Only Method.<br/>
        /// Verifies that all required label data is filled out, including database results.<br/>
        /// If all is okay, sends the label to the printer.<br/>
        /// Otherwise, returns an error message.
        /// </summary>
        public (LabelDescriptor, String) FinalizeAndPrintWeb(LabelDescriptor descriptor)
        {
            Logger.Write(Level.Notice, "LabelProcessor FinalizeAnd Print API");
            Logger.Write(Level.Debug, " - Descriptor received:\r\n" + JsonConvert.SerializeObject(descriptor, Formatting.Indented));

            String msgResult = "";

            // Editable fields must all be filled out
            foreach (String field in descriptor.EditableFields.Keys)
            {
                if (String.IsNullOrWhiteSpace(descriptor.EditableFields[field]))
                    msgResult += "'" + field + "' must not be empty. ";
            }

            if (descriptor.LabelCount <= 0)
                msgResult += "Label count invalid.";

            Logger.Write(Level.Debug, "Outcome of FinalizeAndPrint: DbQueryStatus " + descriptor.DataQueryStatus.ToString() + ", Error message = '" + msgResult + "'");

            // If all is fine - we print the label

            if (descriptor.DataQueryStatus == DbQueryStatus.Success && String.IsNullOrWhiteSpace(msgResult))
            {
                Logger.Write(Level.Debug, "Label finalized.");
                descriptor.ReadyToPrint = true;
                msgResult += SendLabelToPrinter(descriptor);
            }

            return (descriptor, msgResult);
        }


        /// <summary>
        /// Web App Only Method.<br/>
        /// Search in database for a list of options when the user query contains wildcards
        /// using SQLSearchQuery.<br/>
        /// This is a synchronous method, returning the full data for each option.
        /// </summary>
        public List<Dictionary<String, String>> DbWildcardQueryWeb(Dictionary<String, String>? queryVars)
        {
            Logger.Write(Level.Notice, "DbWildcardQueryWeb start.");
            Logger.Write(Level.Debug, "Profile SearchSQLQuery = " + mProfile.SearchSqlQuery);

            var dbResult = new List<Dictionary<string, string>>();
            try
            {
                Logger.Write(Level.Debug, " - Query criteria: \r\n" + JsonConvert.SerializeObject(queryVars, Formatting.Indented));
                if (queryVars == null)
                    throw new ArgumentException("Query criteria empty.");
                if (String.IsNullOrWhiteSpace(mProfile.SearchSqlQuery))
                    throw new ArgumentException("Cannot search - no database search SQL query defined.");


                var db = CreateDb(mProfile.DatabaseType);
                dbResult = db.QueryListData(mProfile.SearchSqlQuery, queryVars, mDescriptor.DbResultFields);

                Logger.Write(Level.Notice, "Database Query Success.");
            }
            catch (Exception ex)
            {
                Logger.Write(Level.Debug, "Error - " + ex.Message);
                throw;
            }

            return dbResult;
        }


        #endregion


        #region Public Events Raised by this Class

        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public delegate void PrintCompleteEventHandler(object sender, EventArgs e);
        public delegate void QueryCompleteEventHandler(object sender, DbQueryEventArgs e);

        public event MessageEventHandler? MessageEvent;
        public event PrintCompleteEventHandler? PrintCompleteEvent;
        public event QueryCompleteEventHandler? DbQueryCompleteEvent;

        /// <summary>
        /// Event for notification messages.
        /// </summary>
        protected virtual void TriggerLabelMessageEvent(String message)
        {
            Logger.Write(Level.Debug, "LabelProcessor: TriggerLabelMessage: " + message);
            // Invoke events asynchronously to ensure propagation:
            if (MessageEvent != null)
            {
                foreach (MessageEventHandler handler in MessageEvent.GetInvocationList())
                {
                    Task.Run(() => handler.Invoke(this, new MessageEventArgs {  Message = message }));
                }
            }
        }


        /// <summary>
        /// Event indicating that a label has completed printing.
        /// </summary>
        protected virtual void TriggerPrintCompleteEvent()
        {
            Logger.Write(Level.Debug, "LabelProcessor: TriggerPrintCompleteEvent");
            // Invoke events asynchronously to ensure propagation:
            if (PrintCompleteEvent != null)
            {
                foreach (PrintCompleteEventHandler handler in PrintCompleteEvent.GetInvocationList())
                {
                    Task.Run(() => handler.Invoke(this, EventArgs.Empty));
                }
            }
        }


        /// <summary>
        /// Event fired when database query is complete to transmit result.
        /// </summary>
        protected virtual void TriggerQueryCompleteEvent(Dictionary<String, String> dbResult)
        {
            Logger.Write(Level.Debug, "LabelProcessor: TriggerQueryCompleteEvent");
            // Invoke events asynchronously to ensure propagation:
            if (DbQueryCompleteEvent != null)
            {
                foreach (QueryCompleteEventHandler handler in DbQueryCompleteEvent.GetInvocationList())
                {
                    Task.Run(() => handler.Invoke(this, new DbQueryEventArgs(dbResult)));
                }
            }
        }

        #endregion

        #region Internal Data Processing Methods

        /// <summary>
        /// Query database for the values for the label.<br/>
        /// This is an asynchronous method.<br/>
        /// It should be called when the variables required to filter the query
        /// from the database has been filled out. 
        /// </summary>
        private async void QueryDatabaseAsync(String sqlQuery, bool UseOnlyFirstQueryField = false)
        {
            Logger.Write(Level.Debug, "Database Query start.");
            mDescriptor.DataQueryStatus = DbQueryStatus.Pending;

            String message = await Task.Run(() =>
            {
                try
                {
                    Dictionary<String, String> queryCriteria = GetQueryCriteria(UseOnlyFirstQueryField);
                    Dictionary<String, String> dbResult = new Dictionary<string, string>();

                    var db = CreateDb(mProfile.DatabaseType);
                    dbResult = db.QueryData(sqlQuery, queryCriteria, mDescriptor.DbResultFields);

                    mDescriptor.UpdateDescriptorDbResult(DbQueryStatus.Success, dbResult);
                    Logger.Write(Level.Debug, "DbTask: Updated Descriptor after query:\r\n" + JsonConvert.SerializeObject(mDescriptor, Formatting.Indented));
                    
                    TriggerQueryCompleteEvent(dbResult);

                    return "";
                }
                catch (Exception ex)
                {
                    Logger.Write(Level.Debug, "DbTask: Error - " + ex.Message);
                    mDescriptor.UpdateDescriptorDbResult(DbQueryStatus.Failed);
                    return ex.Message;
                }
            });

            Logger.Write(Level.Debug, "Database Query end. QueryStatus = " + mDescriptor.DataQueryStatus.ToString());

            if (!String.IsNullOrEmpty(message))
            {
                TriggerLabelMessageEvent(message);
            }
        }



        /// <summary>
        /// -- Web only -- <br/>
        /// Query database for the values for the label.<br/>
        /// This is a synchronous variant of this method which blocks the calling thread.<br/>
        /// It should be called when the variables required to filter the query
        /// from the database has been filled out. 
        /// </summary>
        private String QueryDatabaseWeb(String sqlQuery, bool UseOnlyFirstQueryField = false)
        {
            Logger.Write(Level.Notice, "Database Query start.");
            mDescriptor.DataQueryStatus = DbQueryStatus.Pending;

            String message = "";
            try
            {
                Dictionary<String, String> queryCriteria = GetQueryCriteria(UseOnlyFirstQueryField);
                Dictionary<String, String> dbResult = new Dictionary<string, string>();

                var db = CreateDb(mProfile.DatabaseType);
                dbResult = db.QueryData(sqlQuery, queryCriteria, mDescriptor.DbResultFields);

                mDescriptor.UpdateDescriptorDbResult(DbQueryStatus.Success, dbResult);
                Logger.Write(Level.Notice, "Database Query Success.");
                Logger.Write(Level.Debug, "DbTask: Updated Descriptor after query:\r\n" + JsonConvert.SerializeObject(mDescriptor, Formatting.Indented));
            }
            catch (ArgumentException argEx)
            {
                Logger.Write(Level.Debug, "DbTask: Argument Error - " + argEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Write(Level.Debug, "DbTask: Error - " + ex.Message);
                mDescriptor.UpdateDescriptorDbResult(DbQueryStatus.Failed);
                message = ex.Message;
            }

            Logger.Write(Level.Notice, "Database Query end. QueryStatus = " + mDescriptor.DataQueryStatus.ToString());

            return message;
        }



        /// <summary>
        /// Obtains query criteria fields, depending whether we should use only the first
        /// query field (in the case of a numeric alternate query), or all fields.
        /// </summary>
        private Dictionary<string, string> GetQueryCriteria(bool useOnlyFirstQueryField)
        {
            Dictionary<String, String> queryCriteria = new Dictionary<String, String>();

            int idx = 1;
            foreach (var entry in mDescriptor.DbQueryFields)
            {
                if (useOnlyFirstQueryField == false || idx == 1)
                {
                    queryCriteria.Add(entry.Key, entry.Value);
                    idx++;
                }
            }

            Logger.Write(Level.Debug, "DbTask: Query criteria = " + JsonConvert.SerializeObject(mDescriptor.DbQueryFields, Formatting.Indented));
            return queryCriteria;
        }




        private void WaitForDataEntryComplete()
        {
            String msgResult = "";

            if (mDescriptor.DataQueryStatus == DbQueryStatus.Pending)
            {
                Logger.Write(Level.Debug, " -- database query not complete, waiting ...");
                DbQueryTimer.Start();
            }
            else if (mDescriptor.DataQueryStatus == DbQueryStatus.Success)
            {
                Logger.Write(Level.Debug, " -- database query complete, sending label to printing.");
                
                msgResult = SendLabelToPrinter(mDescriptor);
                
                if (String.IsNullOrWhiteSpace(msgResult))
                    TriggerPrintCompleteEvent();
                else
                    TriggerLabelMessageEvent(msgResult);
            }
            else
            {
                Logger.Write(Level.Debug, "FinalizeLabelProcessing: Cannot print label - database query failed.");
            }

        }


        private void DbQueryTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            WaitForDataEntryComplete();
        }



        /// <summary>
        /// Print the label based on the data in the specified label descriptor
        /// </summary>
        private String SendLabelToPrinter(LabelDescriptor descriptor)
        {
            Logger.Write(Level.Notice, "Sending label to printer.");

            // Validation

            if (mPrinter == null)
            {
                Logger.Write(Level.Error, "Cannot print: no active printer defined.");
                return "Cannot print: no active printer defined.";
            }

            Logger.Write(Level.Debug, "Printer Name: '" + mPrinter.Name + "', IP Address: " + mPrinter.IPAddress + ", port " + mPrinter.Port);

            if (mPrinter.IPAddress.ToString() == "0.0.0.0")
                return "Cannot print: invalid printer address 0.0.0.0";

            if (mPrinter.IPAddress.AddressFamily != AddressFamily.InterNetwork)
                return "Cannot print: IP address of printer is not IPv4: " + mPrinter.IPAddress;

            try
            {
                if (String.IsNullOrWhiteSpace(mProfile.LabelTemplate))
                    return "No label template file configured. You must specify a file which exists on disk.";
                if (!File.Exists(mProfile.LabelTemplate))
                    return "Specified label template file '" + mProfile.LabelTemplate + "' does not exist on disk.";
                //
                mZPLTemplate = mProfile.LabelTemplate.ReadToString();
            }
            catch (Exception ex)
            {
                String msg = "Can't read label template '" + mProfile.LabelTemplate + "':" + ex.Message;
                TriggerLabelMessageEvent(msg);
                return msg;
            }

            // Make deep copy
            LabelDescriptor labelDesc = new LabelDescriptor(descriptor);
            String ZPLText = "";

            // Value Replacement

            try
            {
                ZPLText = labelDesc.FillPrinterTemplate(mZPLTemplate);
            }
            catch (Exception ex)
            {
                Logger.Write(Level.Error, ex.Message);
                return ex.Message;
            }

            // TEST - Disable PRINTING for now ----------
            // Logger.Write(LogLevel.Debug, "Diagnostics: Starting to print label");
            // return "";
            // ----------

            // Actual Label Printing

            try
            {
                Logger.Write(Level.Debug, "Starting to print label.");
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(mPrinter.IPAddress, mPrinter.Port);

                    using (StreamWriter sw = new StreamWriter(client.GetStream()))
                    {
                        sw.Write(ZPLText);
                        sw.Flush();
                        sw.Close();
                    }
                    client.Close();
                }
                TriggerLabelMessageEvent("Label printed");
                return "";
            }
            catch (Exception ex)
            {
                return "Cannot print label to '" + mPrinter.Name + "': " + ex.Message;
            }
        }


        private async void TestDatabaseConnection(Profile profile)
        {
            TriggerLabelMessageEvent("Connecting to database ...");

            if (String.IsNullOrEmpty(profile.DbConnectionString))
                TriggerLabelMessageEvent("Error: No database connection string found.");
            if (String.IsNullOrEmpty(profile.DbTimeZone))
                TriggerLabelMessageEvent("Error: No database server time zone info found.");

            String message = await Task.Run(() =>
            {
                try
                {
                    var db = CreateDb(mProfile.DatabaseType);
                    db.TestConnection(profile.DbConnectionString);
                    return "Connection to " + mProfile.DatabaseType.ToString() + " database successful.";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            });

            TriggerLabelMessageEvent(message);
        }


        /// <summary>
        /// Checks whether a Numeric-Search-SQL-query exists.
        /// This indicates that the alternate numeric search (searching by barcode or other
        /// numeric ID) is enabled.
        /// </summary>
        private Boolean NumericQueryExists()
        {
            return (!String.IsNullOrWhiteSpace(mProfile.SqlQueryNumeric));
        }

        #endregion

        #region Database Factory

        private DbWrapperBase CreateDb(DBType DatabaseType)
        {
            switch (DatabaseType)
            {
                case DBType.SQLite:
                    return new DbWrapperSQLite(mProfile.DbConnectionString, mProfile.DbTimeZone);
                case DBType.PostgreSQL:
                    return new DbWrapperPostgreSQL(mProfile.DbConnectionString, mProfile.DbTimeZone);
                case DBType.SqlServer:
                    return new DbWrapperMSSQL(mProfile.DbConnectionString, mProfile.DbTimeZone);
                case DBType.Oracle:
                    return new DbWrapperOracle(mProfile.DbConnectionString, mProfile.DbTimeZone);
                default:
                    throw new NotSupportedException("No database configured - cannot create database instance.");
            }
        }

        #endregion
    }

    #region Custom EventArg Classes 

    public class MessageEventArgs : EventArgs
    {
        public String Message { get; set; } = "";

        public MessageEventArgs() { }

        public MessageEventArgs(String message)
        {
            this.Message = message;
        }
    }

    public class ProfileEventArgs : EventArgs
    {
        public List<Profile> Profiles { get; set; } = new List<Profile>();
        public List<Printer> Printers { get; set; } = new List<Printer>();

        public ProfileEventArgs() { }

        public ProfileEventArgs(List<Profile> profiles, List<Printer> printers)
        {
            this.Profiles = profiles;
            this.Printers = printers;
        }
    }

    public class DbQueryEventArgs : EventArgs
    {
        public Dictionary<String, String> DbResult { get; set; } = new Dictionary<String, String>();

        public DbQueryEventArgs() { }

        public DbQueryEventArgs(Dictionary<String, String> dbResult)
        {
            this.DbResult = dbResult;
        }
    }


    #endregion

    #region Enumerations

    public enum LabelContentType
    {
        JSON = 0,
        XML = 1
    }

    #endregion
}
