using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    class PPU
    {
        public MemoryStore PPUMemory;
        public ushort[] PPUMirrorMap = new ushort[0x8000];
        private bool[] PPUReadOnly = new bool[0x8000];
        private byte[] SPRMemory = new byte[0x100];
        private byte[] PalMemory = new byte[0x20];

        private NESCore nes;
        bool interruptNMI;
        bool spriteOverflow;
        bool spriteZeroHit;
        bool addrLatch;
        int spriteAddr;

        bool spriteTable;
        bool backgroundTable;
        bool tallSprites;
        bool nmiEnable;
        bool vramInc;

        bool grayScale;
        bool leftmostBackground;
        bool leftmostSprites;
        bool backgroundRendering;
        bool spriteRendering;
        bool redEmph;
        bool greenEmph;
        bool blueEmph;

        ushort colorMask;

        private int cycles;
        private int lastUpdateCycle;

        private int loopyT;
        private int loopyX;
        private int loopyV;
        private byte readBuffer;

        public PPU(NESCore nes, int numvrom)
        {
            this.nes = nes;
            if (numvrom > 0)
                PPUMemory = new MemoryStore(0x20 + (numvrom * 0x08), false);
            else
                PPUMemory = new MemoryStore(0x20 + (4 * 0x08), false);
            PPUMemory.swapOffset = 0x20;
            for (int i = 0; i < 0x8000; i++)
            {
                PPUMirrorMap[i] = (ushort)i;
            }
            PPUMirror(0x3F00, 0x3F10, 1, 1);
            PPUMirror(0x3F04, 0x3F14, 1, 1);
            PPUMirror(0x3F08, 0x3F18, 1, 1);
            PPUMirror(0x3F0C, 0x3F1C, 1, 1);
            PPUMirror(0x2000, 0x3000, 0x0F00, 1);
            PPUMirror(0x3F00, 0x3F20, 0x20, 7);
        }
        private void PPUMirror(ushort address, ushort mirrorAddress, ushort length, int repeat)
        {
            for (int j = 0; j < repeat; j++)
                for (int i = 0; i < length; i++)
                    PPUMirrorMap[mirrorAddress + i + (j * length)] = (ushort)(PPUMirrorMap[address + i]);
        }
        public byte Read(byte value, ushort address)
        {
            byte nextByte = value;
            if (address == 0x2002) //PPU Status register
            {
                Update();
                nextByte = 0;
                if (spriteOverflow)
                    nextByte |= 0x20;
                if (spriteZeroHit)
                    nextByte |= 0x40;
                if (interruptNMI)
                    nextByte |= 80;
                interruptNMI = false;
                addrLatch = false;
            }
            else if (address == 0x2004) //OAM Read
            {
                nextByte = SPRMemory[spriteAddr];
            }
            else if (address == 0x2007) //PPU Data
            {
                Update();
                if ((loopyV & 0x3F00) == 0x3F00)
                {
                    nextByte = PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F];
                    readBuffer = PPUMemory[PPUMirrorMap[loopyV & 0x2FFF]];
                }
                else
                {
                    nextByte = readBuffer;
                    readBuffer = PPUMemory[PPUMirrorMap[loopyV & 0x3FFF]];
                }
                loopyV = (loopyV + (vramInc ? 0x20 : 0x01)) & 0x7FFF;
            }
            return nextByte;
        }

        public void Write(byte value, ushort address)
        {
            if (address == 0x2000)
            {
                Update();
                loopyT = (loopyT & 0xF3FF) | ((value & 3) << 10);
                vramInc = (value & 0x04) != 0;
                spriteTable = (value & 0x08) != 0;
                backgroundTable = (value & 0x10) != 0;
                tallSprites = (value & 0x20) != 0;
                nmiEnable = (value & 0x80) != 0;
            }
            else if (address == 0x2001) //PPU Mask
            {
                Update();
                grayScale = (value & 0x01) != 0;
                leftmostBackground = (value & 0x02) != 0;
                leftmostSprites = (value & 0x04) != 0;
                backgroundRendering = (value & 0x08) != 0;
                spriteRendering = (value & 0x10) != 0;
                redEmph = (value & 0x20) != 0;
                greenEmph = (value & 0x40) != 0;
                blueEmph = (value & 0x80) != 0;
                colorMask = 0;
                if (redEmph)
                    colorMask |= 1;
                if (greenEmph)
                    colorMask |= 2;
                if (blueEmph)
                    colorMask |= 4;
                colorMask <<= 8;
            }
            else if (address == 0x2003) //OAM Address
            {
                spriteAddr = value;
            }
            else if (address == 0x2004) //OAM Write
            {
                Update();
                SPRMemory[spriteAddr] = value;
                spriteAddr++;
            }
            else if (address == 0x4014) //Sprite DMA
            {
                Update();
                int startAddress = value << 8;
                for (int i = 0; i < 0x100; i++)
                    SPRMemory[(spriteAddr + i) & 0xFF] = nes.Memory[nes.MirrorMap[(startAddress + i) & 0xFFFF]];
                nes.AddCycles(513);
            }
            else if (address == 0x2005) //PPUScroll
            {
                Update();
                if (!addrLatch) //1st Write
                {
                    loopyT = ((loopyT & 0x7FE0) | (value >> 3));
                    loopyX = value & 0x07;
                }
                else //2nd Write
                    loopyT = ((loopyT & 0x0C1F) | ((value & 0x07) << 12) | ((value & 0xF8) << 2));
                addrLatch = !addrLatch;
            }
            else if (address == 0x2006) //PPUAddr
            {
                Update();
                if (addrLatch)//1st Write
                {
                    loopyT = ((loopyT & 0x00FF) | ((value & 0x3F) << 8));
                    //if (oldA12 == 0 && oldA12 != ((loopyT >> 12) & 1))
                    //    nes.romMapper.MapperScanline(scanline, vblank);
                }
                else //2nd Write
                {
                    loopyT = ((loopyT & 0xFF00) | value);
                    loopyV = loopyT;
                }
                addrLatch = !addrLatch;
            }
            else if (address == 0x2007) //PPU Write
            {
                Update();
                if ((loopyV & 0x3F00) == 0x3F00)
                    PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F] = (byte)(value & 0x3F);
                else
                    PPUMemory[PPUMirrorMap[loopyV & 0x3FFF]] = value;
                loopyV = ((loopyV + (vramInc ? 0x20 : 0x01)) & 0x7FFF);
            }
        }

        private void HorizontalIncrement()
        {
            loopyV = (loopyV & 0x7FE0) | ((loopyV + 0x01) & 0x1F);
            if ((loopyV & 0x1F) == 0)
                loopyV ^= 0x0400;
        }
        private void VerticalIncrement()
        {
            loopyV = (loopyV + 0x1000) & 0x7FFF;//Vert Increment
            if ((loopyV & 0x7000) == 0)
            {
                loopyV = (loopyV & 0x7C1F) | ((loopyV + 0x20) & 0x03E0);
                if ((loopyV & 0x03E0) == 0x03C0)
                    loopyV = (loopyV & 0x7C1F) ^ 0x0800;
            }
        }
        private void VerticalReset()
        {
            if (backgroundRendering | spriteRendering)//dummy line
                loopyV = loopyT; //Vert reset
        }
        private void HorizontalReset()
        {
            if (backgroundRendering | spriteRendering)
                loopyV = (ushort)((loopyV & 0x7BE0) | (loopyT & 0x041F)); //Horz reset
        }

        public void AddCycles(int cycles)
        {
            this.cycles += cycles *= 3;
        }
        //tile = NameTableRead( 0x2000 | ( ppu_addr & 0x0FFF) ); 
        //chraddr = pattern_page | (tile << 4) | ((ppu_addr >> 12) & 7); 

        //lo_chrbitplane = CHRRead( chraddr ); 
        //hi_chrbitplane = CHRRead( chraddr | 8 );
        public void Update()
        {

        }
    }
}
