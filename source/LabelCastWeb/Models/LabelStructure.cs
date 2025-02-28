using System;
using System.Data;
using LabelCast;


namespace LabelCastWeb.Models
{
    public class LabelStructure
    {
        public DataTable EntryTable { get; set; } = new DataTable();
        public LabelDescriptor LabelDescriptor { get; set; } = new LabelDescriptor();

        public List<String> ProfileList { get; set; } = new List<String>();
        public String ActiveProfile { get; set; } = "";

        public List<String> PrinterList { get; set; } = new List<String>();
        public String ActivePrinter { get; set; } = "";


        // Properties for API method "formqueue" for classic IE6 pages

        // Form Page message
        public String PageMessage { get; set; } = "";

        // EditIndex (index of input element to focus next)
        public int PageEditIndex { get; set; } = 0;

        // List of DbResult fields which are not DbQuery fields and not Editable fields
        // (so that we can add them to the page as form fields). Used for final form
        // submission to print the label
        public Dictionary<String, String> PageResultFields { get; set; } = new Dictionary<String, String>();
    }
}
