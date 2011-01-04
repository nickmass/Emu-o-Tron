using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m068 : Mapper
    {
        private byte ntReg1;
        private byte ntReg2;
        private bool ntRom;
        private bool mirroring;
        public m068(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                switch (address & 0xF000)
                {
                    case 0x8000:
                        nes.PPU.PPUMemory.Swap2kROM(0x0000, value % (nes.rom.vROM / 2));
                        break;
                    case 0x9000:
                        nes.PPU.PPUMemory.Swap2kROM(0x0800, value % (nes.rom.vROM / 2));
                        break;
                    case 0xA000:
                        nes.PPU.PPUMemory.Swap2kROM(0x1000, value % (nes.rom.vROM / 2));
                        break;
                    case 0xB000:
                        nes.PPU.PPUMemory.Swap2kROM(0x1800, value % (nes.rom.vROM / 2));
                        break;
                    case 0xC000:
                        ntReg1 = (byte)((value & 0x7F) | 0x80);
                        SyncMirroring();
                        break;
                    case 0xD000:
                        ntReg2 = (byte)((value & 0x7F) | 0x80);
                        SyncMirroring();
                        break;
                    case 0xE000:
                        ntRom = (value & 0x10) != 0;
                        mirroring = (value & 1) == 0;
                        SyncMirroring();
                        break;
                    case 0xF000:
                        nes.Memory.Swap16kROM(0x8000, value % (nes.rom.vROM / 16));
                        break;
                }
            }
        }
        private void SyncMirroring()
        {
            if (ntRom)
            {
                if (mirroring)
                {//Vert
                    nes.PPU.PPUMemory.memMap[0x8] = nes.PPU.PPUMemory.swapOffset + (ntReg1 % nes.rom.vROM);
                    nes.PPU.PPUMemory.memMap[0x9] = nes.PPU.PPUMemory.swapOffset + (ntReg2 % nes.rom.vROM);
                    nes.PPU.PPUMemory.memMap[0xA] = nes.PPU.PPUMemory.swapOffset + (ntReg1 % nes.rom.vROM);
                    nes.PPU.PPUMemory.memMap[0xB] = nes.PPU.PPUMemory.swapOffset + (ntReg2 % nes.rom.vROM);
                }
                else
                {//Horz
                    nes.PPU.PPUMemory.memMap[0x8] = nes.PPU.PPUMemory.swapOffset + (ntReg1 % nes.rom.vROM);
                    nes.PPU.PPUMemory.memMap[0x9] = nes.PPU.PPUMemory.swapOffset + (ntReg1 % nes.rom.vROM);
                    nes.PPU.PPUMemory.memMap[0xA] = nes.PPU.PPUMemory.swapOffset + (ntReg2 % nes.rom.vROM);
                    nes.PPU.PPUMemory.memMap[0xB] = nes.PPU.PPUMemory.swapOffset + (ntReg2 % nes.rom.vROM);
                }
            }
            else
            {
                if (mirroring)
                    nes.PPU.PPUMemory.VerticalMirroring();
                else
                    nes.PPU.PPUMemory.HorizontalMirroring();
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(ntReg1);
            writer.Write(ntReg2);
            writer.Write(ntRom);
            writer.Write(mirroring);
        }
        public override void StateLoad(BinaryReader reader)
        {
            ntReg1 = reader.ReadByte();
            ntReg2 = reader.ReadByte();
            ntRom = reader.ReadBoolean();
            mirroring = reader.ReadBoolean();
        }
    }
}