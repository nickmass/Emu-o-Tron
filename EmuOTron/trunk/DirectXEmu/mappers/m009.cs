using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DirectXEmu.mappers
{
    class m009 : Mapper
    {
        int latch0;
        int latch1;
        int fd0;
        int fe0;
        int fd1;
        int fe1;
        public m009(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
        {
            this.numPRGRom = numPRGRom;
            this.numVRom = numVRom;
            this.Memory = Memory;
            this.PPUMemory = PPUMemory;
            this.mapper = 9;
        }
        public override void MapperInit()
        {
            Memory.Swap8kROM(0x8000, 0);
            Memory.Swap8kROM(0xA000, (numPRGRom * 2) - 3);
            Memory.Swap8kROM(0xC000, (numPRGRom * 2) - 2);
            Memory.Swap8kROM(0xE000, (numPRGRom * 2) - 1);
            PPUMemory.Swap4kROM(0x0000, 0);
            PPUMemory.Swap4kROM(0x0000, 1);
        }
        public override void MapperWrite(ushort address, byte value)
        {
            if (address >= 0xA000)
            {
                if (address >= 0xF000)
                {
                    if ((value & 1) != 0)
                        PPUMemory.HorizontalMirroring();
                    else
                        PPUMemory.VerticalMirroring();
                }
                else if (address >= 0xE000)
                {
                    value = (byte)(value % (numVRom * 2)); //0xFE
                    fe1 = value;
                    if(latch1 ==  0xFE)
                        PPUMemory.Swap4kROM(0x1000, value);
                }
                else if (address >= 0xD000)
                {
                    value = (byte)(value % (numVRom * 2)); //0xFD
                    fd1 = value;
                    if (latch1 == 0xFD)
                        PPUMemory.Swap4kROM(0x1000, value);
                }
                else if (address >= 0xC000)
                {
                    value = (byte)(value % (numVRom * 2)); //0xFE
                    fe0 = value;
                    if (latch0 == 0xFE)
                        PPUMemory.Swap4kROM(0x0000, value);
                }
                else if (address >= 0xB000)
                {
                    value = (byte)(value % (numVRom * 2)); //0xFD
                    fd0 = value;
                    if (latch0 == 0xFD)
                        PPUMemory.Swap4kROM(0x0000, value);
                }
                else
                {
                    value = (byte)(value % (numPRGRom * 2));
                    Memory.Swap8kROM(0x8000, value);
                }
            }
        }
        public override void MapperIRQ(int scanline, int vblank)
        {
            if (scanline == 0)
            {
                latch0 = vblank;
                if (latch0 == 0xFD)
                    PPUMemory.Swap4kROM(0x0000, fd0);
                else
                    PPUMemory.Swap4kROM(0x0000, fe0);
            }
            else if(scanline == 1)
            {
                latch1 = vblank;
                if (latch1 == 0xFD)
                    PPUMemory.Swap4kROM(0x1000, fd1);
                else
                    PPUMemory.Swap4kROM(0x1000, fe1);
            }
        }
        public override void StateLoad(System.IO.MemoryStream buf)
        {
            BinaryReader reader = new BinaryReader(buf);
            latch0 = reader.ReadInt32();
            latch1 = reader.ReadInt32();
            fd0 = reader.ReadInt32();
            fe0 = reader.ReadInt32();
            fd1 = reader.ReadInt32();
            fe1 = reader.ReadInt32();
        }
        public override void StateSave(ref System.IO.MemoryStream buf)
        {
            BinaryWriter writer = new BinaryWriter(buf);
            writer.Write(latch0);
            writer.Write(latch1);
            writer.Write(fd0);
            writer.Write(fe0);
            writer.Write(fd1);
            writer.Write(fe1);
            writer.Flush();
        }
    }
}