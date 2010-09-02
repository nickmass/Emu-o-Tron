using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DirectXEmu.mappers
{
    public abstract class Mapper
    {
        public int mapper;
        public int numPRGRom;
        public int numVRom;
        public bool interruptMapper;
        protected MemoryStore Memory;
        protected MemoryStore PPUMemory;
        public abstract void MapperInit();
        public abstract void MapperWrite(ushort address, byte value);
        public abstract void MapperIRQ(int scanline, int vblank);
        public abstract void StateSave(ref MemoryStream buf);
        public abstract void StateLoad(MemoryStream buf);
    }
}
