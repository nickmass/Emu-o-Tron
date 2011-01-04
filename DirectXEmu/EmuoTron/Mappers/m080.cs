using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m080 : Mapper
    {
        public m080(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap8kROM(0x8000, 0 % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xA000, 1 % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xC000, 2 % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x7EF0 && address < 0x7F00)
            {
                switch (address)
                {
                    case 0x7EF0:
                        nes.PPU.PPUMemory.Swap1kROM(0x0000, (value & 0xFE) % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0400, ((value & 0xFE) + 1) % nes.rom.vROM);
                        break;
                    case 0x7EF1:
                        nes.PPU.PPUMemory.Swap1kROM(0x0800, (value & 0xFE) % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0C00, ((value & 0xFE) + 1) % nes.rom.vROM);
                        break;
                    case 0x7EF2:
                        nes.PPU.PPUMemory.Swap1kROM(0x1000, value % nes.rom.vROM);
                        break;
                    case 0x7EF3:
                        nes.PPU.PPUMemory.Swap1kROM(0x1400, value % nes.rom.vROM);
                        break;
                    case 0x7EF4:
                        nes.PPU.PPUMemory.Swap1kROM(0x1800, value % nes.rom.vROM);
                        break;
                    case 0x7EF5:
                        nes.PPU.PPUMemory.Swap1kROM(0x1C00, value % nes.rom.vROM);
                        break;
                    case 0x7EF6:
                        if ((value & 1) == 0)
                            nes.PPU.PPUMemory.HorizontalMirroring();
                        else
                            nes.PPU.PPUMemory.VerticalMirroring();
                        break;
                    case 0x7EFA:
                    case 0x7EFB:
                        nes.Memory.Swap8kROM(0x8000, value % (nes.rom.prgROM / 8));
                        break;
                    case 0x7EFC:
                    case 0x7EFD:
                        nes.Memory.Swap8kROM(0xA000, value % (nes.rom.prgROM / 8));
                        break;
                    case 0x7EFE:
                    case 0x7EFF:
                        nes.Memory.Swap8kROM(0xC000, value % (nes.rom.prgROM / 8));
                        break;
                }
            }
        }
    }
}
