namespace DirectXEmu
{
    partial class PaletteConfig
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
            this.trkHue = new System.Windows.Forms.TrackBar();
            this.trkSat = new System.Windows.Forms.TrackBar();
            this.trkGamma = new System.Windows.Forms.TrackBar();
            this.radInternal = new System.Windows.Forms.RadioButton();
            this.radExternal = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnDefaults = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tmrPalUpdate = new System.Windows.Forms.Timer(this.components);
            this.openPalette = new System.Windows.Forms.OpenFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.trkBrightness = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.trkHue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkSat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkGamma)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkBrightness)).BeginInit();
            this.SuspendLayout();
            // 
            // trkHue
            // 
            this.trkHue.LargeChange = 25;
            this.trkHue.Location = new System.Drawing.Point(23, 192);
            this.trkHue.Maximum = 150;
            this.trkHue.Name = "trkHue";
            this.trkHue.Size = new System.Drawing.Size(104, 45);
            this.trkHue.TabIndex = 0;
            this.trkHue.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trkHue.Scroll += new System.EventHandler(this.trkHue_Scroll);
            // 
            // trkSat
            // 
            this.trkSat.LargeChange = 25;
            this.trkSat.Location = new System.Drawing.Point(23, 141);
            this.trkSat.Maximum = 150;
            this.trkSat.Name = "trkSat";
            this.trkSat.Size = new System.Drawing.Size(104, 45);
            this.trkSat.TabIndex = 1;
            this.trkSat.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trkSat.Scroll += new System.EventHandler(this.trkSat_Scroll);
            // 
            // trkGamma
            // 
            this.trkGamma.LargeChange = 25;
            this.trkGamma.Location = new System.Drawing.Point(23, 39);
            this.trkGamma.Maximum = 200;
            this.trkGamma.Name = "trkGamma";
            this.trkGamma.Size = new System.Drawing.Size(104, 45);
            this.trkGamma.TabIndex = 2;
            this.trkGamma.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trkGamma.Scroll += new System.EventHandler(this.trkGamma_Scroll);
            // 
            // radInternal
            // 
            this.radInternal.AutoSize = true;
            this.radInternal.Location = new System.Drawing.Point(6, 19);
            this.radInternal.Name = "radInternal";
            this.radInternal.Size = new System.Drawing.Size(60, 17);
            this.radInternal.TabIndex = 3;
            this.radInternal.TabStop = true;
            this.radInternal.Text = "Internal";
            this.radInternal.UseVisualStyleBackColor = true;
            this.radInternal.CheckedChanged += new System.EventHandler(this.radInternal_CheckedChanged);
            // 
            // radExternal
            // 
            this.radExternal.AutoSize = true;
            this.radExternal.Location = new System.Drawing.Point(72, 19);
            this.radExternal.Name = "radExternal";
            this.radExternal.Size = new System.Drawing.Size(63, 17);
            this.radExternal.TabIndex = 4;
            this.radExternal.TabStop = true;
            this.radExternal.Text = "External";
            this.radExternal.UseVisualStyleBackColor = true;
            this.radExternal.CheckedChanged += new System.EventHandler(this.radExternal_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.btnDefaults);
            this.groupBox1.Controls.Add(this.trkBrightness);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.trkGamma);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.trkHue);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.trkSat);
            this.groupBox1.Location = new System.Drawing.Point(285, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 258);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Internal Palette";
            // 
            // btnDefaults
            // 
            this.btnDefaults.Location = new System.Drawing.Point(119, 229);
            this.btnDefaults.Name = "btnDefaults";
            this.btnDefaults.Size = new System.Drawing.Size(75, 23);
            this.btnDefaults.TabIndex = 10;
            this.btnDefaults.Text = "Defaults";
            this.btnDefaults.UseVisualStyleBackColor = true;
            this.btnDefaults.Click += new System.EventHandler(this.btnDefaults_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(133, 192);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(27, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Hue";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(133, 141);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Saturation";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(133, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Gamma";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtPath);
            this.groupBox2.Controls.Add(this.btnBrowse);
            this.groupBox2.Location = new System.Drawing.Point(285, 276);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(200, 74);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "External Palette";
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(6, 19);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(188, 20);
            this.txtPath.TabIndex = 7;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(119, 45);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 7;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.radInternal);
            this.groupBox3.Controls.Add(this.radExternal);
            this.groupBox3.Location = new System.Drawing.Point(12, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(138, 48);
            this.groupBox3.TabIndex = 7;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Palette";
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(329, 356);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 8;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(410, 356);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // tmrPalUpdate
            // 
            this.tmrPalUpdate.Interval = 16;
            this.tmrPalUpdate.Tick += new System.EventHandler(this.tmrPalUpdate_Tick);
            // 
            // panel1
            // 
            this.panel1.Location = new System.Drawing.Point(18, 66);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(256, 256);
            this.panel1.TabIndex = 8;
            // 
            // trkBrightness
            // 
            this.trkBrightness.LargeChange = 25;
            this.trkBrightness.Location = new System.Drawing.Point(23, 90);
            this.trkBrightness.Maximum = 100;
            this.trkBrightness.Name = "trkBrightness";
            this.trkBrightness.Size = new System.Drawing.Size(104, 45);
            this.trkBrightness.TabIndex = 11;
            this.trkBrightness.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trkBrightness.Scroll += new System.EventHandler(this.trkBrightness_Scroll);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(133, 90);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Brightness";
            // 
            // PaletteConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(497, 389);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "PaletteConfig";
            this.ShowIcon = false;
            this.Text = "Palette";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PaletteConfig_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.trkHue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkSat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkGamma)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkBrightness)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TrackBar trkHue;
        private System.Windows.Forms.TrackBar trkSat;
        private System.Windows.Forms.TrackBar trkGamma;
        private System.Windows.Forms.RadioButton radInternal;
        private System.Windows.Forms.RadioButton radExternal;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Timer tmrPalUpdate;
        private System.Windows.Forms.OpenFileDialog openPalette;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnDefaults;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TrackBar trkBrightness;
    }
}