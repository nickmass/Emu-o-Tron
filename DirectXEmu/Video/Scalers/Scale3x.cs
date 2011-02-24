using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace DirectXEmu
{
    class Scale3x : Scaler
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
        public Scale3x()
        {
            this.x = 768;
            this.y = 720;
            this.resize = false;
            this.maintainAR = true;
        }
        public override unsafe void PerformScale(int* origPixels, int* resizePixels)
        {
            int a, b, c, d, e, f, g, h, i;
            // a b c
            // d e f
            // g h i
            int e0, e1, e2, e3, e4, e5, e6, e7, e8;
            // e becomes
            // e0 e1 e2
            // e3 e4 e5
            // e6 e7 e8
            for (int imgY = 0; imgY < 240; imgY++)
            {
                for (int imgX = 0; imgX < 256; imgX++)
                {
                    e = origPixels[(imgY * 256) + imgX];
                    if (imgY == 0)
                    {
                        b = e;
                    }
                    else
                    {
                        b = origPixels[((imgY - 1) * 256) + imgX];
                    }
                    if (imgX == 0)
                    {
                        d = e;
                    }
                    else
                    {
                        d = origPixels[((imgY) * 256) + (imgX - 1)];
                    }
                    if (imgX == 256 - 1)
                    {
                        f = e;
                    }
                    else
                    {
                        f = origPixels[((imgY) * 256) + (imgX + 1)];
                    }
                    if (imgY == 240 - 1)
                    {
                        h = e;
                    }
                    else
                    {
                        h = origPixels[((imgY + 1) * 256) + (imgX)];
                    }
                    if (imgY == 0 || imgX == 0)
                    {
                        a = e;
                    }
                    else
                    {
                        a = origPixels[((imgY - 1) * 256) + (imgX - 1)];
                    }
                    if (imgY == 0 || imgX == 256 - 1)
                    {
                        c = e;
                    }
                    else
                    {
                        c = origPixels[((imgY - 1) * 256) + (imgX + 1)];
                    }
                    if (imgY == 240 - 1 || imgX == 0)
                    {
                        g = e;
                    }
                    else
                    {
                        g = origPixels[((imgY + 1) * 256) + (imgX - 1)];
                    }
                    if (imgY == 240 - 1 || imgX == 256 - 1)
                    {
                        i = e;
                    }
                    else
                    {
                        i = origPixels[((imgY + 1) * 256) + (imgX + 1)];
                    }

                    if (b != h && d != f)
                    {
                        e0 = d == b ? d : e;
                        e1 = (d == b && e != c) || (b == f && e != a) ? b : e;
                        e2 = b == f ? f : e;
                        e3 = (d == b && e != g) || (d == h && e != a) ? d : e;
                        e4 = e;
                        e5 = (b == f && e != i) || (h == f && e != c) ? f : e;
                        e6 = d == h ? d : e;
                        e7 = (d == h && e != i) || (h == f && e != g) ? h : e;
                        e8 = h == f ? f : e;
                    }
                    else
                    {
                        e0 = e;
                        e1 = e;
                        e2 = e;
                        e3 = e;
                        e4 = e;
                        e5 = e;
                        e6 = e;
                        e7 = e;
                        e8 = e;
                    }
                    resizePixels[((imgY * 3) * (256 * 3)) + (imgX * 3)] = e0;
                    resizePixels[((imgY * 3) * (256 * 3)) + ((imgX * 3) + 1)] = e1;
                    resizePixels[(((imgY * 3)) * (256 * 3)) + ((imgX * 3) + 2)] = e2;
                    resizePixels[(((imgY * 3) + 1) * (256 * 3)) + ((imgX * 3))] = e3;
                    resizePixels[(((imgY * 3) + 1) * (256 * 3)) + ((imgX * 3) + 1)] = e4;
                    resizePixels[(((imgY * 3) + 1) * (256 * 3)) + ((imgX * 3) + 2)] = e5;
                    resizePixels[(((imgY * 3) + 2) * (256 * 3)) + ((imgX * 3))] = e6;
                    resizePixels[(((imgY * 3) + 2) * (256 * 3)) + ((imgX * 3) + 1)] = e7;
                    resizePixels[(((imgY * 3) + 2) * (256 * 3)) + ((imgX * 3) + 2)] = e8;
                }
            }
        }
    }
}
