using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Inputs
{
    public abstract class Input
    {
        protected NESCore nes;
        protected Port port;
        public virtual void Power() { }
        public virtual byte Read(byte value, ushort address) { return value; }
        public virtual void Write(byte value, ushort address) { }
        public virtual void StateSave(BinaryWriter writer) { }
        public virtual void StateLoad(BinaryReader reader) { }

    }
    public enum Port
    {
        PortOne,
        PortTwo
    }
}
