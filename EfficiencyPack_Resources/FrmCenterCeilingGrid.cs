using System;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmCenterCeilingGrid : Form
    {
        private CheckBox checkBox1;
        private CheckBox checkBox2;
        private CheckBox checkBox3;
        private CheckBox checkBox4;
        private Label label1;
        private Label label2;
        private Label label4;
        private Label label5;

        public FrmCenterCeilingGrid()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Create and set up the first checkbox and label
            checkBox1 = new CheckBox();
            checkBox1.Location = new System.Drawing.Point(20, 40);
            checkBox1.Size = new System.Drawing.Size(20, 20);

            label1 = new Label();
            label1.Location = new System.Drawing.Point(50, 43);
            label1.Size = new System.Drawing.Size(200, 20);
            label1.Text = "Vertically center a grid line";

            // Create and set up the second checkbox and label
            checkBox2 = new CheckBox();
            checkBox2.Location = new System.Drawing.Point(20, 65);
            checkBox2.Size = new System.Drawing.Size(20, 30);

            label2 = new Label();
            label2.Location = new System.Drawing.Point(50, 72);
            label2.Size = new System.Drawing.Size(200, 20);
            label2.Text = "Horizontally center a grid line";

            // Create and set up the third checkbox and label
            checkBox3 = new CheckBox();
            checkBox3.Location = new System.Drawing.Point(20, 90);
            checkBox3.Size = new System.Drawing.Size(20, 30);

            label4 = new Label();
            label4.Location = new System.Drawing.Point(50, 97);
            label4.Size = new System.Drawing.Size(280, 20);
            label4.Text = "Do you want to center a tile instead of a grid line?";

            // Create and set up the third checkbox and label
            checkBox4 = new CheckBox();
            checkBox4.Location = new System.Drawing.Point(20, 120);
            checkBox4.Size = new System.Drawing.Size(20, 30);

            label5 = new Label();
            label5.Location = new System.Drawing.Point(50, 127);
            label5.Size = new System.Drawing.Size(280, 20);
            label5.Text = "Do you want to dimension the ceiling tiles?";

            // Add the controls to the form
            this.Controls.Add(checkBox1);
            this.Controls.Add(checkBox2);
            this.Controls.Add(checkBox3);
            this.Controls.Add(checkBox4);
            this.Controls.Add(label1);
            this.Controls.Add(label2);
            this.Controls.Add(label4);
            this.Controls.Add(label5);
        }

        public bool IsVerticalSelected { get; private set; }
        public bool IsHorizontalSelected { get; private set; }
        public bool IsGridSelected { get; private set; }
        public bool DimensionTiles { get; private set; }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
        }

        private void btnOK_Click_1(object sender, EventArgs e)
        {
            IsVerticalSelected = checkBox1.Checked;
            IsHorizontalSelected = checkBox2.Checked;
            IsGridSelected = checkBox3.Checked;
            DimensionTiles = checkBox4.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void FrmCenterCeilingGrid_Load(object sender, EventArgs e)
        {
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}