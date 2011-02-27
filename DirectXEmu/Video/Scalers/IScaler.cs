using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DirectXEmu
{
    interface IScaler
    {
        int ResizedX
        {
            get;
        }
        int ResizedY
        {
            get;
        }
        double RatioX
        {
            get;
        }
        double RatioY
        {
            get;
        }
        bool IsResizable
        {
            get;
        }
        bool MaintainAspectRatio
        {
            get;
        }
        unsafe void PerformScale(uint* origPixels, uint* resizePixels);
    }
}
