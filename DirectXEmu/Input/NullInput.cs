using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    class NullInput : IInput
    {
        #region IInput Members

        public void Create()
        {
        }

        public void Reset()
        {
        }

        public void MainLoop()
        {
        }

        public void Destroy()
        {
        }

        public event KeyHandler KeyDownEvent;

        public event KeyHandler KeyUpEvent;

        public event MouseHandler MouseMoveEvent;

        public event MouseHandler MouseDownEvent;

        public event MouseHandler MouseUpEvent;

        #endregion
    }
}
