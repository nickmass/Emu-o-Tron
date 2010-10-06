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
    public partial class RomInfoBox : Form
    {
        string romInfo;
        public RomInfoBox(string romInfo)
        {
            this.romInfo = romInfo;
            InitializeComponent();
        }

        private void RomInfoBox_Load(object sender, EventArgs e)
        {
            this.infoBox.Text = romInfo;
            this.infoBox.Select(0, 0);
        }
    }
}
