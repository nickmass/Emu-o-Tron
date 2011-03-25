using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    interface IRenderer
    {
        void Create();
        void Reset();
        void MainLoop(bool newScreen);
        void Destroy();
        void SmoothOutput(bool smooth);
        void ChangeScaler(IScaler imageScaler);
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
