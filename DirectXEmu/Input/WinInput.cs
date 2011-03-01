using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DirectXEmu
{
    class WinInput : IInput
    {
        Control keyboardSource;
        Control mouseSource;
        MouseArgs currentMouse;
        MouseArgs newMouse;

        List<Keys> pressedKeys = new List<Keys>();
        List<Keys> releasedKeys = new List<Keys>();


        public event KeyHandler KeyDownEvent;
        public event KeyHandler KeyUpEvent;
        public event MouseHandler MouseMoveEvent;
        public event MouseHandler MouseDownEvent;
        public event MouseHandler MouseUpEvent;

        public WinInput(Control keyboardSource, Control mouseSource)
        {
            this.keyboardSource = keyboardSource;
            this.mouseSource = mouseSource;
        }
        public void Create()
        {
            Reset();
        }

        void inputSource_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                newMouse.Click = false;
        }

        void inputSource_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                newMouse.Click = true;
        }

        void inputSource_KeyUp(object sender, KeyEventArgs e)
        {
            releasedKeys.Add(e.KeyCode);
        }

        void inputSource_KeyDown(object sender, KeyEventArgs e)
        {
            pressedKeys.Add(e.KeyCode);
        }

        public void Reset()
        {
            keyboardSource.KeyDown += new KeyEventHandler(inputSource_KeyDown);
            keyboardSource.KeyUp += new KeyEventHandler(inputSource_KeyUp);
            mouseSource.MouseDown += new MouseEventHandler(inputSource_MouseDown);
            mouseSource.MouseUp += new MouseEventHandler(inputSource_MouseUp);
        }

        public void MainLoop()
        {
            foreach (Keys key in pressedKeys)
                KeyDownEvent(this, key);
            pressedKeys.Clear();
            foreach (Keys key in releasedKeys)
                KeyUpEvent(this, key);
            releasedKeys.Clear();
            newMouse.X = Cursor.Position.X;
            newMouse.Y = Cursor.Position.Y;
            if (newMouse.Click != currentMouse.Click)
            {
                if (newMouse.Click)
                    MouseDownEvent(this, newMouse);
                else
                    MouseUpEvent(this, newMouse);
            }
            if (newMouse.X != currentMouse.X || newMouse.Y != currentMouse.Y)
                MouseMoveEvent(this, newMouse);
            currentMouse = newMouse;
        }

        public void Destroy()
        {
        }
    }
}
