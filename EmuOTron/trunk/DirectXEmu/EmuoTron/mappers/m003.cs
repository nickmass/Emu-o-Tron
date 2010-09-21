using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.mappers
{
    class m003 : Mapper
    {
        public m003(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                int table = value & 3;
                if (nes.rom.vROM != 0)
                    table = table % (nes.rom.vROM / 8);
                if (value == nes.Memory[address]) //Bus Conflict
                    nes.PPU.PPUMemory.Swap8kROM(0x0000, table);

            }
        }
        public override byte Read(byte value, ushort address) { return value; }
        public override void IRQ(int scanline, int vblank) { }
        public override void StateLoad(System.IO.MemoryStream buf) { }
        public override void StateSave(ref System.IO.MemoryStream buf) { }
    }
}
