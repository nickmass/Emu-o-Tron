using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DirectXEmu.mappers
{
    class m001 : Mapper
    {
        private byte reg0;
        private byte reg1;
        private byte reg2;
        private byte reg3;
        private byte writeLatch;
        private byte regTmp;
        public m001(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
        {
            this.numPRGRom = numPRGRom;
            this.numVRom = numVRom;
            this.Memory = Memory;
            this.PPUMemory = PPUMemory;
        }
        public override void MapperInit()
        {
            Memory.Swap16kROM(0x8000, 0);
            Memory.Swap16kROM(0xC000, numPRGRom - 1);
            if (numVRom == 0)
                PPUMemory.Swap8kRAM(0x0000, 0);
            else
                PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void MapperWrite(ushort address, byte value)
        {
            if (address >= 0x8000)
            {
                if ((value & 0x80) != 0)
                {
                    writeLatch = 0;
                    reg0 |= 0x0C;
                    Memory.Swap16kROM(0xC000, numPRGRom - 1);
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
                    if ((reg0 & 0x8) != 0) // Switch 16kb
                    {
                        if ((reg0 & 0x4) != 0) // Switch at $8000
                        {
                            Memory.Swap16kROM(0x8000, reg3 % numPRGRom);
                            Memory.Swap16kROM(0xC000, numPRGRom - 1);
                        }
                        else // Switch at $c000
                        {
                            Memory.Swap16kROM(0x8000, 0);
                            Memory.Swap16kROM(0xC000, reg3 % numPRGRom);
                        }
                    }
                    else //switch 32kb
                    {
                        Memory.Swap16kROM(0x8000, (reg3 & 0xFE) % numPRGRom); //32k swap maybe works here, I have my doubts
                        Memory.Swap16kROM(0xC000, ((reg3 & 0xFE) + 1) % numPRGRom);
                    }

                }
                else if (address >= 0xC000) // Chr Reg 1
                {
                    regTmp += (byte)((value & 1) << writeLatch);
                    reg2 = regTmp;
                    regTmp = 0;
                    writeLatch = 0;
                    if ((reg0 & 0x10) != 0)
                    {
                        if (numVRom == 0)
                            PPUMemory.Swap4kRAM(0x1000, reg2);
                        else
                            PPUMemory.Swap4kROM(0x1000, reg2 % (numVRom * 2));
                    }
                }
                else if (address >= 0xA000) //Chr Reg 0
                {
                    regTmp += (byte)((value & 1) << writeLatch);
                    reg1 = regTmp;
                    regTmp = 0;
                    writeLatch = 0;
                    if ((reg0 & 0x10) == 0)
                    {
                        if (numVRom == 0)
                        {
                            PPUMemory.Swap4kRAM(0x0000, reg1 & 1);
                            PPUMemory.Swap4kRAM(0x1000, (reg1 & 1) + 1);
                        }
                        else
                        {
                            PPUMemory.Swap4kROM(0x0000, (reg1 & 1) % (numVRom * 2));
                            PPUMemory.Swap4kROM(0x1000, ((reg1 & 1) + 1) % (numVRom * 2));
                        }
                    }
                    else
                    {
                        if (numVRom == 0)
                            PPUMemory.Swap4kRAM(0x0000, reg1);
                        else
                            PPUMemory.Swap4kROM(0x0000, reg1 % (numVRom * 2));
                    }
                }
                else //Control Reg
                {
                    regTmp += (byte)((value & 1) << writeLatch);
                    reg0 = regTmp;
                    regTmp = 0;
                    writeLatch = 0;
                    if ((reg0 & 3) == 0)
                    {
                        PPUMemory.ScreenOneMirroring();
                    }
                    else if ((reg0 & 3) == 1)
                    {
                        PPUMemory.ScreenTwoMirroring();
                    }
                    else if ((reg0 & 3) == 2)
                    {
                        PPUMemory.VerticalMirroring();
                    }
                    else if ((reg0 & 3) == 3)
                    {
                        PPUMemory.HorizontalMirroring();
                    }

                }
            }

        }
        public override void MapperScanline(int scanline, int vblank) { }
        public override void MapperStateSave(ref MemoryStream buf)
        {
            BinaryWriter writer = new BinaryWriter(buf);
            writer.Seek(0, SeekOrigin.Begin);
            writer.Write(reg0);
            writer.Write(reg1);
            writer.Write(reg2);
            writer.Write(reg3);
            writer.Write(writeLatch);
            writer.Write(regTmp);
            writer.Flush();
        }
        public override void MapperStateLoad(MemoryStream buf)
        {
            BinaryReader reader = new BinaryReader(buf);
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            reg0 = reader.ReadByte();
            reg1 = reader.ReadByte();
            reg2 = reader.ReadByte();
            reg3 = reader.ReadByte();
            writeLatch = reader.ReadByte();
            regTmp = reader.ReadByte();
        }
    }
}
