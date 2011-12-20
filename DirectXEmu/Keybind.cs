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
        public bool filterIllegalInput;
        public ControllerType portOne;
        public ControllerType portTwo;
        public ControllerType expansion;

        private string inputMode;

        public Keybind(Keybinds keys, ControllerType portOne, ControllerType portTwo, ControllerType expansion, bool fourScore, bool filterIllegalInput, string inputMode)
        {
            this.keys = keys;
            this.fourScore = fourScore;
            this.portOne = portOne;
            this.portTwo = portTwo;
            this.expansion = expansion;
            this.inputMode = inputMode;
            InitializeComponent();
            chkFourScore.Checked = fourScore;
            chkFilter.Checked = filterIllegalInput;
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
                default:
                case ControllerType.Empty:
                    cboPortOne.SelectedIndex = 3;
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
                default:
                case ControllerType.Empty:
                    cboPortTwo.SelectedIndex = 3;
                    break;
            }
            switch (expansion)
            {
                case ControllerType.FamiPaddle:
                    cboExpansion.SelectedIndex = 0;
                    break;
                default:
                case ControllerType.Empty:
                    cboExpansion.SelectedIndex = 1;
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
                    portTwo = ControllerType.Empty;
                    break;
            }

        }

        private void chkFourScore_CheckedChanged(object sender, EventArgs e)
        {
            fourScore = chkFourScore.Checked;
        }

        private void chkFilter_CheckedChanged(object sender, EventArgs e)
        {
            filterIllegalInput = chkFilter.Checked;
        }

        private void cboExpansion_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboExpansion.SelectedIndex)
            {
                case 0:
                    expansion = ControllerType.FamiPaddle;
                    break;
                case 1:
                    expansion = ControllerType.Empty;
                    break;
            }
        }

        private void btnGamepad1_Click(object sender, EventArgs e)
        {
            PollKey pollKey;
            Keybinds newKeys = ((Keybinds)bindViewer.SelectedObject);
            pollKey = new PollKey(newKeys.Player1Up, "Up", inputMode);
            pollKey.ShowDialog();
            newKeys.Player1Up = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player1Down, "Down", inputMode);
            pollKey.ShowDialog();
            newKeys.Player1Down = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player1Left, "Left", inputMode);
            pollKey.ShowDialog();
            newKeys.Player1Left = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player1Right, "Right", inputMode);
            pollKey.ShowDialog();
            newKeys.Player1Right = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player1A, "A button", inputMode);
            pollKey.ShowDialog();
            newKeys.Player1A = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player1B, "B button", inputMode);
            pollKey.ShowDialog();
            newKeys.Player1B = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player1Select, "Select button", inputMode);
            pollKey.ShowDialog();
            newKeys.Player1Select = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player1Start, "Start button", inputMode);
            pollKey.ShowDialog();
            newKeys.Player1Start = pollKey.newKey;
            bindViewer.SelectedObject = newKeys;
        }

        private void btnGamepad2_Click(object sender, EventArgs e)
        {
            PollKey pollKey;
            Keybinds newKeys = ((Keybinds)bindViewer.SelectedObject);
            pollKey = new PollKey(newKeys.Player2Up, "Up", inputMode);
            pollKey.ShowDialog();
            newKeys.Player2Up = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player2Down, "Down", inputMode);
            pollKey.ShowDialog();
            newKeys.Player2Down = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player2Left, "Left", inputMode);
            pollKey.ShowDialog();
            newKeys.Player2Left = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player2Right, "Right", inputMode);
            pollKey.ShowDialog();
            newKeys.Player2Right = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player2A, "A button", inputMode);
            pollKey.ShowDialog();
            newKeys.Player2A = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player2B, "B button", inputMode);
            pollKey.ShowDialog();
            newKeys.Player2B = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player2Select, "Select button", inputMode);
            pollKey.ShowDialog();
            newKeys.Player2Select = pollKey.newKey;
            pollKey = new PollKey(newKeys.Player2Start, "Start button", inputMode);
            pollKey.ShowDialog();
            newKeys.Player2Start = pollKey.newKey;
            bindViewer.SelectedObject = newKeys;
        }
    }
}
