using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace EmuoTron.Mappers
{
    class m184 : Mapper
    {
        public m184(NESCore nes)
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
                nes.PPU.PPUMemory.Swap4kROM(0x0000, (value & 7) % (nes.rom.vROM / 4));
                nes.PPU.PPUMemory.Swap4kROM(0x1000, ((value >> 4) & 7) % (nes.rom.vROM / 4));
            }
        }
    }
}
