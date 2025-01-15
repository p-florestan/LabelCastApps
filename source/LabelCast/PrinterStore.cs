using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelCast
{
    /// <summary>
    /// Static printer store to make the printer list available through editors and type
    /// converters for the Profile property grid - we cannot pass a list of printers to these
    /// editors since they are invoked by Windows based on property annotation.
    /// </summary>
    public static class PrinterStore
    {
        /// <summary>
        /// List of configured printers
        /// </summary>
        public static List<Printer> Printers { get; private set; } = new List<Printer>();

        /// <summary>
        /// Method to set the list of printers dynamically 
        /// </summary>
        public static void SetPrinters(List<Printer> printers)
        {
            Printers = printers;
        }

        /// <summary>
        /// Method to add a single new printer object
        /// </summary>
        public static void AddPrinter(Printer printer)
        {
            Printers.Add(printer);
        }

        /// <summary>
        /// Method to remove a printer by name
        /// </summary>
        public static void RemovePrinter(String name)
        {
            Printer? printer = Printers.FirstOrDefault(p => p.Name.Trim().ToLower() == name.Trim().ToLower());
            if (printer != null)
            {
                Printers.Remove(printer);
            }
        }

        /// <summary>
        /// Retrieve printers by name
        /// </summary>
        public static Printer? GetPrinterByName(string name)
        {
            return Printers.FirstOrDefault(p => p.Name == name);
        }

        /// <summary>
        /// Method to clear out the entire list of printes and replace it with a new list.
        /// </summary>
        public static void ReplaceAllPrinters(List<Printer> printerList)
        {
            Printers.Clear();
            foreach (Printer p in printerList)
            {
                Printers.Add(p);
            }
        }

    }

}
