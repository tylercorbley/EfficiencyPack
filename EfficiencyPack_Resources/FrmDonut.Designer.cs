namespace EfficiencyPack
{
    partial class FrmDonut
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbxNumber = new System.Windows.Forms.TextBox();
            this.cmbType2 = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(402, 107);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(125, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(15, 107);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(121, 23);
            this.btnOK.TabIndex = 7;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // cmbType
            // 
            this.cmbType.FormattingEnabled = true;
            this.cmbType.Location = new System.Drawing.Point(15, 39);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(384, 21);
            this.cmbType.TabIndex = 5;
            this.cmbType.Text = "Type";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(142, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "What line type do you want?";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(399, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Offset?";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // tbxNumber
            // 
            this.tbxNumber.Location = new System.Drawing.Point(402, 40);
            this.tbxNumber.Name = "tbxNumber";
            this.tbxNumber.Size = new System.Drawing.Size(125, 20);
            this.tbxNumber.TabIndex = 9;
            this.tbxNumber.Text = "1";
            this.tbxNumber.TextChanged += new System.EventHandler(this.tbxNumber_TextChanged);
            // 
            // cmbType2
            // 
            this.cmbType2.FormattingEnabled = true;
            this.cmbType2.Location = new System.Drawing.Point(15, 81);
            this.cmbType2.Name = "cmbType2";
            this.cmbType2.Size = new System.Drawing.Size(384, 21);
            this.cmbType2.TabIndex = 11;
            this.cmbType2.Text = "Type";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 65);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(179, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "What filled region type do you want?";
            // 
            // FrmDonut
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(539, 163);
            this.Controls.Add(this.cmbType2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbxNumber);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cmbType);
            this.Controls.Add(this.label2);
            this.Name = "FrmDonut";
            this.Text = "Add Filled Region Donut Form";
            this.Load += new System.EventHandler(this.FrmDonut_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbxNumber;
        private System.Windows.Forms.ComboBox cmbType2;
        private System.Windows.Forms.Label label3;
    }
}