using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class mVRC6 : Mapper
    {
        private byte prgReg0;
        private byte prgReg1;
        private byte[] chrReg = new byte[8];
        private byte irqReload;
        private bool irqMode;
        private bool irqEnable;
        private bool irqAckEnable;
        private int irqCounter;
        private int irqScanlineCounter = 341;


        private byte[] regAddr = new byte[4];

        public mVRC6(NESCore nes, byte reg1, byte reg2, byte reg3, byte reg4)
        {
            this.nes = nes;
            this.cycleIRQ = true;
            regAddr[0] = reg1;
            regAddr[1] = reg2;
            regAddr[2] = reg3;
            regAddr[3] = reg4;
            this.nes.APU.external = new Channels.VRC6(reg1, reg2, reg3, reg4);
        }
        public override void Power()
        {
            prgReg0 = 0;
            prgReg1 = 1;
            chrReg[0] = 0;
            chrReg[1] = 0;
            chrReg[2] = 0;
            chrReg[3] = 0;
            chrReg[4] = 0;
            chrReg[5] = 0;
            chrReg[6] = 0;
            chrReg[7] = 0;
            irqReload = 0;
            irqMode = false;
            irqEnable = false;
            irqAckEnable = false;
            irqCounter = 0;
            irqScanlineCounter = 341;
            interruptMapper = false;
            SyncPrg();
            SyncChr();
        }
        public override void Write(byte value, ushort address)
        {

            byte highAddr = (byte)(address >> 8);
            byte lowAddr = (byte)(address & 0xFF);
            nes.APU.external.Write(value, address);
            if (highAddr == 0x80 && (lowAddr == regAddr[0] || lowAddr == regAddr[1] || lowAddr == regAddr[2] || lowAddr == regAddr[3]))
            {
                prgReg0 = value;
                SyncPrg();
            }
            else if (highAddr == 0xC0 && (lowAddr == regAddr[0] || lowAddr == regAddr[1] || lowAddr == regAddr[2] || lowAddr == regAddr[3]))
            {
                prgReg1 = value;
                SyncPrg();
            }
            else if (highAddr == 0xB0 && (lowAddr == regAddr[3]))
            {
                switch ((value >> 2) & 3)
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
            else if ((highAddr == 0xD0 || highAddr == 0xE0) && (lowAddr == regAddr[0] || lowAddr == regAddr[1] || lowAddr == regAddr[2] || lowAddr == regAddr[3]))
            {
                byte chrSelection = (byte)((highAddr - 0xD0) >> 2);
                if (lowAddr == regAddr[0])
                    chrReg[chrSelection] = value;
                else if (lowAddr == regAddr[1])
                    chrReg[chrSelection + 1] = value;
                else if (lowAddr == regAddr[2])
                    chrReg[chrSelection + 2] = value;
                else if (lowAddr == regAddr[3])
                    chrReg[chrSelection + 3] = value;
                SyncChr();
            }
            else if (highAddr == 0xF0 && lowAddr == regAddr[0])
            {
                irqReload = value;
            }
            else if (highAddr == 0xF0 && lowAddr == regAddr[1])
            {
                interruptMapper = false;
                irqAckEnable = (value & 1) != 0;
                irqEnable = (value & 2) != 0;
                irqMode = (value & 4) != 0;
                if (irqEnable)
                {
                    irqScanlineCounter = 341;
                    irqCounter = irqReload;
                }
            }
            else if (highAddr == 0xF0 && lowAddr == regAddr[2])
            {
                interruptMapper = false;
                irqEnable = irqAckEnable;
            }
        }
        private void SyncPrg()
        {
            nes.Memory.Swap16kROM(0x8000, prgReg0);
            nes.Memory.Swap8kROM(0xC000, prgReg1);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
        }
        private void SyncChr()
        {
            for (int i = 0; i < 8; i++)
            {
                nes.PPU.PPUMemory.Swap1kROM((ushort)(i << 10), chrReg[i]);
            }
        }
        public override void IRQ(int cycles)
        {
            if (irqEnable)
            {
                if (!irqMode)
                {
                    irqScanlineCounter -= cycles * 3;
                    if (irqScanlineCounter <= 0)
                    {
                        irqScanlineCounter += 341;
                        if (irqCounter >= 0xFF)
                        {
                            irqCounter = irqReload;
                            interruptMapper = true;
                        }
                        else
                        {
                            irqCounter++;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < cycles; i++)
                    {
                        if (irqCounter >= 0xFF)
                        {
                            irqCounter = irqReload;
                            interruptMapper = true;
                        }
                        else
                        {
                            irqCounter++;
                        }
                    }
                }
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(prgReg0);
            writer.Write(prgReg1);
            for (int i = 0; i < 8; i++)
            {
                writer.Write(chrReg[i]);
            }
            writer.Write(irqReload);
            writer.Write(irqMode);
            writer.Write(irqEnable);
            writer.Write(irqAckEnable);
            writer.Write(irqCounter);
            writer.Write(irqScanlineCounter);
        }
        public override void StateLoad(BinaryReader reader)
        {
            prgReg0 = reader.ReadByte();
            prgReg1 = reader.ReadByte();
            for (int i = 0; i < 8; i++)
            {
                chrReg[i] = reader.ReadByte();
            }
            irqReload = reader.ReadByte();
            irqMode = reader.ReadBoolean();
            irqEnable = reader.ReadBoolean();
            irqAckEnable = reader.ReadBoolean();
            irqCounter = reader.ReadInt32();
            irqScanlineCounter = reader.ReadInt32();
        }
    }
}
