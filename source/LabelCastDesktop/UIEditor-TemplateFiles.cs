using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelCastDesktop
{

    /// <summary>
    /// UI Editor class to edit file names by browsing files with FileDialog.
    /// The only template-file-specific code here is the selection of files to browse.
    /// </summary>
    public class TemplateFileEditor : UITypeEditor
    {
        /// <summary>
        /// Check if the editor can be shown
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        {
            return UITypeEditorEditStyle.Modal;  // Display a modal dialog
        }

        /// <summary>
        /// Open a dialog to edit the IPAddress 
        /// </summary>
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider? provider, object? value)
        {
            if (value is String fileName)
            {
                // Show a dialog to edit the IP address
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.InitialDirectory = "c:\\";
                    dialog.Filter = "Label Templates (*.prn)|*.prn|All files (*.*)|*.*";
                    dialog.FilterIndex = 1;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        // Parse the new IPAddress from the dialog input
                        if (!String.IsNullOrWhiteSpace(dialog.FileName))
                            return dialog.FileName;
                        else
                        {
                            MessageBox.Show("No file selected.");
                        }
                    }
                }
            }

            // If canceled or invalid input, return the original value
            return value;
        }
    }


}
