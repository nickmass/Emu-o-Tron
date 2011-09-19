using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace EmuoTron.Mappers
{
    class m019 : Mapper
    {
        ushort irqCounter;
        bool irqEnable;
        bool disableLowChrRAM;
        bool disableHighChrRAM;
        byte[] chrBanks = new byte[8];
        public m019(NESCore nes)
        {
            this.nes = nes;
            this.cycleIRQ = true;
            if (nes.rom.mapper == 19)
                nes.APU.external = new Channels.N163();
        }
        public override void Power()
        {
            nes.Memory.Swap8kROM(0x8000, 0);
            nes.Memory.Swap8kROM(0xA000, 1);
            nes.Memory.Swap8kROM(0xC000, 2);
            nes.Memory.Swap8kROM(0xE000, (nes.rom.prgROM / 8) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override byte Read(byte value, ushort address)
        {
            if (address >= 0x4800)
            {
                address &= 0xF800;
                if (nes.rom.mapper == 19)
                    nes.APU.external.Read(value, address);
                switch (address)
                {
                    case 0x5000:
                        value = (byte)(irqCounter & 0xFF);
                        interruptMapper = false;
                        break;
                    case 0x5800:
                        value = (byte)((irqCounter >> 8) & 0x7F);
                        if (irqEnable)
                            value |= 0x80;
                        interruptMapper = false;
                        break;
                }
            }
            return value;
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x4800)
            {
                address &= 0xF800;
                if (nes.rom.mapper == 19)
                    nes.APU.external.Write(value, address);
                switch (address)
                {
                    case 0x5000:
                        irqCounter = (ushort)((irqCounter & 0xFF00) | value);
                        interruptMapper = false;
                        break;
                    case 0x5800:
                        irqCounter = (ushort)(((value & 0x7F) << 8) | (irqCounter & 0x00FF));
                        irqEnable = (value & 0x80) != 0;
                        interruptMapper = false;
                        break;
                    case 0xE000:
                        nes.Memory.Swap8kROM(0x8000, value & 0x3F);
                        break;
                    case 0xE800:
                        nes.Memory.Swap8kROM(0xA000, value & 0x3F);
                        disableLowChrRAM = (value & 0x40) != 0;
                        disableHighChrRAM = (value & 0x80) != 0;
                        SyncChr();
                        break;
                    case 0xF000:
                        nes.Memory.Swap8kROM(0xC000, value & 0x3F);
                        break;
                    case 0xC000:
                        if (nes.rom.mapper == 19)
                        {
                            if (value < 0xE0)
                                nes.PPU.PPUMemory.ExternalROMMirroring(0, value);
                            else
                                nes.PPU.PPUMemory.CustomMirroring(0, value & 1);
                        }
                        break;
                    case 0xC800:
                        if (nes.rom.mapper == 19)
                        {
                            if (value < 0xE0)
                                nes.PPU.PPUMemory.ExternalROMMirroring(1, value);
                            else
                                nes.PPU.PPUMemory.CustomMirroring(1, value & 1);
                        }
                        break;
                    case 0xD000:
                        if (nes.rom.mapper == 19)
                        {
                            if (value < 0xE0)
                                nes.PPU.PPUMemory.ExternalROMMirroring(2, value);
                            else
                                nes.PPU.PPUMemory.CustomMirroring(2, value & 1);
                        }
                        break;
                    case 0xD800:
                        if (nes.rom.mapper == 19)
                        {
                            if (value < 0xE0)
                                nes.PPU.PPUMemory.ExternalROMMirroring(3, value);
                            else
                                nes.PPU.PPUMemory.CustomMirroring(3, value & 1);
                        }
                        break;
                    case 0x8000:
                        chrBanks[0] = value;
                        SyncChr();
                        break;
                    case 0x8800:
                        chrBanks[1] = value;
                        SyncChr();
                        break;
                    case 0x9000:
                        chrBanks[2] = value;
                        SyncChr();
                        break;
                    case 0x9800:
                        chrBanks[3] = value;
                        SyncChr();
                        break;
                    case 0xA000:
                        chrBanks[4] = value;
                        SyncChr();
                        break;
                    case 0xA800:
                        chrBanks[5] = value;
                        SyncChr();
                        break;
                    case 0xB000:
                        chrBanks[6] = value;
                        SyncChr();
                        break;
                    case 0xB800:
                        chrBanks[7] = value;
                        SyncChr();
                        break;
                }
            }
        }
        private void SyncChr()
        {
            for (int i = 0; i < 8; i++)
            {
                if (chrBanks[i] < 0xE0 || (i < 4 && disableLowChrRAM) || (i >= 4 && disableHighChrRAM))
                    nes.PPU.PPUMemory.Swap1kROM((ushort)(i * 0x400), chrBanks[i]);
                else
                    nes.PPU.PPUMemory.Swap1kRAM((ushort)(i * 0x400), (chrBanks[i] - 0xE0) & 7, false);
            }
        }
        public override void IRQ(int arg)
        {
            if (irqEnable)
            {
                for (int i = 0; i < arg; i++)
                {
                    if (irqCounter == 0x7FFF)
                        interruptMapper = true;
                    else
                    {
                        irqCounter++;
                        irqCounter &= 0x7FFF;
                    }
                }
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(irqCounter);
            writer.Write(irqEnable);
            writer.Write(disableLowChrRAM);
            writer.Write(disableHighChrRAM);
            writer.Write(chrBanks[0]);
            writer.Write(chrBanks[1]);
            writer.Write(chrBanks[2]);
            writer.Write(chrBanks[3]);
            writer.Write(chrBanks[4]);
            writer.Write(chrBanks[5]);
            writer.Write(chrBanks[6]);
            writer.Write(chrBanks[7]);
        }
        public override void StateLoad(BinaryReader reader)
        {
            irqCounter = reader.ReadUInt16();
            irqEnable = reader.ReadBoolean();
            disableLowChrRAM = reader.ReadBoolean();
            disableHighChrRAM = reader.ReadBoolean();
            chrBanks[0] = reader.ReadByte();
            chrBanks[1] = reader.ReadByte();
            chrBanks[2] = reader.ReadByte();
            chrBanks[3] = reader.ReadByte();
            chrBanks[4] = reader.ReadByte();
            chrBanks[5] = reader.ReadByte();
            chrBanks[6] = reader.ReadByte();
            chrBanks[7] = reader.ReadByte();
        }
    }
}
