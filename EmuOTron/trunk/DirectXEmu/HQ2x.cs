using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace DirectXEmu
{
    class HQ2x : Scaler
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
        public HQ2x()
        {
            this.x = 512;
            this.y = 480;
            this.resize = false;
            this.maintainAR = true;
        }
        public unsafe override Bitmap PerformScale(Bitmap orig)
        {
            Bitmap resizedBitmap = new Bitmap(x, y);
            BitmapData origBMD = orig.LockBits(new Rectangle(0, 0, orig.Width, orig.Height), ImageLockMode.ReadWrite, PixelFormat.Format16bppArgb1555);
            BitmapData resizeBMD = resizedBitmap.LockBits(new Rectangle(0, 0, resizedBitmap.Width, resizedBitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            orig.UnlockBits(origBMD);
            resizedBitmap.UnlockBits(resizeBMD);
            return resizedBitmap;
        }
    }
}
