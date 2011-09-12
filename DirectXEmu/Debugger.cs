using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;

namespace DirectXEmu
{
    public partial class Debugger : Form
    {
        struct Breakpoint
        {
            public bool enabled;
            public int smallAddr;
            public int largeAddr;
            public byte type;
            public int listIndex;
        }
        List<Breakpoint> breakpointsList = new List<Breakpoint>();
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
            chkNMI.Checked = debug.NMIInterrupt;
            chkAPU.Checked = debug.APUInterrupt;
            chkDMC.Checked = debug.DMCInterrupt;
            chkMapper.Checked = debug.MapperInterrupt;
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
            for (int i = 0; i < 31; i++)
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
        private void UpdateBreakpoints()
        {
            lstBreak.Items.Clear();
            for (int i = 0; i < 0x10000; i++)
                debug.breakPoints[i] = 0;
            for(int i = 0; i < breakpointsList.Count; i++)
            {
                Breakpoint bp = breakpointsList[i];
                if (bp.enabled)
                {
                    for (int j = bp.smallAddr; j <= bp.largeAddr; j++)
                    {
                        debug.breakPoints[j] |= bp.type;
                    }
                }
                string typeString = "";
                switch (bp.type)
                {
                    default:
                    case 0:
                        break;
                    case 1:
                        typeString = "R--";
                        break;
                    case 2:
                        typeString = "-W-";
                        break;
                    case 3:
                        typeString = "RW-";
                        break;
                    case 4:
                        typeString = "--X";
                        break;
                    case 5:
                        typeString = "R-X";
                        break;
                    case 6:
                        typeString = "-WX";
                        break;
                    case 7:
                        typeString = "RWX";
                        break;
                }
                if (bp.smallAddr != bp.largeAddr)
                    bp.listIndex = lstBreak.Items.Add((bp.enabled? "+ " : "- ") + bp.smallAddr.ToString("X4") + "-" + bp.largeAddr.ToString("X4") + "\t" + typeString);
                else
                    bp.listIndex = lstBreak.Items.Add((bp.enabled? "+ " : "- ") + bp.smallAddr.ToString("X4") + "     \t" + typeString);
                breakpointsList[i] = bp;
            }
        }
        private void btnAddBreak_Click(object sender, EventArgs e)
        {
            AddBreakpoint addBreak = new AddBreakpoint();
            if (addBreak.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Breakpoint newBreak;
                newBreak.enabled = addBreak.enabled;
                newBreak.type = addBreak.type;
                newBreak.smallAddr = Math.Min(addBreak.break1, addBreak.break2);
                newBreak.largeAddr = Math.Max(addBreak.break1, addBreak.break2);
                newBreak.listIndex = -1;
                breakpointsList.Add(newBreak);
                UpdateBreakpoints();
            }
        }

        private void btnRemoveBreak_Click(object sender, EventArgs e)
        {
            if (lstBreak.SelectedIndex != -1)
            {
                for (int i = 0; i < breakpointsList.Count; i++)
                {
                    if (breakpointsList[i].listIndex == lstBreak.SelectedIndex)
                    {
                        breakpointsList.RemoveAt(i);
                        break;
                    }
                }
                UpdateBreakpoints();
            }
        }

        private void btnEditBreak_Click(object sender, EventArgs e)
        {
            if (lstBreak.SelectedIndex != -1)
            {
                Breakpoint bp;
                bp.enabled = true;
                bp.type = 0;
                bp.smallAddr = 0;
                bp.largeAddr = 0;
                bp.listIndex = -1;
                int oldLoc = -1;
                for (int i = 0; i < breakpointsList.Count; i++)
                {
                    if (breakpointsList[i].listIndex == lstBreak.SelectedIndex)
                    {
                        oldLoc = i;
                        break;
                    }
                }
                bp = breakpointsList[oldLoc];
                AddBreakpoint addBreak = new AddBreakpoint(bp.enabled, bp.type, bp.smallAddr, bp.largeAddr);
                if (addBreak.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    breakpointsList.RemoveAt(oldLoc);
                    Breakpoint newBreak;
                    newBreak.enabled = addBreak.enabled;
                    newBreak.type = addBreak.type;
                    newBreak.smallAddr = Math.Min(addBreak.break1, addBreak.break2);
                    newBreak.largeAddr = Math.Max(addBreak.break1, addBreak.break2);
                    newBreak.listIndex = -1;
                    breakpointsList.Add(newBreak);
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
