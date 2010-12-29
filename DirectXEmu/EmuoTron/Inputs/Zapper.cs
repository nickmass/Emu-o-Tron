using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Inputs
{
    class Zapper : Input
    {
        int readAddress;
        int playerNum;
        public Zapper(NESCore nes, Port port)
        {
            this.nes = nes;
            this.port = port;
            if (port == Port.PortOne)
            {
                readAddress = 0x4016;
                playerNum = 0;
            }
            else if (port == Port.PortTwo)
            {
                readAddress = 0x4017;
                playerNum = 1;
            }
        }
        public override byte Read(byte value, ushort address)
        {
            if (address == readAddress)
            {
                if (nes.players[playerNum].triggerPulled)
                    value |= 0x10;
                if (!(((nes.PPU.screen[nes.players[playerNum].x, nes.players[playerNum].y] & 0x3F) == 0x20) || ((nes.PPU.screen[nes.players[playerNum].x, nes.players[playerNum].y] & 0x3F) == 0x30)))
                    value |= 0x08;
            }
            return value;
        }
    }
}
