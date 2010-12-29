namespace DirectXEmu
{
    partial class Keybind
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
            this.bindViewer = new System.Windows.Forms.PropertyGrid();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.cboPortOne = new System.Windows.Forms.ComboBox();
            this.cboPortTwo = new System.Windows.Forms.ComboBox();
            this.chkFourScore = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // bindViewer
            // 
            this.bindViewer.HelpVisible = false;
            this.bindViewer.Location = new System.Drawing.Point(198, 12);
            this.bindViewer.Name = "bindViewer";
            this.bindViewer.Size = new System.Drawing.Size(328, 396);
            this.bindViewer.TabIndex = 0;
            this.bindViewer.ToolbarVisible = false;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(451, 414);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(370, 414);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // cboPortOne
            // 
            this.cboPortOne.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPortOne.FormattingEnabled = true;
            this.cboPortOne.Items.AddRange(new object[] {
            "Controller",
            "Zapper",
            "Paddle",
            "FamiPaddle",
            "Empty"});
            this.cboPortOne.Location = new System.Drawing.Point(71, 21);
            this.cboPortOne.Name = "cboPortOne";
            this.cboPortOne.Size = new System.Drawing.Size(121, 21);
            this.cboPortOne.TabIndex = 3;
            this.cboPortOne.SelectedIndexChanged += new System.EventHandler(this.cboPortOne_SelectedIndexChanged);
            // 
            // cboPortTwo
            // 
            this.cboPortTwo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPortTwo.FormattingEnabled = true;
            this.cboPortTwo.Items.AddRange(new object[] {
            "Controller",
            "Zapper",
            "Paddle",
            "FamiPaddle",
            "Empty"});
            this.cboPortTwo.Location = new System.Drawing.Point(71, 48);
            this.cboPortTwo.Name = "cboPortTwo";
            this.cboPortTwo.Size = new System.Drawing.Size(121, 21);
            this.cboPortTwo.TabIndex = 4;
            this.cboPortTwo.SelectedIndexChanged += new System.EventHandler(this.cboPortTwo_SelectedIndexChanged);
            // 
            // chkFourScore
            // 
            this.chkFourScore.AutoSize = true;
            this.chkFourScore.Location = new System.Drawing.Point(114, 75);
            this.chkFourScore.Name = "chkFourScore";
            this.chkFourScore.Size = new System.Drawing.Size(78, 17);
            this.chkFourScore.TabIndex = 5;
            this.chkFourScore.Text = "Four Score";
            this.chkFourScore.UseVisualStyleBackColor = true;
            this.chkFourScore.CheckedChanged += new System.EventHandler(this.chkFourScore_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Port One";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Port Two";
            // 
            // Keybind
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(538, 449);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkFourScore);
            this.Controls.Add(this.cboPortTwo);
            this.Controls.Add(this.cboPortOne);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.bindViewer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "Keybind";
            this.ShowIcon = false;
            this.Text = "Configure Key Bindings";
            this.Load += new System.EventHandler(this.Keybind_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PropertyGrid bindViewer;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.ComboBox cboPortOne;
        private System.Windows.Forms.ComboBox cboPortTwo;
        private System.Windows.Forms.CheckBox chkFourScore;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}