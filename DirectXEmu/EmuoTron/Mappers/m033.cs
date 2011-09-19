using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m033 : Mapper
    {
        public m033(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap8kROM(0x8000, 0);
            nes.Memory.Swap8kROM(0xA000, 1);
            nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) - 2);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000 && address < 0xC000)
            {
                switch (address & 0xA003)
                {
                    case 0x8000:
                        nes.Memory.Swap8kROM(0x8000, value & 0x3F);
                        if ((value & 0x40) == 0)
                            nes.PPU.PPUMemory.VerticalMirroring();
                        else
                            nes.PPU.PPUMemory.HorizontalMirroring();
                        break;
                    case 0x8001:
                        nes.Memory.Swap8kROM(0xA000, value & 0x3F);
                        break;
                    case 0x8002:
                        nes.PPU.PPUMemory.Swap2kROM(0x0000, value);
                        break;
                    case 0x8003:
                        nes.PPU.PPUMemory.Swap2kROM(0x0800, value);
                        break;
                    case 0xA000:
                        nes.PPU.PPUMemory.Swap1kROM(0x1000, value);
                        break;
                    case 0xA001:
                        nes.PPU.PPUMemory.Swap1kROM(0x1400, value);
                        break;
                    case 0xA002:
                        nes.PPU.PPUMemory.Swap1kROM(0x1800, value);
                        break;
                    case 0xA003:
                        nes.PPU.PPUMemory.Swap1kROM(0x1C00, value);
                        break;
                }
            }
        }
    }
}
