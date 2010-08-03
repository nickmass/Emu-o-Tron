using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu.mappers
{
    class m009 : Mapper
    {
        public m009(MemoryStore Memory, MemoryStore PPUMemory, int numPRGRom, int numVRom)
        {
            this.numPRGRom = numPRGRom;
            this.numVRom = numVRom;
            this.Memory = Memory;
            this.PPUMemory = PPUMemory;
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
                    PPUMemory.Swap4kROM(0x1000, value);
                }
                else if (address >= 0xD000)
                {
                    value = (byte)(value % (numVRom * 2)); //0xFD
                    PPUMemory.Swap4kROM(0x1000, value);
                }
                else if (address >= 0xC000)
                {
                    value = (byte)(value % (numVRom * 2)); //0xFE
                    PPUMemory.Swap4kROM(0x0000, value);
                }
                else if (address >= 0xB000)
                {
                    value = (byte)(value % (numVRom * 2)); //0xFD
                    PPUMemory.Swap4kROM(0x0000, value);
                }
                else
                {
                    value = (byte)(value % (numPRGRom * 2));
                    Memory.Swap8kROM(0x8000, value);
                }
            }
        }
        public override void MapperIRQ(int scanline, int vblank) { }
        public override void MapperStateLoad(System.IO.MemoryStream buf) { }
        public override void MapperStateSave(ref System.IO.MemoryStream buf) { }
    }
}