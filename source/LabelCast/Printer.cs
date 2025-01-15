using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing.Design;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LabelCast
{
    /// <summary>
    /// Represents label printers. They are assigned to profiles.
    /// </summary>
    public class Printer
    {

        #region Properties 

        public String Name { get; set; } = "";

        public String Description { get; set; } = "";

        [JsonConverter(typeof(IPAddressConverter))]
        public IPAddress IPAddress { get; set; } = IPAddress.Parse("0.0.0.0");
                
        public int Port { get; set; } = 0;

        #endregion

    }

    #region PropertyGrid Converter for Printer Class

    /// <summary>
    /// Type converter to represent Printer objects by Name string in the Profile property grid.
    /// </summary>
    public class PrinterTypeConverter : TypeConverter
    {
        /// <summary>
        /// Get list of names of printers - we convert Printer objects to their name for
        /// display in the property-grid. This offers the selection list.
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            List<String> printerNames = PrinterStore.Printers.Select(p => p.Name).ToList();
            return new StandardValuesCollection(printerNames);
        }

        /// <summary>
        /// Determine what source type we can convert from (System.String).
        /// This method MUST be overridden to make the converter work at all.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return (sourceType == typeof(System.String));
        }


        /// <summary>
        /// Convert a single Printer object to its "Name" string property
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is Printer printer)
            {
                return printer.Name;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Convert a printer name to the corresponding Printer object.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
        {
            if (value is string name)
            {
                return PrinterStore.GetPrinterByName(name);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Indicate that the list of available values is fixed (not editable by the user)
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
        {
            return true;
        }

        /// <summary>
        /// Indicate that the list of available values is a collection of fixed options
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
        {
            return true;
        }

        public override bool IsValid(ITypeDescriptorContext? context, object? value)
        {
            return true;
        }
    }

    #endregion



}
