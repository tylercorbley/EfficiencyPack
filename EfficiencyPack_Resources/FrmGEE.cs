using System;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmGEE : Form
    {
        public string Address { get; private set; }
        public string Resolution { get; private set; }
        public string Radius { get; private set; }

        public FrmGEE()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Initialize the address label
            Label addressLabel = new Label();
            addressLabel.Text = "Enter Address:";
            addressLabel.Location = new System.Drawing.Point(10, 10);
            addressLabel.AutoSize = true;

            // Initialize the address textbox
            TextBox addressTextBox = new TextBox();
            addressTextBox.Location = new System.Drawing.Point(10, 30);
            addressTextBox.Width = 300;

            // Initialize the size label
            Label sizeLabel = new Label();
            sizeLabel.Text = "Enter Radius:";
            sizeLabel.Location = new System.Drawing.Point(10, 60);
            sizeLabel.AutoSize = true;
            Label sizeText = new Label();
            sizeText.Text = "1500' is a good place to start";
            sizeText.Location = new System.Drawing.Point(200, 60);
            sizeText.AutoSize = true;


            // Initialize the size textbox
            TextBox sizeTextBox = new TextBox();
            sizeTextBox.Location = new System.Drawing.Point(100, 60);
            sizeTextBox.Width = 100;
            sizeTextBox.Text = "1500";

            // Initialize the Resolution label
            Label resolutionLabel = new Label();
            resolutionLabel.Text = "Enter Resolution:";
            resolutionLabel.Location = new System.Drawing.Point(10, 80);
            resolutionLabel.AutoSize = true;
            Label resolutionText = new Label();
            resolutionText.Text = "10 is low resolution, 100 is higher resolution.";
            resolutionText.Location = new System.Drawing.Point(200, 80);
            resolutionText.AutoSize = true;

            // Initialize the resolution textbox
            TextBox resolutionTextBox = new TextBox();
            resolutionTextBox.Location = new System.Drawing.Point(100, 80);
            resolutionTextBox.Width = 100;
            resolutionTextBox.Text = "30";

            // Initialize the submit button
            Button submitButton = new Button();
            submitButton.Text = "Submit";
            submitButton.Location = new System.Drawing.Point(10, 110);
            submitButton.Click += (sender, e) =>
            {
                Address = addressTextBox.Text;
                Radius = sizeTextBox.Text;
                Resolution = resolutionTextBox.Text;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            // Initialize the cancel button
            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new System.Drawing.Point(100, 110);
            cancelButton.Click += (sender, e) => this.Close();

            // Add controls to the form
            Controls.Add(addressLabel);
            Controls.Add(addressTextBox);
            Controls.Add(submitButton);
            Controls.Add(cancelButton);
            Controls.Add(sizeLabel);
            Controls.Add(sizeTextBox);
            Controls.Add(resolutionLabel);
            Controls.Add(resolutionTextBox);
            Controls.Add(resolutionText);
            Controls.Add(sizeText);
        }

        private void FrmGEE_Load(object sender, EventArgs e)
        {
        }
    }
}
