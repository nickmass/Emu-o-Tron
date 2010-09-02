using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu.mappers
{
    class m070 : Mapper
    {
        public m070(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
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
            PPUMemory.ScreenOneMirroring();
        }
        public override void MapperWrite(ushort address, byte value)
        {
            if (address >= 0xC000 && address < 0xC100)
            {
                if ((address & 0xFF) == value)
                {
                    Memory.Swap16kROM(0x8000, ((value >> 4) & 7) % numPRGRom);
                    PPUMemory.Swap8kROM(0x0000, (value & 0xF) % numVRom);
                    if ((value & 0x80) == 0)
                        PPUMemory.ScreenOneMirroring();
                    else
                        PPUMemory.ScreenTwoMirroring();
                }
            }
        }
        public override void MapperIRQ(int scanline, int vblank) { }
        public override void StateLoad(System.IO.MemoryStream buf) { }
        public override void StateSave(ref System.IO.MemoryStream buf) { }
    }
}
