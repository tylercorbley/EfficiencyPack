using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmForestGen : Form
    {
        public FrmForestGen(List<String> treeTypes)
        {
            InitializeComponent();

            foreach (String type in treeTypes)
            {
                this.cmbType.Items.Add(type);
            }

            this.cmbType.SelectedIndex = 0;
        }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        public string GetSelectedTreeType()
        {
            return cmbType.SelectedItem.ToString();
        }
        public int HowManyTrees()
        {
            int returnValue;
            if (int.TryParse(tbxNumber.Text, out returnValue) == true)
            {
                return returnValue;
            }
            return 1;
        }

        private void label2_Click(object sender, System.EventArgs e)
        {

        }
    }
}
