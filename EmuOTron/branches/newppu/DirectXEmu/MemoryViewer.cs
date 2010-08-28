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
    public partial class MemoryViewer : Form
    {
        MemoryStore memory;
        ushort[] mirrormap;
        int bytesPerLine = 16;
        int linesPerPage = 32;
        bool updated = false;
        int max = 0;
        StringBuilder pageMaker;
        public MemoryViewer()
        {
            InitializeComponent();
            this.scrollBar.Maximum = (0x8000 / bytesPerLine) - (linesPerPage / 2);
            this.scrollBar.SmallChange = 1;
            this.scrollBar.LargeChange = linesPerPage / 2;
            memPane.MouseWheel += new MouseEventHandler(memPane_MouseWheel);
            toolTip.SetToolTip(this.scrollBar, "Line: 0x" + 0.ToString("X4"));
            toolTip.ReshowDelay = 0;
            toolTip.AutomaticDelay = 0;
            toolTip.AutoPopDelay = 1000;
            toolTip.InitialDelay = 0;
            toolTip.UseAnimation = false;
            toolTip.UseFading = false;
            pageMaker = new StringBuilder();
        }
        public void SetMax(int max)
        {
            this.max = max;
            this.scrollBar.Maximum = (max / bytesPerLine) - (linesPerPage / 2);
        }
        void memPane_MouseWheel(object sender, MouseEventArgs e)
        {
            int change = (e.Delta / 120) * -3;
            int newValue = scrollBar.Value + change;
            int oldValue = scrollBar.Value;
            if (change > 0 && newValue > (scrollBar.Maximum - ((linesPerPage / 2) - 1)))
            {
                scrollBar.Value = (scrollBar.Maximum - ((linesPerPage / 2) - 1));
                change = scrollBar.Value - oldValue;
            }
            else if (change < 0 && newValue < 0)
            {
                scrollBar.Value = 0;
                change = scrollBar.Value - oldValue;
            }
            else
            {
                scrollBar.Value = newValue;
            }
            int scrollChange = (memPane.SelectionStart - (change * 58));
            if (scrollChange >= 0)
            {
                memPane.SelectionStart = scrollChange;
            }
            else
            {
                memPane.SelectionLength = (memPane.SelectionLength + scrollChange >= 0) ? memPane.SelectionLength + scrollChange : 0;
                memPane.SelectionStart = 0;
            }
        }

        private void scrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            pageMaker.Remove(0, pageMaker.Length);
            int begin = scrollBar.Value * bytesPerLine;
            int end = begin + (bytesPerLine * linesPerPage);
            for (int i = begin; i < end && i < max; i++)
            {
                if (i % bytesPerLine == 0)
                {
                    if (i != begin)
                        pageMaker.Append("\r\n");
                    else
                    {
                        toolTip.SetToolTip(this.scrollBar, "Line: 0x" + i.ToString("X4"));
                        toolTip.ReshowDelay = 0;
                        toolTip.AutomaticDelay = 0;
                        toolTip.AutoPopDelay = 1000;
                        toolTip.InitialDelay = 0;
                        toolTip.UseAnimation = false;
                        toolTip.UseFading = false;
                    }
                    pageMaker.Append("0x" + i.ToString("X4") + ": ");
                }
                pageMaker.Append(memory[mirrormap[i]].ToString("X2") + " ");
            }
            memPane.Text = pageMaker.ToString();
        }
        public void updateMemory(MemoryStore memory, ushort[] mirrorMap)
        {
            this.memory = memory;
            this.mirrormap = mirrorMap;
            this.updated = true;
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (updated)
            {
                pageMaker.Remove(0, pageMaker.Length);
                int begin = scrollBar.Value * bytesPerLine;
                int end = begin + (bytesPerLine * linesPerPage);
                for (int i = begin; i < end && i < max; i++)
                {
                    if (i % bytesPerLine == 0)
                    {
                        if (i != begin)
                            pageMaker.Append("\r\n");
                        else
                        {
                            toolTip.SetToolTip(this.scrollBar, "Line: 0x" + i.ToString("X4"));
                        }
                        pageMaker.Append("0x" + i.ToString("X4") + ": ");
                    }
                    pageMaker.Append(memory[mirrormap[i]].ToString("X2") + " ");
                }
                int selStart = memPane.SelectionStart;
                int selLength = memPane.SelectionLength;
                memPane.Text = pageMaker.ToString();
                memPane.SelectionStart = selStart;
                memPane.SelectionLength = selLength;
            }
        }

        private void scrollBar_MouseLeave(object sender, EventArgs e)
        {
            toolTip.RemoveAll();
        }
        int cursorAddr;
        int cursorValue;
        private void memPane_MouseMove(object sender, MouseEventArgs e)
        {
            int x = e.X - 55;
            int y = e.Y;
            if (x >= 0 && y >= 0)
            {
                int charX = x / 21;
                int charY = y / 14;
                if (charX < bytesPerLine && charY < linesPerPage)
                {
                    cursorAddr = (scrollBar.Value * bytesPerLine) + (charY * bytesPerLine) + charX;
                    if (cursorAddr >= 0 && cursorAddr < max && this.memory != null && this.mirrormap != null)
                    {
                        cursorValue = memory[mirrormap[cursorAddr]];
                        txtCursorAddr.Text = "Addr: 0x" + cursorAddr.ToString("X4");
                        txtCursorValue.Text = "Value: " + cursorValue.ToString("X2");
                        txtCursorAddrDec.Text = "Dec Addr: " + cursorAddr.ToString();
                        txtCursorValueDec.Text = "Dec Value: " + cursorValue.ToString();
                    }

                }
            }
        }
    }
}
