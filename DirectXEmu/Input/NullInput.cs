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
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void MainLoop()
        {
            throw new NotImplementedException();
        }

        public void Destroy()
        {
            throw new NotImplementedException();
        }

        public event KeyHandler KeyDownEvent;

        public event KeyHandler KeyUpEvent;

        public event MouseHandler MouseMoveEvent;

        public event MouseHandler MouseDownEvent;

        public event MouseHandler MouseUpEvent;

        #endregion
    }
}
