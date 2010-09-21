using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.mappers
{
    class m071 : Mapper
    {
        public m071(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
            //if Fire Hawk
            //PPUMemory.ScreenOneMirroring();
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0xC000 && address <= 0xFFFF)
                nes.Memory.Swap16kROM(0x8000, value % (nes.rom.prgROM / 16));
            //if Fire Hawk
            /*
            if (address >= 0x8000 && address <= 0x9FFF)
                if ((value & 0x10) != 0)
                    PPUMemory.ScreenOneMirroring();
                else
                    PPUMemory.ScreenTwoMirroring();
             */
        }
        public override byte Read(byte value, ushort address) { return value; }
        public override void IRQ(int scanline, int vblank) { }
        public override void StateLoad(System.IO.MemoryStream buf) { }
        public override void StateSave(ref System.IO.MemoryStream buf) { }
    }
}
