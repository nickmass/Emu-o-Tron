namespace DirectXEmu
{
    partial class AddBreakpoint
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
            this.label1 = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtBreak1 = new System.Windows.Forms.TextBox();
            this.chkRead = new System.Windows.Forms.CheckBox();
            this.chkWrite = new System.Windows.Forms.CheckBox();
            this.chkExecute = new System.Windows.Forms.CheckBox();
            this.txtBreak2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkEnable = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Range";
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(36, 78);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 7;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(117, 78);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // txtBreak1
            // 
            this.txtBreak1.Location = new System.Drawing.Point(63, 6);
            this.txtBreak1.Name = "txtBreak1";
            this.txtBreak1.Size = new System.Drawing.Size(58, 20);
            this.txtBreak1.TabIndex = 1;
            this.txtBreak1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.AddBreakpoint_KeyUp);
            // 
            // chkRead
            // 
            this.chkRead.AutoSize = true;
            this.chkRead.Location = new System.Drawing.Point(13, 32);
            this.chkRead.Name = "chkRead";
            this.chkRead.Size = new System.Drawing.Size(52, 17);
            this.chkRead.TabIndex = 3;
            this.chkRead.Text = "Read";
            this.chkRead.UseVisualStyleBackColor = true;
            this.chkRead.KeyUp += new System.Windows.Forms.KeyEventHandler(this.AddBreakpoint_KeyUp);
            // 
            // chkWrite
            // 
            this.chkWrite.AutoSize = true;
            this.chkWrite.Location = new System.Drawing.Point(70, 32);
            this.chkWrite.Name = "chkWrite";
            this.chkWrite.Size = new System.Drawing.Size(51, 17);
            this.chkWrite.TabIndex = 4;
            this.chkWrite.Text = "Write";
            this.chkWrite.UseVisualStyleBackColor = true;
            this.chkWrite.KeyUp += new System.Windows.Forms.KeyEventHandler(this.AddBreakpoint_KeyUp);
            // 
            // chkExecute
            // 
            this.chkExecute.AutoSize = true;
            this.chkExecute.Location = new System.Drawing.Point(127, 32);
            this.chkExecute.Name = "chkExecute";
            this.chkExecute.Size = new System.Drawing.Size(65, 17);
            this.chkExecute.TabIndex = 5;
            this.chkExecute.Text = "Execute";
            this.chkExecute.UseVisualStyleBackColor = true;
            this.chkExecute.KeyUp += new System.Windows.Forms.KeyEventHandler(this.AddBreakpoint_KeyUp);
            // 
            // txtBreak2
            // 
            this.txtBreak2.Location = new System.Drawing.Point(134, 6);
            this.txtBreak2.Name = "txtBreak2";
            this.txtBreak2.Size = new System.Drawing.Size(58, 20);
            this.txtBreak2.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(123, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(10, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "-";
            // 
            // chkEnable
            // 
            this.chkEnable.AutoSize = true;
            this.chkEnable.Checked = true;
            this.chkEnable.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnable.Location = new System.Drawing.Point(127, 55);
            this.chkEnable.Name = "chkEnable";
            this.chkEnable.Size = new System.Drawing.Size(65, 17);
            this.chkEnable.TabIndex = 6;
            this.chkEnable.Text = "Enabled";
            this.chkEnable.UseVisualStyleBackColor = true;
            // 
            // AddBreakpoint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(204, 114);
            this.Controls.Add(this.chkEnable);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtBreak2);
            this.Controls.Add(this.chkExecute);
            this.Controls.Add(this.chkWrite);
            this.Controls.Add(this.chkRead);
            this.Controls.Add(this.txtBreak1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "AddBreakpoint";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Breakpoint";
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.AddBreakpoint_KeyUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtBreak1;
        private System.Windows.Forms.CheckBox chkRead;
        private System.Windows.Forms.CheckBox chkWrite;
        private System.Windows.Forms.CheckBox chkExecute;
        private System.Windows.Forms.TextBox txtBreak2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkEnable;
    }
}