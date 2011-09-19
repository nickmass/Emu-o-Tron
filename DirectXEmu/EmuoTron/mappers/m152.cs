using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m152 : Mapper
    {
        public m152(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
            nes.PPU.PPUMemory.ScreenOneMirroring();
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                if ((address & 0xFF) == value)
                {
                    nes.Memory.Swap16kROM(0x8000, (value >> 4) & 7);
                    nes.PPU.PPUMemory.Swap8kROM(0x0000, value & 0xF);
                    if ((value & 0x80) == 0)
                        nes.PPU.PPUMemory.ScreenOneMirroring();
                    else
                        nes.PPU.PPUMemory.ScreenTwoMirroring();
                }
            }
        }
    }
}
