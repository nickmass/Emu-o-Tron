using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
namespace DirectXEmu
{
    class Scale2x : Scaler
    {
        public override bool resizeable
        {
            get { return this.resize; }
        }
        public override int xSize
        {
            get { return this.x; }
        }
        public override int ySize
        {
            get { return this.y; }
        }
        public override bool maintainAspectRatio
        {
            get { return this.maintainAR; }
        }
        public Scale2x()
        {
            this.x = 512;
            this.y = 480;
            this.resize = false;
            this.maintainAR = true;
        }
        public override unsafe void PerformScale(int* origPixels, int* resizePixels)
        {
            int b, d, e, f, h;
            int e0, e1, e2, e3;
            for (int imgY = 0; imgY < 240; imgY++)
            {
                for (int imgX = 0; imgX < 256; imgX++)
                {
                    e = origPixels[(imgY * 256) + imgX];
                    if (imgY == 0)
                        b = e;
                    else
                        b = origPixels[((imgY - 1) * 256) + imgX];
                    if (imgX == 0)
                        d = e;
                    else
                        d = origPixels[((imgY) * 256) + (imgX - 1)];
                    if (imgX == 255)
                        f = e;
                    else
                        f = origPixels[((imgY) * 256) + (imgX + 1)];
                    if (imgY == 239)
                        h = e;
                    else
                        h = origPixels[((imgY + 1) * 256) + (imgX)];
                    if (b != h && d != f)
                    {
                        e0 = d == b ? d : e;
                        e1 = b == f ? f : e;
                        e2 = d == h ? d : e;
                        e3 = h == f ? f : e;
                    }
                    else
                    {
                        e0 = e;
                        e1 = e;
                        e2 = e;
                        e3 = e;
                    }
                    resizePixels[((imgY * 2) * (256 * 2)) + (imgX * 2)] = e0;
                    resizePixels[((imgY * 2) * (256 * 2)) + ((imgX * 2) + 1)] = e1;
                    resizePixels[(((imgY * 2) + 1) * (256 * 2)) + (imgX * 2)] = e2;
                    resizePixels[(((imgY * 2) + 1) * (256 * 2)) + ((imgX * 2) + 1)] = e3;
                }
            }
        }
    }
}
