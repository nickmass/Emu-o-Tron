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
        Bitmap resizedBitmap;
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
            resizedBitmap = new Bitmap(x, y);
        }
        public unsafe override Bitmap PerformScale(Bitmap orig)
        {
            BitmapData origBMD = orig.LockBits(new Rectangle(0, 0, orig.Width, orig.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData resizeBMD = resizedBitmap.LockBits(new Rectangle(0, 0, resizedBitmap.Width, resizedBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int* origPixels = (int*)origBMD.Scan0;
            int* resizePixels = (int*)resizeBMD.Scan0;
            int a, b, c, d, e, f, g, h, i;
            // a b c
            // d e f
            // g h i
            int e0, e1, e2, e3, e4, e5, e6, e7, e8;
            // e becomes
            // e0 e1 e2
            // e3 e4 e5
            // e6 e7 e8
            for (int imgY = 0; imgY < origBMD.Height; imgY++)
            {
                for (int imgX = 0; imgX < origBMD.Width; imgX++)
                {
                    e = origPixels[(imgY * origBMD.Width) + imgX];
                    if (imgY == 0)
                    {
                        b = e;
                    }
                    else
                    {
                        b = origPixels[((imgY - 1) * origBMD.Width) + imgX];
                    }
                    if (imgX == 0)
                    {
                        d = e;
                    }
                    else
                    {
                        d = origPixels[((imgY) * origBMD.Width) + (imgX - 1)];
                    }
                    if (imgX == origBMD.Width - 1)
                    {
                        f = e;
                    }
                    else
                    {
                        f = origPixels[((imgY) * origBMD.Width) + (imgX + 1)];
                    }
                    if (imgY == origBMD.Height - 1)
                    {
                        h = e;
                    }
                    else
                    {
                        h = origPixels[((imgY + 1) * origBMD.Width) + (imgX)];
                    }
                    if (imgY == 0 || imgX == 0)
                    {
                        a = e;
                    }
                    else
                    {
                        a = origPixels[((imgY - 1) * origBMD.Width) + (imgX - 1)];
                    }
                    if (imgY == 0 || imgX == origBMD.Width - 1)
                    {
                        c = e;
                    }
                    else
                    {
                        c = origPixels[((imgY - 1) * origBMD.Width) + (imgX + 1)];
                    }
                    if (imgY == origBMD.Height - 1 || imgX == 0)
                    {
                        g = e;
                    }
                    else
                    {
                        g = origPixels[((imgY + 1) * origBMD.Width) + (imgX - 1)];
                    }
                    if (imgY == origBMD.Height - 1 || imgX == origBMD.Width - 1)
                    {
                        i = e;
                    }
                    else
                    {
                        i = origPixels[((imgY + 1) * origBMD.Width) + (imgX + 1)];
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
                    resizePixels[((imgY * 3) * (origBMD.Width * 3)) + (imgX * 3)] = e0;
                    resizePixels[((imgY * 3) * (origBMD.Width * 3)) + ((imgX * 3) + 1)] = e1;
                    resizePixels[(((imgY * 3)) * (origBMD.Width * 3)) + ((imgX * 3) + 2)] = e2;
                    resizePixels[(((imgY * 3) + 1) * (origBMD.Width * 3)) + ((imgX * 3))] = e3;
                    resizePixels[(((imgY * 3) + 1) * (origBMD.Width * 3)) + ((imgX * 3) + 1)] = e4;
                    resizePixels[(((imgY * 3) + 1) * (origBMD.Width * 3)) + ((imgX * 3) + 2)] = e5;
                    resizePixels[(((imgY * 3) + 2) * (origBMD.Width * 3)) + ((imgX * 3))] = e6;
                    resizePixels[(((imgY * 3) + 2) * (origBMD.Width * 3)) + ((imgX * 3) + 1)] = e7;
                    resizePixels[(((imgY * 3) + 2) * (origBMD.Width * 3)) + ((imgX * 3) + 2)] = e8;
                }
            }
            orig.UnlockBits(origBMD);
            resizedBitmap.UnlockBits(resizeBMD);
            return resizedBitmap;
        }
    }
}
