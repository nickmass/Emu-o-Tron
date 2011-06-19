using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m013 : Mapper
    {
        public m013(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap32kROM(0x8000, 0);
            nes.PPU.PPUMemory.Swap4kRAM(0x0000, 0);
            nes.PPU.PPUMemory.Swap4kRAM(0x1000, 1);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                if (value == nes.Memory[address])
                {
                    nes.PPU.PPUMemory.Swap4kRAM(0x1000, (value & 0x3));
                }
            }
        }
    }
}
