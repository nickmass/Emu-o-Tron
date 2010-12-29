using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EmuoTron;

namespace DirectXEmu
{
    public partial class Keybind : Form
    {
        public Keybinds keys;
        public bool fourScore;
        public ControllerType portOne;
        public ControllerType portTwo;

        public Keybind(Keybinds keys, ControllerType portOne, ControllerType portTwo, bool fourScore)
        {
            this.keys = keys;
            this.fourScore = fourScore;
            this.portOne = portOne;
            this.portTwo = portTwo;
            InitializeComponent();
            chkFourScore.Checked = fourScore;
            switch (portOne)
            {
                case ControllerType.Controller:
                    cboPortOne.SelectedIndex = 0;
                    break;
                case ControllerType.Zapper:
                    cboPortOne.SelectedIndex = 1;
                    break;
                case ControllerType.Paddle:
                    cboPortOne.SelectedIndex = 2;
                    break;
                case ControllerType.FamiPaddle:
                    cboPortOne.SelectedIndex = 3;
                    break;
                default:
                case ControllerType.Empty:
                    cboPortOne.SelectedIndex = 4;
                    break;
            }
            switch (portTwo)
            {
                case ControllerType.Controller:
                    cboPortTwo.SelectedIndex = 0;
                    break;
                case ControllerType.Zapper:
                    cboPortTwo.SelectedIndex = 1;
                    break;
                case ControllerType.Paddle:
                    cboPortTwo.SelectedIndex = 2;
                    break;
                case ControllerType.FamiPaddle:
                    cboPortTwo.SelectedIndex = 3;
                    break;
                default:
                case ControllerType.Empty:
                    cboPortTwo.SelectedIndex = 4;
                    break;
            }

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

        private void cboPortOne_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboPortOne.SelectedIndex)
            {
                case 0:
                    portOne = ControllerType.Controller;
                    break;
                case 1:
                    portOne = ControllerType.Zapper;
                    break;
                case 2:
                    portOne = ControllerType.Paddle;
                    break;
                case 3:
                    portOne = ControllerType.FamiPaddle;
                    break;
                case 4:
                    portOne = ControllerType.Empty;
                    break;
            }

        }

        private void cboPortTwo_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboPortTwo.SelectedIndex)
            {
                case 0:
                    portTwo = ControllerType.Controller;
                    break;
                case 1:
                    portTwo = ControllerType.Zapper;
                    break;
                case 2:
                    portTwo = ControllerType.Paddle;
                    break;
                case 3:
                    portTwo = ControllerType.FamiPaddle;
                    break;
                case 4:
                    portTwo = ControllerType.Empty;
                    break;
            }

        }

        private void chkFourScore_CheckedChanged(object sender, EventArgs e)
        {
            fourScore = chkFourScore.Checked;
        }
    }
}
