using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmDtlLn : Form
    {
        public FrmDtlLn(List<String> lineTypes)
        {
            InitializeComponent();
            foreach (String type in lineTypes)
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
        public string GetSelectedLineStyle()
        {
            return cmbType.SelectedItem.ToString();
        }
        private void FrmDtlLn_Load(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
