using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace EmuoTron.Mappers
{
    class m151 : Mapper
    {
        public m151(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap8kROM(0x8000, 0);
            nes.Memory.Swap8kROM(0xA000, 1);
            nes.Memory.Swap8kROM(0xC000, 2);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            nes.PPU.PPUMemory.Swap4kROM(0x0000, 0);
            nes.PPU.PPUMemory.Swap4kROM(0x1000, 1);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                switch (address)
                {
                    case 0x8000:
                        nes.Memory.Swap8kROM(0x8000, value);
                        break;
                    case 0xA000:
                        nes.Memory.Swap8kROM(0xA000, value);
                        break;
                    case 0xC000:
                        nes.Memory.Swap8kROM(0xC000, value);
                        break;
                    case 0xE000:
                        nes.PPU.PPUMemory.Swap4kROM(0x0000, value);
                        break;
                    case 0xF000:
                        nes.PPU.PPUMemory.Swap4kROM(0x1000, value);
                        break;
                }
            }
        }
    }
}
