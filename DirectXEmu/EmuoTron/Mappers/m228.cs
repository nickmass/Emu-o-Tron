using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace EmuoTron.Mappers
{
    class m228 : Mapper
    {
        byte prgBank;
        byte prgMode;
        byte prgChip;
        byte chrBank;
        byte[] ramBits = new byte[4];
        public m228(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            Write(0x00, 0x8000);
        }
        public override byte Read(byte value, ushort address)
        {
            if (address >= 0x4020 && address < 0x6000)
            {
                value = ramBits[address % 4];
            }
            return value;
        }
        public override void Write(byte value, ushort address)
        {
            //  $8000-FFFF:    [.... ..CC]   Low 2 bits of CHR
            //   A~[..MH HPPP PPO. CCCC]
            //
            //    M = Mirroring (0=Vert, 1=Horz)
            //    H = PRG Chip Select
            //    P = PRG Page Select
            //    O = PRG Mode
            //    C = High 4 bits of CHR
            if (address >= 0x8000)
            {
                chrBank = (byte)((value & 0x3) | ((address & 0xF) << 2));
                prgBank = (byte)((address >> 6) & 0x1F);
                prgChip = (byte)((address >> 11) & 0x3);
                prgMode = (byte)((address >> 5) & 0x1);
                if ((address & 0x2000) == 0)
                    nes.PPU.PPUMemory.VerticalMirroring();
                else
                    nes.PPU.PPUMemory.HorizontalMirroring();
                nes.PPU.PPUMemory.Swap8kROM(0x0000, chrBank);
                if (prgChip == 1)//No chip 2 so this becomes weird. Each chip is 512kb, 512 / 16kb banks = 32
                    prgBank += 32;
                else if (prgChip == 3)
                    prgBank += 64;
                if (prgMode == 0)
                {
                    nes.Memory.Swap16kROM(0x8000, prgBank & 0xFE);
                    nes.Memory.Swap16kROM(0xC000, (prgBank & 0xFE) | 1);
                }
                else
                {
                    nes.Memory.Swap16kROM(0x8000, prgBank);
                    nes.Memory.Swap16kROM(0xC000, prgBank);
                }
            }
            else if (address >= 0x4020 && address < 0x6000)
            {
                ramBits[address % 4] = (byte)(value & 0xF);
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(ramBits[0]);
            writer.Write(ramBits[1]);
            writer.Write(ramBits[2]);
            writer.Write(ramBits[3]);
        }
        public override void StateLoad(BinaryReader reader)
        {
            ramBits[0] = reader.ReadByte();
            ramBits[1] = reader.ReadByte();
            ramBits[2] = reader.ReadByte();
            ramBits[3] = reader.ReadByte();
        }
    }
}
