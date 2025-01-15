using Newtonsoft.Json;
using System;
using System.Data;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Reflection;
using System.Timers;
using System.Windows.Forms;
using System.Xml.Linq;
using LabelCast;
using System.Diagnostics.Eventing.Reader;


namespace LabelCastDesktop
{
    public partial class Form1 : Form
    {

        #region Fields 

        private App mApp;

        // The form uses ProfileDTO and PrinterDTO rather than Profile/Printer
        // as these are the editable versions of the classes
        private List<ProfileDTO> mProfileList;
        private List<PrinterDTO> mACtivePrinterList;

        private bool mIsInitializing = false;
        int mCurrentRow = 0;
        private System.Timers.Timer EndEditTimer;

        #endregion

        #region Constructor

        public Form1()
        {
            InitializeComponent();

            mApp = new App();
            mApp.MessageEvent += OnAppMessage;
            mApp.ProfileEvent += OnProfileUpdate;
            mApp.LabelEvent += OnLabelUpdate;
            mApp.LabelPrintCompleteEvent += LabelPrintCompleteEvent;
            //
            mProfileList = new List<ProfileDTO>();
            mACtivePrinterList = new List<PrinterDTO>();
            //
            tabControl1.SelectedTab = tabLabel;

            // DataGridPrint properties
            // Improves rendering a bit to assign the common props here
            // instead of ConfigureLabelPrinting
            dataGridPrint.ColumnHeadersVisible = false;
            dataGridPrint.RowHeadersVisible = false;
            dataGridPrint.AllowUserToAddRows = false;
            dataGridPrint.BorderStyle = BorderStyle.None;
            dataGridPrint.BackgroundColor = Color.White;
            dataGridPrint.DefaultCellStyle.SelectionBackColor = Color.LightGoldenrodYellow;
            dataGridPrint.DefaultCellStyle.SelectionForeColor = Color.Black;

            EndEditTimer = new System.Timers.Timer(100);
            EndEditTimer.AutoReset = false;
            EndEditTimer.Enabled = false;
            EndEditTimer.Elapsed += EndEditTimer_Elapsed;

            panelBtm.Location = new Point(0, 566);

            this.FormBorderStyle = FormBorderStyle.Fixed3D;
        }


        #endregion

        #region App Events

        /// <summary>
        /// Display message which is sent by an event raised in the App class.
        /// If the event is raised on another than the UI thread, it handles the
        /// necessary Invoke call.
        /// </summary>
        private void OnAppMessage(object sender, MessageEventArgs e)
        {
            Logger.Write(Level.Debug, "Message: " + e.Message);

            if (MsgLabel.InvokeRequired)
                MsgLabel.Invoke(new Action(() => MsgLabel.Text = e.Message));
            else
                MsgLabel.Text = e.Message;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnProfileUpdate(object sender, ProfileEventArgs e)
        {
            Logger.Write(Level.Debug, "OnUpdateProfiles event fired (" + e.Profiles.Count + " profiles to be updated).");

            if (MsgLabel.InvokeRequired)
                MsgLabel.Invoke(new Action(() => { UpdateProfiles(e); }));
            else
                UpdateProfiles(e);
        }


        /// <summary>
        /// Update barcode label entry screen when the active profile changes.
        /// This means updating the DatGridView with the variable names for that new profile.
        /// </summary>
        private void OnLabelUpdate(object sender, EventArgs e)
        {
            if (dataGridPrint.InvokeRequired)
                dataGridPrint.Invoke(new Action(() => ConfigureLabelPrinting()));
            else
                ConfigureLabelPrinting();
        }


        /// <summary>
        /// This event fires when the list of profiles changes, for example when
        /// loaded from disk.
        /// </summary>
        private void UpdateProfiles(ProfileEventArgs e)
        {
            // Profile list from the App event is a deep copy
            mProfileList = e.Profiles.Select(p => new ProfileDTO(p)).ToList();            
            RefreshProfileCombo();
            ConfigureLabelPrinting();

            // This is a reference to PrinterStore.Printers (global object)
            mACtivePrinterList = e.Printers.Select(p => new PrinterDTO(p)).ToList();            
            RefreshPrinterCombo();

            // Update combos on the LabelPrint tab
            RefreshActiveProfileAndPrinterCombos();
        }


        /// <summary>
        /// Update 'comboProfiles' combo box on the "Profiles" tab of the form.
        /// </summary>
        private void RefreshProfileCombo()
        {
            comboProfiles.Items.Clear();
            foreach (ProfileDTO p in mProfileList)
            {
                comboProfiles.Items.Add(p.Name);
            }
            if (comboProfiles.Items.Count > 0)
            {
                // Select the profile to active profile
                String activeProfileName = mApp.GetActiveProfileName();
                if (String.IsNullOrWhiteSpace(activeProfileName))
                    comboProfiles.SelectedIndex = 0;
                else
                    comboProfiles.SelectedItem = activeProfileName;
            }
        }


        /// <summary>
        /// Update 'comboPrinters' combo box on the "Printers" tab of the form.
        /// </summary>
        private void RefreshPrinterCombo()
        {
            comboPrinters.Items.Clear();
            foreach (PrinterDTO prn in mACtivePrinterList)
            {
                comboPrinters.Items.Add(prn.Name);
            }
            if (comboPrinters.Items.Count > 0)
            {
                String activePrinterName = mApp.GetActivePrinterName();
                if (String.IsNullOrWhiteSpace(activePrinterName))
                    comboPrinters.SelectedIndex = 0;
                else
                    comboPrinters.SelectedItem = activePrinterName;
            }                
        }


        /// <summary>
        /// Refresh the ActiveProfile and ActivePrinter combo-boxes on the Label-Print tab 
        /// which shows the currently active profile (according to Client configuration).
        /// </summary>
        private void RefreshActiveProfileAndPrinterCombos()
        {
            // This flag prevents SelectedIndexChanged events
            // from firing during Form startup:
            mIsInitializing = true;

            // ActiveProfile combo

            String abbrev = mApp.GetActiveProfileAbbr();

            comboActiveProfile.Items.Clear();
            int n = 0, profileIdx = 0;
            foreach (ProfileDTO p in mProfileList)
            {
                comboActiveProfile.Items.Add(p.Abbreviation);
                if (p.Abbreviation.ToLower().Trim() == abbrev.ToLower().Trim())
                {
                    profileIdx = n;
                }
                n++;
            }
            if (comboActiveProfile.Items.Count > 0)
            {
                comboActiveProfile.SelectedIndex = profileIdx;
            }

            // ActivePrinter combo

            String name = mApp.GetActivePrinterName();

            comboActivePrinter.Items.Clear();
            int i = 0, printerIdx = 0;
            foreach (PrinterDTO p in mACtivePrinterList)
            {
                comboActivePrinter.Items.Add(p.Name);
                if (p.Name.ToLower().Trim() == name.ToLower().Trim())
                {
                    printerIdx = i;
                }
                i++;
            }
            if (comboActivePrinter.Items.Count > 0)
            {
                comboActivePrinter.SelectedIndex = printerIdx;
            }

            mIsInitializing = false;
        }

        private void LabelPrintCompleteEvent(object sender, EventArgs e)
        {
            ClearInputValues();
        }

        #endregion

        #region Form Control Event Handlers - Profile Tab

        private void comboProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboProfiles.SelectedItem != null)
            {
                var p = mProfileList.FirstOrDefault(p => p.Name == comboProfiles.SelectedItem.ToString());
                if (p != null)
                {
                    propertyGridProfiles.SelectedObject = p;
                }
            }
        }

        private void btnProfileAdd_Click(object sender, EventArgs e)
        {
            String name = "New Profile";
            ProfileDTO profile = new ProfileDTO { Name = name };
            int n = 1;
            do
            {
                var existing = mProfileList.FirstOrDefault(p => p.Name == profile.Name);
                if (existing == null)
                    break;
                else
                {
                    n++;
                    profile.Name = name + " (" + n.ToString() + ")";
                }
            } while (true);

            mProfileList.Add(profile);
            comboProfiles.Items.Add(profile.Name);
            comboProfiles.SelectedItem = profile.Name;
        }


        private void btnDeleteProfile_Click(object sender, EventArgs e)
        {
            if (comboProfiles.SelectedItem == null)
                return;

            String pName = comboProfiles.SelectedItem?.ToString() ?? "";
            if (!String.IsNullOrWhiteSpace(pName))
            {
                ProfileDTO? profile = mProfileList.FirstOrDefault(p => p.Name.Trim().ToLower() == pName.Trim().ToLower());
                if (profile != null)
                {
                    mProfileList.Remove(profile);
                    MsgLabel.Text = "Profile deleted.";
                }
            }
            RefreshProfileCombo();
        }


        private void btnProfileSave_Click(object sender, EventArgs e)
        {
            List<Profile> profiles = mProfileList.Select(p => p.ToProfile()).ToList();
            List<Printer> printers = mACtivePrinterList.Select(p => p.ToPrinter()).ToList();

            String msgResult = mApp.SaveConfiguration(profiles, printers);
            MsgLabel.Text = msgResult;
            RefreshPrinterCombo();
            RefreshProfileCombo();
        }


        /// <summary>
        /// Handle the case that a user changes the NAME of the profile - in this case we need
        /// to update the item in the combo box.
        /// </summary>
        private void propertyGridProfiles_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (e.ChangedItem == null || e.ChangedItem.PropertyDescriptor == null)
                return;

            if (e.ChangedItem.PropertyDescriptor.Name == "Name")
            {
                string changedName = e.ChangedItem.Value as string ?? "";
                comboProfiles.Items[comboProfiles.SelectedIndex] = changedName;
            }
        }

        #endregion 

        #region Form Control Event Handlers - Printers Tab

        private void comboPrinters_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboPrinters.SelectedItem != null)
            {
                var prn = mACtivePrinterList.FirstOrDefault(p => p.Name == comboPrinters.SelectedItem.ToString());
                if (prn != null)
                {
                    propertyGridPrinters.SelectedObject = prn;
                }
            }
        }


        /// <summary>
        /// Add printer to mACtivePrinterList.<br/>
        /// Note that mACtivePrinterList is a reference to PrinterStore.Printers global object..
        /// </summary>
        private void btnAddPrinter_Click(object sender, EventArgs e)
        {
            String name = "New Printer";
            PrinterDTO printer = new PrinterDTO { Name = name };
            int n = 1;
            do
            {
                var existing = mACtivePrinterList.FirstOrDefault(prn => prn.Name == printer.Name);
                if (existing == null)
                    break;
                else
                {
                    n++;
                    printer.Name = name + " (" + n.ToString() + ")";
                }
            } while (true);

            mACtivePrinterList.Add(printer);
            comboPrinters.Items.Add(printer.Name);
            comboPrinters.SelectedItem = printer.Name;
        }


        /// <summary>
        /// Remove printer from mACtivePrinterList.<br/>
        /// Note that mACtivePrinterList is a reference to PrinterStore.Printers global object..
        /// </summary>
        private void btnDeletePrinter_Click(object sender, EventArgs e)
        {
            if (comboPrinters.SelectedItem == null)
                return;

            String pName = comboPrinters.SelectedItem?.ToString() ?? "";
            if (!String.IsNullOrWhiteSpace(pName))
            {
                PrinterDTO? printer = mACtivePrinterList.FirstOrDefault(p => p.Name.Trim().ToLower() == pName.Trim().ToLower());
                if (printer != null)
                {
                    mACtivePrinterList.Remove(printer);
                    MsgLabel.Text = "Printer deleted.";
                }
            }
            RefreshPrinterCombo();
        }


        /// <summary>
        /// Saving printers. This really saves the global object PrinterStore.Printers.
        /// </summary>
        private void btnPrinterSave_Click(object sender, EventArgs e)
        {
            List<Profile> profiles = mProfileList.Select(p => p.ToProfile()).ToList();            
            List<Printer> printers = mACtivePrinterList.Select(p => p.ToPrinter()).ToList();
            
            String msgResult = mApp.SaveConfiguration(profiles, printers);
            MsgLabel.Text = msgResult;

            RefreshPrinterCombo();
            RefreshProfileCombo();
        }


        /// <summary>
        /// Handle the case that a user changes the NAME of the printer - in this case we need
        /// to update the item in the combo box.
        /// </summary>
        private void propertyGridPrinters_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (e.ChangedItem == null || e.ChangedItem.PropertyDescriptor == null)
                return;

            if (e.ChangedItem.PropertyDescriptor.Name == "Name")
            {
                string changedName = e.ChangedItem.Value as string ?? "";
                comboPrinters.Items[comboPrinters.SelectedIndex] = changedName;
            }
        }

        #endregion

        #region Form Control Event Handlers - LabelPrint tab


        private void comboActiveProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Save the configuration only outside Form initialization
            // (This event will fire when the form is started up)
            if (!mIsInitializing)
            {
                String activeProfileAbbr = comboActiveProfile.SelectedItem?.ToString() ?? "";
                mApp.SetActiveProfile(activeProfileAbbr);
                Logger.Write(Level.Debug, "Changed active profile to '" + activeProfileAbbr + "'");
            }
        }


        private void comboActivePrinter_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Save the configuration only outside Form initialization
            // (This event will fire when the form is started up)
            if (!mIsInitializing)
            {
                String activePrinterName = comboActivePrinter.SelectedItem?.ToString() ?? "";
                mApp.SetActivePrinter(activePrinterName);
                Logger.Write(Level.Debug, "Changed active printer to '" + activePrinterName + "'");
            }
        }


        private void dataGridPrint_SelectionChanged(object sender, EventArgs e)
        {
            // Prevent users from selecting cells in column 0 (Variable Names) 
            // (akes the column look like WinForm labels)
            if (dataGridPrint.CurrentCell != null && dataGridPrint.CurrentCell.ColumnIndex == 0)
                dataGridPrint.CurrentCell.Selected = false;
        }

        private void dataGridPrint_KeyUp(object sender, KeyEventArgs e)
        {
            // Prevent users from using the TAB key (to some degree)
            if (e.KeyCode == Keys.Tab)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }


        /// <summary>
        /// DataGridView event which occurs when a cell has been edited and the user
        /// moves on, ending the editing.
        /// </summary>
        private void dataGridPrint_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // We decouple the processing from the event - 
            // because EndEdit also gets called when user moves away from the TabPage,
            // (e.g. to edit profiles) and that must not trigger a database query
            mCurrentRow = e.RowIndex;
            EndEditTimer.Start();
        }


        /// <summary>
        /// Ensuring caret is showing when an edit field is entered
        /// </summary>
        private void dataGridPrint_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > 0)
            {
                dataGridPrint.BeginEdit(true);
            }
        }


        /// <summary>
        /// Process EndEdit event of the DataGridView control (deferred by timer)
        /// </summary>
        private void EndEditTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (dataGridPrint.InvokeRequired)
            {
                dataGridPrint.Invoke(new Action(() =>
                {
                    if (tabControl1.SelectedTab?.Name == "tabLabel")
                    {
                        MsgLabel.Text = "";

                        String varName = dataGridPrint.Rows[mCurrentRow].Cells[0].Value?.ToString() ?? "";
                        String value = dataGridPrint.Rows[mCurrentRow].Cells[1].Value?.ToString() ?? "";

                        if (mApp.BarcodeLabelProcessor != null)
                            mApp.BarcodeLabelProcessor.EditFieldValue(varName, value);
                        else
                            Logger.Write(Level.Error, "Internal Error - LabelProcessor is null in EndEditTimer_Elapsed");
                    }
                    else
                        Logger.Write(Level.Debug, "DataGridPrint EndEdit event fired due to moving away from LabelPrint tab. Not taking any action.");
                }));
            }
        }

        #endregion

        #region Label Data Input Configuration

        /// <summary>
        /// Configures the DataGridView control on the LabelPrint tab
        /// </summary>
        private void ConfigureLabelPrinting()
        {
            Logger.Write(Level.Debug, "ConfigureLabelPrinting");

            Profile? activeProfile = mApp.GetActiveProfile();
            if (activeProfile == null)
            {
                PrintMsgLabel.Text = "No active profile configured.";
                dataGridPrint.ReadOnly = true;
                return;
            }

            dataGridPrint.DataSource = mApp.FieldTable;

            // DatagridView Column Properties
            // (global props are set in Form Init code)

            // Column 0
            dataGridPrint.Columns[0].Width = 130;
            dataGridPrint.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
            dataGridPrint.Columns[0].ReadOnly = true;
            dataGridPrint.Columns[0].Frozen = true;

            var gridFont = dataGridPrint.DefaultCellStyle.Font;
            var boldFont = new Font(gridFont.FontFamily, gridFont.Size, FontStyle.Bold);
            dataGridPrint.Columns[0].DefaultCellStyle.Font = boldFont;

            // Column 1
            dataGridPrint.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridPrint.Columns[1].DefaultCellStyle.BackColor = Color.WhiteSmoke;

            // Different color for the label quantity row
            foreach (DataGridViewRow row in dataGridPrint.Rows)
            {
                if (row.Cells[0].Value.ToString() == "Number of Labels")
                    row.DefaultCellStyle.BackColor = Color.WhiteSmoke;
            }

            FocusTopCell();
        }


        /// <summary>
        /// Set focus on row 0, column 1 and begin editing mode.
        /// </summary>
        private void FocusTopCell()
        {

            Logger.Write(Level.Debug, "FocusTopCell");
            dataGridPrint.Focus();
            dataGridPrint.CurrentCell = dataGridPrint.Rows[0].Cells[1];
            dataGridPrint.Rows[0].Cells[1].Selected = true;
            dataGridPrint.BeginEdit(true);
        }


        /// <summary>
        /// Clears data input by user and puts cursor back to the top of the DataGridView
        /// </summary>
        private void ClearInputValues()
        {
            Thread.Sleep(1000);

            Logger.Write(Level.Debug, "Clearing input values for next label");

            for (int i = 0; i < mApp.FieldTable.Rows.Count; i++)
            {
                mApp.FieldTable.Rows[i][1] = "";
            }

            if (dataGridPrint.InvokeRequired)
            {
                MsgLabel.Invoke(new Action(() => FocusTopCell()));
            }
            else
            {
                FocusTopCell();
            }
        }

        #endregion

        #region Bottom Menu Bar Controls

        private void CheckConfigurationLock()
        {
            if (mApp.IsConfigurationLocked)
            {
                tabPrinters.Enabled = false;
                tabProfiles.Enabled = false;
            }
            else
            {
                tabPrinters.Enabled = true;
                tabProfiles.Enabled = true;
            }
        }

        // ------ Label Tab

        private void panelLabel_MouseEnter(object sender, EventArgs e)
        {
            panelLabel.BackColor = Color.FromArgb(45, 45, 45);
        }

        private void panelLabel_MouseLeave(object sender, EventArgs e)
        {
            panelLabel.BackColor = Color.FromArgb(25, 25, 25);
        }
        private void panelLabel_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabLabel;
        }
        private void pictLabel_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabLabel;
        }


        // ------ Profile Tab

        private void panelProfile_MouseEnter(object sender, EventArgs e)
        {
            panelProfile.BackColor = Color.FromArgb(45, 45, 45);
        }

        private void panelProfile_MouseLeave(object sender, EventArgs e)
        {
            panelProfile.BackColor = Color.FromArgb(25, 25, 25);
        }

        private void panelProfile_Click(object sender, EventArgs e)
        {
            CheckConfigurationLock();
            tabControl1.SelectedTab = tabProfiles;
        }
        private void pictProfile_Click(object sender, EventArgs e)
        {
            CheckConfigurationLock();
            tabControl1.SelectedTab = tabProfiles;
        }


        // ------ Printer Tab

        private void panelPrinter_MouseEnter(object sender, EventArgs e)
        {
            panelPrinter.BackColor = Color.FromArgb(45, 45, 45);
        }

        private void panelPrinter_MouseLeave(object sender, EventArgs e)
        {
            panelPrinter.BackColor = Color.FromArgb(25, 25, 25);
        }
        private void panelPrinter_Click(object sender, EventArgs e)
        {
            CheckConfigurationLock();
            tabControl1.SelectedTab = tabPrinters;
        }
        private void pictPrinter_Click(object sender, EventArgs e)
        {
            CheckConfigurationLock();
            tabControl1.SelectedTab = tabPrinters;
        }

        #endregion

    }
}
