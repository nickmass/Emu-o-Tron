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
    public partial class MemoryViewer : Form
    {
        MemoryStore memory;
        ushort[] mirrormap;
        int bytesPerLine = 16;
        int linesPerPage = 32;
        bool updated = false;
        int max = 0;
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
            this.MouseWheel += new MouseEventHandler(memPane_MouseWheel);

        }
        public void SetMax(int max)
        {
            this.max = max;
            this.scrollBar.Maximum = (max / bytesPerLine) - (linesPerPage / 2);
        }
        void memPane_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
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
                int begin = scrollBar.Value * bytesPerLine;
                int end = begin + (bytesPerLine * linesPerPage);
                if (end > max)
                    end = max;
                byte[] data = new byte[end - begin];
                for (int i = begin; i < end; i++)
                {
                    data[i - begin] = memory[mirrormap[i]];
                }
                memPane.Data = data;
                memPane.StartAddress = begin;
                memPane.BytesPerLine = bytesPerLine;
                memPane.Invalidate();
            }
        }

        private void scrollBar_Scroll(object sender, ScrollEventArgs e)
        {

            int begin = e.NewValue * bytesPerLine;
            int end = begin + (bytesPerLine * linesPerPage);
            if (end > max)
                end = max;
            byte[] data = new byte[end - begin];
            for (int i = begin; i < end; i++)
            {
                data[i - begin] = memory[mirrormap[i]];
            }
            memPane.Data = data;
            memPane.StartAddress = begin;
            memPane.BytesPerLine = bytesPerLine;
            memPane.Invalidate();
            toolTip.SetToolTip(this.scrollBar, "Line: 0x" + begin.ToString("X4"));
            toolTip.ReshowDelay = 0;
            toolTip.AutomaticDelay = 0;
            toolTip.AutoPopDelay = 1000;
            toolTip.InitialDelay = 0;
            toolTip.UseAnimation = false;
            toolTip.UseFading = false;
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
                int begin = scrollBar.Value * bytesPerLine;
                int end = begin + (bytesPerLine * linesPerPage);
                if (end > max)
                    end = max;
                byte[] data = new byte[end - begin];
                for (int i = begin; i < end; i++)
                {
                    data[i - begin] = memory[mirrormap[i]];
                }
                memPane.Data = data;
                memPane.StartAddress = begin;
                memPane.BytesPerLine = bytesPerLine;
                memPane.UpdateBytes();
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
