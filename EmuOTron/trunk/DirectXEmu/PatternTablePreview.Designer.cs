namespace DirectXEmu
{
    partial class PatternTablePreview
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
            this.patternTableViewer = new System.Windows.Forms.PictureBox();
            this.txtScanline = new System.Windows.Forms.TextBox();
            this.lblScanLine = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.patternTableViewer)).BeginInit();
            this.SuspendLayout();
            // 
            // patternTableViewer
            // 
            this.patternTableViewer.Location = new System.Drawing.Point(12, 12);
            this.patternTableViewer.Name = "patternTableViewer";
            this.patternTableViewer.Size = new System.Drawing.Size(512, 256);
            this.patternTableViewer.TabIndex = 0;
            this.patternTableViewer.TabStop = false;
            // 
            // txtScanline
            // 
            this.txtScanline.Location = new System.Drawing.Point(119, 278);
            this.txtScanline.Name = "txtScanline";
            this.txtScanline.Size = new System.Drawing.Size(66, 20);
            this.txtScanline.TabIndex = 2;
            this.txtScanline.TextChanged += new System.EventHandler(this.txtScanLine_TextChanged);
            // 
            // lblScanLine
            // 
            this.lblScanLine.AutoSize = true;
            this.lblScanLine.Location = new System.Drawing.Point(9, 281);
            this.lblScanLine.Name = "lblScanLine";
            this.lblScanLine.Size = new System.Drawing.Size(104, 13);
            this.lblScanLine.TabIndex = 3;
            this.lblScanLine.Text = "Display on scan line:";
            // 
            // PatternTablePreview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(535, 380);
            this.Controls.Add(this.lblScanLine);
            this.Controls.Add(this.txtScanline);
            this.Controls.Add(this.patternTableViewer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "PatternTablePreview";
            this.ShowIcon = false;
            this.Text = "Pattern Tables";
            ((System.ComponentModel.ISupportInitialize)(this.patternTableViewer)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox patternTableViewer;
        private System.Windows.Forms.TextBox txtScanline;
        private System.Windows.Forms.Label lblScanLine;
    }
}