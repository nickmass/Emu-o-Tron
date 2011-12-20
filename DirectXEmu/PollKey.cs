using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace DirectXEmu
{
    public partial class PollKey : Form
    {
        public EmuKeys oldKey;
        public EmuKeys newKey;
        IInput input;
        public PollKey(EmuKeys oldKey, string message, string inputMode)
        {
            InitializeComponent();
            switch (inputMode)
            {
                default:
                case "Win":
                    input = new WinInput(this, this);
                    break;
#if !NO_DX
                case "DX":
                    input = new DXInput(this);
                    break;
                case "XIn":
                    input = new XInInput();
                    break;
#endif
                case "Null":
                    input = new NullInput();
                    break;
            }
            lblMessage.Text = message;
            input.Create();
            input.InputEvent += new InputHandler(input_InputEvent);
            input.InputScalerEvent += new InputScalerHandler(input_InputScalerEvent);
            this.oldKey = this.newKey = oldKey;
            tmrPoll.Enabled = true;
        }

        void input_InputScalerEvent(EmuKeys key, int value)
        {
        }

        void input_InputEvent(EmuKeys key, bool pressed)
        {
            if (key != EmuKeys.None && pressed)
            {
                newKey = key;
                DialogResult = System.Windows.Forms.DialogResult.OK;
            }
        }
        private void PollKey_FormClosing(object sender, FormClosingEventArgs e)
        {
            tmrPoll.Enabled = false;
            Thread.Sleep(50);
            input.Destroy();
        }

        private void tmrPoll_Tick(object sender, EventArgs e)
        {
            input.MainLoop();
        }
    }
}
