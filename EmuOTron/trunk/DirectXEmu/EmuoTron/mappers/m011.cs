using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m011 : Mapper
    {
        public m011(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap32kROM(0x8000, 0);
            nes.PPU.PPUMemory.Swap8kROM(0, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                if (nes.Memory[address] == value)
                {
                    nes.Memory.Swap32kROM(0x8000, (value & 0x03) % (nes.rom.prgROM / 32));
                    nes.PPU.PPUMemory.Swap8kROM(0, ((value >> 4) & 0x0F) % (nes.rom.vROM / 8));
                }
            }
        }
        public override byte Read(byte value, ushort address) { return value; }
        public override void IRQ(int scanline, int vblank) { }
        public override void StateLoad(BinaryReader reader) { }
        public override void StateSave(BinaryWriter writer) { }
    }
}