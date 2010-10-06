using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m070 : Mapper
    {
        public m070(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
            nes.PPU.PPUMemory.ScreenOneMirroring();
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0xC000 && address < 0xC100)
            {
                if ((address & 0xFF) == value)
                {
                    nes.Memory.Swap16kROM(0x8000, ((value >> 4) & 7) % (nes.rom.prgROM / 16));
                    nes.PPU.PPUMemory.Swap8kROM(0x0000, (value & 0xF) % (nes.rom.vROM / 8));
                    if ((value & 0x80) == 0)
                        nes.PPU.PPUMemory.ScreenOneMirroring();
                    else
                        nes.PPU.PPUMemory.ScreenTwoMirroring();
                }
            }
        }
    }
}
