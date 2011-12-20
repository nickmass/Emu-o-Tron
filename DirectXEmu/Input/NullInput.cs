using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    class NullInput : IInput
    {
        #region IInput Members

        public event InputHandler InputEvent;
        public event InputScalerHandler InputScalerEvent;

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

        #endregion
    }
}
