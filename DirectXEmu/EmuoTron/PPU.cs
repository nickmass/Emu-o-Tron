using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron
{
    public class PPU
    {
        public MemoryStore PPUMemory;
        public ushort[] PPUMirrorMap = new ushort[0x8000];
        public byte[] SPRMemory = new byte[0x100];
        public byte[] PalMemory = new byte[0x20];
        public int[] colorChart = new int[0x200];

        private NESCore nes;
        private int palCounter;
        public bool frameComplete;
        public bool interruptNMI;
        private bool spriteOverflow;
        private bool spriteZeroHit;
        private bool addrLatch;
        private bool inVblank;
        private bool wasInVblank;
        private int pendingNMI = 0;
        private int spriteAddr;

        private int spriteTable;
        private int backgroundTable;
        private bool tallSprites;
        private bool nmiEnable;
        private bool vramInc;

        private byte grayScale;
        private bool leftmostBackground;
        private bool leftmostSprites;
        private bool backgroundRendering;
        private bool spriteRendering;
        private ushort colorMask;
        private byte lastWrite;

        public int scanlineCycle;
        public int scanline = -1;

        private int loopyT;
        private int loopyX;
        private int loopyV;
        private byte readBuffer;

        public int[,] screen = new int[240,256];
        private ushort[] pixelMasks = new ushort[256];
        private ushort[] nextPixelMasks = new ushort[256];
        private byte[] pixelGray = new byte[256];
        private byte[] nextPixelGray = new byte[256];
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

        private int vblankEnd;

        private ushort[] zeroUshort = new ushort[256];
        private byte[] zeroGray = new byte[256];
        private bool[] zeroBackground = new bool[256];
        private int[] spriteLine = new int[256];
        private bool[] spriteAboveLine = new bool[256];
        private bool[] spriteBelowLine = new bool[256];

        public PPU(NESCore nes)
        {
            this.nes = nes;
            if (nes.rom.mapper == 19 || nes.rom.mapper == 210)
                PPUMemory = new MemoryStore(0x20 + (nes.rom.vROM) + 8, true);
            else if (nes.rom.vROM > 0)
                PPUMemory = new MemoryStore(0x20 + (nes.rom.vROM), true);
            else
            {
                PPUMemory = new MemoryStore(0x20 + (4 * 0x08), true);
                PPUMemory.SetReadOnly(0x0000, 8, false);
            }
            PPUMemory.swapOffset = 0x20;
            PPUMemory.SetReadOnly(0x2000, 4, false); //Nametables
            PPUMemory.SetReadOnly(0x3C00, 1, false); //Palette area + some mirrored ram
            for (ushort i = 0; i < 0x8000; i++)
                PPUMirrorMap[i] = i;
            for (int i = 0; i < 0x200; i++)
                colorChart[i] = i;
            PPUMirror(0x3F00, 0x3F10, 1, 1);
            PPUMirror(0x3F04, 0x3F14, 1, 1);
            PPUMirror(0x3F08, 0x3F18, 1, 1);
            PPUMirror(0x3F0C, 0x3F1C, 1, 1);
            PPUMirror(0x2000, 0x3000, 0x0F00, 1);
            PPUMirror(0x3F00, 0x3F20, 0x20, 7);

            for (int i = 0; i < 256; i++)
                zeroGray[i] = 0x3F;

            switch (nes.nesRegion)
            {
                default:
                case SystemType.NTSC:
                    vblankEnd = 261;
                    break;
                case SystemType.PAL:
                    vblankEnd = 312;
                    break;

            }
        }
        public void Power()
        {
            for (int i = 0; i < 0x100; i++)
                SPRMemory[i] = 0;
            for (int i = 0x2000; i < 0x2800; i++)
                PPUMemory[i] = 0;
            for (int i = 0; i < 0x20; i++)
                PalMemory[i] = 0x0F; //Sets the background to black on startup to prevent grey flashes, not exactly accurate but it looks nicer
            Write(0, 0x2000);
            Write(0, 0x2001);
            Write(0, 0x2003);
            Write(0, 0x2005);
            Write(0, 0x2006);
            Write(0, 0x2007);
            addrLatch = false;
        }
        public void Reset()
        {
            Write(0, 0x2000);
            Write(0, 0x2001);
            Write(0, 0x2005);
            Write(0, 0x2007);
            addrLatch = false;

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
                SpriteZeroHit();
                if (spriteOverflow)
                    nextByte |= 0x20;
                if (spriteZeroHit)
                    nextByte |= 0x40;
                if ((inVblank && !(scanline == 241 && scanlineCycle == 0)) || (wasInVblank && scanlineCycle == 0 && scanline == -1))
                    nextByte |= 0x80;
                interruptNMI = false;
                inVblank = wasInVblank = false;
                addrLatch = false;
                nextByte |= lastWrite;
            }
            else if (address == 0x2004) //OAM Read
            {
                if ((spriteAddr & 0x03) == 0x02)
                    nextByte = (byte)(SPRMemory[spriteAddr & 0xFF] & 0xE3);
                else
                    nextByte = SPRMemory[spriteAddr & 0xFF];
            }
            else if (address == 0x2007) //PPU Data
            {
                if ((loopyV & 0x3F00) == 0x3F00)
                {
                    nextByte = (byte)(PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F] & grayScale); //random wiki readings claim gray scale is applied here but have seen no roms that test it or evidence to support it.
                    readBuffer = PPUMemory[PPUMirrorMap[loopyV & 0x2FFF]];
                }
                else
                {
                    nextByte = readBuffer;
                    readBuffer = PPUMemory[PPUMirrorMap[loopyV & 0x3FFF]];
                }
                int oldA12 = (loopyV >> 12) & 1;
                loopyV = (loopyV + (vramInc ? 0x20 : 0x01)) & 0x7FFF;
                if ((nes.rom.mapper == 4 || nes.rom.mapper == 48) && oldA12 == 0 && ((loopyV >> 12) & 1) == 1)
                    nes.mapper.IRQ(scanline);
            }
            return nextByte;
        }
        public void Write(byte value, ushort address)
        {
            if (address == 0x2000)
            {
                bool wasEnabled = nmiEnable;
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
                if (inVblank && nmiEnable && !wasEnabled)
                    pendingNMI = 1;
                lastWrite = (byte)(value & 0x1F);
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
                colorMask = (ushort)((value << 1) & 0x1C0);
                lastWrite = (byte)(value & 0x1F);
            }
            else if (address == 0x2003) //OAM Address
            {
                spriteAddr = value;
                lastWrite = (byte)(value & 0x1F);
            }
            else if (address == 0x2004) //OAM Write
            {
                SPRMemory[spriteAddr & 0xFF] = value;
                spriteAddr++;
                lastWrite = (byte)(value & 0x1F);
            }
            else if (address == 0x4014) //Sprite DMA
            {
                int startAddress = value << 8;
                for (int i = 0; i < 0x100; i++)
                    SPRMemory[(spriteAddr + i) & 0xFF] = nes.Memory[nes.MirrorMap[(startAddress + i) & 0xFFFF]];
                lastWrite = (byte)(value & 0x1F);
                nes.AddCycles(513);
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
                lastWrite = (byte)(value & 0x1F);
            }
            else if (address == 0x2006) //PPUAddr
            {
                if (!addrLatch)//1st Write
                {
                    loopyT = ((loopyT & 0x00FF) | ((value & 0x3F) << 8));
                }
                else //2nd Write
                {
                    loopyT = ((loopyT & 0x7F00) | value);
                    int oldA12 = ((loopyV >> 12) & 1); ;
                    loopyV = loopyT;
                    if ((nes.rom.mapper == 4 || nes.rom.mapper == 48) && oldA12 == 0 && ((loopyV >> 12) & 1) == 1)
                        nes.mapper.IRQ(scanline);
                }
                addrLatch = !addrLatch;
                lastWrite = (byte)(value & 0x1F);
            }
            else if (address == 0x2007) //PPU Write
            {
                if ((loopyV & 0x3F00) == 0x3F00)
                    PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F] = (byte)(value & 0x3F);
                else
                    PPUMemory[PPUMirrorMap[loopyV & 0x3FFF]] = value;

                int oldA12 = (loopyV >> 12) & 1;
                loopyV = ((loopyV + (vramInc ? 0x20 : 0x01)) & 0x7FFF);
                if ((nes.rom.mapper == 4 || nes.rom.mapper == 48) && oldA12 == 0 && ((loopyV >> 12) & 1) == 1)
                    nes.mapper.IRQ(scanline);
                lastWrite = (byte)(value & 0x1F);
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
            loopyV = (loopyV + 0x1000) & 0x7FFF;
            if ((loopyV & 0x7000) == 0)
            {
                loopyV = (loopyV & 0x7C1F) | ((loopyV + 0x20) & 0x03E0);
                if ((loopyV & 0x03E0) == 0x03C0)
                    loopyV = (loopyV & 0x7C1F) ^ 0x0800;
            }
        }
        private void VerticalReset()
        {
            loopyV = loopyT;
        }
        private void HorizontalReset()
        {
            loopyV = (loopyV & 0x7BE0) | (loopyT & 0x041F);
        }
        private void SpriteZeroHit()
        {
            int yPosition = SPRMemory[0] + 1;
            int xLocation = SPRMemory[3];
            if(!spriteZeroHit && (backgroundRendering && spriteRendering) && scanline < 240 && (yPosition <= scanline && yPosition + (tallSprites ? 16 : 8) > scanline) && xLocation <= scanlineCycle)
            {
                int tmpV = loopyV;
                Buffer.BlockCopy(zeroUshort, 0, zeroBackground, 0, 256);
                if (nes.rom.mapper == 0x05)
                {
                    ((Mappers.m005)nes.mapper).StartBackground(tallSprites);
                }
                for (int tile = 0; tile < 34; tile++)//each tile on line
                {
                    int tileAddr = PPUMirrorMap[0x2000 | (tmpV & 0x0FFF)];
                    int tileNumber = PPUMemory[tileAddr];
                    int chrAddress = backgroundTable | (tileNumber << 4) | ((tmpV >> 12) & 7);
                    byte lowChr = PPUMemory[chrAddress];
                    byte highChr = PPUMemory[chrAddress | 8];
                    for (int x = 0; x < 8; x++)//each pixel in tile
                    {
                        int xPosition = ((tile * 8) + x) - (loopyX & 0x7);
                        if (xPosition >= 0 && xPosition < 256)
                        {
                            byte color = (byte)(((lowChr & 0x80) >> 7) + ((highChr & 0x80) >> 6));
                            zeroBackground[xPosition] = (color == 0 || (!leftmostBackground && xPosition < 8) || !backgroundRendering);
                        }
                        lowChr <<= 1;
                        highChr <<= 1;
                    }
                    tmpV = (tmpV & 0x7FE0) | ((tmpV + 0x01) & 0x1F);
                    if ((tmpV & 0x1F) == 0)
                        tmpV ^= 0x0400;
                }
                if (nes.rom.mapper == 0x05)
                {
                    ((Mappers.m005)nes.mapper).StartSprites(tallSprites);
                }
                int spriteTable;
                int spriteY = (scanline - yPosition);
                int attr = SPRMemory[2];
                bool horzFlip = (attr & 0x40) != 0;
                bool vertFlip = (attr & 0x80) != 0;
                int spriteTileNumber = SPRMemory[1];
                if (tallSprites)
                {
                    if ((spriteTileNumber & 1) != 0)
                        spriteTable = 0x1000;
                    else
                        spriteTable = 0x0000;
                    spriteTileNumber &= 0xFE;
                    if (spriteY > 7)
                        spriteTileNumber |= 1;
                }
                else
                    spriteTable = this.spriteTable;
                int spriteChrAddress = (spriteTable | (spriteTileNumber << 4) | (spriteY & 7)) + (vertFlip ? tallSprites ? (spriteY > 7) ? Flip[spriteY & 7] - (1 << 4) : Flip[spriteY & 7] + (1 << 4) : Flip[spriteY & 7] : 0); //this is seriously mental :)
                byte spriteLowChr = PPUMemory[spriteChrAddress];
                byte spriteHighChr = PPUMemory[spriteChrAddress | 8];
                for (int xPosition = horzFlip ? xLocation + 7 : xLocation; horzFlip ? xPosition >= xLocation : xPosition < xLocation + 8; xPosition += horzFlip ? -1 : 1)//each pixel in tile
                {
                    if (xPosition < 256 && xPosition <= scanlineCycle)
                    {
                        byte color = (byte)(((spriteLowChr & 0x80) >> 7) + ((spriteHighChr & 0x80) >> 6));
                        if (color != 0 && !(!leftmostSprites && xPosition < 8))
                        {
                            if (!zeroBackground[xPosition] && xPosition != 255)
                                spriteZeroHit = true;
                        }
                    }
                    spriteLowChr <<= 1;
                    spriteHighChr <<= 1;
                }
            }
        }
        public void AddCycles(int cycles)
        {
            if(cycles > 50) //this is dumb but makes some things easier if every scanline is hit atleast once
            {
                AddCycles(cycles - 50);
                cycles = 50;
            }
            else if (pendingNMI == 2) //Blargg's 04-nmi_control.nes tests this, if NMI is enabled during vblank it fires after the NEXT instruction, this is a messy solution to a messy problem
            {
                pendingNMI = 0;
                interruptNMI = true;
            }
            else if (pendingNMI == 1)
            {
                pendingNMI++;
            }
            if (nes.nesRegion == SystemType.PAL)
            {
                int palCycles = 0;
                for (int i = 0; i < cycles; i++)
                {
                    if (palCounter++ % 5 != 0)
                        palCycles += 3;
                    else
                        palCycles += 4;
                }
                for (int i = 0; i < palCycles; i++)
                {
                    if (i + scanlineCycle >= 341)
                    {
                        nextPixelMasks[scanlineCycle + i - 341] = colorMask;
                        nextPixelGray[scanlineCycle + i - 341] = grayScale;
                    }
                    else if (i + scanlineCycle < 256)
                    {
                        pixelMasks[scanlineCycle + i] = colorMask;
                        pixelGray[scanlineCycle + i] = grayScale;
                    }
                }
                scanlineCycle += palCycles;
            }
            else
            {
                if (scanline < 240 && scanline >= 0)
                {
                    for (int i = 0; i < cycles * 3; i++)
                    {
                        if (i + scanlineCycle >= 341)
                        {
                            nextPixelMasks[scanlineCycle + i - 341] = colorMask;
                            nextPixelGray[scanlineCycle + i - 341] = grayScale;
                        }
                        else if (i + scanlineCycle < 256)
                        {
                            pixelMasks[scanlineCycle + i] = colorMask;
                            pixelGray[scanlineCycle + i] = grayScale;
                        }
                    }
                }
                scanlineCycle += (cycles * 3);
            }
            if (scanlineCycle >= 341)//scanline finished
            {
                if (nes.rom.crc == 0x279710DC && scanline == 28)
                    spriteZeroHit = true;
                scanlineCycle -= 341;
                bool spriteZeroLine = false;
                if (turbo)
                {
                    int yPosition = SPRMemory[0] + 1;
                    if (yPosition <= scanline && yPosition + (tallSprites ? 16 : 8) > scanline && !spriteZeroHit)
                        spriteZeroLine = true;
                    else if (backgroundRendering || spriteRendering) //Run through line in turbo mode if it isnt a sprite zero line
                    {
                        if (scanline < 240 && scanline >= 0)//real scanline
                        {
                            if (nes.rom.mapper == 5)
                            {
                                nes.mapper.IRQ(0);
                                ((Mappers.m005)nes.mapper).StartSprites(tallSprites);
                            }
                            for (int tile = 0; tile < 33; tile++)//each tile on line
                                HorizontalIncrement();
                            VerticalIncrement(); //I don't know if I actually need these and Im soo lazy to work out the math for it
                            HorizontalReset();
                        }
                        if ((nes.rom.mapper == 4 || nes.rom.mapper == 48) && scanline < 240)
                            nes.mapper.IRQ(scanline);
                        if (scanline == -1)
                            VerticalReset();
                    }
                }
                if ((backgroundRendering || spriteRendering) && ((turbo && spriteZeroLine) || !turbo))
                {
                    if (scanline < 240 && scanline >= 0)//real scanline
                    {
                        if (nes.rom.mapper == 0x05)
                        {
                            nes.mapper.IRQ(0);
                            ((Mappers.m005)nes.mapper).StartBackground(tallSprites);
                        }
                        for (int tile = 0; tile < 33; tile++)//each tile on line
                        {
                            int tileAddr = PPUMirrorMap[0x2000 | (loopyV & 0x0FFF)];
                            int tileNumber = PPUMemory[tileAddr];
                            int addrTableLookup = AttrTableLookup[tileAddr & 0x3FF];
                            int palette = ((PPUMemory[((tileAddr & 0x3C00) + 0x3C0) + (addrTableLookup & 0xFF)] >> (addrTableLookup >> 12)) & 0x3) << 2; //Shift it over 2 to convert it to a palmemory value
                            int chrAddress = backgroundTable | (tileNumber << 4) | ((loopyV >> 12) & 7);
                            int lowChr = PPUMemory[chrAddress];
                            int highChr = PPUMemory[chrAddress | 8] << 1; //shift high char over 1 for color calc, none = 0, lowchar = 1, highchar = 2, low + high = 3
                            int fineX = (loopyX & 0x7); //Don't like these vars but Im trying to keep as much as possible out of the pixel loop
                            int xPosition = 0;
                            int color = 0;
                            for (int x = 7; x >= 0; x--)//each pixel in tile, draw it backwards to simplify tile shifting and color computing
                            {
                                xPosition = ((tile << 3) | x) - fineX;
                                if (xPosition == (xPosition & 0xFF)) //& 0xFF keeps xposition between 0 and 256
                                {
                                    color = (lowChr & 0x1) | (highChr & 0x2);
                                    zeroBackground[xPosition] = (color == 0 || (!leftmostBackground && xPosition < 8) || !backgroundRendering);
                                    if (zeroBackground[xPosition] || !displayBG)
                                        screen[scanline, xPosition] = colorChart[(PalMemory[0x00] & pixelGray[xPosition]) | pixelMasks[xPosition]];
                                    else
                                        screen[scanline, xPosition] = colorChart[(PalMemory[palette | color] & pixelGray[xPosition]) | pixelMasks[xPosition]];
                                }
                                lowChr >>= 1;
                                highChr >>= 1;
                            }
                            if (nes.rom.mapper == 9 || nes.rom.mapper == 10)//MMC 2 Punch Out!, MMC 4 Fire Emblem
                                nes.mapper.IRQ(chrAddress);
                            HorizontalIncrement();
                        }
                        //HorizontalIncrement(); //fake 34th tile grab probably don't need it
                        VerticalIncrement();
                        HorizontalReset();
                        if (spriteRendering)
                        {
                            if (nes.rom.mapper == 0x05)
                            {
                                ((Mappers.m005)nes.mapper).StartSprites(tallSprites);
                            }
                            int spritesOnLine = 0;
                            for (int sprite = 0; sprite < 256; sprite += 4)
                            {
                                int yPosition = SPRMemory[sprite] + 1;
                                if (yPosition <= scanline && yPosition + (tallSprites ? 16 : 8) > scanline && (spritesOnLine < 8 || !enforceSpriteLimit))
                                {
                                    spritesOnLine++;
                                    int spriteTable;
                                    int spriteY = (scanline - yPosition);
                                    int attr = SPRMemory[sprite | 2];
                                    bool horzFlip = (attr & 0x40) != 0;
                                    bool vertFlip = (attr & 0x80) != 0;
                                    int tileNumber = SPRMemory[sprite | 1];
                                    if (tallSprites)
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
                                    int xLocation = SPRMemory[sprite | 3];
                                    int palette = ((attr & 0x03) << 0x2) | 0x10;
                                    int lowChr = PPUMemory[chrAddress];
                                    int highChr = PPUMemory[chrAddress | 8] << 1;
                                    int color = 0;
                                    int begin = horzFlip ? xLocation : xLocation + 7;
                                    int end = horzFlip ? xLocation + 8 : xLocation - 1;
                                    int direction = horzFlip ? 1 : -1;
                                    bool above = (attr & 0x20) == 0;
                                    for (int xPosition = begin; xPosition != end; xPosition += direction)//each pixel in tile
                                    {
                                        if (xPosition < 256 && !(spriteAboveLine[xPosition] || spriteBelowLine[xPosition]))
                                        {
                                            color = (lowChr & 0x1) | (highChr & 0x2);
                                            if (color != 0 && !(!leftmostSprites && xPosition < 8))
                                            {
                                                spriteAboveLine[xPosition] = above;
                                                spriteBelowLine[xPosition] = !above;
                                                spriteLine[xPosition] = (PalMemory[palette | color] & pixelGray[xPosition]) | pixelMasks[xPosition];
                                                if (sprite == 0 && !zeroBackground[xPosition] && xPosition != 255)
                                                    spriteZeroHit = true;
                                            }
                                        }
                                        lowChr >>= 1;
                                        highChr >>= 1;
                                    }
                                    if (nes.rom.mapper == 9 || nes.rom.mapper == 10)//MMC 2 Punch Out!, MMC 4 Fire Emblem
                                        nes.mapper.IRQ(chrAddress);
                                }
                            }
                            if (spritesOnLine > 8)
                                spriteOverflow = true;

                            if (spritesOnLine != 0 && displaySprites)
                            {
                                for (int column = 0; column < 256; column++)
                                {
                                    if (spriteAboveLine[column] || (spriteBelowLine[column] && zeroBackground[column]))
                                        screen[scanline, column] = colorChart[spriteLine[column]];
                                }
                            }
                        }
                    }

                    if ((nes.rom.mapper == 4 || nes.rom.mapper == 48) && scanline < 240)
                        nes.mapper.IRQ(scanline);
                    if (scanline == -1)
                        VerticalReset();
                }
                else if(!turbo)
                {
                    if (scanline < 240 && scanline >= 0)
                    {
                        if ((loopyV & 0x3F00) == 0x3F00)//Direct color control http://wiki.nesdev.com/w/index.php/Full_palette_demo
                        {
                            for (int i = 0; i < 256; i++)
                                screen[scanline, i] = colorChart[(PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F] & pixelGray[i]) | pixelMasks[i]];
                        }
                        else
                        {
                            for (int i = 0; i < 256; i++)
                                screen[scanline, i] = colorChart[(PalMemory[0x00] & pixelGray[i]) | pixelMasks[i]];
                        }
                    }
                }
                if (generateNameTables && scanline == generateLine)
                    nameTables = GenerateNameTables();
                if (generatePatternTables && scanline == generatePatternLine)
                {
                    patternTablesPalette = GeneratePatternTablePalette();
                    patternTables = GeneratePatternTables();
                }
                scanline++;
                PrepareForNextLine();
                if (scanline == 241)
                {
                    if (nes.rom.mapper == 0x05)
                        nes.mapper.IRQ(1);
                    if (nmiEnable)
                        interruptNMI = true;
                    inVblank = true;
                    //I think I will just put this out of my mind and hope the CPPU rewrite solves everything
                    //scanlineCycle += 36;//Now this makes it pass vbl_clear_time and nmi_sync but fail ppu_vbl_nmi I don't know which is less wrong : /
                }
                else if (scanline == vblankEnd)
                {
                    spriteOverflow = false;
                    spriteZeroHit = false;
                    frameComplete = true;
                    wasInVblank = inVblank;
                    inVblank = false; //Blarggs test claims this is about 37 cycles too late, but I have no idea how that can be. EDIT, passes Blarggs more recent ppu_vbl_nmi clear test so I guess its alright (kinda)
                    scanline = -1;
                }
            }
        }
        private void PrepareForNextLine()
        {
            for (int i = 0; i <= scanlineCycle && i < 256; i++)
            {
                pixelMasks[i] = nextPixelMasks[i];
                pixelGray[i] = nextPixelGray[i];
            }
            Buffer.BlockCopy(zeroGray, 0, nextPixelGray, 0, 256);
            Buffer.BlockCopy(zeroUshort, 0, nextPixelMasks, 0, 512); //Blockcopy is significantly faster then looping over the array, which in turn is faster then allocating a new array.
            Buffer.BlockCopy(zeroUshort, 0, spriteBelowLine, 0, 256);
            Buffer.BlockCopy(zeroUshort, 0, spriteAboveLine, 0, 256);
            Buffer.BlockCopy(zeroUshort, 0, zeroBackground, 0, 256);
        }

        private byte[][,] GenerateNameTables()
        {
            byte[][,] nameTables = new byte[4][,];
            ushort nameTableOffset = 0x2000;
            int xScroll = (((loopyT & 0x3FF) % 32) * 8) + loopyX;
            int yScroll = (((loopyT & 0x3FF) / 32) * 8) + ((loopyT >> 12) & 7);
            int ntScroll = (loopyT >> 10) & 3;
            if (ntScroll == 1 || ntScroll == 3)
                xScroll += 256;
            if (ntScroll == 2 || ntScroll == 3)
                yScroll += 240;
            for (int nameTable = 0; nameTable < 4; nameTable++)
            {
                nameTables[nameTable] = new byte[256, 240];
                int ntX = 0;
                int ntY = 0;
                if (nameTable == 1 || nameTable == 3)
                    ntX = 256;
                if (nameTable == 2 || nameTable == 3)
                    ntY = 240;
                for (int line = 0; line < 240; line++)
                {
                    int pointY = line + ntY;
                    for (int tile = 0; tile < 32; tile++)//each tile on line
                    {
                       
                        int tileAddr = PPUMirrorMap[nameTableOffset + ((line / 8) * 32) + tile];
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
                                    nameTables[nameTable][(tile * 8) + x, line] = PalMemory[0x00];
                                else
                                    nameTables[nameTable][(tile * 8) + x, line] = PalMemory[(palette * 4) + color];
                                int pointX = xPosition + ntX;
                                if(xScroll == pointX || yScroll == pointY)
                                    nameTables[nameTable][(tile * 8) + x, line] |= 0x80;
                            }
                            lowChr <<= 1;
                            highChr <<= 1;
                        }
                    }
                }
                nameTableOffset += 0x400;
            }
            return nameTables;
        }
        private byte[][] GeneratePatternTablePalette()
        {
            byte[][] pal = new byte[8][];
            for (int palette = 0; palette < 8; palette++)
            {
                pal[palette] = new byte[4];
                pal[palette][0] = PalMemory[0x00];
                pal[palette][1] = PalMemory[(palette * 4) + 1];
                pal[palette][2] = PalMemory[(palette * 4) + 2];
                pal[palette][3] = PalMemory[(palette * 4) + 3];
            }
            return pal;
        }
        private byte[][,] GeneratePatternTables()
        {
            byte[][,] patternTables = new byte[2][,];
            for (int patternTable = 0; patternTable < 2; patternTable++)
            {
                patternTables[patternTable] = new byte[128, 128];
                ushort spriteTable = (ushort)(patternTable * 0x1000);
                for (int line = 0; line < 128; line++)
                {
                    for (int column = 0; column < 16; column++)
                    {
                        byte tileNumber = (byte)(((line / 8) * 16) + (column));
                        int chrAddress = (spriteTable | (tileNumber << 4) | (line & 7));
                        byte lowChr = PPUMemory[chrAddress];
                        byte highChr = PPUMemory[chrAddress | 8];
                        for (int x = 0; x < 8; x++)//each pixel in tile
                        {
                            byte color = (byte)(((lowChr & 0x80) >> 7) + ((highChr & 0x80) >> 6));
                            patternTables[patternTable][(column*8) + x, line] = color;
                            lowChr <<= 1;
                            highChr <<= 1;
                        }
                    }
                }
            }
            return patternTables;
        }
        public void StateSave(BinaryWriter writer)
        {
            PPUMemory.StateSave(writer);
            for(int i = 0; i < 0x100; i++)
                writer.Write(SPRMemory[i]);
            for(int i = 0; i < 0x20; i++)
                writer.Write(PalMemory[i]);
            writer.Write(frameComplete);
            writer.Write(interruptNMI);
            writer.Write(spriteOverflow);
            writer.Write(spriteZeroHit);
            writer.Write(addrLatch);
            writer.Write(inVblank);
            writer.Write(spriteAddr);
            writer.Write(spriteTable);
            writer.Write(backgroundTable);
            writer.Write(tallSprites);
            writer.Write(nmiEnable);
            writer.Write(vramInc);
            writer.Write(grayScale);
            writer.Write(leftmostBackground);
            writer.Write(leftmostSprites);
            writer.Write(backgroundRendering);
            writer.Write(spriteRendering);
            writer.Write(colorMask);
            writer.Write(scanlineCycle);
            writer.Write(scanline);
            writer.Write(loopyT);
            writer.Write(loopyX);
            writer.Write(loopyV);
            writer.Write(readBuffer);
            writer.Write(palCounter);
            writer.Write(pendingNMI);
            writer.Write(lastWrite);
        }
        public void StateLoad(BinaryReader reader)
        {
            PPUMemory.StateLoad(reader);
            for (int i = 0; i < 0x100; i++)
                SPRMemory[i] = reader.ReadByte();
            for (int i = 0; i < 0x20; i++)
                PalMemory[i] = reader.ReadByte();
            frameComplete = reader.ReadBoolean();
            interruptNMI = reader.ReadBoolean();
            spriteOverflow = reader.ReadBoolean();
            spriteZeroHit = reader.ReadBoolean();
            addrLatch = reader.ReadBoolean();
            inVblank = reader.ReadBoolean();
            spriteAddr = reader.ReadInt32();
            spriteTable = reader.ReadInt32();
            backgroundTable = reader.ReadInt32();
            tallSprites = reader.ReadBoolean();
            nmiEnable = reader.ReadBoolean();
            vramInc = reader.ReadBoolean();
            grayScale = reader.ReadByte();
            leftmostBackground = reader.ReadBoolean();
            leftmostSprites = reader.ReadBoolean();
            backgroundRendering = reader.ReadBoolean();
            spriteRendering = reader.ReadBoolean();
            colorMask = reader.ReadUInt16();
            scanlineCycle = reader.ReadInt32();
            scanline = reader.ReadInt32();
            loopyT = reader.ReadInt32();
            loopyX = reader.ReadInt32();
            loopyV = reader.ReadInt32();
            readBuffer = reader.ReadByte();
            palCounter = reader.ReadInt32();
            pendingNMI = reader.ReadInt32();
            lastWrite = reader.ReadByte();
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
