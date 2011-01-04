using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m048: Mapper
    {
        private byte irqCounter;
        private byte irqLatch;
        private bool irqReload;
        private bool irqEnable;
        public m048(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap8kROM(0x8000, 0 % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xA000, 1 % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) - 2);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                switch (address & 0xE003)
                {
                    case 0x8000:
                        nes.Memory.Swap8kROM(0x8000, value % (nes.rom.prgROM / 8));
                        break;
                    case 0x8001:
                        nes.Memory.Swap8kROM(0xA000, value % (nes.rom.prgROM / 8));
                        break;
                    case 0x8002:
                        nes.PPU.PPUMemory.Swap2kROM(0x0000, value % (nes.rom.vROM / 2));
                        break;
                    case 0x8003:
                        nes.PPU.PPUMemory.Swap2kROM(0x0800, value % (nes.rom.vROM / 2));
                        break;
                    case 0xA000:
                        nes.PPU.PPUMemory.Swap1kROM(0x1000, value % nes.rom.vROM);
                        break;
                    case 0xA001:
                        nes.PPU.PPUMemory.Swap1kROM(0x1400, value % nes.rom.vROM);
                        break;
                    case 0xA002:
                        nes.PPU.PPUMemory.Swap1kROM(0x1800, value % nes.rom.vROM);
                        break;
                    case 0xA003:
                        nes.PPU.PPUMemory.Swap1kROM(0x1C00, value % nes.rom.vROM);
                        break;
                    case 0xC000:
                        irqLatch = (byte)(value ^ 0xFF);
                        break;
                    case 0xC001:
                        irqReload = true;
                        break;
                    case 0xC002:
                        irqEnable = true;
                        break;
                    case 0xC003:
                        irqEnable = false;
                        interruptMapper = false;
                        break;
                    case 0xE000:
                        if ((value & 0x40) == 0)
                            nes.PPU.PPUMemory.VerticalMirroring();
                        else
                            nes.PPU.PPUMemory.HorizontalMirroring();
                        break;
                }
            }
        }
        public override void IRQ(int scanline)
        {
            bool wasNotZero = (irqCounter != 0);
            if (irqCounter == 0 || irqReload)
            {
                irqCounter = irqLatch;
                if (irqLatch == 0 && irqEnable)
                    interruptMapper = true;
            }
            else
                irqCounter--;
            if (irqCounter == 0 && wasNotZero && irqEnable)
                interruptMapper = true;
            if (irqLatch != 0)
                irqReload = false;
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(irqCounter);
            writer.Write(irqLatch);
            writer.Write(irqReload);
            writer.Write(irqEnable);
        }
        public override void StateLoad(BinaryReader reader)
        {
            irqCounter = reader.ReadByte();
            irqLatch = reader.ReadByte();
            irqReload = reader.ReadBoolean();
            irqEnable = reader.ReadBoolean();
        }
    }
}
