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
        public double GetFloorHeight()
        {
            double returnValue;
            if (double.TryParse(tbxNumber.Text, out returnValue) == true)
            {
                return (returnValue / 12);
            }
            return .25 / 12;
        }
        public string GetSelectedFloorType()
        {
            return cmbType.SelectedItem.ToString();
        }
        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void tbxNumber_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
