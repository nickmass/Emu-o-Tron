using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    class NullRenderer : IRenderer
    {
        public NullRenderer()
        {
        }

        public void Create()
        {
        }

        public void Reset()
        {
        }

        public void MainLoop(bool newScreen)
        {
        }

        public void Destroy()
        {
        }

        public void ChangeScaler(IScaler imageScaler)
        {
        }

        public void DrawMessage(string message, Anchor anchor, int xOffset, int yOffset)
        {
        }

        public event EventHandler DrawMessageEvent;

        public void SmoothOutput(bool smooth)
        {
        }

    }
}
