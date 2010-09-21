using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.mappers
{
    class m099 : Mapper
    {
        public m099(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            if (nes.rom.vROM == 0)
                nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
            else
                nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address == 0x4016)
                nes.PPU.PPUMemory.Swap8kROM(0x0000, ((value >> 2) & 0x01));
        }
        public override byte Read(byte value, ushort address) { return value; }
        public override void IRQ(int scanline, int vblank) { }
        public override void StateLoad(System.IO.MemoryStream buf) { }
        public override void StateSave(ref System.IO.MemoryStream buf) { }
    }
}
