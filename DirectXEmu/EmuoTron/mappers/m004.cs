using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m004 : Mapper
    {
        private byte bankSelect;
        private byte irqCounter;
        private byte irqLatch;
        private bool irqReload;
        private bool irqEnable;
        private bool writeProtect;

        private byte[] chrBanks = new byte[8];
        private byte[] prgBanks = new byte[2];

        public m004(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            for (int i = 0; i < 2; i++)
                prgBanks[i] = 0;
            for (int i = 0; i < 8; i++)
                chrBanks[i] = 0;
            bankSelect = 0;
            writeProtect = false;
            Sync();
            irqCounter = 0;
            irqLatch = 0;
            irqReload = false;
            irqEnable = false;
            interruptMapper = false;
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                if ((address & 1) == 0)
                {
                    if (address >= 0xE000) //IRQ disable
                    {
                        irqEnable = false;
                        interruptMapper = false;
                    }
                    else if (address >= 0xC000) //IRQ Latch
                    {
                        irqLatch = value;
                    }
                    else if (address >= 0xA000) //Mirroring
                    {
                        if ((value & 0x01) == 0)
                        {
                            nes.PPU.PPUMemory.VerticalMirroring();
                        }
                        else
                        {
                            nes.PPU.PPUMemory.HorizontalMirroring();
                        }
                    }
                    else //Bank Select
                    {
                        bankSelect = value; //This is gross if I did mapper more sane I would make this its own function or something
                        Sync();
                    }
                }
                else
                {
                    if (address >= 0xE000) //IRQ enable
                    {
                        irqEnable = true;
                    }
                    else if (address >= 0xC000) //IRQ reload
                    {
                        irqReload = true;
                    }
                    else if (address >= 0xA000) //PRG RAM protect
                    {
                        writeProtect = ((value & 0x40) != 0) | ((value & 0x80) == 0);
                        Sync();
                    }
                    else //Bank data
                    {
                        switch (bankSelect & 0x07)
                        {
                            case 0:
                                chrBanks[0] = (byte)(value & 0xFE);
                                chrBanks[1] = (byte)((value & 0xFE) | 1);
                                break;
                            case 1:
                                chrBanks[2] = (byte)(value & 0xFE);
                                chrBanks[3] = (byte)((value & 0xFE) | 1);
                                break;
                            case 2:
                                chrBanks[4] = value;
                                break;
                            case 3:
                                chrBanks[5] = value;
                                break;
                            case 4:
                                chrBanks[6] = value;
                                break;
                            case 5:
                                chrBanks[7] = value;
                                break;
                            case 6:
                                prgBanks[0] = value;
                                break;
                            case 7:
                                prgBanks[1] = value;
                                break;
                        }
                        Sync();
                    }
                }
            }
        }
        private void Sync()
        {
            SyncChar();
            SyncPrg();
        }
        private void SyncChar()
        {
            if (nes.rom.vROM != 0)
            {
                if ((bankSelect & 0x80) == 0)
                {
                    nes.PPU.PPUMemory.Swap1kROM(0x0000, chrBanks[0]);
                    nes.PPU.PPUMemory.Swap1kROM(0x0400, chrBanks[1]);
                    nes.PPU.PPUMemory.Swap1kROM(0x0800, chrBanks[2]);
                    nes.PPU.PPUMemory.Swap1kROM(0x0C00, chrBanks[3]);
                    nes.PPU.PPUMemory.Swap1kROM(0x1000, chrBanks[4]);
                    nes.PPU.PPUMemory.Swap1kROM(0x1400, chrBanks[5]);
                    nes.PPU.PPUMemory.Swap1kROM(0x1800, chrBanks[6]);
                    nes.PPU.PPUMemory.Swap1kROM(0x1C00, chrBanks[7]);
                }
                else
                {
                    nes.PPU.PPUMemory.Swap1kROM(0x0000, chrBanks[4]);
                    nes.PPU.PPUMemory.Swap1kROM(0x0400, chrBanks[5]);
                    nes.PPU.PPUMemory.Swap1kROM(0x0800, chrBanks[6]);
                    nes.PPU.PPUMemory.Swap1kROM(0x0C00, chrBanks[7]);
                    nes.PPU.PPUMemory.Swap1kROM(0x1000, chrBanks[0]);
                    nes.PPU.PPUMemory.Swap1kROM(0x1400, chrBanks[1]);
                    nes.PPU.PPUMemory.Swap1kROM(0x1800, chrBanks[2]);
                    nes.PPU.PPUMemory.Swap1kROM(0x1C00, chrBanks[3]);
                }
            }
            else
            {
                nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0, false);
            }
        }
        private void SyncPrg()
        {
            nes.Memory.Swap8kRAM(0x6000, 0, writeProtect);
            if ((bankSelect & 0x40) == 0)
            {
                nes.Memory.Swap8kROM(0x8000, prgBanks[0]);
                nes.Memory.Swap8kROM(0xA000, prgBanks[1]);
                nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) - 2);
                nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            }
            else
            {
                nes.Memory.Swap8kROM(0x8000, (nes.rom.prgROM / 8) - 2);
                nes.Memory.Swap8kROM(0xA000, prgBanks[1]);
                nes.Memory.Swap8kROM(0xC000, prgBanks[0]);
                nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            }
        }
        public override void IRQ(int scanline)
        {
            //MMC3b / MMC_Normal - SMB3 and I think the majority of other MMC3 games so will use this method.
            bool wasNotZero = (irqCounter != 0);
            if (irqCounter == 0 || irqReload)
            {
                irqCounter = irqLatch;
                if (irqLatch == 0 && irqEnable)
                    interruptMapper = true;
            }
            else
                irqCounter--;
            if (irqCounter == 0 && wasNotZero && irqEnable)
                interruptMapper = true;
            if (irqLatch != 0)
                irqReload = false;


            /*
            //MMC3a / MMC3_alt - Crystalis and some others.
            if (irqCounter == 0 || irqReload)
            {
                irqCounter = irqLatch;
                if (irqCounter == 0 && irqEnable && irqReload)
                    interruptMapper = true;
                irqReload = false;
            }
            else
            {
                irqCounter--;
                if (irqCounter == 0 && irqEnable)
                    interruptMapper = true;
            }
             */
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(bankSelect);
            writer.Write(irqCounter);
            writer.Write(irqLatch);
            writer.Write(irqReload);
            writer.Write(irqEnable);
            for (int i = 0; i < 2; i++)
                writer.Write(prgBanks[i]);
            for (int i = 0; i < 8; i++)
                writer.Write(chrBanks[i]);
            writer.Write(writeProtect);
        }
        public override void StateLoad(BinaryReader reader)
        {
            bankSelect = reader.ReadByte();
            irqCounter = reader.ReadByte();
            irqLatch = reader.ReadByte();
            irqReload = reader.ReadBoolean();
            irqEnable = reader.ReadBoolean();
            for (int i = 0; i < 2; i++)
                prgBanks[i] = reader.ReadByte();
            for (int i = 0; i < 8; i++)
                chrBanks[i] = reader.ReadByte();
            writeProtect = reader.ReadBoolean();
        }
    }
}
