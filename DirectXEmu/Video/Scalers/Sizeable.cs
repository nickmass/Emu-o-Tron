using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace DirectXEmu
{
    class Sizeable : IScaler
    {
        private int _resizedX;
        private int _resizedY;
        private bool _isResizable;
        private bool _maintainAspectRatio;
        private double _ratioX;
        private double _ratioY;

        public int ResizedX { get { return _resizedX; } }
        public int ResizedY { get { return _resizedY; } }
        public double RatioX { get { return _ratioX; } }
        public double RatioY { get { return _ratioY; } }
        public bool IsResizable { get { return _isResizable; } }
        public bool MaintainAspectRatio { get { return _maintainAspectRatio; } }

        public Sizeable()
        {
            _resizedX = 256;
            _resizedY = 240;
             _ratioX = 16;
            _ratioY = 15;
            _isResizable = true;
            _maintainAspectRatio = true;
        }

        public unsafe void PerformScale(uint* origPixels, uint* resizePixels)
        {
            int size = _resizedX * _resizedY;
            for (int i = 0; i < size; i++)
                resizePixels[i] = origPixels[i];
        }
    }
}
