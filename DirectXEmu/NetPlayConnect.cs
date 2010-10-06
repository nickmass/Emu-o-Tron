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
    public partial class NetPlayConnect : Form
    {
        public string nick;
        public string ip;
        public NetPlayConnect()
        {
            InitializeComponent();
        }
        public NetPlayConnect(string defaultIp)
        {
            InitializeComponent();
            txtIP.Text = defaultIp;
            txtIP.Enabled = false;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            nick = txtName.Text;
            ip = txtIP.Text;
            if (nick.Length > 0 && nick.Length < 25)
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
