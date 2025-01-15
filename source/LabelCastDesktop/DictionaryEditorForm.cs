using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace LabelCastDesktop
{

    public class DictionaryEditorForm : Form
    {
        private Button okButton = new Button();
        private Button cancelButton = new Button();
        private DataGridView dataGrid1 = new DataGridView();
        private BindingSource mBindingSource;

        private void InitializeComponent()
        {
            okButton = new Button();
            cancelButton = new Button();
            dataGrid1 = new DataGridView();
            ((ISupportInitialize)dataGrid1).BeginInit();
            SuspendLayout();
            // 
            // okButton
            // 
            okButton.Location = new Point(350, 422);
            okButton.Name = "okButton";
            okButton.Size = new Size(75, 23);
            okButton.TabIndex = 0;
            okButton.Text = "OK";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += this.okButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.Location = new Point(269, 422);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 23);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // dataGrid1
            // 
            dataGrid1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGrid1.Location = new Point(1, 1);
            dataGrid1.Name = "dataGrid1";
            dataGrid1.Size = new Size(436, 390);
            dataGrid1.TabIndex = 2;
            //dataGrid1.ColumnHeadersVisible = false;
            dataGrid1.RowHeadersVisible = false;
            dataGrid1.BorderStyle = BorderStyle.None;
            dataGrid1.BackgroundColor = Color.White;
            dataGrid1.DefaultCellStyle.SelectionBackColor = Color.LightGoldenrodYellow;
            dataGrid1.DefaultCellStyle.SelectionForeColor = Color.Black;

            // 
            // DictionaryEditorForm
            // 
            ClientSize = new Size(437, 457);
            Controls.Add(dataGrid1);
            Controls.Add(cancelButton);
            Controls.Add(okButton);
            Name = "DictionaryEditorForm";
            ((ISupportInitialize)dataGrid1).EndInit();
            ResumeLayout(false);
        }

        public Dictionary<string, string> EditedDictionary { get; private set; }


        public DictionaryEditorForm(Dictionary<string, string> dictionary)
        {
            InitializeComponent();

            EditedDictionary = new Dictionary<string, string>(dictionary);

            okButton.DialogResult = DialogResult.OK;
            cancelButton.DialogResult = DialogResult.Cancel;

            // Convert the dictionary to a list of KeyValueItems
            var list = dictionary.Select(kvp => new KeyValueItem
            {
                Key = kvp.Key,
                Value = kvp.Value
            }).ToList();

            // Initialize the BindingSource and bind the list to it
            mBindingSource = new BindingSource();
            mBindingSource.DataSource = list;

            // Bind the DataGridView to the BindingSource
            dataGrid1.DataSource = mBindingSource;

            // Make sure both Key and Value columns are editable
            dataGrid1.Columns[0].ReadOnly = false; // Column 0 (Key) editable
            dataGrid1.Columns[0].MinimumWidth = 210;
            dataGrid1.Columns[1].ReadOnly = false; // Column 1 (Value) editable
            dataGrid1.Columns[1].MinimumWidth = 210;
        }

        // When the form is closed or when you need to save the changes
        private void SaveChanges()
        {
            // Update the dictionary based on the DataGridView's data
            var updatedList = mBindingSource.List.Cast<KeyValueItem>().ToList();
            EditedDictionary = updatedList.ToDictionary(item => item.Key, item => item.Value);
        }

        // Button click event to save changes
        private void okButton_Click(object? sender, EventArgs e)
        {
            SaveChanges();
            MessageBox.Show("Changes saved.");
        }
    }



    public class KeyValueItem
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
