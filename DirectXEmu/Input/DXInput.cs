using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SlimDX.DirectInput;

namespace DirectXEmu
{
    class DXInput : IInput
    {
        DirectInput device;
        Keyboard keyboard;
        KeyboardState keyState;
        Mouse mouse;
        MouseState mouseState;
        Form inputSource;
        List<Key> pressedKeys = new List<Key>();
        Dictionary<Key, Keys> dxTranslate = new Dictionary<Key, Keys>();
        MouseArgs currentMouse;

        public event KeyHandler KeyDownEvent;
        public event KeyHandler KeyUpEvent;
        public event MouseHandler MouseMoveEvent;
        public event MouseHandler MouseDownEvent;
        public event MouseHandler MouseUpEvent;

        public DXInput(Form inputSource)
        {
            this.inputSource = inputSource;
            LoadKeyTranslations();
        }
        public void Create()
        {
            device = new DirectInput();
            keyState = new KeyboardState();
            mouseState = new MouseState();
            Reset();
        }

        public void Reset()
        {
            keyboard = new Keyboard(device);
            keyboard.SetCooperativeLevel(inputSource, CooperativeLevel.Foreground | CooperativeLevel.Nonexclusive);
            keyboard.Acquire();
            mouse = new Mouse(device);
            mouse.SetCooperativeLevel(inputSource, CooperativeLevel.Foreground | CooperativeLevel.Nonexclusive);
            mouse.Acquire();
        }

        public void MainLoop()
        {
            HandleKeyboard();
            HandleMouse();
        }
        private void HandleMouse()
        {
            if (mouse.Acquire().IsFailure)
                return;
            if (mouse.Poll().IsFailure)
                return;
            try
            {
                mouse.GetCurrentState(ref mouseState);
                MouseArgs newMouse;
                newMouse.Click = mouseState.IsPressed(0);
                newMouse.X = Cursor.Position.X; //Not even going to bother with attempting to use crazy relative Direct Input mouse data
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
            catch
            {
                mouse.Acquire();
            }
        }
        private void HandleKeyboard()
        {
            if (keyboard.Acquire().IsFailure)
                return;
            if (keyboard.Poll().IsFailure)
                return;
            try
            {
                keyboard.GetCurrentState(ref keyState);
                List<Key> newPressedKeys = new List<Key>();
                foreach (Key key in keyState.AllKeys)
                {
                    if(keyState.IsPressed(key))
                    {
                        newPressedKeys.Add(key);
                        if (!pressedKeys.Contains(key))
                            KeyDownEvent(this, dxTranslate[key]);
                    }
                    else
                    {
                        if (pressedKeys.Contains(key))
                            KeyUpEvent(this, dxTranslate[key]);
                    }
                }
                pressedKeys = newPressedKeys;
            }
            catch
            {
                keyboard.Acquire();
            }
        }

        public void Destroy()
        {
            keyboard.Dispose();
            mouse.Dispose();
            device.Dispose();
        }

        private void LoadKeyTranslations() //There is a chance that mshome keyboard bug is in play here rendering this useless on many other PCs, but my default keybinds work for me so whatever.
        {
            dxTranslate[Key.A] = Keys.A;
            dxTranslate[Key.Apostrophe] = Keys.OemQuotes;
            dxTranslate[Key.B] = Keys.B;
            dxTranslate[Key.Backslash] = Keys.OemBackslash;
            dxTranslate[Key.Backspace] = Keys.Back;
            dxTranslate[Key.C] = Keys.C;
            dxTranslate[Key.CapsLock] = Keys.CapsLock;
            dxTranslate[Key.Colon] = Keys.OemQuotes;
            dxTranslate[Key.Comma] = Keys.Oemcomma;
            dxTranslate[Key.D] = Keys.D;
            dxTranslate[Key.D0] = Keys.D0;
            dxTranslate[Key.D1] = Keys.D1;
            dxTranslate[Key.D2] = Keys.D2;
            dxTranslate[Key.D3] = Keys.D3;
            dxTranslate[Key.D4] = Keys.D4;
            dxTranslate[Key.D5] = Keys.D5;
            dxTranslate[Key.D6] = Keys.D6;
            dxTranslate[Key.D7] = Keys.D7;
            dxTranslate[Key.D8] = Keys.D8;
            dxTranslate[Key.D9] = Keys.D9;
            dxTranslate[Key.Delete] = Keys.Delete;
            dxTranslate[Key.DownArrow] = Keys.Down;
            dxTranslate[Key.E] = Keys.E;
            dxTranslate[Key.End] = Keys.End;
            dxTranslate[Key.PreviousTrack] = Keys.Oemplus; //*************CHECK
            dxTranslate[Key.Escape] = Keys.Escape;
            dxTranslate[Key.F] = Keys.F;
            dxTranslate[Key.F1] = Keys.F1;
            dxTranslate[Key.F2] = Keys.F2;
            dxTranslate[Key.F3] = Keys.F3;
            dxTranslate[Key.F4] = Keys.F4;
            dxTranslate[Key.F5] = Keys.F5;
            dxTranslate[Key.F6] = Keys.F6;
            dxTranslate[Key.F7] = Keys.F7;
            dxTranslate[Key.F8] = Keys.F8;
            dxTranslate[Key.F9] = Keys.F9;
            dxTranslate[Key.F10] = Keys.F10;
            dxTranslate[Key.F11] = Keys.F11;
            dxTranslate[Key.F12] = Keys.F12;
            dxTranslate[Key.F13] = Keys.F13;
            dxTranslate[Key.F14] = Keys.F14;
            dxTranslate[Key.F15] = Keys.F15;
            dxTranslate[Key.G] = Keys.G;
            dxTranslate[Key.Kanji] = Keys.Oemtilde;
            dxTranslate[Key.H] = Keys.H;
            dxTranslate[Key.Home] = Keys.Home;
            dxTranslate[Key.I] = Keys.I;
            dxTranslate[Key.Insert] = Keys.Insert;
            dxTranslate[Key.J] = Keys.J;
            dxTranslate[Key.K] = Keys.K;
            dxTranslate[Key.L] = Keys.L;
            dxTranslate[Key.LeftAlt] = Keys.Alt;
            dxTranslate[Key.LeftArrow] = Keys.Left;
            dxTranslate[Key.LeftBracket] = Keys.OemCloseBrackets;
            dxTranslate[Key.AT] = Keys.OemOpenBrackets;//WEEEEIRD
            dxTranslate[Key.LeftControl] = Keys.LControlKey;
            dxTranslate[Key.LeftShift] = Keys.ShiftKey;
            dxTranslate[Key.LeftWindowsKey] = Keys.LWin;
            dxTranslate[Key.M] = Keys.M;
            dxTranslate[Key.Minus] = Keys.Subtract;
            dxTranslate[Key.N] = Keys.N;
            dxTranslate[Key.NumberLock] = Keys.NumLock;
            dxTranslate[Key.NumberPad0] = Keys.NumPad0;
            dxTranslate[Key.NumberPad1] = Keys.NumPad1;
            dxTranslate[Key.NumberPad2] = Keys.NumPad2;
            dxTranslate[Key.NumberPad3] = Keys.NumPad3;
            dxTranslate[Key.NumberPad4] = Keys.NumPad4;
            dxTranslate[Key.NumberPad5] = Keys.NumPad5;
            dxTranslate[Key.NumberPad6] = Keys.NumPad6;
            dxTranslate[Key.NumberPad7] = Keys.NumPad7;
            dxTranslate[Key.NumberPad8] = Keys.NumPad8;
            dxTranslate[Key.NumberPad9] = Keys.NumPad9;
            dxTranslate[Key.NumberPadComma] = Keys.Oemcomma;
            dxTranslate[Key.NumberPadEnter] = Keys.Enter;//CHECK
            dxTranslate[Key.NumberPadMinus] = Keys.Subtract;
            dxTranslate[Key.NumberPadPeriod] = Keys.OemPeriod;
            dxTranslate[Key.NumberPadPlus] = Keys.Oemplus;
            dxTranslate[Key.NumberPadSlash] = Keys.Divide;
            dxTranslate[Key.NumberPadStar] = Keys.Multiply;
            dxTranslate[Key.O] = Keys.O;
            dxTranslate[Key.Oem102] = Keys.Oem102;
            dxTranslate[Key.P] = Keys.P;
            dxTranslate[Key.PageDown] = Keys.PageDown;
            dxTranslate[Key.PageUp] = Keys.PageUp;
            dxTranslate[Key.Pause] = Keys.Pause;
            dxTranslate[Key.Period] = Keys.OemPeriod;
            dxTranslate[Key.PrintScreen] = Keys.PrintScreen;
            dxTranslate[Key.Q] = Keys.Q;
            dxTranslate[Key.R] = Keys.R;
            dxTranslate[Key.Return] = Keys.Return;
            dxTranslate[Key.RightAlt] = Keys.Alt;
            dxTranslate[Key.RightArrow] = Keys.Right;
            dxTranslate[Key.RightBracket] = Keys.OemBackslash;
            dxTranslate[Key.RightControl] = Keys.RControlKey;
            dxTranslate[Key.RightShift] = Keys.ShiftKey;
            dxTranslate[Key.RightWindowsKey] = Keys.RWin;
            dxTranslate[Key.S] = Keys.S;
            dxTranslate[Key.ScrollLock] = Keys.Scroll;
            dxTranslate[Key.Semicolon] = Keys.OemSemicolon;
            dxTranslate[Key.Slash] = Keys.Divide;
            dxTranslate[Key.Space] = Keys.Space;
            dxTranslate[Key.T] = Keys.T;
            dxTranslate[Key.Tab] = Keys.Tab;
            dxTranslate[Key.U] = Keys.U;
            dxTranslate[Key.UpArrow] = Keys.Up;
            dxTranslate[Key.V] = Keys.V;
            dxTranslate[Key.W] = Keys.W;
            dxTranslate[Key.X] = Keys.X;
            dxTranslate[Key.Y] = Keys.Y;
            dxTranslate[Key.Z] = Keys.Z;
        }
    }
}