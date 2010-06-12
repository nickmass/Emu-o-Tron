namespace DirectXEmu
{
    partial class OpenRomDialog
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
            this.fileList = new System.Windows.Forms.ListView();
            this.directoryList = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // fileList
            // 
            this.fileList.Location = new System.Drawing.Point(140, 12);
            this.fileList.Name = "fileList";
            this.fileList.Size = new System.Drawing.Size(412, 286);
            this.fileList.TabIndex = 0;
            this.fileList.UseCompatibleStateImageBehavior = false;
            // 
            // directoryList
            // 
            this.directoryList.Location = new System.Drawing.Point(13, 12);
            this.directoryList.Name = "directoryList";
            this.directoryList.Size = new System.Drawing.Size(121, 286);
            this.directoryList.TabIndex = 1;
            this.directoryList.UseCompatibleStateImageBehavior = false;
            // 
            // OpenRomDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(564, 349);
            this.Controls.Add(this.directoryList);
            this.Controls.Add(this.fileList);
            this.Name = "OpenRomDialog";
            this.Text = "OpenRomDialog";
            this.Load += new System.EventHandler(this.openRomDialog_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView fileList;
        private System.Windows.Forms.ListView directoryList;
    }
}