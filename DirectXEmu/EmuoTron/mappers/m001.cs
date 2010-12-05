using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m001 : Mapper
    {
        private byte[] regs = new byte[4];
        private byte latch;
        private byte latchPos;
        //private byte lastReg;
        public m001(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
		    regs[0] = 0x0C;
		    regs[1] = 0x00;
		    regs[2] = 0x00;
		    regs[3] = 0x00;
            latch = 0;
            latchPos = 0;
            Sync();
        }

        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                byte reg = (byte)((address >> 13) & 3);
                if ((value & 0x80) != 0)
                {
                    latch = latchPos = 0;
                    regs[0] |= 0x0C;
                    return;
                }
                //Bill and Ted hack shamelessly stonlen from Nintendular, going to leave it disabled for now Bill and Ted isnt a signifigant game and I don't know for certain what other titles this may effect.
                /*
                if (reg != lastReg)
		            latch = latchPos = 0;
                lastReg = reg;
                */
                latch |= (byte)((value & 1) << latchPos++);
                if (latchPos == 5)
                {
                    regs[reg] = (byte)(latch & 0x1F);
                    latch = latchPos = 0;
                    Sync();
                }
            }

        }
        private int PrgLow()
        {
            if ((regs[0] & 0x8) != 0)
            {
                if ((regs[0] & 0x4) != 0)
                    return (regs[3] & 0xF);
                else
                    return 0;
            }
            else
                return regs[3] & 0xE;
        }
        private int PrgHigh()
        {
            if ((regs[0] & 0x8) != 0)
            {
                if ((regs[0] & 0x4) != 0)
                    return 0xF;
                else
                    return (regs[3] & 0xF);
            }
            else
                return (regs[3] & 0xE) | 1;
        }
        private int ChrLow()
        {
            if ((regs[0] & 0x10) == 0)
                return regs[1] & 0x1E;
            else
                return regs[1] & 0x1F;
        }
        private int ChrHigh()
        {
            if ((regs[0] & 0x10) == 0)
                return (regs[1] & 0x1E) | 1;
            else
                return regs[2] & 0x1F;
        }

        private void Sync()
        {
            //Ignored in MMC1A, defaults to enabled in MMC1B, defaults to disabled in MMC1C
            nes.Memory.SetReadOnly(0x6000, 8, ((regs[3] & 0x10) != 0));

            if (nes.rom.board == "NES-SUROM" || nes.rom.board == "NES-SXROM")
            {
                nes.Memory.Swap16kROM(0x8000, (PrgLow() & 0xF) | (ChrLow() & 0x10));
                nes.Memory.Swap16kROM(0xC000, (PrgHigh() & 0xF) | (ChrLow() & 0x10));
            }
            else
            {
                nes.Memory.Swap16kROM(0x8000, PrgLow() % (nes.rom.prgROM / 16));
                nes.Memory.Swap16kROM(0xC000, PrgHigh() % (nes.rom.prgROM / 16));
            }
            if (nes.rom.vROM == 0)
            {
                nes.PPU.PPUMemory.Swap4kRAM(0x0000, ChrLow() % 8);
                nes.PPU.PPUMemory.Swap4kRAM(0x1000, ChrHigh() % 8);
            }
            else
            {
                nes.PPU.PPUMemory.Swap4kROM(0x0000, ChrLow() % (nes.rom.vROM / 4));
                nes.PPU.PPUMemory.Swap4kROM(0x1000, ChrHigh() % (nes.rom.vROM / 4));
            }
            switch (regs[0] & 3)
            {
                case 0:
                    nes.PPU.PPUMemory.ScreenOneMirroring();
                    break;
                case 1:
                    nes.PPU.PPUMemory.ScreenTwoMirroring();
                    break;
                case 2:
                    nes.PPU.PPUMemory.VerticalMirroring();
                    break;
                case 3:
                    nes.PPU.PPUMemory.HorizontalMirroring();
                    break;
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(regs[0]);
            writer.Write(regs[1]);
            writer.Write(regs[2]);
            writer.Write(regs[3]);
            writer.Write(latchPos);
            writer.Write(latch);
        }
        public override void StateLoad(BinaryReader reader)
        {
            regs[0] = reader.ReadByte();
            regs[1] = reader.ReadByte();
            regs[2] = reader.ReadByte();
            regs[3] = reader.ReadByte();
            latchPos = reader.ReadByte();
            latch = reader.ReadByte();
        }
    }
}
