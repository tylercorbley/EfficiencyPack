using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmFlrRm : Form
    {
        public FrmFlrRm(List<String> floorTypes)
        {
            InitializeComponent();
            foreach (String type in floorTypes)
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
        public string GetSelectedFloorType()
        {
            return cmbType.SelectedItem.ToString();
        }
        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
