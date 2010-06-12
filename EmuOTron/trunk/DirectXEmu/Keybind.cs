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
    public partial class Keybind : Form
    {
        public Keybinds keys;
        public Keybind(Keybinds keys)
        {
            this.keys = keys;
            InitializeComponent();
        }

        private void Keybind_Load(object sender, EventArgs e)
        {
            this.bindViewer.SelectedObject = keys;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.keys = (Keybinds)this.bindViewer.SelectedObject;
            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
