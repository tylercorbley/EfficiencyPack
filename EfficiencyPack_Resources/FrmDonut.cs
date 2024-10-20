using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmDonut : Form
    {
        public FrmDonut(List<String> lineStyles, List<String> filledStyles)
        {
            InitializeComponent();
            foreach (String type in lineStyles)
            {
                this.cmbType.Items.Add(type);
            }
            foreach (String type in filledStyles)
            {
                this.cmbType2.Items.Add(type);
            }
            //"*DL-2"
            //"DG - SOLID (white)"
            int index1 = lineStyles.IndexOf("*DL-2");
            int index2 = filledStyles.IndexOf("DG - SOLID (white)");
            this.cmbType.SelectedIndex = index1;
            this.cmbType2.SelectedIndex = index2;
        }
        public double getOffset()
        {
            double returnValue;
            if (double.TryParse(tbxNumber.Text, out returnValue) == true)
            {
                if (returnValue != 0)
                {
                    return (returnValue);
                }
            }
            return .5;
        }
        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
        private void btnOK_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
        public string GetSelectedLineStyle()
        {
            return cmbType.SelectedItem.ToString();
        }
        public string GetSelectedFillRegion()
        {
            return cmbType2.SelectedItem.ToString();
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

        private void FrmDonut_Load(object sender, EventArgs e)
        {

        }
    }
}
