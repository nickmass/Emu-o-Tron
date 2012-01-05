namespace DirectXEmu
{
    partial class MemoryVis
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
            this.components = new System.ComponentModel.Container();
            this.visPanel = new System.Windows.Forms.Panel();
            this.tmrUpdate = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // visPanel
            // 
            this.visPanel.Location = new System.Drawing.Point(12, 12);
            this.visPanel.Name = "visPanel";
            this.visPanel.Size = new System.Drawing.Size(512, 512);
            this.visPanel.TabIndex = 0;
            // 
            // tmrUpdate
            // 
            this.tmrUpdate.Tick += new System.EventHandler(this.tmrUpdate_Tick);
            // 
            // MemoryVis
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(536, 538);
            this.Controls.Add(this.visPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "MemoryVis";
            this.ShowIcon = false;
            this.Text = "Memory Visualizer";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel visPanel;
        private System.Windows.Forms.Timer tmrUpdate;
    }
}