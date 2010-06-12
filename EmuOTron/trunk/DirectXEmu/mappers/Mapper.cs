using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu.mappers
{
    abstract class Mapper
    {
        public int numPRGRom;
        public int numVRom;
        public bool interruptMapper;
        protected MemoryStore Memory;
        protected MemoryStore PPUMemory;
        public abstract void MapperInit();
        public abstract void MapperWrite(ushort address, byte value);
        public abstract void MapperScanline(int scanline, int vblank);

    }
}
