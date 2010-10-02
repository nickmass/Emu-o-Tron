namespace DirectXEmu
{
    partial class Debugger
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
            this.txtLog = new System.Windows.Forms.TextBox();
            this.scrlLog = new System.Windows.Forms.VScrollBar();
            this.btnOk = new System.Windows.Forms.Button();
            this.chkCarry = new System.Windows.Forms.CheckBox();
            this.chkZero = new System.Windows.Forms.CheckBox();
            this.chkInterrupt = new System.Windows.Forms.CheckBox();
            this.chkDecimal = new System.Windows.Forms.CheckBox();
            this.chkBreak = new System.Windows.Forms.CheckBox();
            this.chkUnused = new System.Windows.Forms.CheckBox();
            this.chkOverflow = new System.Windows.Forms.CheckBox();
            this.chkNegative = new System.Windows.Forms.CheckBox();
            this.txtY = new System.Windows.Forms.TextBox();
            this.txtX = new System.Windows.Forms.TextBox();
            this.txtS = new System.Windows.Forms.TextBox();
            this.txtA = new System.Windows.Forms.TextBox();
            this.txtPC = new System.Windows.Forms.TextBox();
            this.lstStack = new System.Windows.Forms.ListBox();
            this.lstBreak = new System.Windows.Forms.ListBox();
            this.btnAddBreak = new System.Windows.Forms.Button();
            this.btnEditBreak = new System.Windows.Forms.Button();
            this.btnRemoveBreak = new System.Windows.Forms.Button();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnStep = new System.Windows.Forms.Button();
            this.btnRunFor = new System.Windows.Forms.Button();
            this.btnSeekPC = new System.Windows.Forms.Button();
            this.btnSeekTo = new System.Windows.Forms.Button();
            this.txtRunFor = new System.Windows.Forms.TextBox();
            this.txtSeekTo = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtLastExec = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txtCycle = new System.Windows.Forms.TextBox();
            this.txtScanline = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtLog
            // 
            this.txtLog.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLog.Location = new System.Drawing.Point(12, 38);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(337, 370);
            this.txtLog.TabIndex = 21;
            // 
            // scrlLog
            // 
            this.scrlLog.LargeChange = 1;
            this.scrlLog.Location = new System.Drawing.Point(330, 38);
            this.scrlLog.Maximum = 65535;
            this.scrlLog.Name = "scrlLog";
            this.scrlLog.Size = new System.Drawing.Size(19, 370);
            this.scrlLog.TabIndex = 1;
            this.scrlLog.Scroll += new System.Windows.Forms.ScrollEventHandler(this.scrlLog_Scroll);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(540, 385);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // chkCarry
            // 
            this.chkCarry.AutoCheck = false;
            this.chkCarry.AutoSize = true;
            this.chkCarry.Location = new System.Drawing.Point(449, 12);
            this.chkCarry.Name = "chkCarry";
            this.chkCarry.Size = new System.Drawing.Size(50, 17);
            this.chkCarry.TabIndex = 3;
            this.chkCarry.Text = "Carry";
            this.chkCarry.UseVisualStyleBackColor = true;
            // 
            // chkZero
            // 
            this.chkZero.AutoCheck = false;
            this.chkZero.AutoSize = true;
            this.chkZero.Location = new System.Drawing.Point(449, 35);
            this.chkZero.Name = "chkZero";
            this.chkZero.Size = new System.Drawing.Size(48, 17);
            this.chkZero.TabIndex = 4;
            this.chkZero.Text = "Zero";
            this.chkZero.UseVisualStyleBackColor = true;
            // 
            // chkInterrupt
            // 
            this.chkInterrupt.AutoCheck = false;
            this.chkInterrupt.AutoSize = true;
            this.chkInterrupt.Location = new System.Drawing.Point(449, 58);
            this.chkInterrupt.Name = "chkInterrupt";
            this.chkInterrupt.Size = new System.Drawing.Size(65, 17);
            this.chkInterrupt.TabIndex = 5;
            this.chkInterrupt.Text = "Interrupt";
            this.chkInterrupt.UseVisualStyleBackColor = true;
            // 
            // chkDecimal
            // 
            this.chkDecimal.AutoCheck = false;
            this.chkDecimal.AutoSize = true;
            this.chkDecimal.Location = new System.Drawing.Point(449, 81);
            this.chkDecimal.Name = "chkDecimal";
            this.chkDecimal.Size = new System.Drawing.Size(64, 17);
            this.chkDecimal.TabIndex = 6;
            this.chkDecimal.Text = "Decimal";
            this.chkDecimal.UseVisualStyleBackColor = true;
            // 
            // chkBreak
            // 
            this.chkBreak.AutoCheck = false;
            this.chkBreak.AutoSize = true;
            this.chkBreak.Location = new System.Drawing.Point(535, 12);
            this.chkBreak.Name = "chkBreak";
            this.chkBreak.Size = new System.Drawing.Size(54, 17);
            this.chkBreak.TabIndex = 7;
            this.chkBreak.Text = "Break";
            this.chkBreak.UseVisualStyleBackColor = true;
            // 
            // chkUnused
            // 
            this.chkUnused.AutoCheck = false;
            this.chkUnused.AutoSize = true;
            this.chkUnused.Location = new System.Drawing.Point(535, 35);
            this.chkUnused.Name = "chkUnused";
            this.chkUnused.Size = new System.Drawing.Size(63, 17);
            this.chkUnused.TabIndex = 8;
            this.chkUnused.Text = "Unused";
            this.chkUnused.UseVisualStyleBackColor = true;
            // 
            // chkOverflow
            // 
            this.chkOverflow.AutoCheck = false;
            this.chkOverflow.AutoSize = true;
            this.chkOverflow.Location = new System.Drawing.Point(535, 58);
            this.chkOverflow.Name = "chkOverflow";
            this.chkOverflow.Size = new System.Drawing.Size(68, 17);
            this.chkOverflow.TabIndex = 9;
            this.chkOverflow.Text = "Overflow";
            this.chkOverflow.UseVisualStyleBackColor = true;
            // 
            // chkNegative
            // 
            this.chkNegative.AutoCheck = false;
            this.chkNegative.AutoSize = true;
            this.chkNegative.Location = new System.Drawing.Point(535, 81);
            this.chkNegative.Name = "chkNegative";
            this.chkNegative.Size = new System.Drawing.Size(69, 17);
            this.chkNegative.TabIndex = 10;
            this.chkNegative.Text = "Negative";
            this.chkNegative.UseVisualStyleBackColor = true;
            // 
            // txtY
            // 
            this.txtY.Location = new System.Drawing.Point(568, 171);
            this.txtY.Name = "txtY";
            this.txtY.ReadOnly = true;
            this.txtY.Size = new System.Drawing.Size(47, 20);
            this.txtY.TabIndex = 11;
            // 
            // txtX
            // 
            this.txtX.Location = new System.Drawing.Point(568, 145);
            this.txtX.Name = "txtX";
            this.txtX.ReadOnly = true;
            this.txtX.Size = new System.Drawing.Size(47, 20);
            this.txtX.TabIndex = 12;
            // 
            // txtS
            // 
            this.txtS.Location = new System.Drawing.Point(482, 171);
            this.txtS.Name = "txtS";
            this.txtS.ReadOnly = true;
            this.txtS.Size = new System.Drawing.Size(47, 20);
            this.txtS.TabIndex = 13;
            // 
            // txtA
            // 
            this.txtA.Location = new System.Drawing.Point(482, 145);
            this.txtA.Name = "txtA";
            this.txtA.ReadOnly = true;
            this.txtA.Size = new System.Drawing.Size(47, 20);
            this.txtA.TabIndex = 14;
            // 
            // txtPC
            // 
            this.txtPC.Location = new System.Drawing.Point(529, 119);
            this.txtPC.Name = "txtPC";
            this.txtPC.ReadOnly = true;
            this.txtPC.Size = new System.Drawing.Size(86, 20);
            this.txtPC.TabIndex = 15;
            // 
            // lstStack
            // 
            this.lstStack.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lstStack.FormattingEnabled = true;
            this.lstStack.Location = new System.Drawing.Point(529, 197);
            this.lstStack.Name = "lstStack";
            this.lstStack.Size = new System.Drawing.Size(86, 173);
            this.lstStack.TabIndex = 16;
            // 
            // lstBreak
            // 
            this.lstBreak.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lstBreak.FormattingEnabled = true;
            this.lstBreak.Location = new System.Drawing.Point(355, 229);
            this.lstBreak.Name = "lstBreak";
            this.lstBreak.Size = new System.Drawing.Size(87, 82);
            this.lstBreak.TabIndex = 17;
            // 
            // btnAddBreak
            // 
            this.btnAddBreak.Location = new System.Drawing.Point(448, 229);
            this.btnAddBreak.Name = "btnAddBreak";
            this.btnAddBreak.Size = new System.Drawing.Size(75, 23);
            this.btnAddBreak.TabIndex = 18;
            this.btnAddBreak.Text = "Add";
            this.btnAddBreak.UseVisualStyleBackColor = true;
            this.btnAddBreak.Click += new System.EventHandler(this.btnAddBreak_Click);
            // 
            // btnEditBreak
            // 
            this.btnEditBreak.Location = new System.Drawing.Point(448, 258);
            this.btnEditBreak.Name = "btnEditBreak";
            this.btnEditBreak.Size = new System.Drawing.Size(75, 23);
            this.btnEditBreak.TabIndex = 19;
            this.btnEditBreak.Text = "Edit";
            this.btnEditBreak.UseVisualStyleBackColor = true;
            this.btnEditBreak.Click += new System.EventHandler(this.btnEditBreak_Click);
            // 
            // btnRemoveBreak
            // 
            this.btnRemoveBreak.Location = new System.Drawing.Point(448, 287);
            this.btnRemoveBreak.Name = "btnRemoveBreak";
            this.btnRemoveBreak.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveBreak.TabIndex = 20;
            this.btnRemoveBreak.Text = "Remove";
            this.btnRemoveBreak.UseVisualStyleBackColor = true;
            this.btnRemoveBreak.Click += new System.EventHandler(this.btnRemoveBreak_Click);
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(355, 12);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 23);
            this.btnRun.TabIndex = 0;
            this.btnRun.Text = "Pause";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // btnStep
            // 
            this.btnStep.Location = new System.Drawing.Point(355, 41);
            this.btnStep.Name = "btnStep";
            this.btnStep.Size = new System.Drawing.Size(75, 23);
            this.btnStep.TabIndex = 22;
            this.btnStep.Text = "Step";
            this.btnStep.UseVisualStyleBackColor = true;
            this.btnStep.Click += new System.EventHandler(this.btnStep_Click);
            // 
            // btnRunFor
            // 
            this.btnRunFor.Location = new System.Drawing.Point(355, 70);
            this.btnRunFor.Name = "btnRunFor";
            this.btnRunFor.Size = new System.Drawing.Size(75, 23);
            this.btnRunFor.TabIndex = 23;
            this.btnRunFor.Text = "Run For...";
            this.btnRunFor.UseVisualStyleBackColor = true;
            this.btnRunFor.Click += new System.EventHandler(this.btnRunFor_Click);
            // 
            // btnSeekPC
            // 
            this.btnSeekPC.Location = new System.Drawing.Point(355, 145);
            this.btnSeekPC.Name = "btnSeekPC";
            this.btnSeekPC.Size = new System.Drawing.Size(75, 23);
            this.btnSeekPC.TabIndex = 24;
            this.btnSeekPC.Text = "Seek PC";
            this.btnSeekPC.UseVisualStyleBackColor = true;
            this.btnSeekPC.Click += new System.EventHandler(this.btnSeekPC_Click);
            // 
            // btnSeekTo
            // 
            this.btnSeekTo.Location = new System.Drawing.Point(355, 174);
            this.btnSeekTo.Name = "btnSeekTo";
            this.btnSeekTo.Size = new System.Drawing.Size(75, 23);
            this.btnSeekTo.TabIndex = 25;
            this.btnSeekTo.Text = "Seek To...";
            this.btnSeekTo.UseVisualStyleBackColor = true;
            this.btnSeekTo.Click += new System.EventHandler(this.btnSeekTo_Click);
            // 
            // txtRunFor
            // 
            this.txtRunFor.Location = new System.Drawing.Point(355, 99);
            this.txtRunFor.Name = "txtRunFor";
            this.txtRunFor.Size = new System.Drawing.Size(75, 20);
            this.txtRunFor.TabIndex = 26;
            // 
            // txtSeekTo
            // 
            this.txtSeekTo.Location = new System.Drawing.Point(355, 203);
            this.txtSeekTo.Name = "txtSeekTo";
            this.txtSeekTo.Size = new System.Drawing.Size(75, 20);
            this.txtSeekTo.TabIndex = 27;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(437, 122);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 28;
            this.label1.Text = "Program Counter";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(462, 148);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(14, 13);
            this.label2.TabIndex = 29;
            this.label2.Text = "A";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(548, 174);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(14, 13);
            this.label3.TabIndex = 30;
            this.label3.Text = "Y";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(548, 150);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(14, 13);
            this.label4.TabIndex = 31;
            this.label4.Text = "X";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(462, 174);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(14, 13);
            this.label5.TabIndex = 32;
            this.label5.Text = "S";
            // 
            // txtLastExec
            // 
            this.txtLastExec.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLastExec.Location = new System.Drawing.Point(12, 9);
            this.txtLastExec.Name = "txtLastExec";
            this.txtLastExec.ReadOnly = true;
            this.txtLastExec.Size = new System.Drawing.Size(337, 20);
            this.txtLastExec.TabIndex = 33;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(370, 350);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(33, 13);
            this.label6.TabIndex = 34;
            this.label6.Text = "Cycle";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(355, 324);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(48, 13);
            this.label7.TabIndex = 35;
            this.label7.Text = "Scanline";
            // 
            // txtCycle
            // 
            this.txtCycle.Location = new System.Drawing.Point(409, 347);
            this.txtCycle.Name = "txtCycle";
            this.txtCycle.ReadOnly = true;
            this.txtCycle.Size = new System.Drawing.Size(47, 20);
            this.txtCycle.TabIndex = 36;
            // 
            // txtScanline
            // 
            this.txtScanline.Location = new System.Drawing.Point(409, 321);
            this.txtScanline.Name = "txtScanline";
            this.txtScanline.ReadOnly = true;
            this.txtScanline.Size = new System.Drawing.Size(47, 20);
            this.txtScanline.TabIndex = 37;
            // 
            // Debugger
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(627, 420);
            this.Controls.Add(this.txtScanline);
            this.Controls.Add(this.txtCycle);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtLastExec);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtSeekTo);
            this.Controls.Add(this.txtRunFor);
            this.Controls.Add(this.btnSeekTo);
            this.Controls.Add(this.btnSeekPC);
            this.Controls.Add(this.btnRunFor);
            this.Controls.Add(this.btnStep);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.btnRemoveBreak);
            this.Controls.Add(this.btnEditBreak);
            this.Controls.Add(this.btnAddBreak);
            this.Controls.Add(this.lstBreak);
            this.Controls.Add(this.lstStack);
            this.Controls.Add(this.txtPC);
            this.Controls.Add(this.txtA);
            this.Controls.Add(this.txtS);
            this.Controls.Add(this.txtX);
            this.Controls.Add(this.txtY);
            this.Controls.Add(this.chkNegative);
            this.Controls.Add(this.chkOverflow);
            this.Controls.Add(this.chkUnused);
            this.Controls.Add(this.chkBreak);
            this.Controls.Add(this.chkDecimal);
            this.Controls.Add(this.chkInterrupt);
            this.Controls.Add(this.chkZero);
            this.Controls.Add(this.chkCarry);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.scrlLog);
            this.Controls.Add(this.txtLog);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "Debugger";
            this.ShowIcon = false;
            this.Text = "Debugger";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Debugger_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.VScrollBar scrlLog;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.CheckBox chkCarry;
        private System.Windows.Forms.CheckBox chkZero;
        private System.Windows.Forms.CheckBox chkInterrupt;
        private System.Windows.Forms.CheckBox chkDecimal;
        private System.Windows.Forms.CheckBox chkBreak;
        private System.Windows.Forms.CheckBox chkUnused;
        private System.Windows.Forms.CheckBox chkOverflow;
        private System.Windows.Forms.CheckBox chkNegative;
        private System.Windows.Forms.TextBox txtY;
        private System.Windows.Forms.TextBox txtX;
        private System.Windows.Forms.TextBox txtS;
        private System.Windows.Forms.TextBox txtA;
        private System.Windows.Forms.TextBox txtPC;
        private System.Windows.Forms.ListBox lstStack;
        private System.Windows.Forms.ListBox lstBreak;
        private System.Windows.Forms.Button btnAddBreak;
        private System.Windows.Forms.Button btnEditBreak;
        private System.Windows.Forms.Button btnRemoveBreak;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnStep;
        private System.Windows.Forms.Button btnRunFor;
        private System.Windows.Forms.Button btnSeekPC;
        private System.Windows.Forms.Button btnSeekTo;
        private System.Windows.Forms.TextBox txtRunFor;
        private System.Windows.Forms.TextBox txtSeekTo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtLastExec;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtCycle;
        private System.Windows.Forms.TextBox txtScanline;
    }
}