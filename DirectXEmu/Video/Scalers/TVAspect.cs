using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    class TVAspect : Scaler
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
        public TVAspect()
        {
            this.x = 256;
            this.y = 240;
            this.arX = 4;
            this.arY = 3;   
            this.resize = true;
            this.maintainAR = true;
        }
        public override unsafe void PerformScale(int* origPixels, int* resizePixels)
        {
            int size = x * y;
            for (int i = 0; i < size; i++)
                resizePixels[i] = origPixels[i];
        }
    }
}