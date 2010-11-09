using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m085 : Mapper
    {
        private byte prgReg0;
        private byte prgReg1;
        private byte prgReg2;
        private byte[] chrReg = new byte[8];
        private byte irqReload;
        private bool irqMode;
        private bool irqEnable;
        private bool irqAckEnable;
        private int irqCounter;
        private int irqScanlineCounter = 341;


        private byte regAddr;
        private byte altRegAddr;

        public m085(NESCore nes, byte reg1, byte altReg1)
        {
            this.nes = nes;
            regAddr = reg1;
            altRegAddr = altReg1;
        }
        public override void Init()
        {
            nes.Memory.Swap8kROM(0x8000, 0);
            nes.Memory.Swap8kROM(0xA000, 1);
            nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) - 2);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);

            if (nes.rom.vROM == 0)
                nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
            else
                nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {

            byte highAddr = (byte)(address >> 8);
            byte lowAddr = (byte)(address & 0xFF);

            if (lowAddr == altRegAddr)
                lowAddr = regAddr;

            if (highAddr == 0x80 && (lowAddr == 00))
            {
                prgReg0 = value;
                SyncPrg();
            }
            else if (highAddr == 0x80 && (lowAddr == regAddr))
            {
                prgReg1 = value;
                SyncPrg();
            }
            else if (highAddr == 0x90 && (lowAddr == 00))
            {
                prgReg2 = value;
                SyncPrg();
            }
            else if (highAddr == 0xE0 && (lowAddr == 00))
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
            else if ((highAddr == 0xA0 || highAddr == 0xB0 || highAddr == 0xC0 || highAddr == 0xD0) && (lowAddr == 00 || lowAddr == regAddr))
            {
                byte chrSelection = (byte)((highAddr - 0xA0) >> 3);
                if (lowAddr == 00)
                    chrReg[chrSelection] = value;
                else if (lowAddr == regAddr)
                    chrReg[chrSelection+1] = value;
                SyncChr();
            }
            else if (highAddr == 0xE0 && lowAddr == regAddr)
            {
                irqReload = value;
            }
            else if (highAddr == 0xF0 && lowAddr == 00)
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
            else if (highAddr == 0xF0 && lowAddr == regAddr)
            {
                interruptMapper = false;
                irqEnable = irqAckEnable;
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
            if (nes.rom.vROM == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    nes.PPU.PPUMemory.Swap1kRAM((ushort)(i << 10), (chrReg[i]) % 8);
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    nes.PPU.PPUMemory.Swap1kROM((ushort)(i << 10), (chrReg[i]) % nes.rom.vROM);
                }
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
            writer.Write(prgReg2);
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
            prgReg2 = reader.ReadByte();
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
