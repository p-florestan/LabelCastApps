using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LabelCastDesktop
{

    /// <summary>
    /// UI Editor class to edit IP Address values in the property grid for Printer objects.
    /// The built-in editors cannot handle it. This class is the overall class which can be
    /// assigned in an attribute to the IPAddress property. It invokes and editing dialog in turn.
    /// </summary>
    public class IPAddressEditor : UITypeEditor
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
            if (value is IPAddress ipAddress)
            {
                // Show a dialog to edit the IP address
                using (IPAddressDialog dialog = new IPAddressDialog(ipAddress.ToString()))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        // Parse the new IPAddress from the dialog input
                        if (IPAddress.TryParse(dialog.IPAddress, out IPAddress? newIP))
                        {
                            return newIP;  // Return the updated IPAddress
                        }
                        else
                        {
                            MessageBox.Show("Invalid IP Address.");
                        }
                    }
                }
            }

            return value;  // If canceled or invalid input, return the original value
        }
    }



    public class IPAddressDialog : Form
    {
        private TextBox ipAddressTextBox;
        private Button okButton;
        private Button cancelButton;

        public string IPAddress { get; private set; } = "0.0.0.0";

        public IPAddressDialog(string currentIPAddress)
        {
            this.Text = "Edit IP Address";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new System.Drawing.Size(300, 120);

            ipAddressTextBox = new TextBox
            {
                Text = currentIPAddress,
                Location = new System.Drawing.Point(15, 15),
                Width = 250
            };

            okButton = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(70, 50),
                DialogResult = DialogResult.OK
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(150, 50),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(ipAddressTextBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

        }

        // When OK is clicked, store the IP address
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (this.DialogResult == DialogResult.OK)
            {
                IPAddress = ipAddressTextBox.Text;
            }
        }
    }

}
