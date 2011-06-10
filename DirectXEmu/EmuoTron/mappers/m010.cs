using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m010 : Mapper
    {
        bool latch0;
        bool latch1;
        int fd0;
        int fe0;
        int fd1;
        int fe1;
        public m010(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            latch0 = false;
            latch1 = false;
            fd0 = 0;
            fe0 = 0;
            fd1 = 0;
            fe1 = 0;
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0xA000)
            {
                if (address >= 0xF000)
                {
                    if ((value & 1) != 0)
                        nes.PPU.PPUMemory.HorizontalMirroring();
                    else
                        nes.PPU.PPUMemory.VerticalMirroring();
                }
                else if (address >= 0xE000)
                {
                    value = (byte)(value % (nes.rom.vROM / 4)); //0xFE
                    fe1 = value;
                    if (latch1)
                        nes.PPU.PPUMemory.Swap4kROM(0x1000, fe1);
                }
                else if (address >= 0xD000)
                {
                    value = (byte)(value % (nes.rom.vROM / 4)); //0xFD
                    fd1 = value;
                    if (!latch1)
                        nes.PPU.PPUMemory.Swap4kROM(0x1000, fd1);
                }
                else if (address >= 0xC000)
                {
                    value = (byte)(value % (nes.rom.vROM / 4)); //0xFE
                    fe0 = value;
                    if (latch0)
                        nes.PPU.PPUMemory.Swap4kROM(0x0000, fe0);
                }
                else if (address >= 0xB000)
                {
                    value = (byte)(value % (nes.rom.vROM / 4)); //0xFD
                    fd0 = value;
                    if (!latch0)
                        nes.PPU.PPUMemory.Swap4kROM(0x0000, fd0);
                }
                else
                {
                    value = (byte)(value % (nes.rom.prgROM / 16));
                    nes.Memory.Swap16kROM(0x8000, value);
                }
            }
        }
        public override void IRQ(int chrAddress)
        {
            if ((chrAddress & 0xFFF0) == 0xFD0)
            {
                latch0 = false;
                nes.PPU.PPUMemory.Swap4kROM(0x0000, fd0);
            }
            else if ((chrAddress & 0xFFF0) == 0xFE0)
            {
                latch0 = true;
                nes.PPU.PPUMemory.Swap4kROM(0x0000, fe0);
            }
            else if ((chrAddress & 0xFFF0) == 0x1FD0)
            {
                latch1 = false;
                nes.PPU.PPUMemory.Swap4kROM(0x1000, fd1);
            }
            else if ((chrAddress & 0xFFF0) == 0x1FE0)
            {
                latch1 = true;
                nes.PPU.PPUMemory.Swap4kROM(0x1000, fe1);
            }
        }
        public override void StateLoad(BinaryReader reader)
        {
            latch0 = reader.ReadBoolean();
            latch1 = reader.ReadBoolean();
            fd0 = reader.ReadInt32();
            fe0 = reader.ReadInt32();
            fd1 = reader.ReadInt32();
            fe1 = reader.ReadInt32();
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(latch0);
            writer.Write(latch1);
            writer.Write(fd0);
            writer.Write(fe0);
            writer.Write(fd1);
            writer.Write(fe1);
        }
    }
}