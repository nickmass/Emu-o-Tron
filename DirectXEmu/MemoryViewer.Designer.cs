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
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.txtCursorAddr = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtCursorValue = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtCursorAddrDec = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtCursorValueDec = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip1.SuspendLayout();
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
            this.memPane.Size = new System.Drawing.Size(429, 461);
            this.memPane.TabIndex = 0;
            this.memPane.WordWrap = false;
            this.memPane.MouseMove += new System.Windows.Forms.MouseEventHandler(this.memPane_MouseMove);
            // 
            // scrollBar
            // 
            this.scrollBar.Location = new System.Drawing.Point(419, 13);
            this.scrollBar.Name = "scrollBar";
            this.scrollBar.Size = new System.Drawing.Size(20, 459);
            this.scrollBar.TabIndex = 1;
            this.scrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.scrollBar_Scroll);
            this.scrollBar.MouseLeave += new System.EventHandler(this.scrollBar_MouseLeave);
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 10;
            this.updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.txtCursorAddr,
            this.txtCursorValue,
            this.txtCursorAddrDec,
            this.txtCursorValueDec});
            this.statusStrip1.Location = new System.Drawing.Point(0, 481);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(452, 24);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 2;
            // 
            // txtCursorAddr
            // 
            this.txtCursorAddr.AutoSize = false;
            this.txtCursorAddr.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.txtCursorAddr.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.txtCursorAddr.Name = "txtCursorAddr";
            this.txtCursorAddr.Size = new System.Drawing.Size(100, 19);
            this.txtCursorAddr.Text = "Addr: 0x0000";
            this.txtCursorAddr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtCursorValue
            // 
            this.txtCursorValue.AutoSize = false;
            this.txtCursorValue.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.txtCursorValue.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.txtCursorValue.Name = "txtCursorValue";
            this.txtCursorValue.Size = new System.Drawing.Size(75, 19);
            this.txtCursorValue.Text = "Value: 00";
            this.txtCursorValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtCursorAddrDec
            // 
            this.txtCursorAddrDec.AutoSize = false;
            this.txtCursorAddrDec.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.txtCursorAddrDec.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.txtCursorAddrDec.Name = "txtCursorAddrDec";
            this.txtCursorAddrDec.Size = new System.Drawing.Size(100, 19);
            this.txtCursorAddrDec.Text = "Dec Addr: 0";
            this.txtCursorAddrDec.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtCursorValueDec
            // 
            this.txtCursorValueDec.AutoSize = false;
            this.txtCursorValueDec.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
                        | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.txtCursorValueDec.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.txtCursorValueDec.Name = "txtCursorValueDec";
            this.txtCursorValueDec.Size = new System.Drawing.Size(100, 19);
            this.txtCursorValueDec.Text = "Dec Value: 0";
            this.txtCursorValueDec.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MemoryViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(452, 505);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.scrollBar);
            this.Controls.Add(this.memPane);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MemoryViewer";
            this.ShowIcon = false;
            this.Text = "Memory Viewer";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox memPane;
        private System.Windows.Forms.VScrollBar scrollBar;
        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel txtCursorAddr;
        private System.Windows.Forms.ToolStripStatusLabel txtCursorValue;
        private System.Windows.Forms.ToolStripStatusLabel txtCursorAddrDec;
        private System.Windows.Forms.ToolStripStatusLabel txtCursorValueDec;
    }
}