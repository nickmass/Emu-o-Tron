﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace DirectXEmu
{
    class NearestNeighbor1x : Scaler
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
        public NearestNeighbor1x()
        {
            this.x = 256;
            this.y = 240;
            this.resize = false;
            this.maintainAR = true;
        }
        public unsafe override Bitmap PerformScale(Bitmap orig)
        {
            return orig;
        }
    }
}
