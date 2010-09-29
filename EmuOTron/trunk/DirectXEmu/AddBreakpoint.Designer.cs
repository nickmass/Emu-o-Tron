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
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.radExec = new System.Windows.Forms.RadioButton();
            this.radWrite = new System.Windows.Forms.RadioButton();
            this.radRead = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Address";
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(117, 55);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(36, 55);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // txtAddress
            // 
            this.txtAddress.Location = new System.Drawing.Point(63, 6);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(129, 20);
            this.txtAddress.TabIndex = 3;
            // 
            // radExec
            // 
            this.radExec.AutoSize = true;
            this.radExec.Location = new System.Drawing.Point(128, 32);
            this.radExec.Name = "radExec";
            this.radExec.Size = new System.Drawing.Size(64, 17);
            this.radExec.TabIndex = 4;
            this.radExec.TabStop = true;
            this.radExec.Text = "Execute";
            this.radExec.UseVisualStyleBackColor = true;
            // 
            // radWrite
            // 
            this.radWrite.AutoSize = true;
            this.radWrite.Location = new System.Drawing.Point(72, 32);
            this.radWrite.Name = "radWrite";
            this.radWrite.Size = new System.Drawing.Size(50, 17);
            this.radWrite.TabIndex = 5;
            this.radWrite.TabStop = true;
            this.radWrite.Text = "Write";
            this.radWrite.UseVisualStyleBackColor = true;
            // 
            // radRead
            // 
            this.radRead.AutoSize = true;
            this.radRead.Location = new System.Drawing.Point(15, 32);
            this.radRead.Name = "radRead";
            this.radRead.Size = new System.Drawing.Size(51, 17);
            this.radRead.TabIndex = 6;
            this.radRead.TabStop = true;
            this.radRead.Text = "Read";
            this.radRead.UseVisualStyleBackColor = true;
            // 
            // AddBreakpoint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(204, 88);
            this.Controls.Add(this.radRead);
            this.Controls.Add(this.radWrite);
            this.Controls.Add(this.radExec);
            this.Controls.Add(this.txtAddress);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "AddBreakpoint";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Breakpoint";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.RadioButton radExec;
        private System.Windows.Forms.RadioButton radWrite;
        private System.Windows.Forms.RadioButton radRead;
    }
}