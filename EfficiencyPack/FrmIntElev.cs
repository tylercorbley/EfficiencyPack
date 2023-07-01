using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmIntElev : Form
    {
        public FrmIntElev(List<String> viewTypes)
        {
            InitializeComponent();
            foreach (String type in viewTypes)
            {
                this.cmbType.Items.Add(type);
            }
            this.cmbType.SelectedIndex = 0;
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
        public string GetSelectedViewType()
        {
            return cmbType.SelectedItem.ToString();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
