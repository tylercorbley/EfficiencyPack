using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmRmPlan : Form
    {
        public FrmRmPlan(List<String> viewTypes)
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

        //public bool PlanOrRCP()
        //{
        //    return checkBox1.Checked;
        //}
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnOK_Click_1(object sender, EventArgs e)
        {

        }

        private void FrmRmPlan_Load(object sender, EventArgs e)
        {

        }
    }
}
