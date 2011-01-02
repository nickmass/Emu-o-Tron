using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace EmuoTron.Mappers
{
    class m226 : Mapper
    {
        byte prgBank;
        byte prgMode;
        public m226(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                if (address == 0x8001)
                {
                    prgBank = (byte)((prgBank & 0xBF) | ((value & 1) << 6));
                }
                else
                {
                    prgBank = (byte)((prgBank & 0xE0) | (value & 0x1F));
                    prgBank = (byte)((prgBank & 0xDF) | ((value & 0x80) >> 2));
                    prgMode = (byte)((value >> 5) & 1);
                    if ((value & 0x40) == 0)
                        nes.PPU.PPUMemory.HorizontalMirroring();
                    else
                        nes.PPU.PPUMemory.VerticalMirroring();

                }
                PrgSync();
            }
        }
        private void PrgSync()
        {
            if (prgMode == 0)
            {
                nes.Memory.Swap16kROM(0x8000, (prgBank & 0xFE) % (nes.rom.prgROM / 16));
                nes.Memory.Swap16kROM(0xC000, ((prgBank & 0xFE)+1) % (nes.rom.prgROM / 16));
            }
            else
            {
                nes.Memory.Swap16kROM(0x8000, (prgBank) % (nes.rom.prgROM / 16));
                nes.Memory.Swap16kROM(0xC000, (prgBank) % (nes.rom.prgROM / 16));
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(prgBank);
            writer.Write(prgMode);
        }
        public override void StateLoad(BinaryReader reader)
        {
            prgBank = reader.ReadByte();
            prgMode = reader.ReadByte();
        }
    }
}
