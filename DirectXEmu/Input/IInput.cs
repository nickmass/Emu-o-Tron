using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DirectXEmu
{
    interface IInput
    {
        void Create();
        void Reset();
        void MainLoop();
        void Destroy();
        event KeyHandler KeyDownEvent;
        event KeyHandler KeyUpEvent;
        event MouseHandler MouseMoveEvent;
        event MouseHandler MouseDownEvent;
        event MouseHandler MouseUpEvent;
    }
    public struct MouseArgs
    {
        public int X;
        public int Y;
        public bool Click;
    }
    public delegate void KeyHandler(object sender, Keys key);
    public delegate void MouseHandler(object sender, MouseArgs mouse);

}
