using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class mVRC4 : Mapper
    {
        private byte prgReg0;
        private byte prgReg1;
        private bool prgMode;
        private byte[] chrLow = new byte[8];
        private byte[] chrHigh = new byte[8];
        private byte irqReload;
        private bool irqMode;
        private bool irqEnable;
        private bool irqAckEnable;
        private int irqCounter;
        private int irqScanlineCounter = 341;


        private byte[] regAddr = new byte[4];
        private byte[] altRegAddr = new byte[4];

        public mVRC4(NESCore nes, byte reg1, byte reg2, byte reg3, byte reg4, byte altReg1, byte altReg2, byte altReg3, byte altReg4)
        {
            this.nes = nes;
            regAddr[0] = reg1;
            regAddr[1] = reg2;
            regAddr[2] = reg3;
            regAddr[3] = reg4;
            altRegAddr[0] = altReg1;
            altRegAddr[1] = altReg2;
            altRegAddr[2] = altReg3;
            altRegAddr[3] = altReg4;
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

            byte highAddr = (byte)(address >> 8);
            byte lowAddr = (byte)(address & 0xFF);

            if (lowAddr == altRegAddr[0])
                lowAddr = regAddr[0];
            if (lowAddr == altRegAddr[1])
                lowAddr = regAddr[1];
            if (lowAddr == altRegAddr[2])
                lowAddr = regAddr[2];
            if (lowAddr == altRegAddr[3])
                lowAddr = regAddr[3];

            if (highAddr == 0x80 && (lowAddr == regAddr[0] || lowAddr == regAddr[1] || lowAddr == regAddr[2] || lowAddr == regAddr[3]))
            {
                prgReg0 = (byte)(value & 0x1F);
                SyncPrg();
            }
            else if (highAddr == 0xA0 && (lowAddr == regAddr[0] || lowAddr == regAddr[1] || lowAddr == regAddr[2] || lowAddr == regAddr[3]))
            {
                prgReg1 = (byte)(value & 0x1F);
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
            else if (highAddr == 0xF0 && lowAddr == regAddr[0])
            {
                irqReload = (byte)((irqReload & 0xF0) | (value & 0xF));
            }
            else if (highAddr == 0xF0 && lowAddr == regAddr[1])
            {
                irqReload = (byte)((irqReload & 0xF) | ((value & 0xF) << 4));
            }
            else if (highAddr == 0xF0 && lowAddr == regAddr[2])
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
            else if (highAddr == 0xF0 && lowAddr == regAddr[3])
            {
                interruptMapper = false;
                irqEnable = irqAckEnable;
            }
        }
        private void SyncPrg()
        {
            if (prgMode)
            {
                nes.Memory.Swap8kROM(0x8000, (nes.rom.prgROM / 8) - 2);
                nes.Memory.Swap8kROM(0xA000, prgReg1 % (nes.rom.prgROM / 8));
                nes.Memory.Swap8kROM(0xC000, prgReg0 % (nes.rom.prgROM / 8));
                nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            }
            else
            {
                nes.Memory.Swap8kROM(0x8000, prgReg0 % (nes.rom.prgROM / 8));
                nes.Memory.Swap8kROM(0xA000, prgReg1 % (nes.rom.prgROM / 8));
                nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) - 2);
                nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            }
        }
        private void SyncChr()
        {
            for (int i = 0; i < 8; i++)
            {
                nes.PPU.PPUMemory.Swap1kROM((ushort)(i << 10), (chrLow[i] | (chrHigh[i] << 4)) % nes.rom.vROM);
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
            writer.Write(prgMode);
            for(int i = 0; i < 8; i++)
            {
                writer.Write(chrLow[i]);
                writer.Write(chrHigh[i]);
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
            prgMode = reader.ReadBoolean();
            for (int i = 0; i < 8; i++)
            {
                chrLow[i] = reader.ReadByte();
                chrHigh[i] = reader.ReadByte();
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
