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

        public String PageMessage { get; set; } = "";
    }
}
