namespace LabelCastDesktop
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            MsgLabel = new Label();
            tabControl1 = new TabControl();
            tabPrinters = new TabPage();
            imgPrinters = new PictureBox();
            propertyGridPrinters = new PropertyGrid();
            btnAddPrinter = new Button();
            btnPrinterSave = new Button();
            comboPrinters = new ComboBox();
            btnDeletePrinter = new Button();
            tabProfiles = new TabPage();
            imgProfiles = new PictureBox();
            propertyGridProfiles = new PropertyGrid();
            btnProfileAdd = new Button();
            btnProfileSave = new Button();
            btnDeleteProfile = new Button();
            comboProfiles = new ComboBox();
            tabLabel = new TabPage();
            txtLabelValues = new TextBox();
            btnClear = new Button();
            label2 = new Label();
            label1 = new Label();
            comboActivePrinter = new ComboBox();
            imgLabelPrint = new PictureBox();
            dataGridPrint = new DataGridView();
            PrintMsgLabel = new Label();
            comboActiveProfile = new ComboBox();
            panelLabel = new Panel();
            pictLabel = new PictureBox();
            panelProfile = new Panel();
            pictProfile = new PictureBox();
            panelPrinter = new Panel();
            pictPrinter = new PictureBox();
            panelBtm = new Panel();
            tabControl1.SuspendLayout();
            tabPrinters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)imgPrinters).BeginInit();
            tabProfiles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)imgProfiles).BeginInit();
            tabLabel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)imgLabelPrint).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridPrint).BeginInit();
            panelLabel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictLabel).BeginInit();
            panelProfile.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictProfile).BeginInit();
            panelPrinter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictPrinter).BeginInit();
            SuspendLayout();
            // 
            // MsgLabel
            // 
            MsgLabel.AutoSize = true;
            MsgLabel.Location = new Point(12, 605);
            MsgLabel.Name = "MsgLabel";
            MsgLabel.Size = new Size(0, 15);
            MsgLabel.TabIndex = 1;
            // 
            // tabControl1
            // 
            tabControl1.Alignment = TabAlignment.Bottom;
            tabControl1.Controls.Add(tabPrinters);
            tabControl1.Controls.Add(tabProfiles);
            tabControl1.Controls.Add(tabLabel);
            tabControl1.Dock = DockStyle.Top;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(511, 589);
            tabControl1.TabIndex = 2;
            // 
            // tabPrinters
            // 
            tabPrinters.Controls.Add(imgPrinters);
            tabPrinters.Controls.Add(propertyGridPrinters);
            tabPrinters.Controls.Add(btnAddPrinter);
            tabPrinters.Controls.Add(btnPrinterSave);
            tabPrinters.Controls.Add(comboPrinters);
            tabPrinters.Controls.Add(btnDeletePrinter);
            tabPrinters.Location = new Point(4, 4);
            tabPrinters.Name = "tabPrinters";
            tabPrinters.Padding = new Padding(3);
            tabPrinters.Size = new Size(503, 561);
            tabPrinters.TabIndex = 0;
            tabPrinters.Text = "Printers";
            tabPrinters.UseVisualStyleBackColor = true;
            // 
            // imgPrinters
            // 
            imgPrinters.Image = (Image)resources.GetObject("imgPrinters.Image");
            imgPrinters.Location = new Point(0, 10);
            imgPrinters.Name = "imgPrinters";
            imgPrinters.Size = new Size(40, 32);
            imgPrinters.SizeMode = PictureBoxSizeMode.Zoom;
            imgPrinters.TabIndex = 8;
            imgPrinters.TabStop = false;
            // 
            // propertyGridPrinters
            // 
            propertyGridPrinters.Location = new Point(6, 49);
            propertyGridPrinters.Name = "propertyGridPrinters";
            propertyGridPrinters.Size = new Size(485, 462);
            propertyGridPrinters.TabIndex = 6;
            propertyGridPrinters.PropertyValueChanged += propertyGridPrinters_PropertyValueChanged;
            // 
            // btnAddPrinter
            // 
            btnAddPrinter.Location = new Point(333, 13);
            btnAddPrinter.Name = "btnAddPrinter";
            btnAddPrinter.Size = new Size(75, 23);
            btnAddPrinter.TabIndex = 5;
            btnAddPrinter.Text = "Add";
            btnAddPrinter.UseVisualStyleBackColor = true;
            btnAddPrinter.Click += btnAddPrinter_Click;
            // 
            // btnPrinterSave
            // 
            btnPrinterSave.Location = new Point(416, 532);
            btnPrinterSave.Name = "btnPrinterSave";
            btnPrinterSave.Size = new Size(75, 23);
            btnPrinterSave.TabIndex = 3;
            btnPrinterSave.Text = "Save";
            btnPrinterSave.UseVisualStyleBackColor = true;
            btnPrinterSave.Click += btnPrinterSave_Click;
            // 
            // comboPrinters
            // 
            comboPrinters.DropDownStyle = ComboBoxStyle.DropDownList;
            comboPrinters.FormattingEnabled = true;
            comboPrinters.Location = new Point(77, 14);
            comboPrinters.Name = "comboPrinters";
            comboPrinters.Size = new Size(244, 23);
            comboPrinters.TabIndex = 1;
            comboPrinters.SelectedIndexChanged += comboPrinters_SelectedIndexChanged;
            // 
            // btnDeletePrinter
            // 
            btnDeletePrinter.Location = new Point(414, 13);
            btnDeletePrinter.Name = "btnDeletePrinter";
            btnDeletePrinter.Size = new Size(75, 23);
            btnDeletePrinter.TabIndex = 0;
            btnDeletePrinter.Text = "Delete";
            btnDeletePrinter.UseVisualStyleBackColor = true;
            btnDeletePrinter.Click += btnDeletePrinter_Click;
            // 
            // tabProfiles
            // 
            tabProfiles.Controls.Add(imgProfiles);
            tabProfiles.Controls.Add(propertyGridProfiles);
            tabProfiles.Controls.Add(btnProfileAdd);
            tabProfiles.Controls.Add(btnProfileSave);
            tabProfiles.Controls.Add(btnDeleteProfile);
            tabProfiles.Controls.Add(comboProfiles);
            tabProfiles.Location = new Point(4, 4);
            tabProfiles.Name = "tabProfiles";
            tabProfiles.Padding = new Padding(3);
            tabProfiles.Size = new Size(503, 561);
            tabProfiles.TabIndex = 1;
            tabProfiles.Text = "Profiles";
            tabProfiles.UseVisualStyleBackColor = true;
            // 
            // imgProfiles
            // 
            imgProfiles.Image = (Image)resources.GetObject("imgProfiles.Image");
            imgProfiles.Location = new Point(0, 10);
            imgProfiles.Name = "imgProfiles";
            imgProfiles.Size = new Size(40, 32);
            imgProfiles.SizeMode = PictureBoxSizeMode.Zoom;
            imgProfiles.TabIndex = 9;
            imgProfiles.TabStop = false;
            // 
            // propertyGridProfiles
            // 
            propertyGridProfiles.Location = new Point(6, 49);
            propertyGridProfiles.Name = "propertyGridProfiles";
            propertyGridProfiles.Size = new Size(485, 464);
            propertyGridProfiles.TabIndex = 6;
            propertyGridProfiles.PropertyValueChanged += propertyGridProfiles_PropertyValueChanged;
            // 
            // btnProfileAdd
            // 
            btnProfileAdd.Location = new Point(333, 13);
            btnProfileAdd.Name = "btnProfileAdd";
            btnProfileAdd.Size = new Size(75, 23);
            btnProfileAdd.TabIndex = 5;
            btnProfileAdd.Text = "Add";
            btnProfileAdd.UseVisualStyleBackColor = true;
            btnProfileAdd.Click += btnProfileAdd_Click;
            // 
            // btnProfileSave
            // 
            btnProfileSave.Location = new Point(414, 532);
            btnProfileSave.Name = "btnProfileSave";
            btnProfileSave.Size = new Size(75, 23);
            btnProfileSave.TabIndex = 3;
            btnProfileSave.Text = "Save";
            btnProfileSave.UseVisualStyleBackColor = true;
            btnProfileSave.Click += btnProfileSave_Click;
            // 
            // btnDeleteProfile
            // 
            btnDeleteProfile.Location = new Point(414, 13);
            btnDeleteProfile.Name = "btnDeleteProfile";
            btnDeleteProfile.Size = new Size(75, 23);
            btnDeleteProfile.TabIndex = 2;
            btnDeleteProfile.Text = "Delete";
            btnDeleteProfile.UseVisualStyleBackColor = true;
            btnDeleteProfile.Click += btnDeleteProfile_Click;
            // 
            // comboProfiles
            // 
            comboProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
            comboProfiles.FormattingEnabled = true;
            comboProfiles.Location = new Point(77, 12);
            comboProfiles.Name = "comboProfiles";
            comboProfiles.Size = new Size(244, 23);
            comboProfiles.TabIndex = 0;
            comboProfiles.SelectedIndexChanged += comboProfiles_SelectedIndexChanged;
            // 
            // tabLabel
            // 
            tabLabel.Controls.Add(txtLabelValues);
            tabLabel.Controls.Add(btnClear);
            tabLabel.Controls.Add(label2);
            tabLabel.Controls.Add(label1);
            tabLabel.Controls.Add(comboActivePrinter);
            tabLabel.Controls.Add(imgLabelPrint);
            tabLabel.Controls.Add(dataGridPrint);
            tabLabel.Controls.Add(PrintMsgLabel);
            tabLabel.Controls.Add(comboActiveProfile);
            tabLabel.Location = new Point(4, 4);
            tabLabel.Name = "tabLabel";
            tabLabel.Padding = new Padding(3);
            tabLabel.Size = new Size(503, 561);
            tabLabel.TabIndex = 2;
            tabLabel.Text = "Label-Print";
            tabLabel.UseVisualStyleBackColor = true;
            // 
            // txtLabelValues
            // 
            txtLabelValues.Font = new Font("Lucida Console", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtLabelValues.Location = new Point(8, 369);
            txtLabelValues.Multiline = true;
            txtLabelValues.Name = "txtLabelValues";
            txtLabelValues.Size = new Size(483, 157);
            txtLabelValues.TabIndex = 9;
            // 
            // btnClear
            // 
            btnClear.Location = new Point(416, 532);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(75, 23);
            btnClear.TabIndex = 8;
            btnClear.Text = "Clear";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(336, 54);
            label2.Name = "label2";
            label2.Size = new Size(42, 15);
            label2.TabIndex = 7;
            label2.Text = "Printer";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(336, 17);
            label1.Name = "label1";
            label1.Size = new Size(41, 15);
            label1.TabIndex = 6;
            label1.Text = "Profile";
            // 
            // comboActivePrinter
            // 
            comboActivePrinter.FormattingEnabled = true;
            comboActivePrinter.Location = new Point(391, 51);
            comboActivePrinter.Name = "comboActivePrinter";
            comboActivePrinter.Size = new Size(100, 23);
            comboActivePrinter.TabIndex = 5;
            comboActivePrinter.SelectedIndexChanged += comboActivePrinter_SelectedIndexChanged;
            // 
            // imgLabelPrint
            // 
            imgLabelPrint.Image = (Image)resources.GetObject("imgLabelPrint.Image");
            imgLabelPrint.Location = new Point(4, 5);
            imgLabelPrint.Name = "imgLabelPrint";
            imgLabelPrint.Size = new Size(55, 40);
            imgLabelPrint.SizeMode = PictureBoxSizeMode.Zoom;
            imgLabelPrint.TabIndex = 4;
            imgLabelPrint.TabStop = false;
            // 
            // dataGridPrint
            // 
            dataGridPrint.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridPrint.Location = new Point(6, 86);
            dataGridPrint.Name = "dataGridPrint";
            dataGridPrint.Size = new Size(485, 277);
            dataGridPrint.TabIndex = 3;
            dataGridPrint.CellEndEdit += dataGridPrint_CellEndEdit;
            dataGridPrint.CellEnter += dataGridPrint_CellEnter;
            dataGridPrint.CellLeave += dataGridPrint_CellLeave;
            dataGridPrint.SelectionChanged += dataGridPrint_SelectionChanged;
            dataGridPrint.KeyUp += dataGridPrint_KeyUp;
            // 
            // PrintMsgLabel
            // 
            PrintMsgLabel.AutoSize = true;
            PrintMsgLabel.Location = new Point(8, 9);
            PrintMsgLabel.Name = "PrintMsgLabel";
            PrintMsgLabel.Size = new Size(0, 15);
            PrintMsgLabel.TabIndex = 2;
            // 
            // comboActiveProfile
            // 
            comboActiveProfile.FormattingEnabled = true;
            comboActiveProfile.Location = new Point(391, 14);
            comboActiveProfile.Name = "comboActiveProfile";
            comboActiveProfile.Size = new Size(100, 23);
            comboActiveProfile.TabIndex = 1;
            comboActiveProfile.SelectedIndexChanged += comboActiveProfile_SelectedIndexChanged;
            // 
            // panelLabel
            // 
            panelLabel.BackColor = Color.FromArgb(25, 25, 25);
            panelLabel.Controls.Add(pictLabel);
            panelLabel.Location = new Point(340, 630);
            panelLabel.Name = "panelLabel";
            panelLabel.Size = new Size(171, 52);
            panelLabel.TabIndex = 3;
            panelLabel.Click += panelLabel_Click;
            panelLabel.MouseEnter += panelLabel_MouseEnter;
            panelLabel.MouseLeave += panelLabel_MouseLeave;
            // 
            // pictLabel
            // 
            pictLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictLabel.BackgroundImage = (Image)resources.GetObject("pictLabel.BackgroundImage");
            pictLabel.BackgroundImageLayout = ImageLayout.Zoom;
            pictLabel.Location = new Point(65, 10);
            pictLabel.Name = "pictLabel";
            pictLabel.Size = new Size(41, 27);
            pictLabel.TabIndex = 0;
            pictLabel.TabStop = false;
            pictLabel.Click += pictLabel_Click;
            // 
            // panelProfile
            // 
            panelProfile.BackColor = Color.FromArgb(25, 25, 25);
            panelProfile.Controls.Add(pictProfile);
            panelProfile.Location = new Point(170, 630);
            panelProfile.Name = "panelProfile";
            panelProfile.Size = new Size(170, 52);
            panelProfile.TabIndex = 4;
            panelProfile.Click += panelProfile_Click;
            panelProfile.MouseEnter += panelProfile_MouseEnter;
            panelProfile.MouseLeave += panelProfile_MouseLeave;
            // 
            // pictProfile
            // 
            pictProfile.BackgroundImage = (Image)resources.GetObject("pictProfile.BackgroundImage");
            pictProfile.BackgroundImageLayout = ImageLayout.Zoom;
            pictProfile.Location = new Point(65, 10);
            pictProfile.Name = "pictProfile";
            pictProfile.Size = new Size(40, 27);
            pictProfile.TabIndex = 1;
            pictProfile.TabStop = false;
            pictProfile.Click += pictProfile_Click;
            // 
            // panelPrinter
            // 
            panelPrinter.BackColor = Color.FromArgb(25, 25, 25);
            panelPrinter.Controls.Add(pictPrinter);
            panelPrinter.Location = new Point(0, 630);
            panelPrinter.Name = "panelPrinter";
            panelPrinter.Size = new Size(170, 52);
            panelPrinter.TabIndex = 5;
            panelPrinter.Click += panelPrinter_Click;
            panelPrinter.MouseEnter += panelPrinter_MouseEnter;
            panelPrinter.MouseLeave += panelPrinter_MouseLeave;
            // 
            // pictPrinter
            // 
            pictPrinter.BackgroundImage = (Image)resources.GetObject("pictPrinter.BackgroundImage");
            pictPrinter.BackgroundImageLayout = ImageLayout.Zoom;
            pictPrinter.Location = new Point(65, 10);
            pictPrinter.Name = "pictPrinter";
            pictPrinter.Size = new Size(40, 27);
            pictPrinter.TabIndex = 2;
            pictPrinter.TabStop = false;
            pictPrinter.Click += pictPrinter_Click;
            // 
            // panelBtm
            // 
            panelBtm.Location = new Point(0, 593);
            panelBtm.Name = "panelBtm";
            panelBtm.Size = new Size(510, 28);
            panelBtm.TabIndex = 6;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(511, 682);
            Controls.Add(panelBtm);
            Controls.Add(panelProfile);
            Controls.Add(panelPrinter);
            Controls.Add(panelLabel);
            Controls.Add(tabControl1);
            Controls.Add(MsgLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "LabelCast";
            tabControl1.ResumeLayout(false);
            tabPrinters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)imgPrinters).EndInit();
            tabProfiles.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)imgProfiles).EndInit();
            tabLabel.ResumeLayout(false);
            tabLabel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)imgLabelPrint).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridPrint).EndInit();
            panelLabel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictLabel).EndInit();
            panelProfile.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictProfile).EndInit();
            panelPrinter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictPrinter).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label MsgLabel;
        private TabControl tabControl1;
        private TabPage tabPrinters;
        private TabPage tabProfiles;
        private TabPage tabLabel;
        private ComboBox comboProfiles;
        private Button btnDeleteProfile;
        private ComboBox comboPrinters;
        private Button btnDeletePrinter;
        private Button btnProfileSave;
        private Button btnPrinterSave;
        private Button btnAddPrinter;
        private Button btnProfileAdd;
        private PropertyGrid propertyGridProfiles;
        private PropertyGrid propertyGridPrinters;
        private ComboBox comboActiveProfile;
        private Label PrintMsgLabel;
        private DataGridView dataGridPrint;
        private PictureBox imgLabelPrint;
        private PictureBox imgPrinters;
        private PictureBox imgProfiles;
        private Panel panelLabel;
        private Panel panelProfile;
        private Panel panelPrinter;
        private PictureBox pictLabel;
        private PictureBox pictProfile;
        private PictureBox pictPrinter;
        private Panel panelBtm;
        private Label label2;
        private Label label1;
        private ComboBox comboActivePrinter;
        private Button btnClear;
        private TextBox txtLabelValues;
    }
}
