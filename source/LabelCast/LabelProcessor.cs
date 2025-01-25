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
        /// Printer associated with this LabelProcessor instance.
        /// (Profiles define a default printer, but users can modify.)
        /// </summary>
        public Printer? ActivePrinter
        {
            get { return mPrinter; }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Generate a DataTable representation of the fields which must be 
        /// present in the form to fill out (search fields and editable fields)
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

            foreach (String searchField in mProfile.EditableFields)
            {
                DataRow row = fieldTable.NewRow();
                row[0] = searchField;
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

        
        /// <summary>
        /// Process an edited value.<br/>
        /// Optimized for Windows desktop app usage.<br/>
        /// his method should be called every time a user has entered a value into 
        /// one of the editable fields.
        /// </summary>
        public void EditFieldValue(string varName, string value)
        {
            Logger.Write(Level.Notice, "LabelProcessor EditFieldValue API: field '" + varName + "', value: " + value);

            if (mDescriptor.DbQueryFields.ContainsKey(varName))
            {
                mDescriptor.DbQueryFields[varName] = value;
                if (varName == mDescriptor.LastSearchField)
                {
                    Logger.Write(Level.Debug, " - detected that this is the last search variable");
                    QueryDatabaseAsync();
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
        /// -- Web only -- <br/>
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
                if (editVar == mDescriptor.LastSearchField)
                {
                    Logger.Write(Level.Debug, " - detected last search variable. Now quering database.");
                    
                    // Any database related error will throw "DataException"
                    try
                    {
                        msgResult = QueryDatabase();
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
        /// -- Web only -- <br/>
        /// Query database for a list of options when the user query contains wildcards.
        /// This does return the full data for each option.
        /// </summary>
        public List<Dictionary<String, String>> QueryDatabaseOptionValues(Dictionary<String, String>? queryVars)
        {
            Logger.Write(Level.Notice, "Database Option Values Query start.");

            Logger.Write(Level.Debug, "Profile SQLQuery = " + mProfile.SqlQuery);
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

        #region Public Events

        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public delegate void PrintCompleteEventHandler(object sender, EventArgs e);

        public event MessageEventHandler? MessageEvent;
        public event PrintCompleteEventHandler? PrintCompleteEvent;

        /// <summary>
        /// Event for notification messages.
        /// </summary>
        protected virtual void TriggerLabelMessageEvent(String message)
        {
            Logger.Write(Level.Debug, "TriggerLabelMessage: " + message);
            MessageEvent?.Invoke(this, new MessageEventArgs(message));
        }


        /// <summary>
        /// Event indicating that a label has completed printing.
        /// </summary>
        protected virtual void TriggerPrintCompleteEvent()
        {
            Logger.Write(Level.Debug, "TriggerPrintCompleteEvent");
            PrintCompleteEvent?.Invoke(this, EventArgs.Empty);
        }

        #endregion


        #region Internal Data Processing Methods

        /// <summary>
        /// Query database for the values for the label.<br/>
        /// This is an asynchronous method.<br/>
        /// It should be called when the variables required to filter the query
        /// from the database has been filled out. 
        /// </summary>
        private async void QueryDatabaseAsync()
        {
            Logger.Write(Level.Debug, "Database Query start.");
            mDescriptor.DataQueryStatus = DbQueryStatus.Pending;

            String message = await Task.Run(() =>
            {
                try
                {
                    Logger.Write(Level.Debug, "DbTask: Query criteria = " + JsonConvert.SerializeObject(mDescriptor.DbQueryFields, Formatting.Indented));

                    Dictionary<String, String> dbResult = new Dictionary<string, string>();

                    var db = CreateDb(mProfile.DatabaseType);
                    dbResult = db.QueryData(mProfile.SqlQuery, mDescriptor.DbQueryFields, mDescriptor.DbResultFields);

                    mDescriptor.UpdateDescriptorDbResult(DbQueryStatus.Success, dbResult);
                    Logger.Write(Level.Debug, "DbTask: Updated Descriptor after query:\r\n" + JsonConvert.SerializeObject(mDescriptor, Formatting.Indented));

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
        private String QueryDatabase()
        {
            Logger.Write(Level.Notice, "Database Query start.");
            mDescriptor.DataQueryStatus = DbQueryStatus.Pending;

            String message = "";
            try
            {
                Logger.Write(Level.Debug, "DbTask: Query criteria = " + JsonConvert.SerializeObject(mDescriptor.DbQueryFields, Formatting.Indented));

                Dictionary<String, String> dbResult = new Dictionary<string, string>();

                var db = CreateDb(mProfile.DatabaseType);
                dbResult = db.QueryData(mProfile.SqlQuery, mDescriptor.DbQueryFields, mDescriptor.DbResultFields);

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
                {
                    TriggerPrintCompleteEvent();
                }
                else
                {
                    TriggerLabelMessageEvent(msgResult);
                }
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

        #endregion

        #region Database Factory

        private DbWrapperBase CreateDb(DBType DatabaseType)
        {
            switch(DatabaseType)
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

    #endregion

    #region Enumerations

    public enum LabelContentType
    {
        JSON = 0,
        XML = 1
    }

    #endregion
}
