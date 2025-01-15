using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing.Design;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace LabelCast
{
    /// <summary>
    /// Represents a single configuration profile. <br/>
    /// Multiple such profiles can exist which denote different label formats or
    /// different data providers.
    /// </summary>
    public class Profile
    {

        #region Constructors

        public Profile() { }

        #endregion

        #region Properties 

        // -- General Properties 

        public String Name { get; set; } = "";
        public String Description { get; set; } = "";
        public String Abbreviation { get; set; } = "";


        // -- Database 

        [JsonConverter(typeof(StringEnumConverter))]
        public DBType DatabaseType { get; set; } = DBType.None;
        public String DbConnectionString { get; set; } = "";
        public String DbTimeZone { get; set; } = "";
        public String SqlQuery { get; set; } = "";
        public String SearchSqlQuery { get; set; } = "";
        public String DisplayField { get; set; } = "";


        // -- Fields

        public List<String> SearchFields { get; set; } = new List<String>();
        public List<String> DataFields { get; set; } = new List<String>();
        public List<String> EditableFields { get; set; } = new List<String>();


        // -- Label Printing 

        public String LabelTemplate { get; set; } = "";

        public String? DefaultPrinter { get; set; } = "";
        // public Printer? DefaultPrinter { get; set; }


        // -- JSON API Configuration

        public String JSONSchema { get; set; } = "";

        [JsonConverter(typeof(StringEnumConverter))] 
        public ValidateOption JSONSchemaOption { get; set; }

        public Dictionary<String, String> JSONProfileMap { get; set; } = new Dictionary<String, String>();
        public Dictionary<String, String> JSONSearchFieldMap { get; set; } = new Dictionary<String, String>();
        public Dictionary<String, String> JSONDataFieldMap { get; set; } = new Dictionary<String, String>();
        public Dictionary<String, String> JSONEditFieldMap { get; set; } = new Dictionary<String, String>();
        public List<String> JSONPrinterList { get; set; } = new List<String>();
        public String JSONLabelCount { get; set; } = "";


        // -- XML API Configuration

        public String XMLSchema { get; set; } = "";

        [JsonConverter(typeof(StringEnumConverter))]
        public ValidateOption XMLSchemaOption { get; set; }

        public Dictionary<String, String> XMLProfileMap { get; set; } = new Dictionary<String, String>();
        public Dictionary<String, String> XMLSearchFieldMap { get; set; } = new Dictionary<String, String>();
        public Dictionary<String, String> XMLDataFieldMap { get; set; } = new Dictionary<String, String>();
        public Dictionary<String, String> XMLEditFieldMap { get; set; } = new Dictionary<String, String>();
        public List<String> XMLPrinterList { get; set; } = new List<String>();
        public String XMLLabelCount { get; set; } = "";

        #endregion

        #region Public API

        /// <summary>
        /// Converts this profile to JSON format.
        /// </summary>
        internal String ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        #endregion

    }

    #region Enumerations 

    public enum DBType
    {
        None = 0,
        SQLite = 1,
        PostgreSQL = 2,
        SqlServer = 3,
        Oracle = 4
    }

    public enum ValidateOption
    {
        UseProfileSchema = 0,
        UseSchemaInDocument = 1,
        DoNotValidate = 2
    }

    #endregion




}
