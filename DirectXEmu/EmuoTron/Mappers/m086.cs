using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m086 : Mapper
    {
        public m086(NESCore nes)
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
            if (address >= 0x6000 && address < 0x7000)
            {
                nes.Memory.Swap32kROM(0x8000, (value >> 4) % (nes.rom.prgROM / 32));
                nes.PPU.PPUMemory.Swap8kROM(0, ((value & 3) | ((value & 0x40) >> 4)) % (nes.rom.vROM / 8));
            }
        }
    }
}
