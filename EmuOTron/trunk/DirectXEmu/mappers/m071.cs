using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu.mappers
{
    class m071 : Mapper
    {
        public m071(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
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
            PPUMemory.Swap8kRAM(0x0000, 0);
            //if Fire Hawk
            //PPUMemory.ScreenOneMirroring();
        }
        public override void MapperWrite(ushort address, byte value)
        {
            if (address >= 0xC000 && address <= 0xFFFF)
                Memory.Swap16kROM(0x8000, value % numPRGRom);
            //if Fire Hawk
            /*
            if (address >= 0x8000 && address <= 0x9FFF)
                if ((value & 0x10) != 0)
                    PPUMemory.ScreenOneMirroring();
                else
                    PPUMemory.ScreenTwoMirroring();
             */
        }
        public override void MapperScanline(int scanline, int vblank) { }
        public override void MapperStateLoad(System.IO.MemoryStream buf) { }
        public override void MapperStateSave(ref System.IO.MemoryStream buf) { }
    }
}
