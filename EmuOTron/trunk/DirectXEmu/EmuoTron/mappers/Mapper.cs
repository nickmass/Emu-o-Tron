using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    public abstract class Mapper
    {
        protected NESCore nes;
        public bool interruptMapper;
        public abstract void Init();
        public abstract byte Read(byte value, ushort address);
        public abstract void Write(byte value, ushort address);
        public abstract void IRQ(int scanline, int vblank);
        public abstract void StateSave(BinaryWriter writer);
        public abstract void StateLoad(BinaryReader reader);
    }
}
