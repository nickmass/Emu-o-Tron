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
        public Scale2x()
        {
            this.x = 512;
            this.y = 480;
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
            int b, d, e, f, h;
            int e0, e1, e2, e3;
            for (int imgY = 0; imgY < origBMD.Height; imgY++)
            {
                for (int imgX = 0; imgX < origBMD.Width; imgX++)
                {
                    e = origPixels[(imgY * origBMD.Width) + imgX];
                    if (imgY == 0)
                        b = e;
                    else
                        b = origPixels[((imgY - 1) * origBMD.Width) + imgX];
                    if (imgX == 0)
                        d = e;
                    else
                        d = origPixels[((imgY) * origBMD.Width) + (imgX - 1)];
                    if (imgX == origBMD.Width - 1)
                        f = e;
                    else
                        f = origPixels[((imgY) * origBMD.Width) + (imgX + 1)];
                    if (imgY == origBMD.Height - 1)
                        h = e;
                    else
                        h = origPixels[((imgY + 1) * origBMD.Width) + (imgX)];
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
                    resizePixels[((imgY * 2) * (origBMD.Width * 2)) + (imgX * 2)] = e0;
                    resizePixels[((imgY * 2) * (origBMD.Width * 2)) + ((imgX * 2) + 1)] = e1;
                    resizePixels[(((imgY * 2) + 1) * (origBMD.Width * 2)) + (imgX * 2)] = e2;
                    resizePixels[(((imgY * 2) + 1) * (origBMD.Width * 2)) + ((imgX * 2) + 1)] = e3;
                }
            }
            orig.UnlockBits(origBMD);
            resizedBitmap.UnlockBits(resizeBMD);
            return resizedBitmap;
        }
    }
}
