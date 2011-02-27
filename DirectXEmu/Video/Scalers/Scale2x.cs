using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
namespace DirectXEmu
{
    class Scale2x : IScaler
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

        public Scale2x()
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
            uint b, d, e, f, h;
            uint e0, e1, e2, e3;
            for (int imgY = 0; imgY < 240; imgY++)
            {
                for (int imgX = 0; imgX < 256; imgX++)
                {
                    e = origPixels[(imgY * 256) + imgX];
                    if (imgY == 0)
                        b = e;
                    else
                        b = origPixels[((imgY - 1) * 256) + imgX];
                    if (imgX == 0)
                        d = e;
                    else
                        d = origPixels[((imgY) * 256) + (imgX - 1)];
                    if (imgX == 255)
                        f = e;
                    else
                        f = origPixels[((imgY) * 256) + (imgX + 1)];
                    if (imgY == 239)
                        h = e;
                    else
                        h = origPixels[((imgY + 1) * 256) + (imgX)];
                    if (b != h && d != f)
                    {
                        e0 = d == b ? d : e;
                        e1 = b == f ? f : e;
                        e2 = d == h ? d : e;
                        e3 = h == f ? f : e;
                    }
                    else
                    {
                        e0 = e;
                        e1 = e;
                        e2 = e;
                        e3 = e;
                    }
                    resizePixels[((imgY * 2) * (256 * 2)) + (imgX * 2)] = e0;
                    resizePixels[((imgY * 2) * (256 * 2)) + ((imgX * 2) + 1)] = e1;
                    resizePixels[(((imgY * 2) + 1) * (256 * 2)) + (imgX * 2)] = e2;
                    resizePixels[(((imgY * 2) + 1) * (256 * 2)) + ((imgX * 2) + 1)] = e3;
                }
            }
        }
    }
}
