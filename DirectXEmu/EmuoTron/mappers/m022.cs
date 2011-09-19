using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m022 : Mapper
    {
        private byte prgReg0;
        private byte prgReg1;
        private bool prgMode;
        private byte[] chrLow = new byte[8];
        private byte[] chrHigh = new byte[8];

        private byte[] regAddr = new byte[4];

        public m022(NESCore nes)
        {
            this.nes = nes;
            regAddr[0] = 0;
            regAddr[1] = 2;
            regAddr[2] = 1;
            regAddr[3] = 3;
        }
        public override void Power()
        {
            nes.Memory.Swap8kROM(0x8000, 0);
            nes.Memory.Swap8kROM(0xA000, 1);
            nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) - 2);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {

            byte highAddr = (byte)(address >> 8);
            byte lowAddr = (byte)(address & 0xFF);

            if (highAddr == 0x80 && (lowAddr == regAddr[0] || lowAddr == regAddr[1] || lowAddr == regAddr[2] || lowAddr == regAddr[3]))
            {
                prgReg0 = (byte)(value & 0xF);
                SyncPrg();
            }
            else if (highAddr == 0xA0 && (lowAddr == regAddr[0] || lowAddr == regAddr[1] || lowAddr == regAddr[2] || lowAddr == regAddr[3]))
            {
                prgReg1 = (byte)(value & 0xF);
                SyncPrg();
            }
            else if (highAddr == 0x90 && (lowAddr == regAddr[2] || lowAddr == regAddr[3]))
            {
                prgMode = ((value & 0x2) != 0);
                SyncPrg();
            }
            else if (highAddr == 0x90 && (lowAddr == regAddr[0] || lowAddr == regAddr[1]))
            {
                switch (value & 3)
                {
                    case 0:
                        nes.PPU.PPUMemory.VerticalMirroring();
                        break;
                    case 1:
                        nes.PPU.PPUMemory.HorizontalMirroring();
                        break;
                    case 2:
                        nes.PPU.PPUMemory.ScreenOneMirroring();
                        break;
                    case 3:
                        nes.PPU.PPUMemory.ScreenTwoMirroring();
                        break;
                }
            }
            else if ((highAddr == 0xB0 || highAddr == 0xC0 || highAddr == 0xD0 || highAddr == 0xE0) && (lowAddr == regAddr[0] || lowAddr == regAddr[1] || lowAddr == regAddr[2] || lowAddr == regAddr[3]))
            {
                byte bank = (byte)(value & 0xF);
                byte chrSelection = (byte)((highAddr - 0xB0) >> 3);
                if (lowAddr == regAddr[0])
                    chrLow[chrSelection] = bank;
                else if (lowAddr == regAddr[1])
                    chrHigh[chrSelection] = bank;
                else if (lowAddr == regAddr[2])
                    chrLow[chrSelection + 1] = bank;
                else if (lowAddr == regAddr[3])
                    chrHigh[chrSelection + 1] = bank;
                SyncChr();
            }
        }
        private void SyncPrg()
        {
            if (prgMode)
            {
                nes.Memory.Swap8kROM(0x8000, (nes.rom.prgROM / 8) - 2);
                nes.Memory.Swap8kROM(0xA000, prgReg1);
                nes.Memory.Swap8kROM(0xC000, prgReg0);
                nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            }
            else
            {
                nes.Memory.Swap8kROM(0x8000, prgReg0);
                nes.Memory.Swap8kROM(0xA000, prgReg1);
                nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) - 2);
                nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            }
        }
        private void SyncChr()
        {
            for (int i = 0; i < 8; i++)
            {
                nes.PPU.PPUMemory.Swap1kROM((ushort)(i << 10), (chrLow[i] | (chrHigh[i] << 4)) >> 1);
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(prgReg0);
            writer.Write(prgReg1);
            writer.Write(prgMode);
            for (int i = 0; i < 8; i++)
            {
                writer.Write(chrLow[i]);
                writer.Write(chrHigh[i]);
            }
        }
        public override void StateLoad(BinaryReader reader)
        {
            prgReg0 = reader.ReadByte();
            prgReg1 = reader.ReadByte();
            prgMode = reader.ReadBoolean();
            for (int i = 0; i < 8; i++)
            {
                chrLow[i] = reader.ReadByte();
                chrHigh[i] = reader.ReadByte();
            }
        }
    }
}
