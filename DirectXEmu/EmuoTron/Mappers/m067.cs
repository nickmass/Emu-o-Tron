using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m067 : Mapper
    {
        private ushort irqCounter;
        private bool irqEnable;
        private bool irqLatch;
        public m067(NESCore nes)
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
            if (address >= 0x8000)
            {
                switch (address & 0xF800)
                {
                    case 0x8800:
                        nes.PPU.PPUMemory.Swap2kROM(0x0000, value % (nes.rom.vROM / 2));
                        break;
                    case 0x9800:
                        nes.PPU.PPUMemory.Swap2kROM(0x0800, value % (nes.rom.vROM / 2));
                        break;
                    case 0xA800:
                        nes.PPU.PPUMemory.Swap2kROM(0x1000, value % (nes.rom.vROM / 2));
                        break;
                    case 0xB800:
                        nes.PPU.PPUMemory.Swap2kROM(0x1800, value % (nes.rom.vROM / 2));
                        break;
                    case 0xC800:
                        if (irqLatch)
                            irqCounter = (ushort)((irqCounter & 0xFF00) | value);
                        else
                            irqCounter = (ushort)((irqCounter & 0x00FF) | (value << 8));
                        irqLatch = !irqLatch;
                        break;
                    case 0xD800:
                        irqEnable = (value & 0x10) != 0;
                        irqLatch = false;
                        interruptMapper = false;
                        break;
                    case 0xE800:
                        switch (value & 0x3)
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
                    case 0xF800:
                        nes.Memory.Swap16kROM(0x8000, value % (nes.rom.vROM / 16));
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
                    if (irqCounter == 0)
                    {
                        interruptMapper = true;
                        irqEnable = false;
                        irqCounter--;
                        break; //break out to keep it at 0xFFFF when doing multiple cycles at a time.
                    }
                    irqCounter--;
                }
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(irqCounter);
            writer.Write(irqEnable);
            writer.Write(irqLatch);
        }
        public override void StateLoad(BinaryReader reader)
        {
            irqCounter = reader.ReadUInt16();
            irqEnable = reader.ReadBoolean();
            irqLatch = reader.ReadBoolean();
        }
    }
}