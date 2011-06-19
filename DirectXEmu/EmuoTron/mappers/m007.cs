using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m007 : Mapper
    {
        public m007(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap32kROM(0x8000, 0);
            nes.PPU.PPUMemory.Swap8kRAM(0, 0);
            nes.PPU.PPUMemory.ScreenOneMirroring();
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                //if (Memory[address] == value) Should have bus conflicts on most carts, but this kills marble maddness and doesnt seem to fix anything else, will have to wait for when board types are in.
                {
                    nes.Memory.Swap32kROM(0x8000, (value) % (nes.rom.prgROM / 32));
                    if ((value & 0x10) == 0)
                        nes.PPU.PPUMemory.ScreenOneMirroring();
                    else
                        nes.PPU.PPUMemory.ScreenTwoMirroring();
                }
            }
        }
    }
}
