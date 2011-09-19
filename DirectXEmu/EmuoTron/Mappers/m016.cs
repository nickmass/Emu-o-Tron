using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace EmuoTron.Mappers
{
    class m016 : Mapper
    {
        ushort irqCounter;
        bool irqEnable;
        public m016(NESCore nes)
        {
            this.nes = nes;
            this.cycleIRQ = true;
        }
        public override void Power()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x6000)
            {
                switch (address & 0x000F)
                {
                    case 0:
                        nes.PPU.PPUMemory.Swap1kROM(0x0000, value);
                        break;
                    case 1:
                        nes.PPU.PPUMemory.Swap1kROM(0x0400, value);
                        break;
                    case 2:
                        nes.PPU.PPUMemory.Swap1kROM(0x0800, value);
                        break;
                    case 3:
                        nes.PPU.PPUMemory.Swap1kROM(0x0C00, value);
                        break;
                    case 4:
                        nes.PPU.PPUMemory.Swap1kROM(0x1000, value);
                        break;
                    case 5:
                        nes.PPU.PPUMemory.Swap1kROM(0x1400, value);
                        break;
                    case 6:
                        nes.PPU.PPUMemory.Swap1kROM(0x1800, value);
                        break;
                    case 7:
                        nes.PPU.PPUMemory.Swap1kROM(0x1C00, value);
                        break;
                    case 8:
                        nes.Memory.Swap16kROM(0x8000, value);
                        break;
                    case 9:
                        switch (value & 3)
                        {
                            case 0:
                                nes.PPU.PPUMemory.VerticalMirroring();
                                break;
                            case 1:
                                nes.PPU.PPUMemory.HorizontalMirroring();
                                break;
                            case 2:
                                nes.PPU.PPUMemory.ScreenOneMirroring();
                                break;
                            case 3:
                                nes.PPU.PPUMemory.ScreenTwoMirroring();
                                break;
                        }
                        break;
                    case 0xA:
                        irqEnable = (value & 1) == 1;
                        interruptMapper = false;
                        break;
                    case 0xB:
                        irqCounter = (ushort)((irqCounter & 0xFF00) | value);
                        break;
                    case 0xC:
                        irqCounter = (ushort)((value << 8) | (irqCounter & 0x00FF));
                        break;
                    case 0xD: //EEPROM I/O complex, misunderstood, and differs between mapper 16 and 159
                        break;
                }
            }
        }
        public override void IRQ(int arg)
        {
            if (irqEnable)
            {
                for (int i = 0; i < arg; i++)
                {
                    irqCounter--;
                    if (irqCounter == 0)
                        interruptMapper = true;
                }
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(irqCounter);
            writer.Write(irqEnable);
        }
        public override void StateLoad(BinaryReader reader)
        {
            irqCounter = reader.ReadUInt16();
            irqEnable = reader.ReadBoolean();
        }
    }
}
