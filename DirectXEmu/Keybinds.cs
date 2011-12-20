using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing;
using System.Drawing.Design;
namespace DirectXEmu
{
    public struct Keybinds 
    {
        [CategoryAttribute("General")]
        [BrowsableAttribute(true)]
        public EmuKeys Rewind
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        [BrowsableAttribute(true)]
        public EmuKeys FastForward
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        [BrowsableAttribute(true)]
        public EmuKeys SaveState
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        [BrowsableAttribute(true)]
        public EmuKeys LoadState
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        [BrowsableAttribute(true)]
        public EmuKeys Pause
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        [BrowsableAttribute(true)]
        public EmuKeys Reset
        {
            get;
            set;
        }
        [CategoryAttribute("General")]
        [BrowsableAttribute(true)]
        public EmuKeys Power
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1Up
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1Down
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1Left
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1Right
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1Start
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1Select
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1A
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1B
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1TurboA
        {
            get;
            set;
        }
        [CategoryAttribute("Player 1")]
        [BrowsableAttribute(true)]
        public EmuKeys Player1TurboB
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2Up
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2Down
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2Left
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2Right
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2Start
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2Select
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2A
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2B
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2TurboA
        {
            get;
            set;
        }
        [CategoryAttribute("Player 2")]
        [BrowsableAttribute(true)]
        public EmuKeys Player2TurboB
        {
            
            get;
            set;
        }
    }
}
