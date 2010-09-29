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
            try
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
                txtSP.Text = debug.RegS.ToString("X2");
                txtLastExec.Text = debug.LogOp(debug.lastExec);
                lstStack.Items.Clear();
                for (int i = 0x100; i < 0x200; i++)
                {
                    lstStack.Items.Add((i & 0xFF).ToString("X2") + " = " + debug.Peek(i).ToString("X2"));
                }
                lstStack.SelectedIndex = debug.RegS;
                logStart = debug.RegPC;
                UpdateScroll();
                if (debug.debugInterrupt)
                    btnRun.Text = "Run";
                else
                    btnRun.Text = "Pause";
            }
            catch //hack to get around thread safety
            {
                updated = false;
            }
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
                int line = int.Parse(txtSeekTo.Text, System.Globalization.NumberStyles.HexNumber, null);
                if (line >= 0 && line < 0x10000)
                {
                    logStart = line;
                    UpdateScroll();
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException formatEx)
            {
                MessageBox.Show("Seek destination must be a hex integer from 0x0000 to 0xFFFF.");
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
                    logStart = e.NewValue;
                    UpdateScroll();
                }
            }
        }
        Dictionary<int, int> breakpoints = new Dictionary<int,int>();
        private void UpdateBreakpoints()
        {
            breakpoints.Clear();
            lstBreak.Items.Clear();
            foreach(ushort addr in debug.breakRead)
            {
                breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + " Read"), addr);
            }
            foreach (ushort addr in debug.breakWrite)
            {
                breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + " Write"), addr | (1 << 16));
            }
            foreach (ushort addr in debug.breakExecute)
            {
                breakpoints.Add(lstBreak.Items.Add(addr.ToString("X4") + " Execute"), addr | (2 << 16));
            }
        }
        private void btnAddBreak_Click(object sender, EventArgs e)
        {
            AddBreakpoint addBreak = new AddBreakpoint();
            if (addBreak.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                switch(addBreak.type)
                {
                    case 0:
                        debug.AddReadBreakpoint((ushort)addBreak.address);
                        break;
                    case 1:
                        debug.AddWriteBreakpoint((ushort)addBreak.address);
                        break;
                    case 2:
                        debug.AddExecuteBreakpoint((ushort)addBreak.address);
                        break;
                }
                UpdateBreakpoints();
            }
        }

        private void btnRemoveBreak_Click(object sender, EventArgs e)
        {
            if (lstBreak.SelectedIndex != -1)
            {
                if (breakpoints[lstBreak.SelectedIndex] >> 16 == 0)
                    debug.breakRead.Remove((ushort)(breakpoints[lstBreak.SelectedIndex] & 0xFFFF));
                else if (breakpoints[lstBreak.SelectedIndex] >> 16 == 1)
                    debug.breakWrite.Remove((ushort)(breakpoints[lstBreak.SelectedIndex] & 0xFFFF));
                else if (breakpoints[lstBreak.SelectedIndex] >> 16 == 2)
                    debug.breakExecute.Remove((ushort)(breakpoints[lstBreak.SelectedIndex] & 0xFFFF));
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
                    if (breakpoints[lstBreak.SelectedIndex] >> 16 == 0)
                        debug.breakRead.Remove((ushort)(breakpoints[lstBreak.SelectedIndex] & 0xFFFF));
                    else if (breakpoints[lstBreak.SelectedIndex] >> 16 == 1)
                        debug.breakWrite.Remove((ushort)(breakpoints[lstBreak.SelectedIndex] & 0xFFFF));
                    else if (breakpoints[lstBreak.SelectedIndex] >> 16 == 2)
                        debug.breakExecute.Remove((ushort)(breakpoints[lstBreak.SelectedIndex] & 0xFFFF));
                    switch (addBreak.type)
                    {
                        case 0:
                            debug.AddReadBreakpoint((ushort)addBreak.address);
                            break;
                        case 1:
                            debug.AddWriteBreakpoint((ushort)addBreak.address);
                            break;
                        case 2:
                            debug.AddExecuteBreakpoint((ushort)addBreak.address);
                            break;
                    }
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
    }
}
