using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m105 : Mapper
    {
        private byte[] regs = new byte[4];
        private byte latch;
        private byte latchPos;
        private int counter;
        private int tripValue;
        private bool initialized;
        private int baseDIPValue = 0x20000000;
        private int dip1Value = 0x2000000;
        private int dip2Value = 0x4000000;
        private int dip3Value = 0x8000000;
        private int dip4Value = 0x10000000;
        private bool dip1 = false;
        private bool dip2 = false;
        private bool dip3 = true;
        private bool dip4 = false;
        public m105(NESCore nes)
        {
            this.nes = nes;
            this.cycleIRQ = true;
        }
        public override void Power()
        {
            tripValue = baseDIPValue;
            if (dip1)
                tripValue |= dip1Value;
            if (dip2)
                tripValue |= dip2Value;
            if (dip3)
                tripValue |= dip3Value;
            if (dip4)
                tripValue |= dip4Value;
		    regs[0] = 0x0C;
		    regs[1] = 0x00;
		    regs[2] = 0x00;
		    regs[3] = 0x00;
            latch = 0;
            latchPos = 0;
            initialized = false;
            nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
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
        private void Sync()
        {
            nes.Memory.SetReadOnly(0x6000, 8, ((regs[3] & 0x10) != 0));
            if (initialized)
            {
                if ((regs[1] & 0x08) == 0)
                {
                    nes.Memory.Swap32kROM(0x8000, (regs[1] >> 1) & 3); //First 128k
                }
                else
                {
                    nes.Memory.Swap16kROM(0x8000, (PrgLow() % ((nes.rom.prgROM / 2) / 16)) + 8);//Second 128k
                    nes.Memory.Swap16kROM(0xC000, (PrgHigh() % ((nes.rom.prgROM / 2) / 16)) + 8);
                }
            }
            else
            {
                nes.Memory.Swap32kROM(0x8000, 0);
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
        public override void IRQ(int arg)
        {
            if ((regs[1] & 0x10) == 0)
            {
                counter += arg;
                if (counter >= tripValue)
                    interruptMapper = true;
            }
            else
            {
                counter = 0;
                interruptMapper = false;
                initialized = true;
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
            writer.Write(counter);
            writer.Write(tripValue);
            writer.Write(initialized);
        }
        public override void StateLoad(BinaryReader reader)
        {
            regs[0] = reader.ReadByte();
            regs[1] = reader.ReadByte();
            regs[2] = reader.ReadByte();
            regs[3] = reader.ReadByte();
            latchPos = reader.ReadByte();
            latch = reader.ReadByte();
            counter = reader.ReadInt32();
            tripValue = reader.ReadInt32();
            initialized = reader.ReadBoolean();
        }
    }
}
