using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu.mappers
{
    class m034 : Mapper
    {
        public m034(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
        {
            this.numPRGRom = numPRGRom;
            this.numVRom = numVRom;
            this.Memory = Memory;
            this.PPUMemory = PPUMemory;
        }
        public override void MapperInit()
        {
            if (numVRom == 0)//BNROM
            {
                Memory.Swap32kROM(0x8000, 0);
                PPUMemory.Swap8kRAM(0x0000, 0);
            }
            else //NINA-001
            {
                Memory.Swap32kROM(0x8000, 0);
                PPUMemory.Swap8kROM(0x0000, 0);
            }
        }
        public override void MapperWrite(ushort address, byte value)
        {
            if (numVRom == 0)//BNROM
            {
                if (address >= 0x8000)
                {
                    if (Memory[address] == value)
                        Memory.Swap32kROM(0x8000, (value & 3) % (numPRGRom / 2));
                }
            }
            else //NINA-001
            {
                if (address == 0x7FFE)
                {
                    PPUMemory.Swap4kROM(0x0000, (value & 0x0F) % (numVRom * 4));
                }
                else if (address == 0x7FFF)
                {
                    PPUMemory.Swap4kROM(0x1000, (value & 0x0F) % (numVRom * 4));
                }
                else if (address == 0x7FFD)
                {
                    Memory.Swap32kROM(0x8000, (value & 1) % (numPRGRom / 2));
                }
            }
        }
        public override void MapperScanline(int scanline, int vblank) { }
    }
}
