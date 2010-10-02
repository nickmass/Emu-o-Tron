namespace DirectXEmu
{
    partial class CheatFinder
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
            this.lstResults = new System.Windows.Forms.ListBox();
            this.cboOp = new System.Windows.Forms.ComboBox();
            this.txtCompare = new System.Windows.Forms.TextBox();
            this.btnFilter = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.cboUnknown = new System.Windows.Forms.ComboBox();
            this.btnUnkFilter = new System.Windows.Forms.Button();
            this.btnUnkSearch = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lstResults
            // 
            this.lstResults.FormattingEnabled = true;
            this.lstResults.Location = new System.Drawing.Point(12, 12);
            this.lstResults.Name = "lstResults";
            this.lstResults.Size = new System.Drawing.Size(86, 238);
            this.lstResults.TabIndex = 0;
            // 
            // cboOp
            // 
            this.cboOp.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboOp.FormattingEnabled = true;
            this.cboOp.Items.AddRange(new object[] {
            "=",
            "!=",
            ">",
            "<"});
            this.cboOp.Location = new System.Drawing.Point(144, 9);
            this.cboOp.Name = "cboOp";
            this.cboOp.Size = new System.Drawing.Size(31, 21);
            this.cboOp.TabIndex = 1;
            // 
            // txtCompare
            // 
            this.txtCompare.Location = new System.Drawing.Point(181, 9);
            this.txtCompare.Name = "txtCompare";
            this.txtCompare.Size = new System.Drawing.Size(132, 20);
            this.txtCompare.TabIndex = 2;
            // 
            // btnFilter
            // 
            this.btnFilter.Location = new System.Drawing.Point(157, 35);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(75, 23);
            this.btnFilter.TabIndex = 3;
            this.btnFilter.Text = "Filter";
            this.btnFilter.UseVisualStyleBackColor = true;
            this.btnFilter.Click += new System.EventHandler(this.btnFilter_Click);
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(238, 35);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(75, 23);
            this.btnSearch.TabIndex = 4;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(104, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Value";
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(238, 227);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 7;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(104, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Unkown Value";
            // 
            // cboUnknown
            // 
            this.cboUnknown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboUnknown.FormattingEnabled = true;
            this.cboUnknown.Items.AddRange(new object[] {
            "has changed.",
            "has not changed.",
            "has increased.",
            "has decreased."});
            this.cboUnknown.Location = new System.Drawing.Point(187, 71);
            this.cboUnknown.Name = "cboUnknown";
            this.cboUnknown.Size = new System.Drawing.Size(126, 21);
            this.cboUnknown.TabIndex = 9;
            // 
            // btnUnkFilter
            // 
            this.btnUnkFilter.Location = new System.Drawing.Point(157, 98);
            this.btnUnkFilter.Name = "btnUnkFilter";
            this.btnUnkFilter.Size = new System.Drawing.Size(75, 23);
            this.btnUnkFilter.TabIndex = 10;
            this.btnUnkFilter.Text = "Filter";
            this.btnUnkFilter.UseVisualStyleBackColor = true;
            this.btnUnkFilter.Click += new System.EventHandler(this.btnUnkFilter_Click);
            // 
            // btnUnkSearch
            // 
            this.btnUnkSearch.Location = new System.Drawing.Point(238, 98);
            this.btnUnkSearch.Name = "btnUnkSearch";
            this.btnUnkSearch.Size = new System.Drawing.Size(75, 23);
            this.btnUnkSearch.TabIndex = 11;
            this.btnUnkSearch.Text = "Search";
            this.btnUnkSearch.UseVisualStyleBackColor = true;
            this.btnUnkSearch.Click += new System.EventHandler(this.btnUnkSearch_Click);
            // 
            // CheatFinder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(325, 262);
            this.Controls.Add(this.btnUnkSearch);
            this.Controls.Add(this.btnUnkFilter);
            this.Controls.Add(this.cboUnknown);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.btnFilter);
            this.Controls.Add(this.txtCompare);
            this.Controls.Add(this.cboOp);
            this.Controls.Add(this.lstResults);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "CheatFinder";
            this.ShowIcon = false;
            this.Text = "Cheat Finder";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lstResults;
        private System.Windows.Forms.ComboBox cboOp;
        private System.Windows.Forms.TextBox txtCompare;
        private System.Windows.Forms.Button btnFilter;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboUnknown;
        private System.Windows.Forms.Button btnUnkFilter;
        private System.Windows.Forms.Button btnUnkSearch;
    }
}