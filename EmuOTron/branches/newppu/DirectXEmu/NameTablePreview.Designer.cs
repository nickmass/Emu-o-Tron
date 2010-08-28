namespace DirectXEmu
{
    partial class NameTablePreview
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
            this.nameTableView = new System.Windows.Forms.PictureBox();
            this.lblTackBar = new System.Windows.Forms.Label();
            this.txtScanline = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.nameTableView)).BeginInit();
            this.SuspendLayout();
            // 
            // nameTableView
            // 
            this.nameTableView.Location = new System.Drawing.Point(12, 12);
            this.nameTableView.Name = "nameTableView";
            this.nameTableView.Size = new System.Drawing.Size(512, 480);
            this.nameTableView.TabIndex = 0;
            this.nameTableView.TabStop = false;
            // 
            // lblTackBar
            // 
            this.lblTackBar.AutoSize = true;
            this.lblTackBar.Location = new System.Drawing.Point(9, 504);
            this.lblTackBar.Name = "lblTackBar";
            this.lblTackBar.Size = new System.Drawing.Size(101, 13);
            this.lblTackBar.TabIndex = 3;
            this.lblTackBar.Text = "Display on scanline:";
            // 
            // txtScanline
            // 
            this.txtScanline.Location = new System.Drawing.Point(116, 501);
            this.txtScanline.Name = "txtScanline";
            this.txtScanline.Size = new System.Drawing.Size(62, 20);
            this.txtScanline.TabIndex = 4;
            this.txtScanline.Text = "0";
            this.txtScanline.TextChanged += new System.EventHandler(this.txtScanline_TextChanged);
            // 
            // NameTablePreview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(535, 531);
            this.Controls.Add(this.txtScanline);
            this.Controls.Add(this.lblTackBar);
            this.Controls.Add(this.nameTableView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "NameTablePreview";
            this.ShowIcon = false;
            this.Text = "Name Tables";
            ((System.ComponentModel.ISupportInitialize)(this.nameTableView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox nameTableView;
        private System.Windows.Forms.Label lblTackBar;
        private System.Windows.Forms.TextBox txtScanline;
    }
}