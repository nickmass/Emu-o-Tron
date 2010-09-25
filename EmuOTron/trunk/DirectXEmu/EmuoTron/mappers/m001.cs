using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m001 : Mapper
    {
        private byte reg0;
        private byte reg1;
        private byte reg2;
        private byte reg3;
        private byte writeLatch;
        private byte regTmp;
        public m001(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            if (nes.rom.vROM == 0)
                nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
            else
                nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                if ((value & 0x80) != 0)
                {
                    reg0 |= 0x0C;
                    regTmp = 0;
                    writeLatch = 0;
                    RegZeroChange();
                }
                else if (writeLatch != 4)
                {
                    regTmp += (byte)((value & 1) << writeLatch);
                    writeLatch++;
                }
                else if (address >= 0xE000) //Prg reg  
                {
                    regTmp += (byte)((value & 1) << writeLatch);
                    reg3 = regTmp;
                    regTmp = 0;
                    writeLatch = 0;
                    PrgRegChange();
                }
                else if (address >= 0xC000) // Chr Reg 1
                {
                    regTmp += (byte)((value & 1) << writeLatch);
                    reg2 = regTmp;
                    regTmp = 0;
                    writeLatch = 0;
                    ChrRegOneChange();
                }
                else if (address >= 0xA000) //Chr Reg 0
                {
                    regTmp += (byte)((value & 1) << writeLatch);
                    reg1 = regTmp;
                    regTmp = 0;
                    writeLatch = 0;
                    ChrRegZeroChange();
                }
                else //Control Reg
                {
                    regTmp += (byte)((value & 1) << writeLatch);
                    reg0 = regTmp;
                    regTmp = 0;
                    writeLatch = 0;
                    RegZeroChange();
                    if ((reg0 & 3) == 0)
                        nes.PPU.PPUMemory.ScreenOneMirroring();
                    else if ((reg0 & 3) == 1)
                        nes.PPU.PPUMemory.ScreenTwoMirroring();
                    else if ((reg0 & 3) == 2)
                        nes.PPU.PPUMemory.VerticalMirroring();
                    else if ((reg0 & 3) == 3)
                        nes.PPU.PPUMemory.HorizontalMirroring();
                }
            }

        }
        private void PrgRegChange()
        {
            //Ignored in MMC1A, defaults to enabled in MMC1B, defaults to disabled in MMC1C
            //nes.Memory.SetReadOnly(0x6000, 8, ((reg3 & 0x10) != 0));
            if ((reg0 & 0x8) != 0) // Switch 16kb
            {
                if ((reg0 & 0x4) != 0) // Switch at $8000
                {
                    nes.Memory.Swap16kROM(0x8000, (reg3 & 0xF) % (nes.rom.prgROM / 16));
                    nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
                }
                else // Switch at $c000
                {
                    nes.Memory.Swap16kROM(0x8000, 0);
                    nes.Memory.Swap16kROM(0xC000, (reg3 & 0xF) % (nes.rom.prgROM / 16));
                }
            }
            else //switch 32kb
            {
                nes.Memory.Swap16kROM(0x8000, (reg3 & 0xE) % (nes.rom.prgROM / 16));
                nes.Memory.Swap16kROM(0xC000, ((reg3 & 0xE) + 1) % (nes.rom.prgROM / 16));
            }
        }
        private void ChrRegOneChange()
        {
            if ((reg0 & 0x10) != 0)
            {
                if (nes.rom.vROM == 0)
                    nes.PPU.PPUMemory.Swap4kRAM(0x1000, reg2 & 0x1F);
                else
                    nes.PPU.PPUMemory.Swap4kROM(0x1000, (reg2 & 0x1F) % (nes.rom.vROM / 4));
            }
        }
        private void ChrRegZeroChange()
        {
            if ((reg0 & 0x10) == 0)
            {
                if (nes.rom.vROM == 0)
                {
                    nes.PPU.PPUMemory.Swap4kRAM(0x0000, reg1 & 0x1E);
                    nes.PPU.PPUMemory.Swap4kRAM(0x1000, (reg1 & 0x1E) + 1);
                }
                else
                {
                    nes.PPU.PPUMemory.Swap4kROM(0x0000, (reg1 & 0x1E) % (nes.rom.vROM / 4));
                    nes.PPU.PPUMemory.Swap4kROM(0x1000, ((reg1 & 0x1E) + 1) % (nes.rom.vROM / 4));
                }
            }
            else
            {
                if (nes.rom.vROM == 0)
                    nes.PPU.PPUMemory.Swap4kRAM(0x0000, reg1 & 0x1F);
                else
                    nes.PPU.PPUMemory.Swap4kROM(0x0000, (reg1 & 0x1F) % (nes.rom.vROM / 4));
            }
        }
        private void RegZeroChange()
        {
            PrgRegChange();
            ChrRegZeroChange();
            ChrRegOneChange();
        }
        public override byte Read(byte value, ushort address) { return value; }
        public override void IRQ(int scanline, int vblank) { }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(reg0);
            writer.Write(reg1);
            writer.Write(reg2);
            writer.Write(reg3);
            writer.Write(writeLatch);
            writer.Write(regTmp);
        }
        public override void StateLoad(BinaryReader reader)
        {
            reg0 = reader.ReadByte();
            reg1 = reader.ReadByte();
            reg2 = reader.ReadByte();
            reg3 = reader.ReadByte();
            writeLatch = reader.ReadByte();
            regTmp = reader.ReadByte();
        }
    }
}
