using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m075 : Mapper
    {
        private byte prgReg0;
        private byte prgReg1;
        private byte prgReg2;
        private byte chrReg0;
        private byte chrReg1;

        public m075(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap8kROM(0x8000, 0);
            nes.Memory.Swap8kROM(0xA000, 1);
            nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) - 2);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {

            byte highAddr = (byte)(address >> 12);
            if (highAddr == 0x8)
            {
                prgReg0 = (byte)(value & 0xF);
                SyncPrg();
            }
            else if (highAddr == 0xA)
            {
                prgReg1 = (byte)(value & 0xF);
                SyncPrg();
            }
            else if (highAddr == 0xC)
            {
                prgReg2 = (byte)(value & 0xF);
                SyncPrg();
            }
            else if (highAddr == 0x9)
            {
                if ((value & 1) == 0)
                    nes.PPU.PPUMemory.VerticalMirroring();
                else
                    nes.PPU.PPUMemory.HorizontalMirroring();
                chrReg0 = (byte)(((value & 2) << 3) | (chrReg0 & 0xF));
                chrReg1 = (byte)(((value & 4) << 2) | (chrReg1 & 0xF));
                SyncChr();
            }
            else if (highAddr == 0xE)
            {
                chrReg0 = (byte)((value & 0xF) | (chrReg0 & 0x10));
                SyncChr();
            }
            else if (highAddr == 0xF)
            {
                chrReg1 = (byte)((value & 0xF) | (chrReg1 & 0x10));
                SyncChr();
            }
        }
        private void SyncPrg()
        {
            nes.Memory.Swap8kROM(0x8000, prgReg0 % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xA000, prgReg1 % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xC000, prgReg2 % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
        }
        private void SyncChr()
        {
            nes.PPU.PPUMemory.Swap4kROM(0x0000, chrReg0 % (nes.rom.vROM / 4));
            nes.PPU.PPUMemory.Swap4kROM(0x1000, chrReg1 % (nes.rom.vROM / 4));
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(prgReg0);
            writer.Write(prgReg1);
            writer.Write(prgReg2);
            writer.Write(chrReg0);
            writer.Write(chrReg1);
        }
        public override void StateLoad(BinaryReader reader)
        {
            prgReg0 = reader.ReadByte();
            prgReg1 = reader.ReadByte();
            prgReg2 = reader.ReadByte();
            chrReg0 = reader.ReadByte();
            chrReg1 = reader.ReadByte();
        }
    }
}
