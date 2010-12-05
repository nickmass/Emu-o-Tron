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
        public m004(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
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
            if (address >= 0x8000)
            {
                if (address % 2 == 0)
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
                        nes.Memory.SetReadOnly(0x6000, 8, ((value & 0x40) != 0) | ((value & 0x80) == 0));
                    }
                    else //Bank data
                    {
                        if ((bankSelect & 0x07) == 0)
                        {
                            if ((bankSelect & 0x80) == 0)
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x0000, value);
                                    if (value + 1 >= nes.rom.vROM)
                                        nes.PPU.PPUMemory.Swap1kROM(0x0400, 0);
                                    else
                                        nes.PPU.PPUMemory.Swap1kROM(0x0400, value + 1);
                                }
                                else
                                {
                                    nes.PPU.PPUMemory.Swap1kRAM(0x0000, value);
                                    nes.PPU.PPUMemory.Swap1kRAM(0x0400, value + 1);
                                }
                            }
                            else
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x1000, value);
                                    if (value + 1 >= nes.rom.vROM)
                                        nes.PPU.PPUMemory.Swap1kROM(0x1400, 0);
                                    else
                                        nes.PPU.PPUMemory.Swap1kROM(0x1400, value + 1);
                                }
                                else
                                {
                                    nes.PPU.PPUMemory.Swap1kRAM(0x1000, value);
                                    nes.PPU.PPUMemory.Swap1kRAM(0x1400, value + 1);
                                }
                            }
                        }
                        else if ((bankSelect & 0x07) == 01)
                        {
                            if ((bankSelect & 0x80) == 0)
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x0800, value);
                                    if (value + 1 >= nes.rom.vROM)
                                        nes.PPU.PPUMemory.Swap1kROM(0x0C00, 0);
                                    else
                                        nes.PPU.PPUMemory.Swap1kROM(0x0C00, value + 1);
                                }
                                else
                                {
                                    nes.PPU.PPUMemory.Swap1kRAM(0x0800, value);
                                    nes.PPU.PPUMemory.Swap1kRAM(0x0C00, value + 1);
                                }
                            }
                            else
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x1800, value);
                                    if (value + 1 >= nes.rom.vROM)
                                        nes.PPU.PPUMemory.Swap1kROM(0x1C00, 0);
                                    else
                                        nes.PPU.PPUMemory.Swap1kROM(0x1C00, value + 1);
                                }
                                else
                                {
                                    nes.PPU.PPUMemory.Swap1kRAM(0x1800, value);
                                    nes.PPU.PPUMemory.Swap1kRAM(0x1C00, value + 1);
                                }
                            }
                        }
                        else if ((bankSelect & 0x07) == 02)
                        {
                            if ((bankSelect & 0x80) == 0)
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x1000, value);
                                }
                                else
                                    nes.PPU.PPUMemory.Swap1kRAM(0x1000, value);
                            }
                            else
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x0000, value);
                                }
                                else
                                    nes.PPU.PPUMemory.Swap1kRAM(0x0000, value);
                            }
                        }
                        else if ((bankSelect & 0x07) == 03)
                        {
                            if ((bankSelect & 0x80) == 0)
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x1400, value);
                                }
                                else
                                    nes.PPU.PPUMemory.Swap1kRAM(0x1400, value);
                            }
                            else
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x0400, value);
                                }
                                else
                                    nes.PPU.PPUMemory.Swap1kRAM(0x0400, value);
                            }
                        }
                        else if ((bankSelect & 0x07) == 04)
                        {
                            if ((bankSelect & 0x80) == 0)
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x1800, value);
                                }
                                else
                                    nes.PPU.PPUMemory.Swap1kRAM(0x1800, value);
                            }
                            else
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x0800, value);
                                }
                                else
                                    nes.PPU.PPUMemory.Swap1kRAM(0x0800, value);
                            }
                        }
                        else if ((bankSelect & 0x07) == 05)
                        {
                            if ((bankSelect & 0x80) == 0)
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x1C00, value);
                                }
                                else
                                    nes.PPU.PPUMemory.Swap1kRAM(0x1C00, value);
                            }
                            else
                            {
                                if (nes.rom.vROM != 0)
                                {
                                    value = (byte)(value % nes.rom.vROM);
                                    nes.PPU.PPUMemory.Swap1kROM(0x0C00, value);
                                }
                                else
                                    nes.PPU.PPUMemory.Swap1kRAM(0x0C00, value);
                            }
                        }
                        else if ((bankSelect & 0x07) == 06)
                        {
                            if ((bankSelect & 0x40) == 0)
                            {
                                value = (byte)(value % (nes.rom.prgROM / 8));
                                nes.Memory.Swap8kROM(0x8000, value);
                                nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) - 2);
                            }
                            else
                            {
                                value = (byte)(value % (nes.rom.prgROM / 8));
                                nes.Memory.Swap8kROM(0xC000, value);
                                nes.Memory.Swap8kROM(0x8000, (nes.rom.prgROM / 8) - 2);
                            }
                        }
                        else if ((bankSelect & 0x07) == 07)
                        {
                            value = (byte)(value % (nes.rom.prgROM / 8));
                            nes.Memory.Swap8kROM(0xA000, value);
                        }
                    }
                }
            }
        }
        public override void IRQ(int scanline)
        {
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
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(bankSelect);
            writer.Write(irqCounter);
            writer.Write(irqLatch);
            writer.Write(irqReload);
            writer.Write(irqEnable);
        }
        public override void StateLoad(BinaryReader reader)
        {
            bankSelect = reader.ReadByte();
            irqCounter = reader.ReadByte();
            irqLatch = reader.ReadByte();
            irqReload = reader.ReadBoolean();
            irqEnable = reader.ReadBoolean();
        }
    }
}
