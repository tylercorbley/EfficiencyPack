using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmExplodeCAD : Form
    {
        public FrmExplodeCAD(List<String> lineStyles, List<string> subcategoryNames)
        {
            InitializeComponent();
            int X = subcategoryNames.Count;
            for (int i = 0; i < X; i++)
            {
                int widthOffset = 190;
                int heightBoth = 20;
                //labelsVVV
                Label lblLineStyle = new Label();
                lblLineStyle.Text = subcategoryNames[i];
                lblLineStyle.Name = "label" + i; // Unique name for each label
                lblLineStyle.Location = new Point(00, i * heightBoth); // Adjust Y position based on index
                lblLineStyle.Height = heightBoth;
                lblLineStyle.Width = widthOffset;
                lblLineStyle.Paint += Label_Paint; // Attach Paint event handler
                panel1.Controls.Add(lblLineStyle); // Add Label to the Panel
                //combo boxesVVVV
                ComboBox cmbLineStyle = new ComboBox();
                cmbLineStyle.Name = subcategoryNames[i]; // Unique name for each ComboBox
                cmbLineStyle.Location = new Point(widthOffset, i * heightBoth); // Adjust Y position based on index
                cmbLineStyle.Width = widthOffset;
                cmbLineStyle.Height = heightBoth;
                cmbLineStyle.Items.Add("Skip");
                foreach (String lineStyle in lineStyles)
                {
                    cmbLineStyle.Items.Add(lineStyle);
                }
                cmbLineStyle.SelectedIndex = 0;
                panel1.Controls.Add(cmbLineStyle); // Add ComboBox to the Panel
            }
        }

        private void Label_Paint(object sender, PaintEventArgs e)
        {
            Label label = sender as Label;
            if (label != null)
            {
                int borderWidth = 1; // Adjust the border thickness here
                using (Pen borderPen = new Pen(Color.Black, borderWidth))
                {
                    e.Graphics.DrawLine(borderPen, 0, label.Height - 1, label.Width, label.Height - 1); // Bottom line
                }
            }

        }
        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
        public Dictionary<string, string> GetSelectedItemsFromComboBoxes()
        {
            Dictionary<string, string> selectedItems = new Dictionary<string, string>();

            foreach (Control control in panel1.Controls)
            {
                if (control is ComboBox comboBox)
                {
                    string selectedItem = comboBox.SelectedItem as string;
                    string layerName = comboBox.Name;
                    if (!string.IsNullOrEmpty(selectedItem))
                    {
                        selectedItems[layerName] = selectedItem;
                    }
                }
            }

            return selectedItems;
        }


        //public List<string> GetSelectedItemsFromComboBoxes()
        //{
        //    List<string> selectedItems = new List<string>();

        //    foreach (Control control in panel1.Controls)
        //    {
        //        if (control is ComboBox comboBox)
        //        {
        //            string selectedItem = comboBox.SelectedItem as string;
        //            if (!string.IsNullOrEmpty(selectedItem))
        //            {
        //                selectedItems.Add(selectedItem);
        //            }
        //        }
        //    }

        //    return selectedItems;
        //}
        //public string GetSelectedLineStyles()
        //{
        //    return cmbLineStyle.SelectedItem.ToString();
        //}

        //public void UpdateListView(List<string> subcategoryNames)
        //{
        //    // Clear existing items in the ListView
        //    listView1.Items.Clear();
        //    //listView1.Columns.Clear();

        //    // Add subcategory names to the ListView
        //    foreach (string name in subcategoryNames)
        //    {
        //        ListViewItem item = new ListViewItem(name);
        //        listView1.Items.Add(item);
        //    }

        //    // Set ListView to details view mode
        //    //listView1.View = View.Details;

        //    // Set column width to fit content
        //    listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        //}


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnOK_Click_1(object sender, EventArgs e)
        {

        }

        private void FrmExplodeCAD_Load(object sender, EventArgs e)
        {

        }
    }
}
