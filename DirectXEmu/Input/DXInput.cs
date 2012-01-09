#if !NO_DX
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
        Dictionary<Key, EmuKeys> dxTranslate = new Dictionary<Key, EmuKeys>();
        private int mouseX;
        private int mouseY;
        private bool[] currentMouseButtons = new bool[5];
        Joystick joystick;
        JoystickState joystickState;
        bool joystickAttached = false;

        public event InputHandler InputEvent;
        public event InputScalerHandler InputScalerEvent;

        private bool joyUpPressed;
        private bool joyDownPressed;
        private bool joyLeftPressed;
        private bool joyRightPressed;
        private bool[] currentJoyButtons = new bool[10];


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
            joystickState = new JoystickState();
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
            IList<DeviceInstance> attachedJoysticks = device.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly);
            if (attachedJoysticks.Count > 0)
            {
                joystick = new Joystick(device, attachedJoysticks[0].InstanceGuid);
                joystick.SetCooperativeLevel(inputSource, CooperativeLevel.Foreground | CooperativeLevel.Nonexclusive);
                joystick.Acquire();
                joystickAttached = true;
            }
        }

        public void MainLoop()
        {
            HandleKeyboard();
            HandleMouse();
            if (joystickAttached)
                HandleJoystick();
        }
        private void HandleJoystick()
        {
            if (joystick.Acquire().IsFailure)
                return;
            if (joystick.Poll().IsFailure)
                return;
            try
            {
                joystick.GetCurrentState(ref joystickState);
                if (joystickState.X < 0x3FFF)
                {
                    if (!joyLeftPressed)
                        InputEvent(EmuKeys.JoyLeft, true);
                    joyLeftPressed = true;
                }
                else
                {
                    if (joyLeftPressed)
                        InputEvent(EmuKeys.JoyLeft, false);
                    joyLeftPressed = false;
                }
                if (joystickState.X > 0xBFFF)
                {
                    if (!joyRightPressed)
                        InputEvent(EmuKeys.JoyRight, true);
                    joyRightPressed = true;
                }
                else
                {
                    if (joyRightPressed)
                        InputEvent(EmuKeys.JoyRight, false);
                    joyRightPressed = false;
                }
                if (joystickState.Y < 0x3FFF)
                {
                    if (!joyUpPressed)
                        InputEvent(EmuKeys.JoyUp, true);
                    joyUpPressed = true;
                }
                else
                {
                    if (joyUpPressed)
                        InputEvent(EmuKeys.JoyUp, false);
                    joyUpPressed = false;
                }
                if (joystickState.Y > 0xBFFF)
                {
                    if (!joyDownPressed)
                        InputEvent(EmuKeys.JoyDown, true);
                    joyDownPressed = true;
                }
                else
                {
                    if (joyDownPressed)
                        InputEvent(EmuKeys.JoyDown, false);
                    joyDownPressed = false;
                }

                bool[] buttons = joystickState.GetButtons();

                for (int i = 0; i < buttons.Length && i < 10; i++)
                {
                    if(buttons[i] != currentJoyButtons[i])
                    {
                        switch (i)
                        {
                            case 0:
                                InputEvent(EmuKeys.Joy1, buttons[i]);
                                break;
                            case 1:
                                InputEvent(EmuKeys.Joy2, buttons[i]);
                                break;
                            case 2:
                                InputEvent(EmuKeys.Joy3, buttons[i]);
                                break;
                            case 3:
                                InputEvent(EmuKeys.Joy4, buttons[i]);
                                break;
                            case 4:
                                InputEvent(EmuKeys.Joy5, buttons[i]);
                                break;
                            case 5:
                                InputEvent(EmuKeys.Joy6, buttons[i]);
                                break;
                            case 6:
                                InputEvent(EmuKeys.Joy7, buttons[i]);
                                break;
                            case 7:
                                InputEvent(EmuKeys.Joy8, buttons[i]);
                                break;
                            case 8:
                                InputEvent(EmuKeys.Joy9, buttons[i]);
                                break;
                            case 9:
                                InputEvent(EmuKeys.Joy10, buttons[i]);
                                break;
                        }
                        currentJoyButtons[i] = buttons[i];
                    }
                }
            }
            catch
            {
                joystick.Acquire();
            }

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
                
                bool[] buttons = mouseState.GetButtons();

                for (int i = 0; i < buttons.Length && i < 5; i++)
                {
                    if (buttons[i] != currentMouseButtons[i])
                    {
                        switch (i)
                        {
                            case 0:
                                InputEvent(EmuKeys.Mouse1, buttons[i]);
                                break;
                            case 1:
                                InputEvent(EmuKeys.Mouse2, buttons[i]);
                                break;
                            case 2:
                                InputEvent(EmuKeys.Mouse3, buttons[i]);
                                break;
                            case 3:
                                InputEvent(EmuKeys.Mouse4, buttons[i]);
                                break;
                            case 4:
                                InputEvent(EmuKeys.Mouse5, buttons[i]);
                                break;
                        }
                        currentMouseButtons[i] = buttons[i];
                    }
                }
                int newMouseX = Cursor.Position.X; //Not even going to bother with attempting to use crazy relative Direct Input mouse data
                int newMouseY = Cursor.Position.Y;
                if (newMouseX != mouseX)
                    InputScalerEvent(EmuKeys.MouseX, newMouseX);
                if (newMouseY != mouseY)
                    InputScalerEvent(EmuKeys.MouseY, newMouseY);
                mouseX = newMouseX;
                mouseY = newMouseY;

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
                        if (!pressedKeys.Contains(key) && dxTranslate.ContainsKey(key))
                            InputEvent(dxTranslate[key], true);
                    }
                    else
                    {
                        if (pressedKeys.Contains(key) && dxTranslate.ContainsKey(key))
                            InputEvent(dxTranslate[key], false);
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
            if(joystick != null)
                joystick.Dispose();
            device.Dispose();
        }

        private void LoadKeyTranslations() //There is a chance that mshome keyboard bug is in play here rendering this useless on many other PCs, but my default keybinds work for me so whatever.
        {
            dxTranslate[Key.A] = EmuKeys.A;
            dxTranslate[Key.Colon] = EmuKeys.Apostrophe;
            dxTranslate[Key.B] = EmuKeys.B;
            dxTranslate[Key.Backslash] = EmuKeys.Backslash;
            dxTranslate[Key.Backspace] = EmuKeys.Backspace;
            dxTranslate[Key.C] = EmuKeys.C;
            dxTranslate[Key.CapsLock] = EmuKeys.CapsLock;
            dxTranslate[Key.Comma] = EmuKeys.Comma;
            dxTranslate[Key.D] = EmuKeys.D;
            dxTranslate[Key.D0] = EmuKeys.D0;
            dxTranslate[Key.D1] = EmuKeys.D1;
            dxTranslate[Key.D2] = EmuKeys.D2;
            dxTranslate[Key.D3] = EmuKeys.D3;
            dxTranslate[Key.D4] = EmuKeys.D4;
            dxTranslate[Key.D5] = EmuKeys.D5;
            dxTranslate[Key.D6] = EmuKeys.D6;
            dxTranslate[Key.D7] = EmuKeys.D7;
            dxTranslate[Key.D8] = EmuKeys.D8;
            dxTranslate[Key.D9] = EmuKeys.D9;
            dxTranslate[Key.Delete] = EmuKeys.Delete;
            dxTranslate[Key.DownArrow] = EmuKeys.DownArrow;
            dxTranslate[Key.E] = EmuKeys.E;
            dxTranslate[Key.End] = EmuKeys.End;
            dxTranslate[Key.Escape] = EmuKeys.Escape;
            dxTranslate[Key.F] = EmuKeys.F;
            dxTranslate[Key.F1] = EmuKeys.F1;
            dxTranslate[Key.F2] = EmuKeys.F2;
            dxTranslate[Key.F3] = EmuKeys.F3;
            dxTranslate[Key.F4] = EmuKeys.F4;
            dxTranslate[Key.F5] = EmuKeys.F5;
            dxTranslate[Key.F6] = EmuKeys.F6;
            dxTranslate[Key.F7] = EmuKeys.F7;
            dxTranslate[Key.F8] = EmuKeys.F8;
            dxTranslate[Key.F9] = EmuKeys.F9;
            dxTranslate[Key.F10] = EmuKeys.F10;
            dxTranslate[Key.F11] = EmuKeys.F11;
            dxTranslate[Key.F12] = EmuKeys.F12;
            dxTranslate[Key.G] = EmuKeys.G;
            dxTranslate[Key.Kanji] = EmuKeys.Grave;
            dxTranslate[Key.H] = EmuKeys.H;
            dxTranslate[Key.Home] = EmuKeys.Home;
            dxTranslate[Key.I] = EmuKeys.I;
            dxTranslate[Key.Insert] = EmuKeys.Insert;
            dxTranslate[Key.J] = EmuKeys.J;
            dxTranslate[Key.K] = EmuKeys.K;
            dxTranslate[Key.L] = EmuKeys.L;
            dxTranslate[Key.LeftAlt] = EmuKeys.LeftAlt;
            dxTranslate[Key.LeftArrow] = EmuKeys.LeftArrow;
            dxTranslate[Key.LeftBracket] = EmuKeys.RightBracket;
            dxTranslate[Key.AT] = EmuKeys.LeftBracket;
            dxTranslate[Key.LeftControl] = EmuKeys.LeftControl;
            dxTranslate[Key.LeftShift] = EmuKeys.LeftShift;
            dxTranslate[Key.LeftWindowsKey] = EmuKeys.LeftWindowsKey;
            dxTranslate[Key.M] = EmuKeys.M;
            dxTranslate[Key.Minus] = EmuKeys.Minus;
            dxTranslate[Key.N] = EmuKeys.N;
            dxTranslate[Key.NumberLock] = EmuKeys.NumberLock;
            dxTranslate[Key.NumberPad0] = EmuKeys.NumberPad0;
            dxTranslate[Key.NumberPad1] = EmuKeys.NumberPad1;
            dxTranslate[Key.NumberPad2] = EmuKeys.NumberPad2;
            dxTranslate[Key.NumberPad3] = EmuKeys.NumberPad3;
            dxTranslate[Key.NumberPad4] = EmuKeys.NumberPad4;
            dxTranslate[Key.NumberPad5] = EmuKeys.NumberPad5;
            dxTranslate[Key.NumberPad6] = EmuKeys.NumberPad6;
            dxTranslate[Key.NumberPad7] = EmuKeys.NumberPad7;
            dxTranslate[Key.NumberPad8] = EmuKeys.NumberPad8;
            dxTranslate[Key.NumberPad9] = EmuKeys.NumberPad9;
            dxTranslate[Key.NumberPadEnter] = EmuKeys.NumberPadEnter;
            dxTranslate[Key.NumberPadMinus] = EmuKeys.NumberPadMinus;
            dxTranslate[Key.NumberPadPeriod] = EmuKeys.NumberPadPeriod;
            dxTranslate[Key.NumberPadPlus] = EmuKeys.NumberPadPlus;
            dxTranslate[Key.NumberPadSlash] = EmuKeys.NumberPadSlash;
            dxTranslate[Key.NumberPadStar] = EmuKeys.NumberPadStar;
            dxTranslate[Key.O] = EmuKeys.O;
            dxTranslate[Key.P] = EmuKeys.P;
            dxTranslate[Key.PageDown] = EmuKeys.PageDown;
            dxTranslate[Key.PageUp] = EmuKeys.PageUp;
            dxTranslate[Key.Pause] = EmuKeys.Pause;
            dxTranslate[Key.Period] = EmuKeys.Period;
            dxTranslate[Key.PrintScreen] = EmuKeys.PrintScreen;
            dxTranslate[Key.Q] = EmuKeys.Q;
            dxTranslate[Key.R] = EmuKeys.R;
            dxTranslate[Key.Return] = EmuKeys.Return;
            dxTranslate[Key.RightAlt] = EmuKeys.RightAlt;
            dxTranslate[Key.RightArrow] = EmuKeys.RightArrow;
            dxTranslate[Key.RightBracket] = EmuKeys.Backslash;
            dxTranslate[Key.RightControl] = EmuKeys.RightControl;
            dxTranslate[Key.RightShift] = EmuKeys.RightShift;
            dxTranslate[Key.RightWindowsKey] = EmuKeys.RightWindowsKey;
            dxTranslate[Key.S] = EmuKeys.S;
            dxTranslate[Key.ScrollLock] = EmuKeys.ScrollLock;
            dxTranslate[Key.Semicolon] = EmuKeys.Semicolon;
            dxTranslate[Key.Slash] = EmuKeys.Slash;
            dxTranslate[Key.Space] = EmuKeys.Space;
            dxTranslate[Key.T] = EmuKeys.T;
            dxTranslate[Key.Tab] = EmuKeys.Tab;
            dxTranslate[Key.U] = EmuKeys.U;
            dxTranslate[Key.UpArrow] = EmuKeys.UpArrow;
            dxTranslate[Key.V] = EmuKeys.V;
            dxTranslate[Key.W] = EmuKeys.W;
            dxTranslate[Key.X] = EmuKeys.X;
            dxTranslate[Key.Y] = EmuKeys.Y;
            dxTranslate[Key.Z] = EmuKeys.Z;
        }
    }
}
#endif