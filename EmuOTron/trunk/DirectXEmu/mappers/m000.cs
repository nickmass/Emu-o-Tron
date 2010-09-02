using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu.mappers
{
    class m000 : Mapper
    {
        public m000(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
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
            if (numVRom == 0)
                PPUMemory.Swap8kRAM(0x0000, 0);
            else
                PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void MapperWrite(ushort address, byte value) { }
        public override void MapperIRQ(int scanline, int vblank) { }
        public override void StateLoad(System.IO.MemoryStream buf) { }
        public override void StateSave(ref System.IO.MemoryStream buf) { }
    }
}
