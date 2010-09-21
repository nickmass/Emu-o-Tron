using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m034 : Mapper
    {
        public m034(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            if (nes.rom.vROM == 0)//BNROM
            {
                nes.Memory.Swap32kROM(0x8000, 0);
                nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
            }
            else //NINA-001
            {
                nes.Memory.Swap32kROM(0x8000, 0);
                nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
            }
        }
        public override void Write(byte value, ushort address)
        {
            if (nes.rom.vROM == 0)//BNROM
            {
                if (address >= 0x8000)
                {
                    if (nes.Memory[address] == value)
                        nes.Memory.Swap32kROM(0x8000, (value & 3) % (nes.rom.prgROM / 32));
                }
            }
            else //NINA-001
            {
                if (address == 0x7FFE)
                {
                    nes.PPU.PPUMemory.Swap4kROM(0x0000, (value & 0x0F) % (nes.rom.vROM / 4));
                }
                else if (address == 0x7FFF)
                {
                    nes.PPU.PPUMemory.Swap4kROM(0x1000, (value & 0x0F) % (nes.rom.vROM / 4));
                }
                else if (address == 0x7FFD)
                {
                    nes.Memory.Swap32kROM(0x8000, (value & 1) % (nes.rom.prgROM / 32));
                }
            }
        }
        public override byte Read(byte value, ushort address) { return value; }
        public override void IRQ(int scanline, int vblank) { }
        public override void StateLoad(BinaryReader reader) { }
        public override void StateSave(BinaryWriter writer) { }
    }
}
