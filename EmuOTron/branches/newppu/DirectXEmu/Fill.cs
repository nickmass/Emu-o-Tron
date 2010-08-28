using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace DirectXEmu
{
    class Fill : Scaler
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
        public Fill()
        {
            this.x = 256;
            this.y = 240;
            this.resize = true;
            this.maintainAR = false;
        }
        public unsafe override Bitmap PerformScale(Bitmap orig)
        {
            return orig;
        }
    }
}
