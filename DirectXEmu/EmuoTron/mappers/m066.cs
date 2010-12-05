using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m066 : Mapper
    {
        public m066(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap32kROM(0x8000, 0);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                nes.Memory.Swap32kROM(0x8000, ((value >> 4) & 3) % (nes.rom.prgROM / 32));
                nes.PPU.PPUMemory.Swap8kROM(0x0000, (value & 3) % (nes.rom.vROM / 8));
            }
        }
    }
}