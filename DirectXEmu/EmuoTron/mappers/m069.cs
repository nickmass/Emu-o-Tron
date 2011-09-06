using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m069 : Mapper
    {
        private int indexReg;
        private int irqCounter;
        private bool irqEnabled;
        private bool irqCountdown;
        public m069(NESCore nes)
        {
            this.nes = nes;
            this.cycleIRQ = true;
            this.nes.APU.external = new Channels.FME7();
        }
        public override void Power()
        {
            nes.Memory.Swap8kROM(0x8000, 0);
            nes.Memory.Swap8kROM(0xA000, 1);
            nes.Memory.Swap8kROM(0xC000, 2);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            nes.PPU.PPUMemory.Swap1kROM(0x0000, 0);
            nes.PPU.PPUMemory.Swap1kROM(0x0400, 1);
            nes.PPU.PPUMemory.Swap1kROM(0x0800, 2);
            nes.PPU.PPUMemory.Swap1kROM(0x0C00, 3);
            nes.PPU.PPUMemory.Swap1kROM(0x1000, 4);
            nes.PPU.PPUMemory.Swap1kROM(0x1400, 5);
            nes.PPU.PPUMemory.Swap1kROM(0x1800, 6);
            nes.PPU.PPUMemory.Swap1kROM(0x1C00, 7);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                address &= 0xE000;
                nes.APU.external.Write(value, address);
                if (address == 0x8000)
                {
                    indexReg = value & 0xF;
                }
                else if (address == 0xA000)
                {
                    switch (indexReg)
                    {
                        case 0:
                            nes.PPU.PPUMemory.Swap1kROM(0x0000, value % nes.rom.vROM);
                            break;
                        case 1:
                            nes.PPU.PPUMemory.Swap1kROM(0x0400, value % nes.rom.vROM);
                            break;
                        case 2:
                            nes.PPU.PPUMemory.Swap1kROM(0x0800, value % nes.rom.vROM);
                            break;
                        case 3:
                            nes.PPU.PPUMemory.Swap1kROM(0x0C00, value % nes.rom.vROM);
                            break;
                        case 4:
                            nes.PPU.PPUMemory.Swap1kROM(0x1000, value % nes.rom.vROM);
                            break;
                        case 5:
                            nes.PPU.PPUMemory.Swap1kROM(0x1400, value % nes.rom.vROM);
                            break;
                        case 6:
                            nes.PPU.PPUMemory.Swap1kROM(0x1800, value % nes.rom.vROM);
                            break;
                        case 7:
                            nes.PPU.PPUMemory.Swap1kROM(0x1C00, value % nes.rom.vROM);
                            break;
                        case 8:
                            if ((value & 0x40) == 0)
                                nes.Memory.Swap8kROM(0x6000, (value & 0x1F) % (nes.rom.prgROM / 8));
                            else if ((value & 0x80) == 0)
                                nes.Memory.Swap8kROM(0x6000, (value & 0x1F) % (nes.rom.prgROM / 8));
                            else
                                nes.Memory.Swap8kRAM(0x6000, 0x3 - (nes.Memory.swapOffset / 8));//really crazy, may not work, also wont be paged.
                            break;
                        case 9:
                            nes.Memory.Swap8kROM(0x8000, value % (nes.rom.prgROM / 8));
                            break;
                        case 10:
                            nes.Memory.Swap8kROM(0xA000, value % (nes.rom.prgROM / 8));
                            break;
                        case 11:
                            nes.Memory.Swap8kROM(0xC000, value % (nes.rom.prgROM / 8));
                            break;
                        case 12:
                            value &= 0x3;
                            if (value == 0)
                                nes.PPU.PPUMemory.VerticalMirroring();
                            else if (value == 1)
                                nes.PPU.PPUMemory.HorizontalMirroring();
                            else if (value == 2)
                                nes.PPU.PPUMemory.ScreenOneMirroring();
                            else
                                nes.PPU.PPUMemory.ScreenTwoMirroring();
                            break;
                        case 13:
                            irqEnabled = (value & 0x01) != 0;
                            irqCountdown = (value & 0x80) != 0;
                            if (!irqEnabled)
                                interruptMapper = false;
                            break;
                        case 14:
                            irqCounter = (irqCounter & 0xFF00) + value;
                            break;
                        case 15:
                            irqCounter = (irqCounter & 0x00FF) + (value << 8);
                            break;

                    }
                }
            }
        }
        public override void IRQ(int scanline)
        {
            if (irqCountdown)
            {
                irqCounter -= scanline;
                if (irqCounter < 0)
                {
                    irqCounter &= 0xFFFF;
                    if (irqEnabled)
                        interruptMapper = true;
                }
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(indexReg);
            writer.Write(irqCounter);
            writer.Write(irqEnabled);
            writer.Write(irqCountdown);
        }
        public override void StateLoad(BinaryReader reader)
        {
            indexReg = reader.ReadInt32();
            irqCounter = reader.ReadInt32();
            irqEnabled = reader.ReadBoolean();
            irqCountdown = reader.ReadBoolean();
        }
    }
}