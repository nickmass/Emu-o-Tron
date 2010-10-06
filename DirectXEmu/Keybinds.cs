using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using SlimDX.DirectInput;
namespace DirectXEmu
{
    public struct Keybinds
    {
        [CategoryAttribute("General")]
        public Key Rewind
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        public Key FastForward
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        public Keys SaveState
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        public Keys LoadState
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        public Keys Pause
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        public Keys Reset
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        public Keys Power
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1Up
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1Down
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1Left
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1Right
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1Start
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1Select
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1A
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1B
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1TurboA
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Key Player1TurboB
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2Up
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2Down
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2Left
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2Right
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2Start
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2Select
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2A
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2B
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2TurboA
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Key Player2TurboB
        {
            
            get;
            set;
        }
    }
}
