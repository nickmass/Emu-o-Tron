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
            this.memPane = new System.Windows.Forms.TextBox();
            this.scrollBar = new System.Windows.Forms.VScrollBar();
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // memPane
            // 
            this.memPane.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.memPane.HideSelection = false;
            this.memPane.Location = new System.Drawing.Point(12, 12);
            this.memPane.Multiline = true;
            this.memPane.Name = "memPane";
            this.memPane.ReadOnly = true;
            this.memPane.Size = new System.Drawing.Size(453, 461);
            this.memPane.TabIndex = 0;
            this.memPane.WordWrap = false;
            // 
            // scrollBar
            // 
            this.scrollBar.Location = new System.Drawing.Point(444, 13);
            this.scrollBar.Name = "scrollBar";
            this.scrollBar.Size = new System.Drawing.Size(20, 459);
            this.scrollBar.TabIndex = 1;
            this.scrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.scrollBar_Scroll);
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 10;
            this.updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            // 
            // MemoryViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(477, 485);
            this.Controls.Add(this.scrollBar);
            this.Controls.Add(this.memPane);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MemoryViewer";
            this.ShowIcon = false;
            this.Text = "Memory Viewer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox memPane;
        private System.Windows.Forms.VScrollBar scrollBar;
        private System.Windows.Forms.Timer updateTimer;
    }
}