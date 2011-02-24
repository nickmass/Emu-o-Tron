﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    interface IRenderer
    {
        void Create();
        void Reset();
        void MainLoop();
        void Destroy();
        void SmoothOutput(bool smooth);
        void ChangeScaler(Scaler imageScaler);
        void DrawMessage(string message, Anchor anchor, int xOffset, int yOffset);
        event EventHandler DrawMessageEvent;
    }
    enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
