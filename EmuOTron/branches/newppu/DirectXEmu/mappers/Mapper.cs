using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DirectXEmu.mappers
{
    abstract class Mapper
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
        public abstract void MapperStateSave(ref MemoryStream buf);
        public abstract void MapperStateLoad(MemoryStream buf);
    }
}
