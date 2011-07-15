using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m018 : Mapper
    {
        enum CounterWidth
        {
            FourBit,
            EightBit,
            TweleveBit,
            SixteenBit
        }
        byte[] chrBanks = new byte[8];

        byte[] prgBanks = new byte[3];

        int irqReload;
        int irqCounter;
        bool irqEnable;

        CounterWidth irqWidth;

        public m018(NESCore nes)
        {
            this.nes = nes;
            this.cycleIRQ = true;
        }
        public override void Power()
        {
            nes.Memory.Swap8kROM(0x8000, 0);
            nes.Memory.Swap8kROM(0xA000, 0);
            nes.Memory.Swap8kROM(0xC000, 0);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                switch (address & 0xF003)
                {
                    case 0x8000:
                        prgBanks[0] = (byte)((prgBanks[0] & 0xF0) | ((value & 0x0F) << 0));
                        PrgSync();
                        break;
                    case 0x8001:
                        prgBanks[0] = (byte)((prgBanks[0] & 0x0F) | ((value & 0x0F) << 4));
                        PrgSync();
                        break;
                    case 0x8002:
                        prgBanks[1] = (byte)((prgBanks[1] & 0xF0) | ((value & 0x0F) << 0));
                        PrgSync();
                        break;
                    case 0x8003:
                        prgBanks[1] = (byte)((prgBanks[1] & 0x0F) | ((value & 0x0F) << 4));
                        PrgSync();
                        break;
                    case 0x9000:
                        prgBanks[2] = (byte)((prgBanks[2] & 0xF0) | ((value & 0x0F) << 0));
                        PrgSync();
                        break;
                    case 0x9001:
                        prgBanks[2] = (byte)((prgBanks[2] & 0x0F) | ((value & 0x0F) << 4));
                        PrgSync();
                        break;
                    case 0xA000:
                        chrBanks[0] = (byte)((chrBanks[0] & 0xF0) | ((value & 0x0F) << 0));
                        ChrSync();
                        break;
                    case 0xA001:
                        chrBanks[0] = (byte)((chrBanks[0] & 0x0F) | ((value & 0x0F) << 4));
                        ChrSync();
                        break;
                    case 0xA002:
                        chrBanks[1] = (byte)((chrBanks[1] & 0xF0) | ((value & 0x0F) << 0));
                        ChrSync();
                        break;
                    case 0xA003:
                        chrBanks[1] = (byte)((chrBanks[1] & 0x0F) | ((value & 0x0F) << 4));
                        ChrSync();
                        break;
                    case 0xB000:
                        chrBanks[2] = (byte)((chrBanks[2] & 0xF0) | ((value & 0x0F) << 0));
                        ChrSync();
                        break;
                    case 0xB001:
                        chrBanks[2] = (byte)((chrBanks[2] & 0x0F) | ((value & 0x0F) << 4));
                        ChrSync();
                        break;
                    case 0xB002:
                        chrBanks[3] = (byte)((chrBanks[3] & 0xF0) | ((value & 0x0F) << 0));
                        ChrSync();
                        break;
                    case 0xB003:
                        chrBanks[3] = (byte)((chrBanks[3] & 0x0F) | ((value & 0x0F) << 4));
                        ChrSync();
                        break;
                    case 0xC000:
                        chrBanks[4] = (byte)((chrBanks[4] & 0xF0) | ((value & 0x0F) << 0));
                        ChrSync();
                        break;
                    case 0xC001:
                        chrBanks[4] = (byte)((chrBanks[4] & 0x0F) | ((value & 0x0F) << 4));
                        ChrSync();
                        break;
                    case 0xC002:
                        chrBanks[5] = (byte)((chrBanks[5] & 0xF0) | ((value & 0x0F) << 0));
                        ChrSync();
                        break;
                    case 0xC003:
                        chrBanks[5] = (byte)((chrBanks[5] & 0x0F) | ((value & 0x0F) << 4));
                        ChrSync();
                        break;
                    case 0xD000:
                        chrBanks[6] = (byte)((chrBanks[6] & 0xF0) | ((value & 0x0F) << 0));
                        ChrSync();
                        break;
                    case 0xD001:
                        chrBanks[6] = (byte)((chrBanks[6] & 0x0F) | ((value & 0x0F) << 4));
                        ChrSync();
                        break;
                    case 0xD002:
                        chrBanks[7] = (byte)((chrBanks[7] & 0xF0) | ((value & 0x0F) << 0));
                        ChrSync();
                        break;
                    case 0xD003:
                        chrBanks[7] = (byte)((chrBanks[7] & 0x0F) | ((value & 0x0F) << 4));
                        ChrSync();
                        break;
                    case 0xE000:
                        irqReload = (irqReload & 0xFFF0) | (value & 0x0F);
                        break;
                    case 0xE001:
                        irqReload = (irqReload & 0xFF0F) | ((value & 0x0F) << 4);
                        break;
                    case 0xE002:
                        irqReload = (irqReload & 0xF0FF) | ((value & 0x0F) << 8);
                        break;
                    case 0xE003:
                        irqReload = (irqReload & 0x0FFF) | ((value & 0x0F) << 12);
                        break;
                    case 0xF000:
                        irqCounter = irqReload;
                        interruptMapper = false;
                        break;
                    case 0xF001:
                        irqEnable = (value & 1) == 1;
                        interruptMapper = false;
                        switch ((value >> 1) & 7)
                        {
                            case 0:
                                irqWidth = CounterWidth.SixteenBit;
                                break;
                            case 1:
                                irqWidth = CounterWidth.TweleveBit;
                                break;
                            case 2:
                            case 3:
                                irqWidth = CounterWidth.EightBit;
                                break;
                            default:
                                irqWidth = CounterWidth.FourBit;
                                break;
                        }
                        break;
                    case 0xF002:
                        switch (value & 3)
                        {
                            case 0:
                                nes.PPU.PPUMemory.HorizontalMirroring();
                                break;
                            case 1:
                                nes.PPU.PPUMemory.VerticalMirroring();
                                break;
                            case 2:
                                nes.PPU.PPUMemory.ScreenOneMirroring();
                                break;
                            case 3:
                                nes.PPU.PPUMemory.ScreenTwoMirroring();
                                break;
                        }
                        break;

                }
            }
        }
        private void PrgSync()
        {
            nes.Memory.Swap8kROM(0x8000, prgBanks[0] % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xA000, prgBanks[1] % (nes.rom.prgROM / 8));
            nes.Memory.Swap8kROM(0xC000, prgBanks[2] % (nes.rom.prgROM / 8));
        }
        private void ChrSync()
        {
            nes.PPU.PPUMemory.Swap1kROM(0x0000, chrBanks[0] % nes.rom.vROM);
            nes.PPU.PPUMemory.Swap1kROM(0x0400, chrBanks[1] % nes.rom.vROM);
            nes.PPU.PPUMemory.Swap1kROM(0x0800, chrBanks[2] % nes.rom.vROM);
            nes.PPU.PPUMemory.Swap1kROM(0x0C00, chrBanks[3] % nes.rom.vROM);
            nes.PPU.PPUMemory.Swap1kROM(0x1000, chrBanks[4] % nes.rom.vROM);
            nes.PPU.PPUMemory.Swap1kROM(0x1400, chrBanks[5] % nes.rom.vROM);
            nes.PPU.PPUMemory.Swap1kROM(0x1800, chrBanks[6] % nes.rom.vROM);
            nes.PPU.PPUMemory.Swap1kROM(0x1C00, chrBanks[7] % nes.rom.vROM);
        }
        public override void IRQ(int cycles)
        {
            if (irqEnable)
            {
                for (int i = 0; i < cycles; i++)
                {
                    switch (irqWidth)
                    {
                        case CounterWidth.FourBit:
                            irqCounter = (irqCounter & 0xFFF0) | (((irqCounter & 0xF) - 1) & 0xF);
                            if ((irqCounter & 0xF) == 0xF)
                                interruptMapper = true;
                            break;
                        case CounterWidth.EightBit:
                            irqCounter = (irqCounter & 0xFF00) | (((irqCounter & 0xFF) - 1) & 0xFF);
                            if ((irqCounter & 0xFF) == 0xFF)
                                interruptMapper = true;
                            break;
                        case CounterWidth.TweleveBit:
                            irqCounter = (irqCounter & 0xF000) | (((irqCounter & 0xFFF) - 1) & 0xFFF);
                            if ((irqCounter & 0xFFF) == 0xFFF)
                                interruptMapper = true;
                            break;
                        case CounterWidth.SixteenBit:
                            irqCounter--;
                            if (irqCounter == -1)
                            {
                                irqCounter = 0xFFFF;
                                interruptMapper = true;
                            }
                            break;
                    }
                }
            }
        }
        public override void StateLoad(BinaryReader reader)
        {
            prgBanks[0] = reader.ReadByte();
            prgBanks[1] = reader.ReadByte();
            prgBanks[2] = reader.ReadByte();
            chrBanks[0] = reader.ReadByte();
            chrBanks[1] = reader.ReadByte();
            chrBanks[2] = reader.ReadByte();
            chrBanks[3] = reader.ReadByte();
            chrBanks[4] = reader.ReadByte();
            chrBanks[5] = reader.ReadByte();
            chrBanks[6] = reader.ReadByte();
            chrBanks[7] = reader.ReadByte();
            irqReload = reader.ReadInt32();
            irqCounter = reader.ReadInt32();
            irqEnable = reader.ReadBoolean();
            irqWidth = (CounterWidth)reader.ReadInt32();

        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(prgBanks[0]);
            writer.Write(prgBanks[1]);
            writer.Write(prgBanks[2]);
            writer.Write(chrBanks[0]);
            writer.Write(chrBanks[1]);
            writer.Write(chrBanks[2]);
            writer.Write(chrBanks[3]);
            writer.Write(chrBanks[4]);
            writer.Write(chrBanks[5]);
            writer.Write(chrBanks[6]);
            writer.Write(chrBanks[7]);
            writer.Write(irqReload);
            writer.Write(irqCounter);
            writer.Write(irqEnable);
            writer.Write((int)irqWidth);
        }
    }
}