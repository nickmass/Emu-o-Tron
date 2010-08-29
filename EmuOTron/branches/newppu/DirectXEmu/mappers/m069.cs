using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DirectXEmu.mappers
{
    class m069 : Mapper
    {
        private int indexReg;
        private int irqCounter;
        private bool irqEnabled;
        private bool irqCountdown;
        public m069(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
        {
            this.numPRGRom = numPRGRom;
            this.numVRom = numVRom;
            this.Memory = Memory;
            this.PPUMemory = PPUMemory;
            this.mapper = 69;
        }
        public override void MapperInit()
        {
            Memory.Swap8kROM(0x8000, 0);
            Memory.Swap8kROM(0xA000, 1);
            Memory.Swap8kROM(0xC000, 2);
            Memory.Swap8kROM(0xE000, (numPRGRom * 2) - 1);
            PPUMemory.Swap1kROM(0x0000, 0);
            PPUMemory.Swap1kROM(0x0400, 1);
            PPUMemory.Swap1kROM(0x0800, 2);
            PPUMemory.Swap1kROM(0x0C00, 3);
            PPUMemory.Swap1kROM(0x1000, 4);
            PPUMemory.Swap1kROM(0x1400, 5);
            PPUMemory.Swap1kROM(0x1800, 6);
            PPUMemory.Swap1kROM(0x1C00, 7);
        }
        public override void MapperWrite(ushort address, byte value)
        {
            if (address == 0x8000)
            {
                indexReg = value & 0xF;
            }
            else if (address == 0xA000)
            {
                switch (indexReg)
                {
                    case 0:
                        PPUMemory.Swap1kROM(0x0000, value % (numVRom * 8));
                        break;
                    case 1:
                        PPUMemory.Swap1kROM(0x0400, value % (numVRom * 8));
                        break;
                    case 2:
                        PPUMemory.Swap1kROM(0x0800, value % (numVRom * 8));
                        break;
                    case 3:
                        PPUMemory.Swap1kROM(0x0C00, value % (numVRom * 8));
                        break;
                    case 4:
                        PPUMemory.Swap1kROM(0x1000, value % (numVRom * 8));
                        break;
                    case 5:
                        PPUMemory.Swap1kROM(0x1400, value % (numVRom * 8));
                        break;
                    case 6:
                        PPUMemory.Swap1kROM(0x1800, value % (numVRom * 8));
                        break;
                    case 7:
                        PPUMemory.Swap1kROM(0x1C00, value % (numVRom * 8));
                        break;
                    case 8:
                        if ((value & 0x40) == 0)
                            Memory.Swap8kROM(0x6000, (value & 0x1F) % (numPRGRom * 2));
                        else if ((value & 0x80) == 0)
                            Memory.Swap8kROM(0x6000, (value & 0x1F) % (numPRGRom * 2));
                        else
                            Memory.Swap8kRAM(0x6000, 0x3 - (Memory.swapOffset / 8));//really crazy, may not work, also wont be paged.
                        break;
                    case 9:
                        Memory.Swap8kROM(0x8000, value % (numPRGRom * 2));
                        break;
                    case 10:
                        Memory.Swap8kROM(0xA000, value % (numPRGRom * 2));
                        break;
                    case 11:
                        Memory.Swap8kROM(0xC000, value % (numPRGRom * 2));
                        break;
                    case 12:
                        value &= 0x3;
                        if (value == 0)
                            PPUMemory.VerticalMirroring();
                        else if (value == 1)
                            PPUMemory.HorizontalMirroring();
                        else if (value == 2)
                            PPUMemory.ScreenOneMirroring();
                        else
                            PPUMemory.ScreenTwoMirroring();
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
        public override void MapperIRQ(int scanline, int vblank)
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
        public override void MapperStateSave(ref MemoryStream buf)
        {
            BinaryWriter writer = new BinaryWriter(buf);
            writer.Write(indexReg);
            writer.Write(irqCounter);
            writer.Write(irqEnabled);
            writer.Write(irqCountdown);
            writer.Flush();
        }
        public override void MapperStateLoad(MemoryStream buf)
        {
            BinaryReader reader = new BinaryReader(buf);
            indexReg = reader.ReadInt32();
            irqCounter = reader.ReadInt32();
            irqEnabled = reader.ReadBoolean();
            irqCountdown = reader.ReadBoolean();
        }
    }
}