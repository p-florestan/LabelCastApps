using LabelCast;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelCastDesktop
{

    public class SearchDialog : Form
    {
        #region Fields 

        private ListView listView;
        private Button okButton;
        private Button cancelButton;
        private List<Dictionary<String, String>> mValueList = new List<Dictionary<String, String>>();

        #endregion

        #region Form Constructor

        public SearchDialog(List<Dictionary<string, string>> valueList)
        {
            this.mValueList = valueList;
            this.Text = "Search";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new System.Drawing.Size(500, 700);

            listView = new ListView
            {
                Location = new System.Drawing.Point(15, 15),
                Width = 250,
                Height = 500,
                FullRowSelect = true
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

            this.Controls.Add(listView);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            // Set the OK button as the default button
            this.AcceptButton = okButton;

            listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

            BindDictionaryToListView(valueList);
        }

        #endregion

        #region Public Properties 

        public Dictionary<String, String> ResultData { get; private set; } = new Dictionary<String, String>();

        #endregion

        #region Internal Methods 

        private void BindDictionaryToListView(List<Dictionary<string, string>> valueList)
        {
            String json = JsonConvert.SerializeObject(valueList, Formatting.Indented);
            Logger.Write(Level.Debug, "valueList in SearchDialog: \r\n" + json);

            listView.Items.Clear();
            listView.View = View.Details; // Set to Details view (grid-like)

            if (valueList.Count == 0)
                MessageBox.Show("No data found.");
            else
            {
                var headerList = valueList[0].Keys;
                foreach(var header in headerList)
                {
                    listView.Columns.Add(header);
                }

                foreach(var dictionary in valueList)
                {
                    var lineValues = dictionary.Values.ToArray();
                    ListViewItem item = new ListViewItem(lineValues);
                    listView.Items.Add(item);
                }
            }

            listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView.Dock = DockStyle.Fill;
        }

        #endregion

        #region Form Event Handlers 

        // When OK is clicked, store the selected line in property ResultData
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                var selectedList = this.listView.SelectedIndices;
                if (selectedList.Count == 0)
                    MessageBox.Show("Nothing selected");
                else
                {
                    int selectedIndex = selectedList[0];
                    // Set property
                    ResultData = mValueList[selectedIndex];
                    base.OnFormClosing(e);
                }                
            }
            else
            {
                base.OnFormClosing(e);
            }
        }


        private void ListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectList = listView.SelectedIndices;
            if (selectList.Count > 0)
            {
                int selectedIndex = selectList[0];
                ResultData = mValueList[selectedIndex];
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

        }



        #endregion
    }

}
