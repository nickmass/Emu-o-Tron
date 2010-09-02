using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu.mappers
{
    class m003 : Mapper
    {
        public m003(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
        {
            this.numPRGRom = numPRGRom;
            this.numVRom = numVRom;
            this.Memory = Memory;
            this.PPUMemory = PPUMemory;
        }
        public override void MapperInit()
        {
            Memory.Swap16kROM(0x8000, 0);
            Memory.Swap16kROM(0xC000, numPRGRom - 1);
            PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void MapperWrite(ushort address, byte value)
        {
            if (address >= 0x8000)
            {
                int table = value & 3;
                if (numVRom != 0)
                    table = table % numVRom;
                if (value == this.Memory[address]) //Bus Conflict
                    PPUMemory.Swap8kROM(0x0000, table);

            }
        }
        public override void MapperIRQ(int scanline, int vblank) { }
        public override void StateLoad(System.IO.MemoryStream buf) { }
        public override void StateSave(ref System.IO.MemoryStream buf) { }
    }
}
