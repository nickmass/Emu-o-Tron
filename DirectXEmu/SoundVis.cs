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
    public partial class SoundVis : Form
    {
        Bitmap screenBuffer = new Bitmap(384, 256, PixelFormat.Format32bppArgb);
        private Graphics screenGfx;
        private uint[] chanColors;

        private const uint blue = 0xFF3333FF;
        private const uint darkBlue = 0xFF3333FE;
        private const uint grey = 0xFF333333;


        private NESCore nes;

        public SoundVis()
        {
            InitializeComponent();
            screenGfx = pnlScreen.CreateGraphics();
            chanColors = new uint[6];
            for (int i = 0; i < 6; i++)
                chanColors[i] = blue;
        }

        public unsafe void Update(NESCore nes)
        {
            this.nes = nes;
            if (nes.APU.volume.square1 != 0)
                chanColors[0] = blue;
            else
                chanColors[0] = grey;
            if (nes.APU.volume.square2 != 0)
                chanColors[1] = blue;
            else
                chanColors[1] = grey;
            if (nes.APU.volume.triangle != 0)
                chanColors[2] = blue;
            else
                chanColors[2] = grey;
            if (nes.APU.volume.noise != 0)
                chanColors[3] = blue;
            else
                chanColors[3] = grey;
            if (nes.APU.volume.dmc != 0)
                chanColors[4] = blue;
            else
                chanColors[4] = grey;
            if (nes.APU.volume.external != 0)
                chanColors[5] = blue;
            else
                chanColors[5] = grey;

            int blueCount = 0;
            int blueItem = -1;
            for(int i = 0; i < 6; i++)
                if(chanColors[i] == blue)
                {
                    blueItem = i;
                    blueCount++;
                }
            if (blueCount == 1)
                chanColors[blueItem] = darkBlue;

            var bmd = screenBuffer.LockBits(new Rectangle(0, 0, 384, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            uint* ptr = (uint*) bmd.Scan0;
            for (int i = 0; i < 384 * 256; i++)
                ptr[i] = 0xFF000000;
            Point lastPoint;
            for (int channel = 0; channel < 6; channel++)
            {
                switch (channel)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        byte[] volume;
                        switch (channel)
                        {
                            default:
                            case 0:
                                volume = nes.debug.square1History;
                                break;
                            case 1:
                                volume = nes.debug.square2History;
                                break;
                            case 2:
                                volume = nes.debug.triangleHistory;
                                break;
                            case 3:
                                volume = nes.debug.noiseHistory;
                                break;
                        }
                        lastPoint = new Point(-1,0);
                        for (int x = 0; x < 128; x++)
                        {
                            Point newPoint = new Point(x + ((channel % 3) * 128), ((channel / 3) * 128) + ((15 - volume[x]) * 8));

                            if (channel != 2)
                            {
                                if (lastPoint.X != -1)
                                {
                                    DrawLine(lastPoint.X + 1, lastPoint.Y, newPoint.X, newPoint.Y, ptr,
                                                chanColors[channel]);
                                }
                                else
                                {
                                    DrawPixel(ptr, newPoint.X, newPoint.Y, chanColors[channel]);
                                }
                            }
                            else if (lastPoint.X != -1)
                            {
                                DrawLine(lastPoint.X, lastPoint.Y, newPoint.X, newPoint.Y, ptr,
                                         chanColors[channel]);
                            }
                            lastPoint = newPoint;
                        }
                        break;
                    case 4:
                        lastPoint = new Point(-1, 0);
                        for (int x = 0; x < 128; x++)
                        {
                            Point newPoint = new Point(x + ((channel % 3) * 128), ((channel / 3) * 128) + ((127 - nes.debug.dmcHistory[x])));
                            if (lastPoint.X != -1)
                            {
                                DrawLine(lastPoint.X, lastPoint.Y, newPoint.X, newPoint.Y, ptr, chanColors[channel]);
                            }
                            lastPoint = newPoint;
                        }
                        break;
                    case 5:
                        lastPoint = new Point(-1, 0);
                        for (int x = 0; x < 128; x++)
                        {
                            Point newPoint = new Point(x + ((channel % 3) * 128), ((channel / 3) * 128) + ((127 - (nes.debug.externalHistory[x] >> 1))));
                            if (lastPoint.X != -1)
                            {
                                DrawLine(lastPoint.X, lastPoint.Y, newPoint.X, newPoint.Y, ptr, chanColors[channel]);
                            }
                            lastPoint = newPoint;
                        }
                        break;
                }
            }
            screenBuffer.UnlockBits(bmd);
            screenGfx.DrawImageUnscaled(screenBuffer, 0,0);
        }

        private int frame;
        public void DumpImage()
        {
            screenBuffer.Save("soundDump"+ frame.ToString("D4")+".bmp", ImageFormat.Bmp);
            frame++;
        }

        private static unsafe void DrawLine(int x0, int y0, int x1, int y1, uint* ptr, uint color)
        {
            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);

            int sx;
            int sy;

            if (x0 < x1)
                sx = 1;
            else
                sx = -1;
            if (y0 < y1)
                sy = 1;
            else
                sy = -1;

            var err = dx - dy;

            while (true)
            {
                DrawPixel(ptr, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                    break;
                var e2 = err * 2;
                if (e2 > dy * -1)
                {
                    err = err - dy;
                    x0 += sx;
                }
                if (e2 >= dx) continue;
                err = err + dx;
                y0 += sy;
            }
        }
        private static unsafe void DrawPixel(uint* ptr, int x, int y, uint color)
        {
            ptr[(y*384) + x] = color;
        }

        private void pnlScreen_MouseUp(object sender, MouseEventArgs e)
        {
            if(!this.Focused)
                return;
            int channel = e.X/128+ ((e.Y/128) * 3);
            switch (chanColors[channel])
            {
                case darkBlue:
                    for (int i = 0; i < 6; i++)
                        chanColors[i] = blue;
                    nes.APU.volume.square1 = 1;
                    nes.APU.volume.square2 = 1;
                    nes.APU.volume.triangle = 1;
                    nes.APU.volume.noise = 1;
                    nes.APU.volume.dmc = 1;
                    nes.APU.volume.external = 1;
                    break;
                case grey:
                case blue:
                    for (int i = 0; i < 6; i++)
                        chanColors[i] = grey;
                    chanColors[channel] = darkBlue;
                    nes.APU.volume.square1 = 0;
                    nes.APU.volume.square2 = 0;
                    nes.APU.volume.triangle = 0;
                    nes.APU.volume.noise = 0;
                    nes.APU.volume.dmc = 0;
                    nes.APU.volume.external = 0;
                    switch (channel)
                    {
                        case 0:
                            nes.APU.volume.square1 = 1;
                            break;
                        case 1:
                            nes.APU.volume.square2 = 1;
                            break;
                        case 2:
                            nes.APU.volume.triangle = 1;
                            break;
                        case 3:
                            nes.APU.volume.noise = 1;
                            break;
                        case 4:
                            nes.APU.volume.dmc = 1;
                            break;
                        case 5:
                            nes.APU.volume.external = 1;
                            break;
                    }
                    break;

            }
        }
    }
}
