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
    public partial class AddBreakpoint : Form
    {
        public bool enabled;
        public int break1;
        public int break2;
        public byte type;
        public AddBreakpoint()
        {
            InitializeComponent();
        }
        public AddBreakpoint(bool enabled, int type, int break1, int break2)
        {
            InitializeComponent();
            chkEnable.Checked = enabled;
            if ((type & 1) != 0)
                chkRead.Checked = true;
            if ((type & 2) != 0)
                chkWrite.Checked = true;
            if ((type & 4) != 0)
                chkExecute.Checked = true;
            txtBreak1.Text = break1.ToString("X4");
            txtBreak1.Focus();
            if(break1 != break2)
                txtBreak2.Text = break2.ToString("X4");
        }

        private void btnOk_Click(object sender, EventArgs e)
        {

            try
            {
                enabled = chkEnable.Checked;
                break1 = int.Parse(txtBreak1.Text, System.Globalization.NumberStyles.HexNumber);
                if (!(break1 >= 0 && break1 < 0x10000))
                    throw new Exception("Bad Address");
                if (txtBreak2.Text == "")
                    break2 = break1;
                else
                {
                    break2 = int.Parse(txtBreak2.Text, System.Globalization.NumberStyles.HexNumber);
                    if (!(break2 >= 0 && break2 < 0x10000))
                        throw new Exception("Bad Address");
                }
                type = 0;
                if (chkRead.Checked)
                    type |= 1;
                if (chkWrite.Checked)
                    type |= 2;
                if (chkExecute.Checked)
                    type |= 4;
                if (type == 0)
                    throw new Exception("No Breakpoint");
                DialogResult = System.Windows.Forms.DialogResult.OK;
            }
            catch (FormatException fex)
            {
                MessageBox.Show("Break address must be a hex integer from 0x0000 to 0xFFFF.");
            }
            catch (Exception ex)
            {
                if (ex.Message == "Bad Address")
                    MessageBox.Show("Break address must be a hex integer from 0x0000 to 0xFFFF.");
                else if (ex.Message == "No Breakpoint")
                    MessageBox.Show("Select a breakpoint type.");
                else
                    throw ex;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void AddBreakpoint_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnOk_Click(this, new EventArgs());
            }
        }
    }
}
