using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EmuoTron;

namespace DirectXEmu
{
    public partial class MemoryVis : Form
    {
        private Debug debug;
        private Bitmap buffer = new Bitmap(512,512, PixelFormat.Format32bppArgb);
        private Graphics screenGfx;
        public MemoryVis(Debug debug)
        {
            InitializeComponent();
            this.debug = debug;
            tmrUpdate.Enabled = true;

            screenGfx = visPanel.CreateGraphics();
        }

        private unsafe void tmrUpdate_Tick(object sender, EventArgs e)
        {
            var bmd = buffer.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            uint* ptr = (uint*) bmd.Scan0;

            double time = debug.cpuTime;
            for(int x = 0; x < 256; x++)
            {
                for(int y = 0; y < 256; y++)
                {
                    int address = (y*256) + x;
                    uint color = (uint)(0xFF000000) | (uint)TimeToColor(debug.memoryReads[address]) | (uint)(TimeToColor(debug.memoryWrites[address]) << 8) | (uint)(TimeToColor(debug.memoryExecutes[address]) << 16);
                    ptr[(((y * 2) + 0) * 512) + ((x * 2) + 0)] = color;
                    ptr[(((y * 2) + 0) * 512) + ((x * 2) + 1)] = color;
                    ptr[(((y * 2) + 1) * 512) + ((x * 2) + 0)] = color;
                    ptr[(((y * 2) + 1) * 512) + ((x * 2) + 1)] = color;
                }
            }

            buffer.UnlockBits(bmd);

            screenGfx.DrawImageUnscaled(buffer, 0, 0);
        }

        private byte TimeToColor(long time)
        {
            long age = debug.cpuTime - time;

            if (time == 0)
                return 0x00;
            if (age > 29781 * 100)
                return 0x10;
            age = 2978100 - age;
            return (byte) (((age/2978100.0)*0xEF) + 0x10);
        }

    }
}
