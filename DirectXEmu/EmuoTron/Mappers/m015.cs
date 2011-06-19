using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m015 : Mapper
    {
        public m015(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap32kROM(0x8000, 0);
            nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                switch (address & 0x03)
                {
                    case 0:
                        nes.Memory.Swap16kROM(0x8000, (value & 0x3F) % (nes.rom.prgROM / 16));
                        nes.Memory.Swap16kROM(0xC000, ((value & 0x3F) | 1) % (nes.rom.prgROM / 16));
                        break;
                    case 1:
                        nes.Memory.Swap16kROM(0x8000, (value & 0x3F) % (nes.rom.prgROM / 16));
                        nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
                        break;
                    case 2:
                        nes.Memory.Swap8kROM(0x8000, (((value & 0x3F) << 1) | (value & 0x80) >> 7) % (nes.rom.prgROM / 8));
                        nes.Memory.Swap8kROM(0xA000, (((value & 0x3F) << 1) | (value & 0x80) >> 7) % (nes.rom.prgROM / 8));
                        nes.Memory.Swap8kROM(0xC000, (((value & 0x3F) << 1) | (value & 0x80) >> 7) % (nes.rom.prgROM / 8));
                        nes.Memory.Swap8kROM(0xE000, (((value & 0x3F) << 1) | (value & 0x80) >> 7) % (nes.rom.prgROM / 8));
                        break;
                    case 3:
                        nes.Memory.Swap16kROM(0x8000, (value & 0x3F) % (nes.rom.prgROM / 16));
                        nes.Memory.Swap16kROM(0xC000, (value & 0x3F) % (nes.rom.prgROM / 16));
                        break;
                }
                if ((value & 0x40) != 0)
                    nes.PPU.PPUMemory.HorizontalMirroring();
                else
                    nes.PPU.PPUMemory.VerticalMirroring();
            }
        }
    }
}
