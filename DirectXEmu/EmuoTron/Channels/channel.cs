using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Channels
{
    public abstract class Channel
    {
        protected NESCore nes;
        public virtual void Power() { }
        public virtual byte Read(byte value, ushort address) { return value; }
        public virtual void Write(byte value, ushort address) { }
        public virtual byte Cycle() { return 0; }
        public virtual void HalfFrame() { }
        public virtual void QuarterFrame() { }
        public virtual void StateSave(BinaryWriter writer) { }
        public virtual void StateLoad(BinaryReader reader) { }
    }
}
