using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu.mappers
{
    class m007 : Mapper
    {
        public m007(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
        {
            this.numPRGRom = numPRGRom;
            this.numVRom = numVRom;
            this.Memory = Memory;
            this.PPUMemory = PPUMemory;
        }
        public override void MapperInit()
        {
            Memory.Swap32kROM(0x8000, 0);
            PPUMemory.Swap8kRAM(0, 0);
            PPUMemory.ScreenOneMirroring();
        }
        public override void MapperWrite(ushort address, byte value)
        {
            if (address >= 0x8000)
            {
                //if (Memory[address] == value) Should have bus conflicts on most carts, but this kills marble maddness and doesnt seem to fix anything else, will have to wait for when board types are in.
                {
                    Memory.Swap32kROM(0x8000, (value & 0x07) % (numPRGRom / 2));
                    if ((value & 0x10) == 0)
                        PPUMemory.ScreenOneMirroring();
                    else
                        PPUMemory.ScreenTwoMirroring();
                }
            }
        }
        public override void MapperIRQ(int scanline, int vblank) { }
        public override void MapperStateLoad(System.IO.MemoryStream buf) { }
        public override void MapperStateSave(ref System.IO.MemoryStream buf) { }
    }
}
