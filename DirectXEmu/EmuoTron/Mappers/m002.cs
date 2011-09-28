using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace EmuoTron.Mappers
{
    class m002 : Mapper
    {
        public m002(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            if (nes.rom.vROM == 0)
                nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0, false);
            else
                nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                int table;
                if ((nes.rom.prgROM / 16) <= 8)
                    table = value & 0x07;
                else
                    table = value & 0x0F;
                if (value == nes.Memory[address])//Bus conflict
                    nes.Memory.Swap16kROM(0x8000, table);
            }
        }
    }
}
