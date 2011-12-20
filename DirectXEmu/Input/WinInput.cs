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

        int mouseX;
        int mouseY;

        bool[] currentMouseButtons = new bool[5];
        bool[] newMouseButtons = new bool[5];

        List<Keys> pressedKeys = new List<Keys>();
        List<Keys> releasedKeys = new List<Keys>();

        Dictionary<Keys, EmuKeys> winTranslate = new Dictionary<Keys, EmuKeys>();

        public event InputHandler InputEvent;
        public event InputScalerHandler InputScalerEvent;

        public WinInput(Control keyboardSource, Control mouseSource)
        {
            this.keyboardSource = keyboardSource;
            this.mouseSource = mouseSource;
            LoadKeyTranslations();
        }
        public void Create()
        {
            Reset();
        }

        void inputSource_MouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    newMouseButtons[0] = false;
                    break;
                case MouseButtons.Right:
                    newMouseButtons[1] = false;
                    break;
                case MouseButtons.Middle:
                    newMouseButtons[2] = false;
                    break;
                case MouseButtons.XButton1:
                    newMouseButtons[3] = false;
                    break;
                case MouseButtons.XButton2:
                    newMouseButtons[4] = false;
                    break;
            }
        }

        void inputSource_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    newMouseButtons[0] = true;
                    break;
                case MouseButtons.Right:
                    newMouseButtons[1] = true;
                    break;
                case MouseButtons.Middle:
                    newMouseButtons[2] = true;
                    break;
                case MouseButtons.XButton1:
                    newMouseButtons[3] = true;
                    break;
                case MouseButtons.XButton2:
                    newMouseButtons[4] = true;
                    break;
            }
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
            {
                if (winTranslate.ContainsKey(key))
                    InputEvent(winTranslate[key], true);
            }
            pressedKeys.Clear();
            foreach (Keys key in releasedKeys)
            {
                if (winTranslate.ContainsKey(key))
                    InputEvent(winTranslate[key], false);
            }
            releasedKeys.Clear();
            for (int i = 0; i < newMouseButtons.Length; i++)
            {
                if (newMouseButtons[i] != currentMouseButtons[i])
                {
                    switch (i)
                    {
                        case 0:
                            InputEvent(EmuKeys.Mouse1, newMouseButtons[i]);
                            break;
                        case 1:
                            InputEvent(EmuKeys.Mouse2, newMouseButtons[i]);
                            break;
                        case 2:
                            InputEvent(EmuKeys.Mouse3, newMouseButtons[i]);
                            break;
                        case 3:
                            InputEvent(EmuKeys.Mouse4, newMouseButtons[i]);
                            break;
                        case 4:
                            InputEvent(EmuKeys.Mouse5, newMouseButtons[i]);
                            break;
                    }
                    currentMouseButtons[i] = newMouseButtons[i];
                }
            }

            int newMouseX = Cursor.Position.X;
            int newMouseY = Cursor.Position.Y;
            if (newMouseX != mouseX)
                InputScalerEvent(EmuKeys.MouseX, newMouseX);
            if (newMouseY != mouseY)
                InputScalerEvent(EmuKeys.MouseY, newMouseY);
            mouseX = newMouseX;
            mouseY = newMouseY;
        }

        public void Destroy()
        {
        }

        private void LoadKeyTranslations()
        {
            winTranslate[Keys.A] = EmuKeys.A;
            winTranslate[Keys.OemQuotes] = EmuKeys.Apostrophe;
            winTranslate[Keys.B] = EmuKeys.B;
            winTranslate[Keys.OemPipe] = EmuKeys.Backslash;
            winTranslate[Keys.Back] = EmuKeys.Backspace;
            winTranslate[Keys.C] = EmuKeys.C;
            winTranslate[Keys.CapsLock] = EmuKeys.CapsLock;
            winTranslate[Keys.Oemcomma] = EmuKeys.Comma;
            winTranslate[Keys.D] = EmuKeys.D;
            winTranslate[Keys.D0] = EmuKeys.D0;
            winTranslate[Keys.D1] = EmuKeys.D1;
            winTranslate[Keys.D2] = EmuKeys.D2;
            winTranslate[Keys.D3] = EmuKeys.D3;
            winTranslate[Keys.D4] = EmuKeys.D4;
            winTranslate[Keys.D5] = EmuKeys.D5;
            winTranslate[Keys.D6] = EmuKeys.D6;
            winTranslate[Keys.D7] = EmuKeys.D7;
            winTranslate[Keys.D8] = EmuKeys.D8;
            winTranslate[Keys.D9] = EmuKeys.D9;
            winTranslate[Keys.Delete] = EmuKeys.Delete;
            winTranslate[Keys.Down] = EmuKeys.DownArrow;
            winTranslate[Keys.E] = EmuKeys.E;
            winTranslate[Keys.End] = EmuKeys.End;
            winTranslate[Keys.Oemplus] = EmuKeys.Equals;
            winTranslate[Keys.Escape] = EmuKeys.Escape;
            winTranslate[Keys.F] = EmuKeys.F;
            winTranslate[Keys.F1] = EmuKeys.F1;
            winTranslate[Keys.F2] = EmuKeys.F2;
            winTranslate[Keys.F3] = EmuKeys.F3;
            winTranslate[Keys.F4] = EmuKeys.F4;
            winTranslate[Keys.F5] = EmuKeys.F5;
            winTranslate[Keys.F6] = EmuKeys.F6;
            winTranslate[Keys.F7] = EmuKeys.F7;
            winTranslate[Keys.F8] = EmuKeys.F8;
            winTranslate[Keys.F9] = EmuKeys.F9;
            winTranslate[Keys.F10] = EmuKeys.F10;
            winTranslate[Keys.F11] = EmuKeys.F11;
            winTranslate[Keys.F12] = EmuKeys.F12;
            winTranslate[Keys.G] = EmuKeys.G;
            winTranslate[Keys.Oemtilde] = EmuKeys.Grave;
            winTranslate[Keys.H] = EmuKeys.H;
            winTranslate[Keys.Home] = EmuKeys.Home;
            winTranslate[Keys.I] = EmuKeys.I;
            winTranslate[Keys.Insert] = EmuKeys.Insert;
            winTranslate[Keys.J] = EmuKeys.J;
            winTranslate[Keys.K] = EmuKeys.K;
            winTranslate[Keys.L] = EmuKeys.L;
            winTranslate[Keys.RButton | Keys.ShiftKey] = EmuKeys.LeftAlt;
            winTranslate[Keys.Left] = EmuKeys.LeftArrow;
            winTranslate[Keys.OemOpenBrackets] = EmuKeys.LeftBracket;
            winTranslate[Keys.LButton | Keys.ShiftKey] = EmuKeys.LeftControl;
            winTranslate[Keys.ShiftKey] = EmuKeys.LeftShift;
            winTranslate[Keys.LWin] = EmuKeys.LeftWindowsKey;
            winTranslate[Keys.M] = EmuKeys.M;
            winTranslate[Keys.OemMinus] = EmuKeys.Minus;
            winTranslate[Keys.N] = EmuKeys.N;
            winTranslate[Keys.NumLock] = EmuKeys.NumberLock;
            winTranslate[Keys.NumPad0] = EmuKeys.NumberPad0;
            winTranslate[Keys.NumPad1] = EmuKeys.NumberPad1;
            winTranslate[Keys.NumPad2] = EmuKeys.NumberPad2;
            winTranslate[Keys.NumPad3] = EmuKeys.NumberPad3;
            winTranslate[Keys.NumPad4] = EmuKeys.NumberPad4;
            winTranslate[Keys.NumPad5] = EmuKeys.NumberPad5;
            winTranslate[Keys.NumPad6] = EmuKeys.NumberPad6;
            winTranslate[Keys.NumPad7] = EmuKeys.NumberPad7;
            winTranslate[Keys.NumPad8] = EmuKeys.NumberPad8;
            winTranslate[Keys.NumPad9] = EmuKeys.NumberPad9;
            winTranslate[Keys.Enter] = EmuKeys.NumberPadEnter;
            winTranslate[Keys.Subtract] = EmuKeys.NumberPadMinus;
            winTranslate[Keys.Decimal] = EmuKeys.NumberPadPeriod;
            winTranslate[Keys.Add] = EmuKeys.NumberPadPlus;
            winTranslate[Keys.Divide] = EmuKeys.NumberPadSlash;
            winTranslate[Keys.Multiply] = EmuKeys.NumberPadStar;
            winTranslate[Keys.O] = EmuKeys.O;
            winTranslate[Keys.P] = EmuKeys.P;
            winTranslate[Keys.PageDown] = EmuKeys.PageDown;
            winTranslate[Keys.PageUp] = EmuKeys.PageUp;
            winTranslate[Keys.Pause] = EmuKeys.Pause;
            winTranslate[Keys.OemPeriod] = EmuKeys.Period;
            winTranslate[Keys.PrintScreen] = EmuKeys.PrintScreen;
            winTranslate[Keys.Q] = EmuKeys.Q;
            winTranslate[Keys.R] = EmuKeys.R;
            winTranslate[Keys.Return] = EmuKeys.Return;
            winTranslate[Keys.Right] = EmuKeys.RightArrow;
            winTranslate[Keys.OemCloseBrackets] = EmuKeys.RightBracket;
            winTranslate[Keys.RControlKey] = EmuKeys.RightControl;
            winTranslate[Keys.RShiftKey] = EmuKeys.RightShift;
            winTranslate[Keys.RWin] = EmuKeys.RightWindowsKey;
            winTranslate[Keys.S] = EmuKeys.S;
            winTranslate[Keys.Scroll] = EmuKeys.ScrollLock;
            winTranslate[Keys.OemSemicolon] = EmuKeys.Semicolon;
            winTranslate[Keys.OemQuestion] = EmuKeys.Slash;
            winTranslate[Keys.Space] = EmuKeys.Space;
            winTranslate[Keys.T] = EmuKeys.T;
            winTranslate[Keys.Tab] = EmuKeys.Tab;
            winTranslate[Keys.U] = EmuKeys.U;
            winTranslate[Keys.Up] = EmuKeys.UpArrow;
            winTranslate[Keys.V] = EmuKeys.V;
            winTranslate[Keys.W] = EmuKeys.W;
            winTranslate[Keys.X] = EmuKeys.X;
            winTranslate[Keys.Y] = EmuKeys.Y;
            winTranslate[Keys.Z] = EmuKeys.Z;
        }
    }
}
