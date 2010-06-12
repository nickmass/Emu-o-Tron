using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DirectXEmu
{
    public partial class ArchiveViewer : Form
    {
        public int selectedFile = -1;
        public ArchiveViewer(string[] fileList)
        {
            InitializeComponent();
            for (int i = 0; i < fileList.Length; i++ )
            {
                this.fileListBox.Items.Add(fileList[i]);
            }
            this.fileListBox.SelectedIndex = 0;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.selectedFile = this.fileListBox.SelectedIndex;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void fileListBox_DoubleClick(object sender, EventArgs e)
        {
            if (this.fileListBox.SelectedIndex != -1)
            {
                this.selectedFile = this.fileListBox.SelectedIndex;
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            }

        }

        private void ArchiveViewer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.fileListBox.SelectedIndex != -1)
                {
                    this.selectedFile = this.fileListBox.SelectedIndex;
                    this.DialogResult = System.Windows.Forms.DialogResult.OK;
                }
            }
        }
    }
}
