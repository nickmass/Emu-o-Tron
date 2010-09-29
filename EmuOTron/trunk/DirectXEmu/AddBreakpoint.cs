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
        public int address;
        public int type;
        public AddBreakpoint()
        {
            InitializeComponent();
        }
        public AddBreakpoint(int type, int address)
        {
            InitializeComponent();
            if (type == 0)
                radRead.Checked = true;
            else if (type == 1)
                radWrite.Checked = true;
            else if (type == 2)
                radExec.Checked = true;
            txtAddress.Text = address.ToString("X4");
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            
            try
            {
                address = int.Parse(txtAddress.Text, System.Globalization.NumberStyles.HexNumber, null);
                if (!(address >= 0 && address < 0x10000))
                    throw new FormatException();
                if (radRead.Checked)
                    type = 0;
                else if (radWrite.Checked)
                    type = 1;
                else if (radExec.Checked)
                    type = 2;
                else
                    throw new Exception();
                DialogResult = System.Windows.Forms.DialogResult.OK;
            }
            catch (FormatException formatEx)
            {
                MessageBox.Show("Seek destination must be a hex integer from 0x0000 to 0xFFFF.");
            }
            catch (Exception radioException)
            {
                MessageBox.Show("Select a breakpoint type.");
            }

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
