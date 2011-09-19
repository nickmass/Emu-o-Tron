using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Inputs
{
    class Zapper : Input
    {
        int playerNum;
        public Zapper(NESCore nes, Port port)
        {
            this.nes = nes;
            this.port = port;
            if (port == Port.PortOne)
            {
                playerNum = 0;
            }
            else if (port == Port.PortTwo)
            {
                playerNum = 1;
            }
        }
        public override byte Read(ushort address)
        {
            byte value = 0;
            if (nes.players[playerNum].triggerPulled)
                value |= 0x10;
            int whitePixels = 0;
            for (int y = -3; y <= 3; y++)
            {
                int adjY = y + nes.players[playerNum].y;
                if (adjY <= nes.PPU.scanline && adjY < 240 && adjY >= 0 && adjY > nes.PPU.scanline - 32)
                {
                    for (int x = -3; x <= 3; x++)
                    {
                        int adjX = nes.players[playerNum].x + x ;
                        if (adjX < 256 && adjX >= 0 && ((adjX <= nes.PPU.scanlineCycle && adjY == nes.PPU.scanline) || (adjY != nes.PPU.scanline && adjY != nes.PPU.scanline - 31) || (adjX > nes.PPU.scanlineCycle && adjY == nes.PPU.scanlineCycle - 31)))
                        {
                            uint screenPixel = nes.PPU.screen[nes.players[playerNum].y + y, nes.players[playerNum].x + x];
                            if ((screenPixel & 0x808080) != 0x00) //Crappy palette dependant light detect, I suppose your TVs settings could affect the lightgun too (maybe).
                                whitePixels++;
                        }
                    }
                }
            }
            if(whitePixels < 36)
                value |= 0x08;
            return value;
        }
    }
}
