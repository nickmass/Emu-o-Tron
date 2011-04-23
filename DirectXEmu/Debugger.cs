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
    public partial class Debugger : Form
    {
        EmuoTron.Debug debug;
        public bool updated = false;
        public bool smartUpdate = false;
        private int logStart;
        public Debugger(EmuoTron.Debug debug)
        {
            this.debug = debug;
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }
        private void btnOk_Click(object sender, EventArgs e)
        {
            updated = true;
            this.Hide();
        }
        public void UpdateDebug()
        {
            updated = true;
            chkCarry.Checked = debug.FlagCarry;
            chkZero.Checked = debug.FlagZero;
            chkInterrupt.Checked = debug.FlagIRQ;
            chkDecimal.Checked = debug.FlagDecimal;
            chkBreak.Checked = debug.FlagBreak;
            chkUnused.Checked = debug.FlagNotUsed;
            chkOverflow.Checked = debug.FlagOverflow;
            chkNegative.Checked = debug.FlagSign;
            txtPC.Text = debug.RegPC.ToString("X4");
            txtA.Text = debug.RegA.ToString("X2");
            txtX.Text = debug.RegX.ToString("X2");
            txtY.Text = debug.RegY.ToString("X2");
            txtS.Text = debug.RegS.ToString("X2");
            txtLastExec.Text = debug.LogOp(debug.lastExec);
            txtScanline.Text = debug.Scanline.ToString();
            txtCycle.Text = debug.Cycle.ToString();
            lstStack.Items.Clear();
            string[] stackItems = new string[0x100];
            for (int i = 0x00; i < 0x100; i++)
            {
                stackItems[i] = i.ToString("X2") + " = " + debug.Peek(i | 0x100).ToString("X2");
            }
            lstStack.Items.AddRange(stackItems);
            lstStack.SelectedIndex = debug.RegS;
            logStart = debug.RegPC;
            UpdateScroll();
            if (debug.debugInterrupt)
                btnRun.Text = "Run";
            else
                btnRun.Text = "Pause";
        }

        private void btnStep_Click(object sender, EventArgs e)
        {
            debug.StepCycles(1);
            debug.debugInterrupt = false;
            updated = false;
        }

        private void btnRunFor_Click(object sender, EventArgs e)
        {
            try
            {
                debug.StepCycles(Convert.ToInt32(txtRunFor.Text));
                debug.debugInterrupt = false;
                updated = false;
            }
            catch (FormatException formatEx)
            {
                MessageBox.Show("Number of cycles to step must be an integer.");
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            debug.irqEnable = false;
            debug.debugInterrupt = !debug.debugInterrupt;
            if (debug.debugInterrupt)
                btnRun.Text = "Run";
            else
                btnRun.Text = "Pause";
            updated = false;
        }

        private void btnSeekPC_Click(object sender, EventArgs e)
        {
            txtLastExec.Text = debug.LogOp(debug.lastExec);
            logStart = debug.RegPC;
            UpdateScroll();
        }

        private void btnSeekTo_Click(object sender, EventArgs e)
        {
            try
            {
                int line = int.Parse(txtSeekTo.Text, System.Globalization.NumberStyles.HexNumber);
                if (line >= 0 && line < 0x10000)
                {
                    logStart = line;
                    UpdateScroll();
                }
                else
                {
                    throw new Exception("Address out of Range");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Address out of Range")
                    MessageBox.Show("Seek destination must be a hex integer from 0x0000 to 0xFFFF.");
                else
                    throw ex;
            }

        }
        private void UpdateScroll()
        {
            scrlLog.Value = logStart;
            StringBuilder log = new StringBuilder();
            int j = logStart;
            for (int i = 0; i < 27; i++)
            {
                if (j < 0xFFFF)
                {
                    log.AppendLine(debug.LogOp(j));
                    j += debug.OpSize(j);
                }
            }
            txtLog.Text = log.ToString().Trim();
        }
        private void scrlLog_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                if (e.Type == ScrollEventType.SmallIncrement)
                {

                    if (logStart < 0xFFFF)
                    {
                        logStart += debug.OpSize(logStart);
                        UpdateScroll();
                    }
                    else
                    {
                        logStart = 0xFFFF;
                        UpdateScroll();
                    }
                }
                else if(e.Type == ScrollEventType.SmallDecrement)
                {
                    if (logStart > 0)
                    {
                        logStart--;
                        UpdateScroll();
                    }
                    else
                    {
                        logStart = 0;
                        UpdateScroll();
                    }
                }
                else
                {
                    if (e.NewValue < 0xFFE4)
                        logStart = e.NewValue;
                    else
                        logStart = 0xFFE4;
                    UpdateScroll();
                }
            }
        }
        Dictionary<int, int> breakpoints = new Dictionary<int,int>();
        private void UpdateBreakpoints()
        {
            breakpoints.Clear();
            lstBreak.Items.Clear();
            for (int addr = 0; addr < 0x10000; addr++)
            {
                switch(debug.breakPoints[addr])
                {
                    default:
                    case 0:
                        break;
                    case 1:
                        breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + "\tR--"), (1 << 16) | addr);
                        break;
                    case 2:
                        breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + "\t-W-"), (2 << 16) | addr);
                        break;
                    case 3:
                        breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + "\tRW-"), (3 << 16) | addr);
                        break;
                    case 4:
                        breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + "\t--X"), (4 << 16) | addr);
                        break;
                    case 5:
                        breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + "\tR-X"), (5 << 16) | addr);
                        break;
                    case 6:
                        breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + "\t-WX"), (6 << 16) | addr);
                        break;
                    case 7:
                        breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + "\tRWX"), (7 << 16) | addr);
                        break;
                }
            }
        }
        private void btnAddBreak_Click(object sender, EventArgs e)
        {
            AddBreakpoint addBreak = new AddBreakpoint();
            if (addBreak.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                debug.breakPoints[addBreak.address & 0xFFFF] = (byte)(addBreak.type | debug.breakPoints[addBreak.address & 0xFFFF]);
                UpdateBreakpoints();
            }
        }

        private void btnRemoveBreak_Click(object sender, EventArgs e)
        {
            if (lstBreak.SelectedIndex != -1)
            {
                debug.breakPoints[breakpoints[lstBreak.SelectedIndex] & 0xFFFF] = 0;
                UpdateBreakpoints();
            }

        }

        private void btnEditBreak_Click(object sender, EventArgs e)
        {
            if (lstBreak.SelectedIndex != -1)
            {
                AddBreakpoint addBreak = new AddBreakpoint(breakpoints[lstBreak.SelectedIndex] >> 16, breakpoints[lstBreak.SelectedIndex] & 0xFFFF);
                if (addBreak.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    debug.breakPoints[breakpoints[lstBreak.SelectedIndex] & 0xFFFF] = 0;
                    debug.breakPoints[addBreak.address & 0xFFFF] = (byte)(addBreak.type | debug.breakPoints[addBreak.address & 0xFFFF]);
                    UpdateBreakpoints();
                }
            }
        }

        private void Debugger_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void chkSpriteHit_CheckedChanged(object sender, EventArgs e)
        {
            debug.breakOnSpriteZeroHit = ((CheckBox)sender).Checked;
        }

        private void chkSpriteOverflow_CheckedChanged(object sender, EventArgs e)
        {
            debug.breakOnSpriteOverflow = ((CheckBox)sender).Checked;
        }
    }
}
