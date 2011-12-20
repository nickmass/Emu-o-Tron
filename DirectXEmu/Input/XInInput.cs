#if !NO_DX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.XInput;
using System.Windows.Forms;

namespace DirectXEmu
{
    class XInInput : IInput //This is really not how this should be implimented but whatev's
    {
        private Controller player1Controller;
        private Controller player2Controller;

        ControllerState currentPlayer1;
        ControllerState currentPlayer2;

        public event InputHandler InputEvent;
        public event InputScalerHandler InputScalerEvent;

        public void Create()
        {
            Reset();
        }

        public void Reset()
        {
            player1Controller = new SlimDX.XInput.Controller(UserIndex.One);
            player2Controller = new SlimDX.XInput.Controller(UserIndex.Two);
        }

        public void MainLoop()//Listen I know this is stupid, it's just I don't use my gamepad much and I really want to get slimdx out of the main program NOW.
        {
            if (player1Controller.IsConnected)
            {
                Gamepad x360State = player1Controller.GetState().Gamepad;
                ControllerState newPlayer1;
                newPlayer1.A = ((x360State.Buttons & GamepadButtonFlags.A) != 0);
                newPlayer1.B = ((x360State.Buttons & GamepadButtonFlags.B) != 0);
                newPlayer1.X = ((x360State.Buttons & GamepadButtonFlags.X) != 0);
                newPlayer1.Y = ((x360State.Buttons & GamepadButtonFlags.Y) != 0);
                newPlayer1.Up = ((x360State.Buttons & GamepadButtonFlags.DPadUp) != 0) || (x360State.LeftThumbY > 15000);
                newPlayer1.Down = ((x360State.Buttons & GamepadButtonFlags.DPadDown) != 0) || (x360State.LeftThumbY < -15000);
                newPlayer1.Left = ((x360State.Buttons & GamepadButtonFlags.DPadLeft) != 0) || (x360State.LeftThumbX < -15000);
                newPlayer1.Right = ((x360State.Buttons & GamepadButtonFlags.DPadRight) != 0) || (x360State.LeftThumbX > 15000);
                newPlayer1.Start = ((x360State.Buttons & GamepadButtonFlags.Start) != 0);
                newPlayer1.Back = ((x360State.Buttons & GamepadButtonFlags.Back) != 0);
                newPlayer1.LBumper = ((x360State.Buttons & GamepadButtonFlags.LeftShoulder) != 0);
                newPlayer1.RBumper = ((x360State.Buttons & GamepadButtonFlags.RightShoulder) != 0);
                newPlayer1.LTrigger = (x360State.LeftTrigger > 100);
                newPlayer1.RTrigger = (x360State.RightTrigger > 100);

                if (newPlayer1.A != currentPlayer1.A)
                {
                    InputEvent(EmuKeys.Joy1, newPlayer1.A);
                }
                if (newPlayer1.B != currentPlayer1.B)
                {
                    InputEvent(EmuKeys.Joy2, newPlayer1.B);
                }
                if (newPlayer1.X != currentPlayer1.X)
                {
                    InputEvent(EmuKeys.Joy3, newPlayer1.X);
                }
                if (newPlayer1.Y != currentPlayer1.Y)
                {
                    InputEvent(EmuKeys.Joy4, newPlayer1.Y);
                }
                if (newPlayer1.Up != currentPlayer1.Up)
                {
                    InputEvent(EmuKeys.JoyUp, newPlayer1.Up);
                }
                if (newPlayer1.Down != currentPlayer1.Down)
                {
                    InputEvent(EmuKeys.JoyDown, newPlayer1.Down);
                }
                if (newPlayer1.Left != currentPlayer1.Left)
                {
                    InputEvent(EmuKeys.JoyLeft, newPlayer1.Left);
                }
                if (newPlayer1.Right != currentPlayer1.Right)
                {
                    InputEvent(EmuKeys.JoyRight, newPlayer1.Right);
                }
                if (newPlayer1.Start != currentPlayer1.Start)
                {
                    InputEvent(EmuKeys.Joy5, newPlayer1.Start);
                }
                if (newPlayer1.Back != currentPlayer1.Back)
                {
                    InputEvent(EmuKeys.Joy6, newPlayer1.Back);
                }
                if (newPlayer1.LBumper != currentPlayer1.LBumper)
                {
                    InputEvent(EmuKeys.Joy7, newPlayer1.LBumper);
                }
                if (newPlayer1.RBumper != currentPlayer1.RBumper)
                {
                    InputEvent(EmuKeys.Joy8, newPlayer1.RBumper);
                }
                if (newPlayer1.LTrigger != currentPlayer1.LTrigger)
                {
                    InputEvent(EmuKeys.Joy9, newPlayer1.LTrigger);
                }
                if (newPlayer1.RTrigger != currentPlayer1.RTrigger)
                {
                    InputEvent(EmuKeys.Joy10, newPlayer1.RTrigger);
                }
                currentPlayer1 = newPlayer1;
            }
            /*
            if (player2Controller.IsConnected)
            {
                Gamepad x360State = player2Controller.GetState().Gamepad;
                ControllerState newPlayer2;
                newPlayer2.A = ((x360State.Buttons & GamepadButtonFlags.A) != 0);
                newPlayer2.B = ((x360State.Buttons & GamepadButtonFlags.B) != 0);
                newPlayer2.X = ((x360State.Buttons & GamepadButtonFlags.X) != 0);
                newPlayer2.Y = ((x360State.Buttons & GamepadButtonFlags.Y) != 0);
                newPlayer2.Up = ((x360State.Buttons & GamepadButtonFlags.DPadUp) != 0) || (x360State.LeftThumbY > 15000);
                newPlayer2.Down = ((x360State.Buttons & GamepadButtonFlags.DPadDown) != 0) || (x360State.LeftThumbY < -15000);
                newPlayer2.Left = ((x360State.Buttons & GamepadButtonFlags.DPadLeft) != 0) || (x360State.LeftThumbX < -15000);
                newPlayer2.Right = ((x360State.Buttons & GamepadButtonFlags.DPadRight) != 0) || (x360State.LeftThumbX > 15000);
                newPlayer2.Start = ((x360State.Buttons & GamepadButtonFlags.Start) != 0);
                newPlayer2.Back = ((x360State.Buttons & GamepadButtonFlags.Back) != 0);
                newPlayer2.LBumper = ((x360State.Buttons & GamepadButtonFlags.LeftShoulder) != 0);
                newPlayer2.RBumper = ((x360State.Buttons & GamepadButtonFlags.RightShoulder) != 0);
                newPlayer2.LTrigger = (x360State.LeftTrigger > 100);
                newPlayer2.RTrigger = (x360State.RightTrigger > 100);

                if (newPlayer2.A != currentPlayer2.A)
                {
                    if (newPlayer2.A)
                        KeyDownEvent(this, Keys.NumPad1);
                    else
                        KeyUpEvent(this, Keys.NumPad1);
                }
                if (newPlayer2.B != currentPlayer2.B)
                {
                    if (newPlayer2.B)
                        KeyDownEvent(this, Keys.NumPad3);
                    else
                        KeyUpEvent(this, Keys.NumPad3);
                }
                if (newPlayer2.X != currentPlayer2.X)
                {
                    if (newPlayer2.X)
                        KeyDownEvent(this, Keys.Home);
                    else
                        KeyUpEvent(this, Keys.Home);
                }
                if (newPlayer2.Y != currentPlayer2.Y)
                {
                    if (newPlayer2.Y)
                        KeyDownEvent(this, Keys.End);
                    else
                        KeyUpEvent(this, Keys.End);
                }
                if (newPlayer2.Up != currentPlayer2.Up)
                {
                    if (newPlayer2.Up)
                        KeyDownEvent(this, Keys.NumPad8);
                    else
                        KeyUpEvent(this, Keys.NumPad8);
                }
                if (newPlayer2.Down != currentPlayer2.Down)
                {
                    if (newPlayer2.Down)
                        KeyDownEvent(this, Keys.NumPad5);
                    else
                        KeyUpEvent(this, Keys.NumPad5);
                }
                if (newPlayer2.Left != currentPlayer2.Left)
                {
                    if (newPlayer2.Left)
                        KeyDownEvent(this, Keys.NumPad4);
                    else
                        KeyUpEvent(this, Keys.NumPad4);
                }
                if (newPlayer2.Right != currentPlayer2.Right)
                {
                    if (newPlayer2.Right)
                        KeyDownEvent(this, Keys.NumPad6);
                    else
                        KeyUpEvent(this, Keys.NumPad6);
                }
                if (newPlayer2.Start != currentPlayer2.Start)
                {
                    if (newPlayer2.Start)
                        KeyDownEvent(this, Keys.NumPad7);
                    else
                        KeyUpEvent(this, Keys.NumPad7);
                }
                if (newPlayer2.Back != currentPlayer2.Back)
                {
                    if (newPlayer2.Back)
                        KeyDownEvent(this, Keys.NumPad9);
                    else
                        KeyUpEvent(this, Keys.NumPad9);
                }
                currentPlayer2 = newPlayer2;
            }*/
        }

        public void Destroy()
        {
        }

        struct ControllerState
        {
            public bool A;
            public bool B;
            public bool X;
            public bool Y;
            public bool Start;
            public bool Back;
            public bool Up;
            public bool Down;
            public bool Left;
            public bool Right;
            public bool RTrigger;
            public bool RBumper;
            public bool LTrigger;
            public bool LBumper;
        }
    }
}

#endif