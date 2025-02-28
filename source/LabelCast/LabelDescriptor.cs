using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LabelCast
{
    public class LabelDescriptor
    {
        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public LabelDescriptor() { }

        /// <summary>
        /// Copy construcor (deep copy)
        /// </summary>
        public LabelDescriptor(LabelDescriptor descriptor)
        {
            this.DbQueryFields = descriptor.DbQueryFields;
            this.DbResultFields = descriptor.DbResultFields;
            this.EditableFields = descriptor.EditableFields;

            this.CurrentEditField = descriptor.CurrentEditField;
            this.LastSearchField = descriptor.LastSearchField;
            this.DataQueryStatus = descriptor.DataQueryStatus;

            this.LabelCount = descriptor.LabelCount;
            this.ReadyToPrint = descriptor.ReadyToPrint;
        }


        /// <summary>
        /// Generating a label descriptor based on field definitions in a Profile object.
        /// </summary>
        public LabelDescriptor(Profile profile)
        {
            bool isFirst = true;
            foreach (String searchField in profile.SearchFields)
            {
                this.DbQueryFields.Add(searchField, "");

                if (isFirst)
                {
                    this.FirstSearchField = searchField;
                    isFirst = false;
                }

                this.LastSearchField = searchField;
            }

            foreach (String dataField in profile.DataFields)
            {
                this.DbResultFields.Add(dataField, "");
            }

            foreach (String field in profile.EditableFields)
            {
                this.EditableFields.Add(field, "");
            }

            this.DisplayField = profile.DisplayField;

        }

        #endregion

        #region Properties

        /// <summary>
        /// Column name and values for database search criteria (WHERE clause)
        /// </summary>
        public Dictionary<String, String> DbQueryFields { get; set; } = new Dictionary<String, String>();

        /// <summary>
        /// Column name and values for database result (SELECT clause)
        /// </summary>
        public Dictionary<String, String> DbResultFields { get; set; } = new Dictionary<String, String>();

        /// <summary>
        /// Field names and values for UI manual entry fields
        /// </summary>
        public Dictionary<String, String> EditableFields { get; set; } = new Dictionary<String, String>();

        /// <summary>
        /// Read-only property to obtain the index of the first edit field.<br/>
        /// This is from the viewpoint of the "FieldTable" object created for the UI which
        /// shows DbQueryFields and EditFields and then LabelCount field. This index is the
        /// first field after the DbQueryFields.
        /// </summary>
        public int FirstEditFieldIndex {  get { return DbQueryFields.Count; } }
            


        /// <summary>
        /// Name of currently edited field in user input.
        /// </summary>
        public String CurrentEditField { get; set; } = "";

        /// <summary>
        /// Key of first (top) entry in DbQueryFields. Used for numeric search queries only.
        /// </summary>
        public String FirstSearchField { get; set; } = "";

        /// <summary>
        /// Key of last (bottom) entry in DbQueryFields. When multiple DbQueryFields exist, 
        /// this is used when the user has entered values into all of them and start db query.
        /// </summary>
        public String LastSearchField { get; set; } = "";

        /// <summary>
        /// Name of database result column to use when displaying options for user wildcard
        /// entry option selection.
        /// </summary>
        public String DisplayField { get; set; } = "";




        /// <summary>
        /// Status and progress of database queries
        /// </summary>
        public DbQueryStatus DataQueryStatus { get; set; } = DbQueryStatus.NoQuery;

        /// <summary>
        /// Status text of a database query. This is primarily used as failure text.
        /// </summary>
        public String DataQueryStatusText { get; set; } = "";

        /// <summary>
        /// Whether thw current database query is an numeric code query
        /// </summary>
        public bool IsNumericCodeQuery { get; set; } = false;



        /// <summary>
        /// Number of labels to print
        /// </summary>
        public int LabelCount { get; set; } = 1;

        /// <summary>
        /// Whether all data for that label has been filled out by the user including
        /// data obtained from database. It is ready to print.
        /// </summary>
        public Boolean ReadyToPrint { get; set; } = false;

        /// <summary>
        /// Error Message. This may relate to any type of editing of the descriptor
        /// (unlike DataQueryStatusText which only relates to database queries).
        /// </summary>
        public String ErrorMessage { get; set; } = "";

        #endregion

        #region Public API

        /// <summary>
        /// Converts this descriptor to JSON format.
        /// </summary>
        public String ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }


        /// <summary>
        /// Fills out the value of the specified field, automatically determining
        /// which type of field it is (DbQueryField, DbResultField, EditableField).
        /// </summary>
        public void EditFieldValue(String field, String value)
        {
            foreach(String key in  DbQueryFields.Keys)
            {
                if (field.ToLower() == key.ToLower())
                    DbQueryFields[key] = value;
            }

            foreach (String key in DbResultFields.Keys)
            {
                if (field.ToLower() == key.ToLower())
                    DbResultFields[key] = value;
            }

            // Some EditableFields might be the same as DbResultFields.
            // In this case, both get filled out, which is exactly what will
            // happen in the UI as well.

            foreach (String key in EditableFields.Keys)
            {
                if (field.ToLower() == key.ToLower())
                    EditableFields[key] = value;
            }
        }



        /// <summary>
        /// Update descriptor values after database query
        /// </summary>
        public void UpdateDescriptorDbResult(DbQueryStatus queryStatus, Dictionary<String, String>? dbResult = null)
        {
            if (queryStatus == DbQueryStatus.Failed)
            {
                lock(this)
                {
                    this.DataQueryStatus = queryStatus;
                }
                return;
            }

            if (dbResult == null)
            {
                lock (this)
                {
                    this.DataQueryStatus = DbQueryStatus.Failed;
                }
                throw new ApplicationException("Internal error: Database query result is NULL.");
            }

            if (queryStatus == DbQueryStatus.Pending)
                throw new ApplicationException("Internal error: database query status is PENDING after query completed - this is contradictory.");

            // Only when the database query succeeded will we check further:
            String InvalidKey = "";
            lock (this)
            {
                // Update DbResultFields
                foreach (var key in this.DbResultFields.Keys)
                {
                    // validate columns
                    if (!dbResult.ContainsKey(key))
                        InvalidKey = key;
                    else
                        this.DbResultFields[key] = dbResult[key];
                }

                // Also update EditFields (some might be both EditField and DbResultField)
                foreach (var key in this.EditableFields.Keys)
                {
                    if (dbResult.ContainsKey(key))                        
                        this.EditableFields[key] = dbResult[key];
                }

                // Finally, also update DbQueryFields - these are mandated to be in DbResult
                // and if a numeric query was done, only a number is there, none are filled out
                // with what the item would have been:
                foreach (var key in this.DbQueryFields.Keys)
                {
                    if (dbResult.ContainsKey(key))
                        this.DbQueryFields[key] = dbResult[key];
                }

                if (String.IsNullOrEmpty(InvalidKey))
                    this.DataQueryStatus = DbQueryStatus.Success;                
                else
                    this.DataQueryStatus = DbQueryStatus.Failed;
            }

            if (!String.IsNullOrEmpty(InvalidKey))
                throw new ApplicationException("Internal error: Database query result does not contain all of the columns which are required per the DataFields property of the profile: column '" + InvalidKey + "' not found.");
        }


        /// <summary>
        /// Replace variable values in ZPL label print template with the values found by database query
        /// and manual edits.
        /// </summary>
        /// <param name="zplTemplate"></param>
        /// <returns></returns>
        public String FillPrinterTemplate(string zplTemplate)
        {
            String errorMsg = "";

            // Note that the variable replacement will fail if capitalization
            // of variable in the profile configuration is different from the ZPL template!
            //
            // Replacement works on best-effort basis - field may or may not be present in template,
            // but if not found, it logs it as debug message.
            //

            String zpl = zplTemplate;
            Logger.Write(Level.Debug, "Label template before value replacement:\r\n--------------------------------\r\n" + zpl + "\r\n--------------------------------\r\n");

            // Replace fields from DbResultFields
            // Do not try to replace fields which also appear in Editable fields (user-edit is priority0

            foreach (String varName in DbResultFields.Keys.Where(key => !EditableFields.ContainsKey(key)))
            {
                if (zpl.IndexOf("^FD" + varName + "^") < 0)
                    Logger.Write(Level.Debug, "Data Field '" + varName + "' from profile not found in label template\r\n");
                else
                    zpl = zpl.Replace("^FD" + varName + "^", "^FD" + DbResultFields[varName] + "^");
            }

            // EditableFields

            foreach (String varName in EditableFields.Keys)
            {
                if (zpl.IndexOf("^FD" + varName + "^") < 0)
                    Logger.Write(Level.Debug, "Editable Field '" + varName + "' from profile not found in label template\r\n");
                else
                    zpl = zpl.Replace("^FD" + varName + "^", "^FD" + EditableFields[varName] + "^");
            }

            // Label Count 

            Logger.Write(Level.Debug, "Label count = " + this.LabelCount.ToString());

            if (zpl.IndexOf("^PQ1,") < 0)
                errorMsg += "Label template does not contain command for label quantity (^PQ)\r\n";
            else
                zpl = zpl.Replace("^PQ1,", "^PQ" + this.LabelCount.ToString() + ",");

            Logger.Write(Level.Debug, "Label template with values replaced:\r\n-------------\r\n" + zpl + "\r\n--------------------------------\r\n");

            if (!String.IsNullOrEmpty(errorMsg))
                throw new ApplicationException("Label Field mismatch:\r\n" + errorMsg);

            return zpl;
        }

        public void ClearValues()
        {
            foreach(String key in DbQueryFields.Keys)
            {
                DbQueryFields[key] = "";
            }

            foreach (String key in DbResultFields.Keys)
            {
                DbResultFields[key] = "";
            }

            foreach (String key in EditableFields.Keys)
            {
                EditableFields[key] = "";
            }

            CurrentEditField = "";
            DataQueryStatus = DbQueryStatus.NoQuery;
            DataQueryStatusText = "";
            LabelCount = 1;
            ReadyToPrint = false;
            ErrorMessage = "";
        }

        #endregion
    }


    public enum DbQueryStatus
    {
        /// <summary>
        /// No query in progress
        /// </summary>        
        NoQuery = 0,

        /// <summary>
        /// Query started and in still in progress
        /// </summary>        
        Pending = 1,

        /// <summary>
        /// Query successfully completed
        /// </summary>
        Success = 2,

        /// <summary>
        /// Query completed with failure
        /// </summary>
        Failed = 3
    }
}