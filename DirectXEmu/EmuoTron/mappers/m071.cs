using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m071 : Mapper
    {
        public m071(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0, false);
            //if Fire Hawk
            if (nes.rom.crc == 0x1BC686A8)
            {
                nes.PPU.PPUMemory.ScreenOneMirroring();
                nes.debug.LogInfo("Fire Hawk mapper hack.");
            }
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0xC000)
                nes.Memory.Swap16kROM(0x8000, value);
            //if Fire Hawk
            if (nes.rom.crc == 0x1BC686A8)
            {
                if (address >= 0x8000 && address <= 0x9FFF)
                    if ((value & 0x10) != 0)
                        nes.PPU.PPUMemory.ScreenOneMirroring();
                    else
                        nes.PPU.PPUMemory.ScreenTwoMirroring();
             }
        }
    }
}
