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
        public bool frameComplete;
        public bool interruptNMI;
        bool spriteOverflow;
        bool spriteZeroHit;
        bool addrLatch;
        bool inVblank;
        int spriteAddr;

        int spriteTable;
        int backgroundTable;
        bool tallSprites;
        bool nmiEnable;
        bool vramInc;

        byte grayScale;
        bool leftmostBackground;
        bool leftmostSprites;
        bool backgroundRendering = true;
        bool spriteRendering;
        bool redEmph;
        bool greenEmph;
        bool blueEmph;

        ushort colorMask;

        private int cycles;
        public int scanlineCycle;
        public int scanline = 241;
        private int lastUpdateCycle;
        private int[] scanlineLengths = { 114, 114, 113 };
        private byte slCounter = 0;

        private int loopyT;
        private int loopyX;
        private int loopyV;
        private byte readBuffer;

        public ushort[,] screen = new ushort[256,240];
        public bool displaySprites = true;
        public bool displayBG = true;
        public bool enforceSpriteLimit = true;
        public bool turbo;

        public bool generateNameTables = false;
        public int generateLine = 0;
        public byte[][,] nameTables;

        public bool generatePatternTables = false;
        public int generatePatternLine = 0;
        public byte[][] patternTablesPalette;
        public byte[][,] patternTables;

        public PPU(NESCore nes, int numvrom)
        {
            this.nes = nes;
            if (numvrom > 0)
                PPUMemory = new MemoryStore(0x20 + (numvrom * 0x08), false);
            else
                PPUMemory = new MemoryStore(0x20 + (4 * 0x08), false);
            PPUMemory.swapOffset = 0x20;
            for (int i = 0; i < 0x8000; i++)
                PPUMirrorMap[i] = (ushort)i;
            PPUMirror(0x3F00, 0x3F10, 1, 1);
            PPUMirror(0x3F04, 0x3F14, 1, 1);
            PPUMirror(0x3F08, 0x3F18, 1, 1);
            PPUMirror(0x3F0C, 0x3F1C, 1, 1);
            PPUMirror(0x2000, 0x3000, 0x0F00, 1);
            PPUMirror(0x3F00, 0x3F20, 0x20, 7);

            for (int i = 0; i < 0x20; i++)
                PalMemory[i] = 0x0F; //Sets the background to black on startup to prevent grey flashes, not exactly accurate but it looks nicer
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
                nextByte = 0;
                if (spriteOverflow)
                    nextByte |= 0x20;
                if (spriteZeroHit)
                    nextByte |= 0x40;
                if (inVblank)
                    nextByte |= 0x80;
                interruptNMI = false;
                inVblank = false;
                addrLatch = false;
            }
            else if (address == 0x2004) //OAM Read
            {
                nextByte = SPRMemory[spriteAddr];
            }
            else if (address == 0x2007) //PPU Data
            {
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
                loopyT = (loopyT & 0xF3FF) | ((value & 3) << 10);
                //loopyT = (loopyT & 0x0C00) | ((value) << 10);
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
                if((value & 0x01) != 0)
                    grayScale = 0x30;
                else
                    grayScale = 0x3F;
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
                SPRMemory[spriteAddr] = value;
                spriteAddr++;
            }
            else if (address == 0x4014) //Sprite DMA
            {
                int startAddress = value << 8;
                for (int i = 0; i < 0x100; i++)
                    SPRMemory[(spriteAddr + i) & 0xFF] = nes.Memory[nes.MirrorMap[(startAddress + i) & 0xFFFF]];
                //nes.AddCycles(513);
            }
            else if (address == 0x2005) //PPUScroll
            {
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
                if (!addrLatch)//1st Write
                {
                    loopyT = ((loopyT & 0x00FF) | ((value & 0x3F) << 8));
                    //if (oldA12 == 0 && oldA12 != ((loopyT >> 12) & 1))
                    //    nes.romMapper.MapperScanline(scanline, vblank);
                }
                else //2nd Write
                {
                    loopyT = ((loopyT & 0x7F00) | value);
                    loopyV = loopyT;
                }
                addrLatch = !addrLatch;
            }
            else if (address == 0x2007) //PPU Write
            {
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
            loopyV = loopyT; //Vert reset
        }
        private void HorizontalReset()
        {
            loopyV = (loopyV & 0x7BE0) | (loopyT & 0x041F); //Horz reset
        }

        public void AddCycles(int cycles)
        {
            scanlineCycle += cycles;
            if (scanlineCycle >= scanlineLengths[slCounter % 3])//scanline finished
            {
                scanlineCycle -= scanlineLengths[slCounter % 3];
                slCounter++;
                if (backgroundRendering | spriteRendering)
                {
                    if (scanline < 240 && scanline >= 0)//real scanline
                    {

                        bool[] zeroBackground = new bool[256];
                        ushort[] spriteLine = new ushort[256];
                        bool[] spriteAboveLine = new bool[256];
                        bool[] spriteBelowLine = new bool[256];
                        for (int tile = 0; tile < 34; tile++)//each tile on line
                        {
                            int tileAddr = PPUMirrorMap[0x2000 | (loopyV & 0x0FFF)];
                            int tileNumber = PPUMemory[tileAddr];
                            int addrTableLookup = AttrTableLookup[tileAddr & 0x3FF];
                            int palette = (PPUMemory[((tileAddr & 0x3C00) + 0x3C0) + (addrTableLookup & 0xFF)] >> (addrTableLookup >> 12)) & 0x03;
                            int chrAddress = backgroundTable | (tileNumber << 4) | ((loopyV >> 12) & 7);
                            byte lowChr = PPUMemory[chrAddress];
                            byte highChr = PPUMemory[chrAddress | 8];
                            for (int x = 0; x < 8; x++)//each pixel in tile
                            {
                                int xPosition = ((tile * 8) + x) - (loopyX & 0x7);
                                if (xPosition >= 0 && xPosition < 256)
                                {
                                    byte color = (byte)(((lowChr & 0x80) >> 7) + ((highChr & 0x80) >> 6));
                                    zeroBackground[xPosition] = (color == 0 || (!leftmostBackground && xPosition < 8) || !backgroundRendering);
                                    if (zeroBackground[xPosition])
                                        screen[xPosition, scanline] = (ushort)((PalMemory[0x00] & grayScale)  | colorMask);
                                    else
                                        screen[xPosition, scanline] = (ushort)((PalMemory[(palette * 4) + color] & grayScale) | colorMask);
                                }
                                lowChr <<= 1;
                                highChr <<= 1;
                            }
                            HorizontalIncrement();
                        }
                        VerticalIncrement();
                        HorizontalReset();
                        if (spriteRendering)
                        {
                            int spritesOnLine = 0;
                            for (int sprite = 0; sprite < 64; sprite++)
                            {

                                int yPosition = SPRMemory[sprite * 4] + 1;
                                if (yPosition <= scanline && yPosition + (tallSprites ? 16 : 8) > scanline && (spritesOnLine < 9 || !enforceSpriteLimit))
                                {
                                    spritesOnLine++;
                                    int spriteTable;
                                    int spriteY = (scanline - yPosition);
                                    int attr = SPRMemory[(sprite * 4) + 2];
                                    bool horzFlip = (attr & 0x40) != 0;
                                    bool vertFlip = (attr & 0x80) != 0;
                                    int tileNumber = SPRMemory[(sprite * 4) + 1];
                                    if(tallSprites)
                                    {
                                        if ((tileNumber & 1) != 0)
                                            spriteTable = 0x1000;
                                        else
                                            spriteTable = 0x0000;
                                        tileNumber &= 0xFE;
                                        if (spriteY > 7)
                                            tileNumber |= 1;
                                    }
                                    else
                                        spriteTable = this.spriteTable;
                                    int chrAddress = (spriteTable | (tileNumber << 4) | (spriteY & 7)) + (vertFlip ? tallSprites ? (spriteY > 7) ? Flip[spriteY & 7] - (1 << 4) : Flip[spriteY & 7] + (1 << 4) : Flip[spriteY & 7] : 0); //this is seriously mental :)
                                    int xLocation = SPRMemory[(sprite * 4) + 3];
                                    int palette = attr & 0x03;
                                    byte lowChr = PPUMemory[chrAddress];
                                    byte highChr = PPUMemory[chrAddress | 8];
                                    for (int xPosition = horzFlip ? xLocation + 7 : xLocation; horzFlip ? xPosition >= xLocation : xPosition < xLocation + 8; xPosition += horzFlip ? -1 : 1)//each pixel in tile
                                    {
                                        if (xPosition < 256 && !(spriteAboveLine[xPosition] || spriteBelowLine[xPosition]))
                                        {
                                            byte color = (byte)(((lowChr & 0x80) >> 7) + ((highChr & 0x80) >> 6));
                                            if (color != 0 && !(!leftmostSprites && xPosition < 8))
                                            {
                                                spriteAboveLine[xPosition] = (attr & 0x20) == 0;
                                                spriteBelowLine[xPosition] = (attr & 0x20) != 0;
                                                spriteLine[xPosition] = (ushort)((PalMemory[(palette * 4) + color + 0x10] & grayScale) | colorMask);
                                                if (sprite == 0 && !zeroBackground[xPosition])
                                                    spriteZeroHit = true;
                                            }
                                        }
                                        lowChr <<= 1;
                                        highChr <<= 1;
                                    }
                                }
                            }
                            if (spritesOnLine >= 9)
                                spriteOverflow = true;

                            if (displaySprites)
                            {
                                if (displayBG)
                                {
                                    for (int column = 0; column < 256; column++)
                                    {
                                        if (spriteAboveLine[column])
                                            screen[column, scanline] = spriteLine[column];
                                        else if (zeroBackground[column] && spriteBelowLine[column])
                                            screen[column, scanline] = spriteLine[column];
                                    }
                                }
                                else
                                {
                                    for (int column = 0; column < 256; column++)
                                        if (spriteAboveLine[column] || spriteBelowLine[column])
                                            screen[column, scanline] = spriteLine[column];
                                        else
                                            screen[column, scanline] = (ushort)((PalMemory[0x00] & grayScale) | colorMask);
                                }
                            }
                        }
                    }

                    if (nes.romMapper.mapper == 4 && (spriteRendering | backgroundRendering) && scanline < 240)
                        nes.romMapper.MapperIRQ(scanline, 0);
                    if (scanline == -1)
                    {
                        VerticalReset();
                    }
                }
                if (scanline == 240)
                {
                    if (nmiEnable)
                        interruptNMI = true;
                    inVblank = true;
                }
                else if (scanline == 261)
                {
                    spriteOverflow = false;
                    spriteZeroHit = false;
                    frameComplete = true;
                    inVblank = false;
                    scanline = -2;
                }
                if (generateNameTables && scanline == generateLine)
                    nameTables = GenerateNameTables();
                if (generatePatternTables && scanline == generatePatternLine)
                {
                    patternTablesPalette = GeneratePatternTablePalette();
                    patternTables = GeneratePatternTables();
                }
                scanline++;
            }
        }

        private byte[][,] GenerateNameTables()
        {
            byte[][,] nameTables = new byte[4][,];
            for (int nameTable = 0; nameTable < 4; nameTable++)
            {
                nameTables[nameTable] = new byte[256, 240];
                for (int line = 0; line < 240; line++)
                {
                    for (int tile = 0; tile < 32; tile++)//each tile on line
                    {
                        ushort nameTableOffset = (ushort)((nameTable * 0x400) + 0x2000);
                        int tileAddr = PPUMirrorMap[nameTableOffset + ((line / 8) * 32) + (tile)];
                        int tileNumber = PPUMemory[tileAddr];
                        int addrTableLookup = AttrTableLookup[tileAddr & 0x3FF];
                        int palette = (PPUMemory[((tileAddr & 0x3C00) + 0x3C0) + (addrTableLookup & 0xFF)] >> (addrTableLookup >> 12)) & 0x03;
                        int chrAddress = backgroundTable | (tileNumber << 4) | ((line % 8) & 7);
                        byte lowChr = PPUMemory[chrAddress];
                        byte highChr = PPUMemory[chrAddress | 8];
                        for (int x = 0; x < 8; x++)//each pixel in tile
                        {
                            int xPosition = ((tile * 8) + x);
                            if (xPosition >= 0 && xPosition < 256)
                            {
                                byte color = (byte)(((lowChr & 0x80) >> 7) + ((highChr & 0x80) >> 6));
                                if (color == 0)
                                    nameTables[nameTable][(tile * 8) + x, line] = (byte)(PalMemory[0x00] & grayScale);
                                else
                                    nameTables[nameTable][(tile * 8) + x, line] = (byte)(PalMemory[(palette * 4) + color] & grayScale);
                            }
                            lowChr <<= 1;
                            highChr <<= 1;
                        }
                    }
                }
            }
            return nameTables;
        }
        private byte[][] GeneratePatternTablePalette()
        {
            byte[][] pal = new byte[8][];
            for (int palette = 0; palette < 8; palette++)
            {
                pal[palette] = new byte[4];
                pal[palette][0] = this.PalMemory[0x00];
                pal[palette][1] = this.PalMemory[(palette * 4) + 1];
                pal[palette][2] = this.PalMemory[(palette * 4) + 2];
                pal[palette][3] = this.PalMemory[(palette * 4) + 3];
            }
            return pal;
        }
        private byte[][,] GeneratePatternTables()
        {
            byte[][,] patternTables = new byte[2][,];
            for (int patternTable = 0; patternTable < 2; patternTable++)
            {
                patternTables[patternTable] = new byte[128, 128];
                for (int line = 0; line < 128; line++)
                {
                    ushort backgroundTable = (ushort)(patternTable * 0x1000);
                    for (int column = 0; column < 128; column++)//For each pixel in scanline
                    {
                        byte tileNumber = (byte)(((line / 8) * 16) + (column / 8));
                        patternTables[patternTable][column, line] = GetTilePixel(backgroundTable, tileNumber, (byte)(column % 8), (byte)(line % 8));
                    }
                }
            }
            return patternTables;
        }
        private byte GetTilePixel(ushort table, byte tile, byte pixelX, byte pixelY)
        {
            byte tileColor1 = (byte)((this.PPUMemory[this.PPUMirrorMap[(table + (tile * 16)) + pixelY]] << pixelX) & 0x80);
            byte tileColor2 = (byte)((this.PPUMemory[this.PPUMirrorMap[(table + (tile * 16) + 8) + pixelY]] << pixelX) & 0x80);
            if (tileColor1 != 0)
                tileColor1 = 1;
            if (tileColor2 != 0)
                tileColor2 = 1;
            return (byte)(tileColor1 + (tileColor2 * 2));
        }

        private int[] Flip = { 7, 5, 3, 1, -1, -3, -5, -7};
        private ushort[] AttrTableLookup = 
          { 0x0000, 0x0000, 0x2000, 0x2000, 0x0001, 0x0001, 0x2001, 0x2001, 0x0002, 0x0002, 0x2002, 0x2002, 0x0003, 0x0003, 0x2003, 0x2003, 
            0x0004, 0x0004, 0x2004, 0x2004, 0x0005, 0x0005, 0x2005, 0x2005, 0x0006, 0x0006, 0x2006, 0x2006, 0x0007, 0x0007, 0x2007, 0x2007, 
            0x0000, 0x0000, 0x2000, 0x2000, 0x0001, 0x0001, 0x2001, 0x2001, 0x0002, 0x0002, 0x2002, 0x2002, 0x0003, 0x0003, 0x2003, 0x2003, 
            0x0004, 0x0004, 0x2004, 0x2004, 0x0005, 0x0005, 0x2005, 0x2005, 0x0006, 0x0006, 0x2006, 0x2006, 0x0007, 0x0007, 0x2007, 0x2007, 
            0x4000, 0x4000, 0x6000, 0x6000, 0x4001, 0x4001, 0x6001, 0x6001, 0x4002, 0x4002, 0x6002, 0x6002, 0x4003, 0x4003, 0x6003, 0x6003, 
            0x4004, 0x4004, 0x6004, 0x6004, 0x4005, 0x4005, 0x6005, 0x6005, 0x4006, 0x4006, 0x6006, 0x6006, 0x4007, 0x4007, 0x6007, 0x6007, 
            0x4000, 0x4000, 0x6000, 0x6000, 0x4001, 0x4001, 0x6001, 0x6001, 0x4002, 0x4002, 0x6002, 0x6002, 0x4003, 0x4003, 0x6003, 0x6003, 
            0x4004, 0x4004, 0x6004, 0x6004, 0x4005, 0x4005, 0x6005, 0x6005, 0x4006, 0x4006, 0x6006, 0x6006, 0x4007, 0x4007, 0x6007, 0x6007, 
            0x0008, 0x0008, 0x2008, 0x2008, 0x0009, 0x0009, 0x2009, 0x2009, 0x000A, 0x000A, 0x200A, 0x200A, 0x000B, 0x000B, 0x200B, 0x200B, 
            0x000C, 0x000C, 0x200C, 0x200C, 0x000D, 0x000D, 0x200D, 0x200D, 0x000E, 0x000E, 0x200E, 0x200E, 0x000F, 0x000F, 0x200F, 0x200F, 
            0x0008, 0x0008, 0x2008, 0x2008, 0x0009, 0x0009, 0x2009, 0x2009, 0x000A, 0x000A, 0x200A, 0x200A, 0x000B, 0x000B, 0x200B, 0x200B, 
            0x000C, 0x000C, 0x200C, 0x200C, 0x000D, 0x000D, 0x200D, 0x200D, 0x000E, 0x000E, 0x200E, 0x200E, 0x000F, 0x000F, 0x200F, 0x200F, 
            0x4008, 0x4008, 0x6008, 0x6008, 0x4009, 0x4009, 0x6009, 0x6009, 0x400A, 0x400A, 0x600A, 0x600A, 0x400B, 0x400B, 0x600B, 0x600B, 
            0x400C, 0x400C, 0x600C, 0x600C, 0x400D, 0x400D, 0x600D, 0x600D, 0x400E, 0x400E, 0x600E, 0x600E, 0x400F, 0x400F, 0x600F, 0x600F, 
            0x4008, 0x4008, 0x6008, 0x6008, 0x4009, 0x4009, 0x6009, 0x6009, 0x400A, 0x400A, 0x600A, 0x600A, 0x400B, 0x400B, 0x600B, 0x600B, 
            0x400C, 0x400C, 0x600C, 0x600C, 0x400D, 0x400D, 0x600D, 0x600D, 0x400E, 0x400E, 0x600E, 0x600E, 0x400F, 0x400F, 0x600F, 0x600F, 
            0x0010, 0x0010, 0x2010, 0x2010, 0x0011, 0x0011, 0x2011, 0x2011, 0x0012, 0x0012, 0x2012, 0x2012, 0x0013, 0x0013, 0x2013, 0x2013, 
            0x0014, 0x0014, 0x2014, 0x2014, 0x0015, 0x0015, 0x2015, 0x2015, 0x0016, 0x0016, 0x2016, 0x2016, 0x0017, 0x0017, 0x2017, 0x2017, 
            0x0010, 0x0010, 0x2010, 0x2010, 0x0011, 0x0011, 0x2011, 0x2011, 0x0012, 0x0012, 0x2012, 0x2012, 0x0013, 0x0013, 0x2013, 0x2013, 
            0x0014, 0x0014, 0x2014, 0x2014, 0x0015, 0x0015, 0x2015, 0x2015, 0x0016, 0x0016, 0x2016, 0x2016, 0x0017, 0x0017, 0x2017, 0x2017, 
            0x4010, 0x4010, 0x6010, 0x6010, 0x4011, 0x4011, 0x6011, 0x6011, 0x4012, 0x4012, 0x6012, 0x6012, 0x4013, 0x4013, 0x6013, 0x6013, 
            0x4014, 0x4014, 0x6014, 0x6014, 0x4015, 0x4015, 0x6015, 0x6015, 0x4016, 0x4016, 0x6016, 0x6016, 0x4017, 0x4017, 0x6017, 0x6017, 
            0x4010, 0x4010, 0x6010, 0x6010, 0x4011, 0x4011, 0x6011, 0x6011, 0x4012, 0x4012, 0x6012, 0x6012, 0x4013, 0x4013, 0x6013, 0x6013, 
            0x4014, 0x4014, 0x6014, 0x6014, 0x4015, 0x4015, 0x6015, 0x6015, 0x4016, 0x4016, 0x6016, 0x6016, 0x4017, 0x4017, 0x6017, 0x6017, 
            0x0018, 0x0018, 0x2018, 0x2018, 0x0019, 0x0019, 0x2019, 0x2019, 0x001A, 0x001A, 0x201A, 0x201A, 0x001B, 0x001B, 0x201B, 0x201B, 
            0x001C, 0x001C, 0x201C, 0x201C, 0x001D, 0x001D, 0x201D, 0x201D, 0x001E, 0x001E, 0x201E, 0x201E, 0x001F, 0x001F, 0x201F, 0x201F, 
            0x0018, 0x0018, 0x2018, 0x2018, 0x0019, 0x0019, 0x2019, 0x2019, 0x001A, 0x001A, 0x201A, 0x201A, 0x001B, 0x001B, 0x201B, 0x201B, 
            0x001C, 0x001C, 0x201C, 0x201C, 0x001D, 0x001D, 0x201D, 0x201D, 0x001E, 0x001E, 0x201E, 0x201E, 0x001F, 0x001F, 0x201F, 0x201F, 
            0x4018, 0x4018, 0x6018, 0x6018, 0x4019, 0x4019, 0x6019, 0x6019, 0x401A, 0x401A, 0x601A, 0x601A, 0x401B, 0x401B, 0x601B, 0x601B, 
            0x401C, 0x401C, 0x601C, 0x601C, 0x401D, 0x401D, 0x601D, 0x601D, 0x401E, 0x401E, 0x601E, 0x601E, 0x401F, 0x401F, 0x601F, 0x601F, 
            0x4018, 0x4018, 0x6018, 0x6018, 0x4019, 0x4019, 0x6019, 0x6019, 0x401A, 0x401A, 0x601A, 0x601A, 0x401B, 0x401B, 0x601B, 0x601B, 
            0x401C, 0x401C, 0x601C, 0x601C, 0x401D, 0x401D, 0x601D, 0x601D, 0x401E, 0x401E, 0x601E, 0x601E, 0x401F, 0x401F, 0x601F, 0x601F, 
            0x0020, 0x0020, 0x2020, 0x2020, 0x0021, 0x0021, 0x2021, 0x2021, 0x0022, 0x0022, 0x2022, 0x2022, 0x0023, 0x0023, 0x2023, 0x2023, 
            0x0024, 0x0024, 0x2024, 0x2024, 0x0025, 0x0025, 0x2025, 0x2025, 0x0026, 0x0026, 0x2026, 0x2026, 0x0027, 0x0027, 0x2027, 0x2027, 
            0x0020, 0x0020, 0x2020, 0x2020, 0x0021, 0x0021, 0x2021, 0x2021, 0x0022, 0x0022, 0x2022, 0x2022, 0x0023, 0x0023, 0x2023, 0x2023, 
            0x0024, 0x0024, 0x2024, 0x2024, 0x0025, 0x0025, 0x2025, 0x2025, 0x0026, 0x0026, 0x2026, 0x2026, 0x0027, 0x0027, 0x2027, 0x2027, 
            0x4020, 0x4020, 0x6020, 0x6020, 0x4021, 0x4021, 0x6021, 0x6021, 0x4022, 0x4022, 0x6022, 0x6022, 0x4023, 0x4023, 0x6023, 0x6023, 
            0x4024, 0x4024, 0x6024, 0x6024, 0x4025, 0x4025, 0x6025, 0x6025, 0x4026, 0x4026, 0x6026, 0x6026, 0x4027, 0x4027, 0x6027, 0x6027, 
            0x4020, 0x4020, 0x6020, 0x6020, 0x4021, 0x4021, 0x6021, 0x6021, 0x4022, 0x4022, 0x6022, 0x6022, 0x4023, 0x4023, 0x6023, 0x6023, 
            0x4024, 0x4024, 0x6024, 0x6024, 0x4025, 0x4025, 0x6025, 0x6025, 0x4026, 0x4026, 0x6026, 0x6026, 0x4027, 0x4027, 0x6027, 0x6027, 
            0x0028, 0x0028, 0x2028, 0x2028, 0x0029, 0x0029, 0x2029, 0x2029, 0x002A, 0x002A, 0x202A, 0x202A, 0x002B, 0x002B, 0x202B, 0x202B, 
            0x002C, 0x002C, 0x202C, 0x202C, 0x002D, 0x002D, 0x202D, 0x202D, 0x002E, 0x002E, 0x202E, 0x202E, 0x002F, 0x002F, 0x202F, 0x202F, 
            0x0028, 0x0028, 0x2028, 0x2028, 0x0029, 0x0029, 0x2029, 0x2029, 0x002A, 0x002A, 0x202A, 0x202A, 0x002B, 0x002B, 0x202B, 0x202B, 
            0x002C, 0x002C, 0x202C, 0x202C, 0x002D, 0x002D, 0x202D, 0x202D, 0x002E, 0x002E, 0x202E, 0x202E, 0x002F, 0x002F, 0x202F, 0x202F, 
            0x4028, 0x4028, 0x6028, 0x6028, 0x4029, 0x4029, 0x6029, 0x6029, 0x402A, 0x402A, 0x602A, 0x602A, 0x402B, 0x402B, 0x602B, 0x602B, 
            0x402C, 0x402C, 0x602C, 0x602C, 0x402D, 0x402D, 0x602D, 0x602D, 0x402E, 0x402E, 0x602E, 0x602E, 0x402F, 0x402F, 0x602F, 0x602F, 
            0x4028, 0x4028, 0x6028, 0x6028, 0x4029, 0x4029, 0x6029, 0x6029, 0x402A, 0x402A, 0x602A, 0x602A, 0x402B, 0x402B, 0x602B, 0x602B, 
            0x402C, 0x402C, 0x602C, 0x602C, 0x402D, 0x402D, 0x602D, 0x602D, 0x402E, 0x402E, 0x602E, 0x602E, 0x402F, 0x402F, 0x602F, 0x602F, 
            0x0030, 0x0030, 0x2030, 0x2030, 0x0031, 0x0031, 0x2031, 0x2031, 0x0032, 0x0032, 0x2032, 0x2032, 0x0033, 0x0033, 0x2033, 0x2033, 
            0x0034, 0x0034, 0x2034, 0x2034, 0x0035, 0x0035, 0x2035, 0x2035, 0x0036, 0x0036, 0x2036, 0x2036, 0x0037, 0x0037, 0x2037, 0x2037, 
            0x0030, 0x0030, 0x2030, 0x2030, 0x0031, 0x0031, 0x2031, 0x2031, 0x0032, 0x0032, 0x2032, 0x2032, 0x0033, 0x0033, 0x2033, 0x2033, 
            0x0034, 0x0034, 0x2034, 0x2034, 0x0035, 0x0035, 0x2035, 0x2035, 0x0036, 0x0036, 0x2036, 0x2036, 0x0037, 0x0037, 0x2037, 0x2037, 
            0x4030, 0x4030, 0x6030, 0x6030, 0x4031, 0x4031, 0x6031, 0x6031, 0x4032, 0x4032, 0x6032, 0x6032, 0x4033, 0x4033, 0x6033, 0x6033, 
            0x4034, 0x4034, 0x6034, 0x6034, 0x4035, 0x4035, 0x6035, 0x6035, 0x4036, 0x4036, 0x6036, 0x6036, 0x4037, 0x4037, 0x6037, 0x6037, 
            0x4030, 0x4030, 0x6030, 0x6030, 0x4031, 0x4031, 0x6031, 0x6031, 0x4032, 0x4032, 0x6032, 0x6032, 0x4033, 0x4033, 0x6033, 0x6033, 
            0x4034, 0x4034, 0x6034, 0x6034, 0x4035, 0x4035, 0x6035, 0x6035, 0x4036, 0x4036, 0x6036, 0x6036, 0x4037, 0x4037, 0x6037, 0x6037, 
            0x0038, 0x0038, 0x2038, 0x2038, 0x0039, 0x0039, 0x2039, 0x2039, 0x003A, 0x003A, 0x203A, 0x203A, 0x003B, 0x003B, 0x203B, 0x203B, 
            0x003C, 0x003C, 0x203C, 0x203C, 0x003D, 0x003D, 0x203D, 0x203D, 0x003E, 0x003E, 0x203E, 0x203E, 0x003F, 0x003F, 0x203F, 0x203F, 
            0x0038, 0x0038, 0x2038, 0x2038, 0x0039, 0x0039, 0x2039, 0x2039, 0x003A, 0x003A, 0x203A, 0x203A, 0x003B, 0x003B, 0x203B, 0x203B, 
            0x003C, 0x003C, 0x203C, 0x203C, 0x003D, 0x003D, 0x203D, 0x203D, 0x003E, 0x003E, 0x203E, 0x203E, 0x003F, 0x003F, 0x203F, 0x203F, 
            0x4038, 0x4038, 0x6038, 0x6038, 0x4039, 0x4039, 0x6039, 0x6039, 0x403A, 0x403A, 0x603A, 0x603A, 0x403B, 0x403B, 0x603B, 0x603B, 
            0x403C, 0x403C, 0x603C, 0x603C, 0x403D, 0x403D, 0x603D, 0x603D, 0x403E, 0x403E, 0x603E, 0x603E, 0x403F, 0x403F, 0x603F, 0x603F, 
            0x4038, 0x4038, 0x6038, 0x6038, 0x4039, 0x4039, 0x6039, 0x6039, 0x403A, 0x403A, 0x603A, 0x603A, 0x403B, 0x403B, 0x603B, 0x603B, 
            0x403C, 0x403C, 0x603C, 0x603C, 0x403D, 0x403D, 0x603D, 0x603D, 0x403E, 0x403E, 0x603E, 0x603E, 0x403F, 0x403F, 0x603F, 0x603F };
    }
}
