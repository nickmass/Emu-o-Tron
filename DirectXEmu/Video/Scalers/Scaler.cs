using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DirectXEmu
{
    abstract class Scaler
    {
        protected int x;
        protected int y;
        protected bool resize;
        protected bool maintainAR;
        public float arX = 16;
        public float arY = 15;
        public abstract int xSize
        {
            get;
        }
        public abstract int ySize
        {
            get;
        }
        public abstract bool resizeable
        {
            get;
        }
        public abstract bool maintainAspectRatio
        {
            get;
        }
        public unsafe abstract void PerformScale(int* origPixels, int* resizePixels);
    }
}
