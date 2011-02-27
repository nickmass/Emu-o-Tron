using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace DirectXEmu
{
    class NearestNeighbor2x : IScaler
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

        public NearestNeighbor2x()
        {
            _resizedX = 512;
            _resizedY = 480;
             _ratioX = 16;
            _ratioY = 15;
            _isResizable = false;
            _maintainAspectRatio = true;
        }
        public unsafe void PerformScale(uint* origPixels, uint* resizePixels)
        {
            for (int imgY = 0; imgY < _resizedY; imgY++)
                for (int imgX = 0; imgX < _resizedX; imgX++)
                    resizePixels[(imgY << 9) | imgX] = origPixels[((imgY & -2) << 7) | (imgX >> 1)];
        }
    }
}
