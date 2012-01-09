namespace DirectXEmu
{
    partial class SoundVis
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
            this.pnlScreen = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // pnlScreen
            // 
            this.pnlScreen.Location = new System.Drawing.Point(12, 12);
            this.pnlScreen.Name = "pnlScreen";
            this.pnlScreen.Size = new System.Drawing.Size(384, 256);
            this.pnlScreen.TabIndex = 0;
            this.pnlScreen.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pnlScreen_MouseUp);
            // 
            // SoundVis
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(407, 277);
            this.Controls.Add(this.pnlScreen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "SoundVis";
            this.ShowIcon = false;
            this.Text = "Sound Visualizer";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlScreen;
    }
}