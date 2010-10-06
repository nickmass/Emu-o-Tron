using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m000 : Mapper
    {
        public m000(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            if (nes.rom.vROM == 0)
                nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
            else
                nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
    }
}
