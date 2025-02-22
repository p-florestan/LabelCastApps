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
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using LabelCast;


namespace LabelCastDesktop
{
    /// <summary>
    /// Represents a single configuration profile. <br/>
    /// This class is a DTO for the desktop app only - decorated with annotations which
    /// are necessary for property editing in WinForm PropertyGrid.
    /// </summary>
    public class ProfileDTO
    {

        #region Constructors

        public ProfileDTO() { }

        public ProfileDTO(Profile p)
        {
            if (p == null)
                throw new ArgumentNullException("Configuration error: no active profile found.");

            this.Name = p.Name;
            this.Description = p.Description;
            this.Abbreviation = p.Abbreviation;

            this.DatabaseType = p.DatabaseType;
            this.DbConnectionString = p.DbConnectionString;
            this.DbTimeZone = p.DbTimeZone;
            this.SqlQuery = p.SqlQuery;
            this.SearchSqlQuery = p.SearchSqlQuery;
            this.SqlQueryNumeric = p.SqlQueryNumeric;
            this.DisplayField = p.DisplayField;

            this.SearchFields = new List<string>(p.SearchFields);
            this.DataFields = new List<string>(p.DataFields);
            this.EditableFields = new List<string>(p.EditableFields);

            this.LabelTemplate = p.LabelTemplate;

            if (p.DefaultPrinter == null)
                this.DefaultPrinter = null;
            else
                this.DefaultPrinter = p.DefaultPrinter;
     
            this.JSONSchema = p.JSONSchema;
            this.JSONSchemaOption = p.JSONSchemaOption;
            this.JSONProfileMap = p.JSONProfileMap;
            this.JSONSearchFieldMap = p.JSONSearchFieldMap;
            this.JSONDataFieldMap = p.JSONDataFieldMap;
            this.JSONEditFieldMap = p.JSONEditFieldMap;
            this.JSONPrinterList = p.JSONPrinterList;
            this.JSONLabelCount = p.JSONLabelCount;

            this.XMLSchema = p.XMLSchema;
            this.XMLSchemaOption = p.XMLSchemaOption;
            this.XMLProfileMap = p.XMLProfileMap;
            this.XMLSearchFieldMap = p.XMLSearchFieldMap;
            this.XMLDataFieldMap = p.XMLDataFieldMap;
            this.XMLEditFieldMap = p.XMLEditFieldMap;
            this.XMLPrinterList = p.XMLPrinterList;
            this.XMLLabelCount = p.XMLLabelCount;
        }

        #endregion

        #region Properties 

        // -- General Properties 

        [Category("(General)"),
            DisplayName("(Name)")]
        public String Name { get; set; } = "";


        [Category("(General)"),
            DisplayName("Description")]
        public String Description { get; set; } = "";


        [Category("(General)"),
            DisplayName("Abbreviation")]
        public String Abbreviation { get; set; } = "";


        // -- Database 

        [Category("Database"),
            Description("Type of database to retrieve variable data from. Supported are Oracle and Microsoft SQL Server."),
            DisplayName("Database Type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DBType DatabaseType { get; set; } = DBType.None;


        [Category("Database"),
            Description("Connection string to connect to the database"),
            DisplayName("DB Connection String")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public String DbConnectionString { get; set; } = "";

        
        [Category("Database"),
            Description("Time zone of the location of database server"),
            DisplayName("Server time zone")]
        public String DbTimeZone { get; set; } = "";

        
        [Category("Database"),
            Description("SQL query string to retrieve variable data. The fields in this statement must be defined in the field lists below."),
            DisplayName("SQL Query")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public String SqlQuery { get; set; } = "";


        [Category("Database"),
            Description("SQL query string to find items when wildcard symbol % is used."),
            DisplayName("SQL Search - Main Query")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public String SearchSqlQuery { get; set; } = "";


        [Category("Database"),
            Description("SQL query string to find items with alternate numeric key (barcode etc)"),
            DisplayName("SQL Query - Optional Numeric Code Query")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public String SqlQueryNumeric { get; set; } = "";


        [Category("Database"),
            Description("Field to display to select options (when wildcards are entered)"),
            DisplayName("SQL Search Display Field")]
        public String DisplayField { get; set; } = "";

        
        // -- Fields

        [Category("Fields"),
            Description("One or more fields to filter the database result set (SQL WHERE clause).")]
        public List<String> SearchFields { get; set; } = new List<String>();
        
        
        [Category("Fields"),
            Description("Fields to be retrieved from the database and which appear on the printed label (SQL SELECT clause).")]
        public List<String> DataFields { get; set; } = new List<String>();
        
        
        [Category("Fields"),
            Description("Fields which are manually editable, and which appear on the printed label. They may be also DataFields but aren't required to.")]
        public List<String> EditableFields { get; set; } = new List<String>();

                
        // -- Label Printing 

        [Category("Label Printing"),
            Description("File name for the ZPL label template.")]
        [Editor(typeof(TemplateFileEditor), typeof(UITypeEditor))] 
        public String LabelTemplate { get; set; } = "";

        
        // We use a custom type converter (to represent printers as strings) and type-editor
        // to be able to select custom class instances for printers:
        [Category("Label Printing"),
            DisplayName("Default Printer"),
            Description("Select the name of the printer to use here. Use Printer tab to define printers."),
            TypeConverter(typeof(PrinterTypeConverter))]
        public String? DefaultPrinter { get; set; }


        // -- JSON API Configuration

        [Category("Request API - JSON"),
            DisplayName("JSON Schema"),
            Description("JSON Schema to validate incoming API request format.")]
        [Editor(typeof(JSONSchemaFileEditor), typeof(UITypeEditor))]
        public String JSONSchema { get; set; } = "";


        [Category("Request API - JSON"),
            DisplayName("JSON Schema Option"),
            Description("How to find the JSON schema to apply or whether to skip validation altogether.")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ValidateOption JSONSchemaOption { get; set; }


        [Category("Request API - JSON"),
            Description("Properties and values in the API request to select the profile to use for label printing." +
            "This is a list of JSON properties with the expected values in the request.")]
        [Editor(typeof(DictionaryEditor), typeof(UITypeEditor))]
        public Dictionary<String, String> JSONProfileMap { get; set; } = new Dictionary<String, String>();


        [Category("Request API - JSON"), 
            Description("Properties and values in the API request to determine data-field values (see DataFields configuration)." +
            "This is a list of field names and the corresponding JSON property in the API request to look up the value in.")]
        [Editor(typeof(DictionaryEditor), typeof(UITypeEditor))]
        public Dictionary<String, String> JSONDataFieldMap { get; set; } = new Dictionary<String, String>();


        [Category("Request API - JSON"),
            Description("Properties and values in the API request to determine edit-field values (see EditableFields configuration)." +
            "This is a list of field names and the corresponding JSON property in the API request to look up the value in.")]
        [Editor(typeof(DictionaryEditor), typeof(UITypeEditor))]
        public Dictionary<String, String> JSONEditFieldMap { get; set; } = new Dictionary<String, String>();


        [Category("Request API - JSON"),
            Description("Properties and values in the API request to determine database-query values.")]
        [Editor(typeof(DictionaryEditor), typeof(UITypeEditor))]
        public Dictionary<String, String> JSONSearchFieldMap { get; set; } = new Dictionary<String, String>();


        [Category("Request API - JSON")]
        public List<String> JSONPrinterList { get; set; } = new List<String>();

        [Category("Request API - JSON")]
        public String JSONLabelCount { get; set; } = "";

        // -- XML API Configuration

        [Category("Request API - XML"),
            DisplayName("XML Schema"),
            Description("XML DTD to validate incoming API request format.")]
        [Editor(typeof(DTDFileEditor), typeof(UITypeEditor))]
        public String XMLSchema { get; set; } = "";


        [Category("Request API - XML"),
            DisplayName("XML Schema Option"),
            Description("How to find the XML DTD to apply or whether to skip validation altogether.")]
        public ValidateOption XMLSchemaOption { get; set; }


        [Category("Request API - XML"),
            Description("Node and attribute values in the API request to select the profile to use for label printing." +
            "This is a list of XPath expressions and the expected values in the request.")]
        [Editor(typeof(DictionaryEditor), typeof(UITypeEditor))]
        public Dictionary<String, String> XMLProfileMap { get; set; } = new Dictionary<String, String>();


        [Category("Request API - XML"),
            Description("Node and attribute values in the API request to determine edit-field values (see EditableFields configuration)" +
            "This is a list of field names and the XPath expressions to look up values in the XML request.")]
        [Editor(typeof(DictionaryEditor), typeof(UITypeEditor))]
        public Dictionary<String, String> XMLDataFieldMap { get; set; } = new Dictionary<String, String>();


        [Category("Request API - XML"),
            Description("Node and attribute values in the API request to determine edit-field values (see EditableFields configuration)" +
            "This is a list of field names and the XPath expressions to look up values in the XML request.")]
        [Editor(typeof(DictionaryEditor), typeof(UITypeEditor))]
        public Dictionary<String, String> XMLEditFieldMap { get; set; } = new Dictionary<String, String>();


        [Category("Request API - XML"),
            Description("Properties and values in the API request to determine database-query values.")]
        [Editor(typeof(DictionaryEditor), typeof(UITypeEditor))]
        public Dictionary<String, String> XMLSearchFieldMap { get; set; } = new Dictionary<String, String>();


        [Category("Request API - XML")]
        public List<String> XMLPrinterList { get; set; } = new List<String>();

        [Category("Request API - XML")]
        public String XMLLabelCount { get; set; } = "";

        #endregion

        #region Public API

        public Profile ToProfile()
        {
            return new Profile
            {
                Name = this.Name,
                Description = this.Description,
                Abbreviation = this.Abbreviation,

                DatabaseType = this.DatabaseType,
                DbConnectionString = this.DbConnectionString,
                DbTimeZone = this.DbTimeZone,
                SqlQuery = this.SqlQuery,
                SearchSqlQuery = this.SearchSqlQuery,
                SqlQueryNumeric = this.SqlQueryNumeric,

                SearchFields = new List<string>(this.SearchFields),
                DataFields = new List<string>(this.DataFields),
                EditableFields = new List<string>(this.EditableFields),

                LabelTemplate = this.LabelTemplate,
                DefaultPrinter = this.DefaultPrinter,

                JSONSchema = this.JSONSchema,
                JSONSchemaOption = this.JSONSchemaOption,
                JSONProfileMap = this.JSONProfileMap,
                JSONSearchFieldMap = this.JSONSearchFieldMap,
                JSONDataFieldMap = this.JSONDataFieldMap,
                JSONEditFieldMap = this.JSONEditFieldMap,
                JSONPrinterList = this.JSONPrinterList,
                JSONLabelCount = this.JSONLabelCount,

                XMLSchema = this.XMLSchema,
                XMLSchemaOption = this.XMLSchemaOption,
                XMLProfileMap = this.XMLProfileMap,
                XMLDataFieldMap = this.XMLDataFieldMap,
                XMLEditFieldMap = this.XMLEditFieldMap,
                XMLSearchFieldMap = this.XMLSearchFieldMap,
                XMLPrinterList = this.XMLPrinterList,
                XMLLabelCount = this.XMLLabelCount
            };
        }

        #endregion
    }


}
