using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace DirectXEmu
{
    class NearestNeighbor2x : Scaler
    {
        private Bitmap resizedBitmap;
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
        public NearestNeighbor2x()
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
            for (int imgY = 0; imgY < y; imgY++)
                for (int imgX = 0; imgX < x; imgX++)
                    resizePixels[(imgY * x) + imgX] = origPixels[(imgY/2 * x /2) + imgX/2];
            orig.UnlockBits(origBMD);
            resizedBitmap.UnlockBits(resizeBMD);
            return resizedBitmap;
        }
    }
}
