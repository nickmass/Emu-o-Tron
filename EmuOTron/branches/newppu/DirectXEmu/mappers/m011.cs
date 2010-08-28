using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu.mappers
{
    class m011 : Mapper
    {
        public m011(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
        {
            this.numPRGRom = numPRGRom;
            this.numVRom = numVRom;
            this.Memory = Memory;
            this.PPUMemory = PPUMemory;
        }
        public override void MapperInit()
        {
            Memory.Swap32kROM(0x8000, 0);
            PPUMemory.Swap8kROM(0, 0);
        }
        public override void MapperWrite(ushort address, byte value)
        {
            if (address >= 0x8000)
            {
                if (Memory[address] == value)
                {
                    Memory.Swap32kROM(0x8000, (value & 0x03) % (numPRGRom / 2));
                    PPUMemory.Swap8kROM(0, ((value >> 4) & 0x0F) % numVRom);
                }
            }
        }
        public override void MapperIRQ(int scanline, int vblank) { }
        public override void MapperStateLoad(System.IO.MemoryStream buf) { }
        public override void MapperStateSave(ref System.IO.MemoryStream buf) { }
    }
}