using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Inputs
{
    class Unisystem : Input
    {
        public Unisystem(NESCore nes)
        {
            this.nes = nes;
        }
        public override byte Read(ushort address)
        {
            byte value = 0;
            if (address == 0x4016)
            {
                //nextbyte should be coming from controller reg with data in bit 1
                /*
                    * Port 4016h/Read:
                    Bit2    Credit Service Button       (0=Released, 1=Service Credit)
                    Bit3-4  DIP Switch 1-2              (0=Off, 1=On)
                    Bit5-6  Credit Left/Right Coin Slot (0=None, 1=Coin) (Acknowledge via 4020h)
                    */
                if (nes.creditService)
                    value |= 0x04;
                else
                    value &= 0xFB;
                if (nes.dip1)
                    value |= 0x08;
                else
                    value &= 0xF7;
                if (nes.dip2)
                    value |= 0x10;
                else
                    value &= 0xEF;
                if (nes.players[0].coin)
                    value |= 0x20;
                else
                    value &= 0xDF;
                if (nes.players[1].coin)
                    value |= 0x40;
                else
                    value &= 0xBF;
            }
            else if (address == 0x4017)
            {
                if (nes.dip3)
                    value |= 0x04;
                else
                    value &= 0xFB;
                if (nes.dip4)
                    value |= 0x08;
                else
                    value &= 0xF7;
                if (nes.dip5)
                    value |= 0x10;
                else
                    value &= 0xEF;
                if (nes.dip6)
                    value |= 0x20;
                else
                    value &= 0xDF;
                if (nes.dip7)
                    value |= 0x40;
                else
                    value &= 0xBF;
                if (nes.dip8)
                    value |= 0x80;
                else
                    value &= 0x7F;
            }
            return value;
        }
        public override void Write(byte value, ushort address)
        {
            if (address == 0x4020)
            {
                if ((value & 1) != 0)
                {
                    nes.players[0].coin = false;
                    nes.players[1].coin = false;
                }
            }
        }
    }
}
