namespace DirectXEmu
{
    partial class MemoryViewer
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
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.memPane = new DirectXEmu.ByteViewer();
            this.SuspendLayout();
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            // 
            // memPane
            // 
            this.memPane.BackColor = System.Drawing.SystemColors.Window;
            this.memPane.BytesPerLine = 16;
            this.memPane.BytesPerPage = 496;
            this.memPane.Data = new byte[0x10];
            this.memPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this.memPane.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.memPane.LinesPerPage = 31;
            this.memPane.Location = new System.Drawing.Point(0, 0);
            this.memPane.Name = "memPane";
            this.memPane.Size = new System.Drawing.Size(434, 483);
            this.memPane.StartAddress = 0;
            this.memPane.TabIndex = 3;
            // 
            // MemoryViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 483);
            this.Controls.Add(this.memPane);
            this.Name = "MemoryViewer";
            this.ShowIcon = false;
            this.Text = "Memory Viewer";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer updateTimer;
        private ByteViewer memPane;
    }
}