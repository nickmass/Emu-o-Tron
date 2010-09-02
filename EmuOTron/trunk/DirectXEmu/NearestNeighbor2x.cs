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
        public override unsafe void PerformScale(int* origPixels, int* resizePixels)
        {
            int halfX = x / 2;
            for (int imgY = 0; imgY < y; imgY++)
                for (int imgX = 0; imgX < x; imgX++)
                    resizePixels[(imgY * x) + imgX] = origPixels[(imgY / 2 * halfX) + imgX / 2];
        }
    }
}
