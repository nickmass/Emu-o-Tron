using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
namespace DirectXEmu
{
    public struct Keybinds
    {
        [CategoryAttribute("General")]
        public Keys Rewind
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        public Keys FastForward
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
        public Keys Player1Up
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Keys Player1Down
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Keys Player1Left
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Keys Player1Right
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Keys Player1Start
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Keys Player1Select
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Keys Player1A
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Keys Player1B
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Keys Player1TurboA
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        public Keys Player1TurboB
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2Up
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2Down
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2Left
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2Right
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2Start
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2Select
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2A
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2B
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2TurboA
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        public Keys Player2TurboB
        {
            
            get;
            set;
        }
    }
}
