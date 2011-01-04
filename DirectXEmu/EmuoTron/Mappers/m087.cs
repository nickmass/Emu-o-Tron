using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m087 : Mapper
    {
        public m087(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x6000 && address < 0x8000)
            {
                nes.PPU.PPUMemory.Swap8kROM(0, (((value & 1) << 1) | ((value & 2) >> 1)) % (nes.rom.vROM / 8));
            }
        }
    }
}
