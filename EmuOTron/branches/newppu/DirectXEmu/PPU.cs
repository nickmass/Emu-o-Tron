﻿#define testPPU1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    public class PPU
    {
        public MemoryStore PPUMemory;
        public ushort[] PPUMirrorMap = new ushort[0x8000];
        private bool[] PPUReadOnly = new bool[0x8000];
        private byte[] SPRMemory = new byte[0x100];
        public byte[] PalMemory = new byte[0x20];

        private NESCore nes;
        bool interruptNMI;
        bool spriteOverflow;
        bool spriteZeroHit;
        bool addrLatch;
        int spriteAddr;

        int spriteTable;
        int backgroundTable;
        bool tallSprites;
        bool nmiEnable;
        bool vramInc;

        bool grayScale;
        bool leftmostBackground;
        bool leftmostSprites;
        bool backgroundRendering = true;
        bool spriteRendering;
        bool redEmph;
        bool greenEmph;
        bool blueEmph;

        ushort colorMask;

        private int cycles;
        private int scanlineCycle;
        private int scanline = 241;
        private int lastUpdateCycle;
        private int[] scanlineLengths = { 114, 114, 113 };
        private byte slCounter = 0;

        private int loopyT;
        private int loopyX;
        private int loopyV;
        private byte readBuffer;

        public bool testPPU = false;

        public byte[][] screen = new byte[240][];

        public PPU(NESCore nes, int numvrom)
        {
#if testPPU
            testPPU = true;
#endif
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

            for (int i = 0; i < 240; i++)
                screen[i] = new byte[256];
        }
        private void PPUMirror(ushort address, ushort mirrorAddress, ushort length, int repeat)
        {
            for (int j = 0; j < repeat; j++)
                for (int i = 0; i < length; i++)
                    PPUMirrorMap[mirrorAddress + i + (j * length)] = (ushort)(PPUMirrorMap[address + i]);
        }
        public byte Read(byte value, ushort address)
        {
#if !testPPU
            return value;
#endif
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
#if !testPPU
            return;
#endif
            if (address == 0x2000)
            {
                Update();
                loopyT = (loopyT & 0xF3FF) | ((value & 3) << 10);
                vramInc = (value & 0x04) != 0;
                if ((value & 0x08) != 0)
                    spriteTable = 0x1000;
                else
                    spriteTable = 0x0000;
                if ((value & 0x10) != 0)
                    backgroundTable = 0x1000;
                else
                    backgroundTable = 0x0000;
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
                if (!addrLatch)//1st Write
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
            if (backgroundRendering | spriteRendering)
                loopyV = loopyT; //Vert reset
        }
        private void HorizontalReset()
        {
            if (backgroundRendering | spriteRendering)
                loopyV = (ushort)((loopyV & 0x7BE0) | (loopyT & 0x041F)); //Horz reset
        }

        public void AddCycles(int cycles)
        {
#if !testPPU
            return;
#endif
            scanlineCycle += cycles;
            if (scanlineCycle >= scanlineLengths[slCounter % 3])//scanline finished
            {
                scanlineCycle -= scanlineLengths[slCounter % 3];
                slCounter++;
                if (scanline < 240 && scanline >= 0)//real scanline
                {
                    if (backgroundRendering)
                    {
                        for (int tile = 0; tile < 32; tile++)//each tile on line
                        {
                            int tileAddr = PPUMirrorMap[0x2000 | (loopyV & 0x0FFF)];
                            int tileNumber = PPUMemory[tileAddr];
                            int addrTableLookup = AttrTableLookup[tileAddr & 0x3FF];
                            int palette = (PPUMemory[tileAddr + (addrTableLookup & 0xFFF)] >> (addrTableLookup >> 12)) & 0x03;
                            int chrAddress = backgroundTable | (tileNumber << 4) | ((loopyV >> 12) & 7);
                            byte lowChr = PPUMemory[chrAddress];
                            byte highChr = PPUMemory[chrAddress | 8];
                            for (int x = 0; x < 8; x++)//each pixel in tile
                            {
                                byte color = (byte)(((lowChr & 0x80) >> 7) + ((highChr & 0x80) >> 7));
                                screen[scanline][(tile * 8) + x] = PalMemory[(palette * 4) + color]; ;//(ushort)(color | colorMask); 
                                lowChr <<= 1;
                                highChr <<= 1;
                            }
                            HorizontalIncrement();
                        }
                    }
                    HorizontalReset();
                    VerticalIncrement();
                }
                scanline++;
                if (scanline == 0)
                    VerticalReset();

                if (scanline == 261)
                    scanline = -1;
            }
        }
        //tile = NameTableRead( 0x2000 | ( ppu_addr & 0x0FFF) ); 
        //chraddr = pattern_page | (tile << 4) | ((ppu_addr >> 12) & 7); 

        //lo_chrbitplane = CHRRead( chraddr ); 
        //hi_chrbitplane = CHRRead( chraddr | 8 );
        public void Update()
        {

        }

        private ushort[] AttrTableLookup = 
        { 0x03C0, 0x03C0, 0x23C0, 0x23C0, 0x03C1, 0x03C1, 0x23C1, 0x23C1, 0x03C2, 0x03C2, 0x23C2, 0x23C2, 0x03C3, 0x03C3, 0x23C3, 0x23C3, 
        0x03C4, 0x03C4, 0x23C4, 0x23C4, 0x03C5, 0x03C5, 0x23C5, 0x23C5, 0x03C6, 0x03C6, 0x23C6, 0x23C6, 0x03C7, 0x03C7, 0x23C7, 0x23C7, 
        0x03C0, 0x03C0, 0x23C0, 0x23C0, 0x03C1, 0x03C1, 0x23C1, 0x23C1, 0x03C2, 0x03C2, 0x23C2, 0x23C2, 0x03C3, 0x03C3, 0x23C3, 0x23C3, 
        0x03C4, 0x03C4, 0x23C4, 0x23C4, 0x03C5, 0x03C5, 0x23C5, 0x23C5, 0x03C6, 0x03C6, 0x23C6, 0x23C6, 0x03C7, 0x03C7, 0x23C7, 0x23C7, 
        0x43C0, 0x43C0, 0x63C0, 0x63C0, 0x43C1, 0x43C1, 0x63C1, 0x63C1, 0x43C2, 0x43C2, 0x63C2, 0x63C2, 0x43C3, 0x43C3, 0x63C3, 0x63C3, 
        0x43C4, 0x43C4, 0x63C4, 0x63C4, 0x43C5, 0x43C5, 0x63C5, 0x63C5, 0x43C6, 0x43C6, 0x63C6, 0x63C6, 0x43C7, 0x43C7, 0x63C7, 0x63C7, 
        0x43C0, 0x43C0, 0x63C0, 0x63C0, 0x43C1, 0x43C1, 0x63C1, 0x63C1, 0x43C2, 0x43C2, 0x63C2, 0x63C2, 0x43C3, 0x43C3, 0x63C3, 0x63C3, 
        0x43C4, 0x43C4, 0x63C4, 0x63C4, 0x43C5, 0x43C5, 0x63C5, 0x63C5, 0x43C6, 0x43C6, 0x63C6, 0x63C6, 0x43C7, 0x43C7, 0x63C7, 0x63C7, 
        0x03C8, 0x03C8, 0x23C8, 0x23C8, 0x03C9, 0x03C9, 0x23C9, 0x23C9, 0x03CA, 0x03CA, 0x23CA, 0x23CA, 0x03CB, 0x03CB, 0x23CB, 0x23CB, 
        0x03CC, 0x03CC, 0x23CC, 0x23CC, 0x03CD, 0x03CD, 0x23CD, 0x23CD, 0x03CE, 0x03CE, 0x23CE, 0x23CE, 0x03CF, 0x03CF, 0x23CF, 0x23CF, 
        0x03C8, 0x03C8, 0x23C8, 0x23C8, 0x03C9, 0x03C9, 0x23C9, 0x23C9, 0x03CA, 0x03CA, 0x23CA, 0x23CA, 0x03CB, 0x03CB, 0x23CB, 0x23CB, 
        0x03CC, 0x03CC, 0x23CC, 0x23CC, 0x03CD, 0x03CD, 0x23CD, 0x23CD, 0x03CE, 0x03CE, 0x23CE, 0x23CE, 0x03CF, 0x03CF, 0x23CF, 0x23CF, 
        0x43C8, 0x43C8, 0x63C8, 0x63C8, 0x43C9, 0x43C9, 0x63C9, 0x63C9, 0x43CA, 0x43CA, 0x63CA, 0x63CA, 0x43CB, 0x43CB, 0x63CB, 0x63CB, 
        0x43CC, 0x43CC, 0x63CC, 0x63CC, 0x43CD, 0x43CD, 0x63CD, 0x63CD, 0x43CE, 0x43CE, 0x63CE, 0x63CE, 0x43CF, 0x43CF, 0x63CF, 0x63CF, 
        0x43C8, 0x43C8, 0x63C8, 0x63C8, 0x43C9, 0x43C9, 0x63C9, 0x63C9, 0x43CA, 0x43CA, 0x63CA, 0x63CA, 0x43CB, 0x43CB, 0x63CB, 0x63CB, 
        0x43CC, 0x43CC, 0x63CC, 0x63CC, 0x43CD, 0x43CD, 0x63CD, 0x63CD, 0x43CE, 0x43CE, 0x63CE, 0x63CE, 0x43CF, 0x43CF, 0x63CF, 0x63CF, 
        0x03D0, 0x03D0, 0x23D0, 0x23D0, 0x03D1, 0x03D1, 0x23D1, 0x23D1, 0x03D2, 0x03D2, 0x23D2, 0x23D2, 0x03D3, 0x03D3, 0x23D3, 0x23D3, 
        0x03D4, 0x03D4, 0x23D4, 0x23D4, 0x03D5, 0x03D5, 0x23D5, 0x23D5, 0x03D6, 0x03D6, 0x23D6, 0x23D6, 0x03D7, 0x03D7, 0x23D7, 0x23D7, 
        0x03D0, 0x03D0, 0x23D0, 0x23D0, 0x03D1, 0x03D1, 0x23D1, 0x23D1, 0x03D2, 0x03D2, 0x23D2, 0x23D2, 0x03D3, 0x03D3, 0x23D3, 0x23D3, 
        0x03D4, 0x03D4, 0x23D4, 0x23D4, 0x03D5, 0x03D5, 0x23D5, 0x23D5, 0x03D6, 0x03D6, 0x23D6, 0x23D6, 0x03D7, 0x03D7, 0x23D7, 0x23D7, 
        0x43D0, 0x43D0, 0x63D0, 0x63D0, 0x43D1, 0x43D1, 0x63D1, 0x63D1, 0x43D2, 0x43D2, 0x63D2, 0x63D2, 0x43D3, 0x43D3, 0x63D3, 0x63D3, 
        0x43D4, 0x43D4, 0x63D4, 0x63D4, 0x43D5, 0x43D5, 0x63D5, 0x63D5, 0x43D6, 0x43D6, 0x63D6, 0x63D6, 0x43D7, 0x43D7, 0x63D7, 0x63D7, 
        0x43D0, 0x43D0, 0x63D0, 0x63D0, 0x43D1, 0x43D1, 0x63D1, 0x63D1, 0x43D2, 0x43D2, 0x63D2, 0x63D2, 0x43D3, 0x43D3, 0x63D3, 0x63D3, 
        0x43D4, 0x43D4, 0x63D4, 0x63D4, 0x43D5, 0x43D5, 0x63D5, 0x63D5, 0x43D6, 0x43D6, 0x63D6, 0x63D6, 0x43D7, 0x43D7, 0x63D7, 0x63D7, 
        0x03D8, 0x03D8, 0x23D8, 0x23D8, 0x03D9, 0x03D9, 0x23D9, 0x23D9, 0x03DA, 0x03DA, 0x23DA, 0x23DA, 0x03DB, 0x03DB, 0x23DB, 0x23DB, 
        0x03DC, 0x03DC, 0x23DC, 0x23DC, 0x03DD, 0x03DD, 0x23DD, 0x23DD, 0x03DE, 0x03DE, 0x23DE, 0x23DE, 0x03DF, 0x03DF, 0x23DF, 0x23DF, 
        0x03D8, 0x03D8, 0x23D8, 0x23D8, 0x03D9, 0x03D9, 0x23D9, 0x23D9, 0x03DA, 0x03DA, 0x23DA, 0x23DA, 0x03DB, 0x03DB, 0x23DB, 0x23DB, 
        0x03DC, 0x03DC, 0x23DC, 0x23DC, 0x03DD, 0x03DD, 0x23DD, 0x23DD, 0x03DE, 0x03DE, 0x23DE, 0x23DE, 0x03DF, 0x03DF, 0x23DF, 0x23DF, 
        0x43D8, 0x43D8, 0x63D8, 0x63D8, 0x43D9, 0x43D9, 0x63D9, 0x63D9, 0x43DA, 0x43DA, 0x63DA, 0x63DA, 0x43DB, 0x43DB, 0x63DB, 0x63DB, 
        0x43DC, 0x43DC, 0x63DC, 0x63DC, 0x43DD, 0x43DD, 0x63DD, 0x63DD, 0x43DE, 0x43DE, 0x63DE, 0x63DE, 0x43DF, 0x43DF, 0x63DF, 0x63DF, 
        0x43D8, 0x43D8, 0x63D8, 0x63D8, 0x43D9, 0x43D9, 0x63D9, 0x63D9, 0x43DA, 0x43DA, 0x63DA, 0x63DA, 0x43DB, 0x43DB, 0x63DB, 0x63DB, 
        0x43DC, 0x43DC, 0x63DC, 0x63DC, 0x43DD, 0x43DD, 0x63DD, 0x63DD, 0x43DE, 0x43DE, 0x63DE, 0x63DE, 0x43DF, 0x43DF, 0x63DF, 0x63DF, 
        0x03E0, 0x03E0, 0x23E0, 0x23E0, 0x03E1, 0x03E1, 0x23E1, 0x23E1, 0x03E2, 0x03E2, 0x23E2, 0x23E2, 0x03E3, 0x03E3, 0x23E3, 0x23E3, 
        0x03E4, 0x03E4, 0x23E4, 0x23E4, 0x03E5, 0x03E5, 0x23E5, 0x23E5, 0x03E6, 0x03E6, 0x23E6, 0x23E6, 0x03E7, 0x03E7, 0x23E7, 0x23E7, 
        0x03E0, 0x03E0, 0x23E0, 0x23E0, 0x03E1, 0x03E1, 0x23E1, 0x23E1, 0x03E2, 0x03E2, 0x23E2, 0x23E2, 0x03E3, 0x03E3, 0x23E3, 0x23E3, 
        0x03E4, 0x03E4, 0x23E4, 0x23E4, 0x03E5, 0x03E5, 0x23E5, 0x23E5, 0x03E6, 0x03E6, 0x23E6, 0x23E6, 0x03E7, 0x03E7, 0x23E7, 0x23E7, 
        0x43E0, 0x43E0, 0x63E0, 0x63E0, 0x43E1, 0x43E1, 0x63E1, 0x63E1, 0x43E2, 0x43E2, 0x63E2, 0x63E2, 0x43E3, 0x43E3, 0x63E3, 0x63E3, 
        0x43E4, 0x43E4, 0x63E4, 0x63E4, 0x43E5, 0x43E5, 0x63E5, 0x63E5, 0x43E6, 0x43E6, 0x63E6, 0x63E6, 0x43E7, 0x43E7, 0x63E7, 0x63E7, 
        0x43E0, 0x43E0, 0x63E0, 0x63E0, 0x43E1, 0x43E1, 0x63E1, 0x63E1, 0x43E2, 0x43E2, 0x63E2, 0x63E2, 0x43E3, 0x43E3, 0x63E3, 0x63E3, 
        0x43E4, 0x43E4, 0x63E4, 0x63E4, 0x43E5, 0x43E5, 0x63E5, 0x63E5, 0x43E6, 0x43E6, 0x63E6, 0x63E6, 0x43E7, 0x43E7, 0x63E7, 0x63E7, 
        0x03E8, 0x03E8, 0x23E8, 0x23E8, 0x03E9, 0x03E9, 0x23E9, 0x23E9, 0x03EA, 0x03EA, 0x23EA, 0x23EA, 0x03EB, 0x03EB, 0x23EB, 0x23EB, 
        0x03EC, 0x03EC, 0x23EC, 0x23EC, 0x03ED, 0x03ED, 0x23ED, 0x23ED, 0x03EE, 0x03EE, 0x23EE, 0x23EE, 0x03EF, 0x03EF, 0x23EF, 0x23EF, 
        0x03E8, 0x03E8, 0x23E8, 0x23E8, 0x03E9, 0x03E9, 0x23E9, 0x23E9, 0x03EA, 0x03EA, 0x23EA, 0x23EA, 0x03EB, 0x03EB, 0x23EB, 0x23EB, 
        0x03EC, 0x03EC, 0x23EC, 0x23EC, 0x03ED, 0x03ED, 0x23ED, 0x23ED, 0x03EE, 0x03EE, 0x23EE, 0x23EE, 0x03EF, 0x03EF, 0x23EF, 0x23EF, 
        0x43E8, 0x43E8, 0x63E8, 0x63E8, 0x43E9, 0x43E9, 0x63E9, 0x63E9, 0x43EA, 0x43EA, 0x63EA, 0x63EA, 0x43EB, 0x43EB, 0x63EB, 0x63EB, 
        0x43EC, 0x43EC, 0x63EC, 0x63EC, 0x43ED, 0x43ED, 0x63ED, 0x63ED, 0x43EE, 0x43EE, 0x63EE, 0x63EE, 0x43EF, 0x43EF, 0x63EF, 0x63EF, 
        0x43E8, 0x43E8, 0x63E8, 0x63E8, 0x43E9, 0x43E9, 0x63E9, 0x63E9, 0x43EA, 0x43EA, 0x63EA, 0x63EA, 0x43EB, 0x43EB, 0x63EB, 0x63EB, 
        0x43EC, 0x43EC, 0x63EC, 0x63EC, 0x43ED, 0x43ED, 0x63ED, 0x63ED, 0x43EE, 0x43EE, 0x63EE, 0x63EE, 0x43EF, 0x43EF, 0x63EF, 0x63EF, 
        0x03F0, 0x03F0, 0x23F0, 0x23F0, 0x03F1, 0x03F1, 0x23F1, 0x23F1, 0x03F2, 0x03F2, 0x23F2, 0x23F2, 0x03F3, 0x03F3, 0x23F3, 0x23F3, 
        0x03F4, 0x03F4, 0x23F4, 0x23F4, 0x03F5, 0x03F5, 0x23F5, 0x23F5, 0x03F6, 0x03F6, 0x23F6, 0x23F6, 0x03F7, 0x03F7, 0x23F7, 0x23F7, 
        0x03F0, 0x03F0, 0x23F0, 0x23F0, 0x03F1, 0x03F1, 0x23F1, 0x23F1, 0x03F2, 0x03F2, 0x23F2, 0x23F2, 0x03F3, 0x03F3, 0x23F3, 0x23F3, 
        0x03F4, 0x03F4, 0x23F4, 0x23F4, 0x03F5, 0x03F5, 0x23F5, 0x23F5, 0x03F6, 0x03F6, 0x23F6, 0x23F6, 0x03F7, 0x03F7, 0x23F7, 0x23F7, 
        0x43F0, 0x43F0, 0x63F0, 0x63F0, 0x43F1, 0x43F1, 0x63F1, 0x63F1, 0x43F2, 0x43F2, 0x63F2, 0x63F2, 0x43F3, 0x43F3, 0x63F3, 0x63F3, 
        0x43F4, 0x43F4, 0x63F4, 0x63F4, 0x43F5, 0x43F5, 0x63F5, 0x63F5, 0x43F6, 0x43F6, 0x63F6, 0x63F6, 0x43F7, 0x43F7, 0x63F7, 0x63F7, 
        0x43F0, 0x43F0, 0x63F0, 0x63F0, 0x43F1, 0x43F1, 0x63F1, 0x63F1, 0x43F2, 0x43F2, 0x63F2, 0x63F2, 0x43F3, 0x43F3, 0x63F3, 0x63F3, 
        0x43F4, 0x43F4, 0x63F4, 0x63F4, 0x43F5, 0x43F5, 0x63F5, 0x63F5, 0x43F6, 0x43F6, 0x63F6, 0x63F6, 0x43F7, 0x43F7, 0x63F7, 0x63F7, 
        0x03F8, 0x03F8, 0x23F8, 0x23F8, 0x03F9, 0x03F9, 0x23F9, 0x23F9, 0x03FA, 0x03FA, 0x23FA, 0x23FA, 0x03FB, 0x03FB, 0x23FB, 0x23FB, 
        0x03FC, 0x03FC, 0x23FC, 0x23FC, 0x03FD, 0x03FD, 0x23FD, 0x23FD, 0x03FE, 0x03FE, 0x23FE, 0x23FE, 0x03FF, 0x03FF, 0x23FF, 0x23FF, 
        0x03F8, 0x03F8, 0x23F8, 0x23F8, 0x03F9, 0x03F9, 0x23F9, 0x23F9, 0x03FA, 0x03FA, 0x23FA, 0x23FA, 0x03FB, 0x03FB, 0x23FB, 0x23FB, 
        0x03FC, 0x03FC, 0x23FC, 0x23FC, 0x03FD, 0x03FD, 0x23FD, 0x23FD, 0x03FE, 0x03FE, 0x23FE, 0x23FE, 0x03FF, 0x03FF, 0x23FF, 0x23FF, 
        0x43F8, 0x43F8, 0x63F8, 0x63F8, 0x43F9, 0x43F9, 0x63F9, 0x63F9, 0x43FA, 0x43FA, 0x63FA, 0x63FA, 0x43FB, 0x43FB, 0x63FB, 0x63FB, 
        0x43FC, 0x43FC, 0x63FC, 0x63FC, 0x43FD, 0x43FD, 0x63FD, 0x63FD, 0x43FE, 0x43FE, 0x63FE, 0x63FE, 0x43FF, 0x43FF, 0x63FF, 0x63FF, 
        0x43F8, 0x43F8, 0x63F8, 0x63F8, 0x43F9, 0x43F9, 0x63F9, 0x63F9, 0x43FA, 0x43FA, 0x63FA, 0x63FA, 0x43FB, 0x43FB, 0x63FB, 0x63FB, 
        0x43FC, 0x43FC, 0x63FC, 0x63FC, 0x43FD, 0x43FD, 0x63FD, 0x63FD, 0x43FE, 0x43FE, 0x63FE, 0x63FE, 0x43FF, 0x43FF, 0x63FF, 0x63FF };
    }
}
