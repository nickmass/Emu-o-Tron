using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    class NTSC : IScaler
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

        private NTSCFilter ntsc;

        public NTSC()
        {
            _resizedX = 640;
            _resizedY = 480;
             _ratioX = 4;
            _ratioY = 3;
            _isResizable = false;
            _maintainAspectRatio = true;
            ntsc = new NTSCFilter(_resizedX, 2);

        }
        public unsafe void PerformScale(uint* origPixels, uint* resizePixels)
        {
            ntsc.Filter(origPixels, resizePixels);
        }
    }
}