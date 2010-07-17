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
            pageMaker = new StringBuilder();
        }
        public void SetMax(int max)
        {
            this.max = max;
            this.scrollBar.Maximum = (max / bytesPerLine) - (linesPerPage / 2);
        }
        void memPane_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (scrollBar.Value + ((e.Delta / 120) * -3) > 0)
                    scrollBar.Value += ((e.Delta / 120) * -3);
                else
                    scrollBar.Value = 0;
            }
            else
            {

                if (scrollBar.Value + ((e.Delta / 120) * -3) < scrollBar.Maximum - ((linesPerPage /2)-1))
                    scrollBar.Value += ((e.Delta / 120) * -3);
                else
                    scrollBar.Value = scrollBar.Maximum - ((linesPerPage / 2)-1);
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
                    pageMaker.Append("0x" + i.ToString("X4") + ": ");
                }
                pageMaker.Append(memory[i].ToString("X2") + " ");
            }
            memPane.Text = pageMaker.ToString();
        }
        public void updateMemory(MemoryStore memory)
        {
            this.memory = memory;
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
                        pageMaker.Append("0x" + i.ToString("X4") + ": ");
                    }
                    pageMaker.Append(memory[i].ToString("X2") + " ");
                }
                int selStart = memPane.SelectionStart;
                int selLength = memPane.SelectionLength;
                memPane.Text = pageMaker.ToString();
                memPane.SelectionStart = selStart;
                memPane.SelectionLength = selLength;
            }
        }

    }
}
