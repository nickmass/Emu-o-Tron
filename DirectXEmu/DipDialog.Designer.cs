namespace DirectXEmu
{
    partial class DipDialog
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
            this.dip1 = new System.Windows.Forms.CheckBox();
            this.dip2 = new System.Windows.Forms.CheckBox();
            this.dip3 = new System.Windows.Forms.CheckBox();
            this.dip4 = new System.Windows.Forms.CheckBox();
            this.dip5 = new System.Windows.Forms.CheckBox();
            this.dip6 = new System.Windows.Forms.CheckBox();
            this.dip7 = new System.Windows.Forms.CheckBox();
            this.dip8 = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // dip1
            // 
            this.dip1.AutoSize = true;
            this.dip1.Location = new System.Drawing.Point(12, 12);
            this.dip1.Name = "dip1";
            this.dip1.Size = new System.Drawing.Size(88, 17);
            this.dip1.TabIndex = 0;
            this.dip1.Text = "DIP Switch 1";
            this.dip1.UseVisualStyleBackColor = true;
            // 
            // dip2
            // 
            this.dip2.AutoSize = true;
            this.dip2.Location = new System.Drawing.Point(12, 35);
            this.dip2.Name = "dip2";
            this.dip2.Size = new System.Drawing.Size(88, 17);
            this.dip2.TabIndex = 1;
            this.dip2.Text = "DIP Switch 2";
            this.dip2.UseVisualStyleBackColor = true;
            // 
            // dip3
            // 
            this.dip3.AutoSize = true;
            this.dip3.Location = new System.Drawing.Point(12, 58);
            this.dip3.Name = "dip3";
            this.dip3.Size = new System.Drawing.Size(88, 17);
            this.dip3.TabIndex = 2;
            this.dip3.Text = "DIP Switch 3";
            this.dip3.UseVisualStyleBackColor = true;
            // 
            // dip4
            // 
            this.dip4.AutoSize = true;
            this.dip4.Location = new System.Drawing.Point(12, 81);
            this.dip4.Name = "dip4";
            this.dip4.Size = new System.Drawing.Size(88, 17);
            this.dip4.TabIndex = 3;
            this.dip4.Text = "DIP Switch 4";
            this.dip4.UseVisualStyleBackColor = true;
            // 
            // dip5
            // 
            this.dip5.AutoSize = true;
            this.dip5.Location = new System.Drawing.Point(134, 12);
            this.dip5.Name = "dip5";
            this.dip5.Size = new System.Drawing.Size(88, 17);
            this.dip5.TabIndex = 4;
            this.dip5.Text = "DIP Switch 5";
            this.dip5.UseVisualStyleBackColor = true;
            // 
            // dip6
            // 
            this.dip6.AutoSize = true;
            this.dip6.Location = new System.Drawing.Point(134, 35);
            this.dip6.Name = "dip6";
            this.dip6.Size = new System.Drawing.Size(88, 17);
            this.dip6.TabIndex = 5;
            this.dip6.Text = "DIP Switch 6";
            this.dip6.UseVisualStyleBackColor = true;
            // 
            // dip7
            // 
            this.dip7.AutoSize = true;
            this.dip7.Location = new System.Drawing.Point(134, 58);
            this.dip7.Name = "dip7";
            this.dip7.Size = new System.Drawing.Size(88, 17);
            this.dip7.TabIndex = 6;
            this.dip7.Text = "DIP Switch 7";
            this.dip7.UseVisualStyleBackColor = true;
            // 
            // dip8
            // 
            this.dip8.AutoSize = true;
            this.dip8.Location = new System.Drawing.Point(134, 81);
            this.dip8.Name = "dip8";
            this.dip8.Size = new System.Drawing.Size(88, 17);
            this.dip8.TabIndex = 7;
            this.dip8.Text = "DIP Switch 8";
            this.dip8.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(147, 104);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 8;
            this.button1.Text = "Ok";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // DipDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(234, 142);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dip8);
            this.Controls.Add(this.dip7);
            this.Controls.Add(this.dip6);
            this.Controls.Add(this.dip5);
            this.Controls.Add(this.dip4);
            this.Controls.Add(this.dip3);
            this.Controls.Add(this.dip2);
            this.Controls.Add(this.dip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "DipDialog";
            this.ShowIcon = false;
            this.Text = "Configure DIP Switches";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        public System.Windows.Forms.CheckBox dip1;
        public System.Windows.Forms.CheckBox dip2;
        public System.Windows.Forms.CheckBox dip3;
        public System.Windows.Forms.CheckBox dip4;
        public System.Windows.Forms.CheckBox dip5;
        public System.Windows.Forms.CheckBox dip6;
        public System.Windows.Forms.CheckBox dip7;
        public System.Windows.Forms.CheckBox dip8;
    }
}