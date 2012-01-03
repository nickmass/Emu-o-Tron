using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron
{
    public enum AccessType
    {
        spriteTile,
        bgTile,
        attrTable,
        nameTable,
        ppuData
    }
    public class PPU
    {
        public MemoryStore PPUMemory;
        public byte[] SPRMemory = new byte[0x100];
        public byte[] PalMemory = new byte[0x20];

        private NESCore nes;
        private int palCounter;
        public bool frameComplete;
        public bool interruptNMI;
        private bool spriteOverflow;
        private bool spriteZeroHit;
        private bool addrLatch;
        private bool inVblank;
        public int pendingNMI = 0;
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
        private int lastWriteDecay;
        private int lastWriteDecayTime = 4284000;

        public int scanlineCycle;
        public int scanline = -1;

        private int loopyT;
        private int loopyX;
        private int loopyV;
        private byte readBuffer;

        public uint[,] screen = new uint[240,256];
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

        private long lastUpdate = 0, currentTime = 0; //Gots to be long or overflow is wayyy too soon, I could make it reset once a frame or something but that doesnt really represent what these vars mean and makes it more complicated.
        private byte tile1Latch = 0;
        private byte tile2Latch = 0;
        private int paletteLatch = 0;
        private int paletteLatcher = 0;
        private int paletteShift = 0;
        private ushort tile1Shift = 0, tile2Shift = 0;
        private byte tileNumber = 0;
        private int tileAddress = 0;
        private int shiftCount = 0;
        private bool oddFrame;
        private int spritesFound = 0;
        private int spriteCount = 0;
        private byte[] spriteTileShift1 = new byte[64];
        private byte[] spriteTileShift2 = new byte[64];
        private int[] spriteCounter = new int[64];
        private int[] spritePalette = new int[64];
        private bool[] spriteAbove = new bool[64];
        private byte[] horzFlipTable;
        private byte[] secondaryOAM = new byte[256];
        private bool spriteZeroLine; //Gets changed on cycle 160 so need a 2nd var to keep track from 0 - 256
        private bool onSpriteZeroLine;
        private int maxSprites = 8;
        private long vblFlagRead;
        private long nmiPendingSet;
        private long vblankEndTime;
        private long nmiEnableTime;

        byte spriteData = 0;
        int secondaryOAMFillPtr = 0;
        int spriteF = 0;
        int secondaryOAMPtr = 0;
        int spriteN = 0;
        int spriteM = 0;
        int spriteE = 0;
        int currentSprite = 0;
        int spriteStage = 0;
        int currentSpriteY = 0;
        int currentSpriteTileNum = 0;
        int currentSpriteAttr = 0;
        int currentSpriteX = 0;
        byte currentSpritePlane1 = 0;
        byte currentSpritePlane2 = 0;
        int currentSpriteChrAddress = 0;

        public PPU(NESCore nes)
        {
            this.nes = nes;
            int vRAM = 0;
            if (nes.rom.vROM == 0 || nes.rom.mapper == 19 || nes.rom.mapper == 210)
                vRAM += 8;
            PPUMemory = new MemoryStore(4, nes.rom.vROM, vRAM, true); //4 hardwired to make doing extra nametables less insane.

            switch (nes.nesRegion)
            {
                default:
                case SystemType.NTSC:
                    vblankEnd = 261;
                    break;
                case SystemType.PAL:
                    vblankEnd = 311;
                    break;
            }
            horzFlipTable = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                horzFlipTable[i] = 0;
                if ((i & 0x80) != 0)
                    horzFlipTable[i] |= 0x01;
                if ((i & 0x40) != 0)
                    horzFlipTable[i] |= 0x02;
                if ((i & 0x20) != 0)
                    horzFlipTable[i] |= 0x04;
                if ((i & 0x10) != 0)
                    horzFlipTable[i] |= 0x08;
                if ((i & 0x08) != 0)
                    horzFlipTable[i] |= 0x10;
                if ((i & 0x04) != 0)
                    horzFlipTable[i] |= 0x20;
                if ((i & 0x02) != 0)
                    horzFlipTable[i] |= 0x40;
                if ((i & 0x01) != 0)
                    horzFlipTable[i] |= 0x80;
            }
        }
        public void Power()
        {
            PPUMemory.memMap[0x2000 >> 0xA] = 0;
            PPUMemory.memMap[0x2400 >> 0xA] = 1;
            PPUMemory.memMap[0x2800 >> 0xA] = PPUMemory.memMap[0x2800 >> 0xA];
            PPUMemory.memMap[0x2C00 >> 0xA] = PPUMemory.memMap[0x2C00 >> 0xA];
            PPUMemory.memMap[0x3000 >> 0xA] = PPUMemory.memMap[0x2000 >> 0xA];
            PPUMemory.memMap[0x3400 >> 0xA] = PPUMemory.memMap[0x2400 >> 0xA];
            PPUMemory.memMap[0x3800 >> 0xA] = PPUMemory.memMap[0x2800 >> 0xA];
            PPUMemory.memMap[0x3C00 >> 0xA] = PPUMemory.memMap[0x2C00 >> 0xA];
            PPUMemory.SetReadOnly(0x2000, 8, false); //Nametables
            if (nes.rom.mirroring == Mirroring.fourScreen)
            {
                PPUMemory.FourScreenMirroring();
                PPUMemory.hardwired = true;
            }
            else if (nes.rom.mirroring == Mirroring.vertical)
                PPUMemory.VerticalMirroring();
            else
                PPUMemory.HorizontalMirroring();
            for (int i = 0; i < 0x100; i++)
                SPRMemory[i] = 0;
            for (int i = 0x2000; i < 0x2800; i++)
                PPUMemory[i] = 0;
            for (int i = 0; i < 0x20; i++)
                PalMemory[i] = 0x0F; //Sets the background to black on startup to prevent grey flashes, not exactly accurate but it looks nicer
            scanline = 241;
            interruptNMI = false;
            pendingNMI = 0;
            inVblank = true;
            scanlineCycle = 0;
            currentTime = 0;
            lastUpdate = 0;
            vramInc = false;
            spriteTable = 0x0000;
            backgroundTable = 0x0000;
            tallSprites = false;
            grayScale = 0x3F;
            leftmostBackground = false;
            leftmostSprites = false;
            backgroundRendering = false;
            spriteRendering = false;
            colorMask = 0;
            spriteData = 0;
            spriteAddr = 0;
            loopyT = 0;
            loopyV = 0;
            loopyX = 0;
            readBuffer = 0;
            addrLatch = false;
            lastWrite = 0;
        }
        public void Reset()
        {
            vramInc = false;
            spriteTable = 0x0000;
            backgroundTable = 0x0000;
            tallSprites = false;
            grayScale = 0x3F;
            leftmostBackground = false;
            leftmostSprites = false;
            backgroundRendering = false;
            spriteRendering = false;
            colorMask = 0;
            loopyT = 0;
            loopyV = 0;
            loopyX = 0;
            readBuffer = 0;
            addrLatch = false;
        }
        public byte Read(ushort address)
        {
            byte nextByte = 0;
            switch (address)
            {
                case 0x2002: //PPU Status register
                    nextByte = 0;
                    if (spriteOverflow)
                        nextByte |= 0x20;
                    if (spriteZeroHit)
                        nextByte |= 0x40;
                    if (inVblank)
                        nextByte |= 0x80;
                    inVblank = false;
                    addrLatch = false;
                    nextByte |= (byte)(lastWrite & 0x1F);
                    vblFlagRead = currentTime;
                    if (vblFlagRead - nmiPendingSet == 0 || vblFlagRead - nmiPendingSet == 1) //suppression.nes, reading 2002 around the time it is set will prevent NMIs for 2 ppu cycles despite entering vblank.
                        pendingNMI = 0;
                    break;
                case 0x2004: //OAM Read
                    if (scanline < 240 && (spriteRendering || backgroundRendering))
                    {
                        if ((spriteAddr & 0x03) == 0x02) //Not sure if open bus and data width functions like this during rendering, but this is surely and improvement.
                        {
                            nextByte = (byte)(spriteData & 0xE3);
                            lastWrite = nextByte;
                            lastWriteDecay = lastWriteDecayTime;
                        }
                        else
                        {
                            nextByte = spriteData;
                        }
                    }
                    else
                    {
                        if ((spriteAddr & 0x03) == 0x02)
                        {
                            nextByte = (byte)(SPRMemory[spriteAddr & 0xFF] & 0xE3);
                            lastWrite = nextByte;
                            lastWriteDecay = lastWriteDecayTime;
                        }
                        else
                        {
                            nextByte = SPRMemory[spriteAddr & 0xFF];
                        }
                    }
                    break;
                case 0x2007: //PPU Data
                    if ((loopyV & 0x3F00) == 0x3F00)
                    {
                        nextByte = (byte)((PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F] & grayScale) | (lastWrite & 0xC0)); //random wiki readings claim gray scale is applied here but have seen no roms that test it or evidence to support it.
                        readBuffer = PPURead(loopyV, AccessType.ppuData);
                    }
                    else
                    {
                        nextByte = readBuffer;
                        readBuffer = PPURead(loopyV, AccessType.ppuData);
                    }
                    int oldA12 = (loopyV >> 12) & 1;
                    if (scanline < 240 && (spriteRendering || backgroundRendering)) //Young Indiana Jones fix, http://nesdev.parodius.com/bbs/viewtopic.php?t=6401
                    {
                        //if (vramInc)
                            VerticalIncrement();  //Doing ONLY verticle inc here regardless of vramInc seems to make Zelda 2's title screen background scroll correct, atleast compared to nintendulator need to make sure hardware is the same.
                        //else
                        //    HorizontalIncrement();//loopyV++;//Some parts of the thread suggest that this should be a horz increment but that breaks Camerica games intro
                    }
                    else
                        loopyV = (loopyV + (vramInc ? 0x20 : 0x01)) & 0x7FFF;
                    if ((nes.rom.mapper == 4 || nes.rom.mapper == 48) && oldA12 == 0 && ((loopyV >> 12) & 1) == 1)
                        nes.mapper.IRQ(0);
                    lastWrite = nextByte;
                    lastWriteDecay = lastWriteDecayTime;
                    break;
                default:
                    nextByte = lastWrite;
                    break;
            }
            return nextByte;
        }
        public void Write(byte value, ushort address)
        {
            switch (address)
            {
                case 0x2000:
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
                    if (!nmiEnable && currentTime - nmiPendingSet < 2)//disabling nmi shortly after vblank will prevent it from ever going off
                        pendingNMI = 0;
                    else if (nmiEnable && !wasEnabled)
                    {
                        nmiEnableTime = currentTime;
                        if (inVblank)
                            pendingNMI = 5; //enabling NMI inside vblank will trigger one immediatly
                    }
                    lastWrite = value;
                    lastWriteDecay = lastWriteDecayTime;
                    break;
                case 0x2001: //PPU Mask
                    if ((value & 0x01) != 0)
                        grayScale = 0x30;
                    else
                        grayScale = 0x3F;
                    leftmostBackground = (value & 0x02) != 0;
                    leftmostSprites = (value & 0x04) != 0;
                    backgroundRendering = (value & 0x08) != 0;
                    spriteRendering = (value & 0x10) != 0;
                    colorMask = (ushort)((value << 1) & 0x1C0);
                    lastWrite = value;
                    lastWriteDecay = lastWriteDecayTime;
                    break;
                case 0x2002:
                    lastWrite = value;
                    lastWriteDecay = lastWriteDecayTime;
                    break;
                case 0x2003: //OAM Address
                    spriteAddr = value;
                    lastWrite = value;
                    lastWriteDecay = lastWriteDecayTime;
                    break;
                case 0x2004: //OAM Write
                    SPRMemory[spriteAddr & 0xFF] = value;
                    spriteAddr++;
                    lastWrite = value;
                    lastWriteDecay = lastWriteDecayTime;
                    break;
                case 0x4014: //Sprite DMA
                    int startAddress = value << 8;
                    for (int i = 0; i < 0x100; i++)
                        SPRMemory[(spriteAddr + i) & 0xFF] = nes.Memory[(startAddress + i) & 0xFFFF];
                    if ((nes.counter & 1) == 0) //Sprite DMA always ends on even cycles.
                        nes.AddCycles(514);
                    else
                        nes.AddCycles(513);
                    break;
                case 0x2005: //PPUScroll
                    if (!addrLatch) //1st Write
                    {
                        loopyT = ((loopyT & 0x7FE0) | (value >> 3));
                        loopyX = value & 0x07;
                    }
                    else //2nd Write
                        loopyT = ((loopyT & 0x0C1F) | ((value & 0x07) << 12) | ((value & 0xF8) << 2));
                    addrLatch = !addrLatch;
                    lastWrite = value;
                    lastWriteDecay = lastWriteDecayTime;
                    break;
                case 0x2006: //PPUAddr
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
                            nes.mapper.IRQ(0);
                    }
                    addrLatch = !addrLatch;
                    lastWrite = value;
                    lastWriteDecay = lastWriteDecayTime;
                    break;
                case 0x2007: //PPU Write
                    if ((loopyV & 0x3F00) == 0x3F00)
                        PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F] = (byte)(value & 0x3F);
                    else
                        PPUWrite(loopyV, value, AccessType.ppuData);

                    int writeOldA12 = (loopyV >> 12) & 1;
                    loopyV = ((loopyV + (vramInc ? 0x20 : 0x01)) & 0x7FFF);
                    if ((nes.rom.mapper == 4 || nes.rom.mapper == 48) && writeOldA12 == 0 && ((loopyV >> 12) & 1) == 1)
                        nes.mapper.IRQ(0);
                    lastWrite = value;
                    lastWriteDecay = lastWriteDecayTime;
                    break;
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

        public void AddCycles(int cycles)
        {
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
                currentTime += palCycles;
            }
            else
            {
                currentTime += cycles * 3;
            }
            Update();
        }

        private void Update()
        {
            while (lastUpdate < currentTime)
            {
                if (scanline < 240 && (backgroundRendering || spriteRendering))
                {
                    if (scanline == -1 && scanlineCycle == 304)
                    {
                        VerticalReset();
                    }
                    else if (scanlineCycle == 251)
                    {
                        VerticalIncrement();
                    }
                    else if (scanlineCycle == 257)
                    {
                        HorizontalReset();
                    }


                    //Sprites
                    if (scanlineCycle < 64) //Fill secondary oam.
                    {
                        if ((scanlineCycle & 1) == 0)
                        {
                            spriteData = secondaryOAM[secondaryOAMFillPtr] = 0xFF;
                            secondaryOAMFillPtr++;
                        }
                        if (scanlineCycle == 63)
                        {
                            for (int i = secondaryOAMFillPtr; i < maxSprites << 2; i++)
                                secondaryOAM[i] = 0xFF;
                        }
                    }
                    else if (scanlineCycle < 256) //Sprite evaluation.
                    {
                        if ((scanlineCycle & 1) == 0)
                        {
                            if (spriteF < 4)
                                spriteData = SPRMemory[(spriteN << 2) + spriteF];
                            else if (spriteF == 4) //Evaluate for overflows, a tad bit insane.
                            {
                                if (spriteE == 0)
                                {
                                    spriteData = SPRMemory[(spriteN << 2) + spriteM];
                                    if (spriteData <= scanline && spriteData > (scanline - (tallSprites ? 16 : 8)))
                                    {
#if DEBUGGER
                                        if (!spriteOverflow)
                                            nes.debug.SpriteOverflow();
#endif
                                        spriteOverflow = true;
                                        spriteE++;
                                        spriteM++;
                                        if (spriteM == 4)
                                        {
                                            spriteN++;
                                            if (spriteN == 64)
                                                spriteN = 0;
                                            spriteM = 0;
                                        }
                                    }
                                    else
                                    {
                                        spriteN++;
                                        if (spriteN == 64)
                                            spriteF = 5;
                                        spriteM++;
                                        if (spriteM == 4)
                                            spriteM = 0;
                                    }
                                }
                                else
                                {
                                    spriteData = SPRMemory[(spriteN << 2) + spriteM];
                                    spriteE++;
                                    spriteM++;
                                    if (spriteM == 4)
                                    {
                                        spriteN++;
                                        if (spriteN == 64)
                                            spriteN = 0;
                                        spriteM = 0;
                                    }
                                    if (spriteE == 4)
                                        spriteE = 0;
                                }

                            }
                            else if(spriteF == 5) //Endless reads till HBlank
                            {
                                if (spriteN == 64)
                                    spriteN = 0;
                                spriteData = SPRMemory[(spriteN << 2)];
                                spriteN++;
                            }
                        }
                        else
                        {
                            if (spriteF == 0)
                            {
                                secondaryOAM[secondaryOAMPtr] = spriteData;
                                if (spriteData <= scanline && spriteData > (scanline - (tallSprites ? 16 : 8)))
                                {
                                    if (spriteN == 0)
                                        spriteZeroLine = true;
                                    secondaryOAMPtr++;
                                    spriteF++;
                                    spritesFound++;
                                }
                                else //Not in range move onto the next sprite
                                {
                                    spriteN++;
                                    if (spriteN == 64)//All sprites evaluated, move into time wasting loop
                                    {
                                        spriteF = 5;
                                    }
                                }
                            }
                            else if (spriteF < 4)//Load in sprite attributes
                            {
                                secondaryOAM[secondaryOAMPtr] = spriteData;
                                secondaryOAMPtr++;
                                spriteF++;
                                if (spriteF == 4)
                                {
                                    spriteN++;
                                    if (spriteN == 64)//All sprites evaluated, move into time wasting loop
                                    {
                                        spriteF = 5;
                                    }
                                    else if (spritesFound < 8)//Finished loading sprite, search for more.
                                    {
                                        spriteF = 0;
                                    }
                                    else //8 sprites have been found check for overflow.
                                    {
                                        spriteF = 4;
                                        if (maxSprites > 8)//Handle disable sprite limit
                                        {
                                            int overflowSpriteN = spriteN;
                                            int overflowOAMPtr = secondaryOAMPtr;
                                            while (overflowSpriteN < maxSprites && overflowSpriteN < 64)
                                            {
                                                if (SPRMemory[overflowSpriteN << 2] <= scanline && SPRMemory[overflowSpriteN << 2] > (scanline - (tallSprites ? 16 : 8)))
                                                {
                                                    spritesFound++;
                                                    secondaryOAM[overflowOAMPtr] = SPRMemory[overflowSpriteN << 2];
                                                    overflowOAMPtr++;
                                                    secondaryOAM[overflowOAMPtr] = SPRMemory[(overflowSpriteN << 2) + 1];
                                                    overflowOAMPtr++;
                                                    secondaryOAM[overflowOAMPtr] = SPRMemory[(overflowSpriteN << 2) + 2];
                                                    overflowOAMPtr++;
                                                    secondaryOAM[overflowOAMPtr] = SPRMemory[(overflowSpriteN << 2) + 3];
                                                    overflowOAMPtr++;
                                                }
                                                overflowSpriteN++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (scanlineCycle < 320)//Load sprite tiles and counters.
                    {
                        switch (spriteStage)
                        {
                            case 0:
                                currentSpriteY = spriteData = secondaryOAM[currentSprite << 2];
                                break;
                            case 1:
                                currentSpriteTileNum = spriteData = secondaryOAM[(currentSprite << 2) + 1];
                                //Should have garbage nametable read
                                break;
                            case 2:
                                currentSpriteAttr = spriteData = secondaryOAM[(currentSprite << 2) + 2];
                                spritePalette[currentSprite] = ((currentSpriteAttr & 3) << 2) | 0x10;
                                spriteAbove[currentSprite] = (currentSpriteAttr & 0x20) == 0;
                                break;
                            case 3:
                                currentSpriteX = spriteData = secondaryOAM[(currentSprite << 2) + 3];
                                spriteCounter[currentSprite] = currentSpriteX;
                                //Should have garbage nametable read
                                int spriteY = ((scanline) - currentSpriteY);
                                bool vertFlip = (currentSpriteAttr & 0x80) != 0;
                                if (tallSprites)
                                {
                                    int spriteTable;
                                    if ((currentSpriteTileNum & 1) != 0)
                                        spriteTable = 0x1000;
                                    else
                                        spriteTable = 0x0000;
                                    currentSpriteTileNum &= 0xFE;
                                    int flipper = 0;
                                    if (spriteY > 7)
                                    {
                                        currentSpriteTileNum |= 1;
                                        if (vertFlip)
                                            flipper = vertFlipTable[spriteY & 7] - 16;
                                    }
                                    else if (vertFlip)
                                        flipper = vertFlipTable[spriteY & 7] + 16;
                                    currentSpriteChrAddress = (spriteTable | (currentSpriteTileNum << 4) | (spriteY & 7)) + flipper;
                                }
                                else
                                {
                                    currentSpriteChrAddress = (this.spriteTable | (currentSpriteTileNum << 4) | (spriteY & 7)) + (vertFlip ? vertFlipTable[spriteY & 7] : 0);
                                }
                                break;
                            case 4:
                                spriteData = secondaryOAM[(currentSprite << 2) + 3];
                                break;
                            case 5:
                                spriteData = secondaryOAM[(currentSprite << 2) + 3];
                                currentSpritePlane1 = PPURead(currentSpriteChrAddress, AccessType.spriteTile);
                                break;
                            case 6:
                                spriteData = secondaryOAM[(currentSprite << 2) + 3];
                                break;
                            case 7:
                                spriteData = secondaryOAM[(currentSprite << 2) + 3];
                                currentSpritePlane2 = PPURead(currentSpriteChrAddress + 8, AccessType.spriteTile);
                                bool horzFlip = (currentSpriteAttr & 0x40) != 0;
                                if (!horzFlip)
                                {
                                    spriteTileShift1[currentSprite] = horzFlipTable[currentSpritePlane1];
                                    spriteTileShift2[currentSprite] = horzFlipTable[currentSpritePlane2];
                                }
                                else
                                {
                                    spriteTileShift1[currentSprite] = currentSpritePlane1;
                                    spriteTileShift2[currentSprite] = currentSpritePlane2;
                                }
                                currentSprite++;
                                break;
                        }
                        spriteStage++;
                        if (spriteStage == 8)
                            spriteStage = 0;
                        if (currentSprite == 8 && maxSprites > 8) //Handle disable sprite limit, I don't really like duplicating this much code but it really seems like the best way. And now I can avoid triggering mappers and what not on extra sprites.
                        {
                            for (int i = 8; i < maxSprites; i++)
                            {
                                int spriteY = ((scanline) - secondaryOAM[i << 2]);
                                int tileNumber = secondaryOAM[(i << 2) + 1];
                                int attr = secondaryOAM[(i << 2) + 2];
                                spriteCounter[i] = secondaryOAM[(i << 2) + 3];
                                spritePalette[i] = ((attr & 3) << 2) | 0x10;
                                spriteAbove[i] = (attr & 0x20) == 0;
                                bool horzFlip = (attr & 0x40) != 0;
                                bool vertFlip = (attr & 0x80) != 0;
                                int chrAddress;
                                if (tallSprites)
                                {
                                    int tallSpriteTable;
                                    if ((tileNumber & 1) != 0)
                                        tallSpriteTable = 0x1000;
                                    else
                                        tallSpriteTable = 0x0000;
                                    tileNumber &= 0xFE;
                                    int flipper = 0;
                                    if (spriteY > 7)
                                    {
                                        tileNumber |= 1;
                                        if (vertFlip)
                                            flipper = vertFlipTable[spriteY & 7] - 16;
                                    }
                                    else if (vertFlip)
                                        flipper = vertFlipTable[spriteY & 7] + 16;
                                    chrAddress = (tallSpriteTable | (tileNumber << 4) | (spriteY & 7)) + flipper;
                                }
                                else
                                {
                                    chrAddress = (spriteTable | (tileNumber << 4) | (spriteY & 7)) + (vertFlip ? vertFlipTable[spriteY & 7] : 0);
                                }
                                if (!horzFlip)
                                {
                                    spriteTileShift1[i] = horzFlipTable[PPUMemory[chrAddress]];
                                    spriteTileShift2[i] = horzFlipTable[PPUMemory[chrAddress + 8]];
                                }
                                else
                                {
                                    spriteTileShift1[i] = PPUMemory[chrAddress];
                                    spriteTileShift2[i] = PPUMemory[chrAddress + 8];
                                }
                            }
                        }
                    }
                    else
                    {
                        spriteData = secondaryOAM[0];
                    }


                    //Background and draw routine.
                    if (scanlineCycle < 256 || (scanlineCycle >= 320 && scanlineCycle < 336))
                    {
                        if (shiftCount == 0)
                        {
                            tileAddress = 0x2000 | (loopyV & 0x0FFF);
                            tileNumber = PPURead(tileAddress, AccessType.nameTable);
                        }
                        else if (shiftCount == 2)
                        {
                            int addrTableLookup = AttrTableLookup[tileAddress & 0x3FF];
                            paletteLatcher = (PPURead(((tileAddress & 0x3C00) + 0x3C0) + (addrTableLookup & 0xFF), AccessType.attrTable) >> (addrTableLookup >> 12)) & 0x3;
                        }
                        else if (shiftCount == 4)
                        {
                            tile1Latch = PPURead(backgroundTable + (tileNumber << 4) + ((loopyV >> 12) & 7), AccessType.bgTile);
                        }
                        else if (shiftCount == 6)
                        {
                            tile2Latch = PPURead(backgroundTable + (tileNumber << 4) + ((loopyV >> 12) & 7) + 8, AccessType.bgTile);
                            HorizontalIncrement();
                        }
                        if (scanlineCycle < 256 && scanline != -1 && (!turbo || (turbo && onSpriteZeroLine && !spriteZeroHit))) //I think this is all I can cut with turbo mode without some messy rewrites : /
                        {
                            int fineX = (loopyX & 0x7);
                            int tileColor = (((tile1Shift << fineX) >> 15) & 1) | (((tile2Shift << fineX) >> 14) & 2);
                            bool zeroBackgroundPixel = (tileColor == 0 || (!leftmostBackground && scanlineCycle < 8) || !backgroundRendering);
                            if (zeroBackgroundPixel || !displayBG)
                                screen[scanline, scanlineCycle] = colorChart[(PalMemory[0x00] & grayScale) | colorMask];
                            else
                                screen[scanline, scanlineCycle] = colorChart[(PalMemory[((((paletteShift << (fineX << 1)) >> 14) & 3) << 2) | tileColor] & grayScale) | colorMask];
                            if (spriteRendering)
                            {
                                bool pixelDrawn = false;
                                for (int sprite = 0; sprite < spriteCount; sprite++)
                                {
                                    spriteCounter[sprite]--;
                                    if (spriteCounter[sprite] < 0 && spriteCounter[sprite] >= -8)
                                    {
                                        int color = (spriteTileShift1[sprite] & 1) | ((spriteTileShift2[sprite] & 1) << 1);
                                        if (color != 0 && !pixelDrawn && !(!leftmostSprites && scanlineCycle < 8))
                                        {
                                            pixelDrawn = true;
                                            if ((spriteAbove[sprite] || zeroBackgroundPixel) && displaySprites)
                                                screen[scanline, scanlineCycle] = colorChart[(PalMemory[spritePalette[sprite] | color] & grayScale) | colorMask];
                                            if (onSpriteZeroLine && sprite == 0 && !zeroBackgroundPixel && scanlineCycle != 255)
                                            {
#if DEBUGGER
                                                if (!spriteZeroHit)
                                                    nes.debug.SpriteZeroHit();
#endif
                                                spriteZeroHit = true;
                                            }
                                        }
                                        spriteTileShift1[sprite] >>= 1;
                                        spriteTileShift2[sprite] >>= 1;
                                    }
                                }
                            }
                        }
                        tile1Shift <<= 1;
                        tile2Shift <<= 1;
                        paletteShift <<= 2;
                        paletteShift |= paletteLatch;
                        shiftCount++;
                        if (shiftCount >= 8)
                        {
                            tile1Shift = (ushort)((tile1Shift & 0xFF00) | (tile1Latch));
                            tile2Shift = (ushort)((tile2Shift & 0xFF00) | (tile2Latch));
                            paletteLatch = paletteLatcher;
                            shiftCount = 0;
                        }
                    }
                    if (nes.rom.mapper == 4 || nes.rom.mapper == 48)
                    {
                        if(scanlineCycle == 264 && spriteTable == 0x1000 && backgroundTable == 0x0000)
                            nes.mapper.IRQ(0);
                        if (scanlineCycle == 8 && scanline == -1 && spriteTable == 0x0000 && backgroundTable == 0x1000) //This table config has a seperate scanline trigger at the start of the frame.
                            nes.mapper.IRQ(0);
                        else if (scanlineCycle == 328 && spriteTable == 0x0000 && backgroundTable == 0x1000)
                            nes.mapper.IRQ(0);

                    }
                    lastUpdate++;
                    scanlineCycle++;
                    if (scanline == -1 && oddFrame && nes.nesRegion == SystemType.NTSC && scanlineCycle == 337)//Skip cycle on odd frames.
                        scanlineCycle++;
                    if (scanlineCycle == 341)
                    {
                        if (generateNameTables && scanline == generateLine)
                            nameTables = GenerateNameTables();
                        if (generatePatternTables && scanline == generatePatternLine)
                        {
                            patternTablesPalette = GeneratePatternTablePalette();
                            patternTables = GeneratePatternTables();
                        }
                        if (nes.rom.mapper == 5)
                            nes.mapper.IRQ(0);
                        scanline++;
                        if (scanline == 240)
                            spriteAddr = 0; //spriteAddress is set to 0 at some point during rendering and should be initialized to 0 for vblank Dusty Diamond's All-Star Softbal depends on this behaviour, I require someone with a powerpak to get more precise timing.
                        scanlineCycle = 0;
                        onSpriteZeroLine = spriteZeroLine;
                        spriteCount = spritesFound;
                        spriteZeroLine = false;
                        shiftCount = 0;
                        secondaryOAMFillPtr = 0;
                        secondaryOAMPtr = 0;
                        spriteF = 0;
                        spriteN = 0;
                        spriteM = 0;
                        spriteE = 0;
                        spritesFound = 0;
                        currentSprite = 0;
                        spriteStage = 0;
                    }
                }
                else
                {
                    if (scanline >= 0 && scanline < 240)
                    {
                        if (scanlineCycle < 256)
                        {
                            if ((loopyV & 0x3F00) == 0x3F00)//Direct color control http://wiki.nesdev.com/w/index.php/Full_palette_demo
                                screen[scanline, scanlineCycle] = colorChart[(PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F] & grayScale) | colorMask];
                            else
                                screen[scanline, scanlineCycle] = colorChart[(PalMemory[0x00] & grayScale) | colorMask];
                        }
                    }
                    lastUpdate++;
                    scanlineCycle++;
                    shiftCount++;
                    if (nes.rom.mapper == 5)
                        nes.mapper.IRQ(1);
                    if (scanline == vblankEnd - 1 && scanlineCycle == 341)
                    {
                        if (lastUpdate - nmiEnableTime <= 1) //Attempting to force an nmi in vblank 1 ppu cycle before the end of vblank will fail.
                            pendingNMI = 0;
                    }
                    if (scanlineCycle == 341)
                    {
                        if (generateNameTables && scanline == generateLine)
                            nameTables = GenerateNameTables();
                        if (generatePatternTables && scanline == generatePatternLine)
                        {
                            patternTablesPalette = GeneratePatternTablePalette();
                            patternTables = GeneratePatternTables();
                        }
                        scanline++;
                        scanlineCycle = 0;
                        onSpriteZeroLine = spriteZeroLine;
                        spriteCount = spritesFound;
                        spriteZeroLine = false;
                        shiftCount = 0;
                        secondaryOAMFillPtr = 0;
                        secondaryOAMPtr = 0;
                        spriteF = 0;
                        spriteN = 0;
                        spriteM = 0;
                        spriteE = 0;
                        spritesFound = 0;
                        currentSprite = 0;
                        spriteStage = 0;
                        if (scanline == 241)
                        {
                            frameComplete = true;
                            if (vblFlagRead - lastUpdate != -1) //Reading 2002 at the same time as it is set will cause the PPU to never enter vblank
                            {
                                inVblank = true;
                                if (nmiEnable)
                                {
                                    nmiPendingSet = lastUpdate;
                                    pendingNMI = 3;
                                }
                            }
                        }
                        else if (scanline == vblankEnd)
                        {
                            vblankEndTime = lastUpdate;
                            scanline = -1;
                            inVblank = false;
                            spriteZeroHit = false;
                            spriteOverflow = false;
                            oddFrame = !oddFrame;
                            if (enforceSpriteLimit)
                                maxSprites = 8;
                            else
                                maxSprites = 64;
                        }
                    }
                }
                if (pendingNMI != 0)
                {
                    pendingNMI--;
                    if (pendingNMI == 0)
                        interruptNMI = true;
                }
                if (lastWriteDecay > 0)
                    lastWriteDecay--;
                else
                    lastWrite = 0;
            }
        }
        private byte PPURead(int addr, AccessType access)
        {
            ushort address = (ushort)(addr & 0x3FFF);
            byte value = PPUMemory[address];
            if (nes.rom.mapper == 5)
            {
                value = ((Mappers.m005)nes.mapper).PPURead(address, value, access, tallSprites);
            }
            else if (nes.rom.mapper == 9 || nes.rom.mapper == 10)//MMC 2 Punch Out!, MMC 4 Fire Emblem
            {
                nes.mapper.IRQ(address);
            }
            return value;
        }
        private void PPUWrite(int addr, byte value, AccessType access)
        {
            ushort address = (ushort)(addr & 0x3FFF);
            PPUMemory[address] = value;
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
                       
                        int tileAddr = nameTableOffset + ((line / 8) * 32) + tile;
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
            writer.Write(lastWriteDecay);
            writer.Write(lastUpdate);
            writer.Write(currentTime);
            writer.Write(vblFlagRead);
            writer.Write(oddFrame);

            writer.Write(tile1Latch);
            writer.Write(tile2Latch);
            writer.Write(paletteLatch);
            writer.Write(paletteLatcher);
            writer.Write(paletteShift);
            writer.Write(tile1Shift);
            writer.Write(tile2Shift);
            writer.Write(tileNumber);
            writer.Write(tileAddress);
            writer.Write(shiftCount);
            writer.Write(spritesFound);
            writer.Write(spriteZeroLine);
            writer.Write(onSpriteZeroLine);
            writer.Write(maxSprites);
            for (int i = 0; i < 64; i++)
                writer.Write(spriteTileShift1[i]);
            for (int i = 0; i < 64; i++)
                writer.Write(spriteTileShift2[i]);
            for (int i = 0; i < 64; i++)
                writer.Write(spriteCounter[i]);
            for (int i = 0; i < 64; i++)
                writer.Write(spritePalette[i]);
            for (int i = 0; i < 64; i++)
                writer.Write(spriteAbove[i]);
            for (int i = 0; i < 256; i++)
                writer.Write(secondaryOAM[i]);
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
            lastWriteDecay = reader.ReadInt32();
            lastUpdate = reader.ReadInt64();
            currentTime = reader.ReadInt64();
            vblFlagRead = reader.ReadInt64();
            oddFrame = reader.ReadBoolean();

            tile1Latch = reader.ReadByte();
            tile2Latch = reader.ReadByte();
            paletteLatch = reader.ReadInt32();
            paletteLatcher = reader.ReadInt32();
            paletteShift = reader.ReadInt32();
            tile1Shift = reader.ReadUInt16();
            tile2Shift = reader.ReadUInt16();
            tileNumber = reader.ReadByte();
            tileAddress = reader.ReadInt32();
            shiftCount = reader.ReadInt32();
            spritesFound = reader.ReadInt32();
            spriteZeroLine = reader.ReadBoolean();
            onSpriteZeroLine = reader.ReadBoolean();
            maxSprites = reader.ReadInt32();
            for (int i = 0; i < 64; i++)
                spriteTileShift1[i] = reader.ReadByte();
            for (int i = 0; i < 64; i++)
                spriteTileShift2[i] = reader.ReadByte();
            for (int i = 0; i < 64; i++)
                spriteCounter[i] = reader.ReadInt32();
            for (int i = 0; i < 64; i++)
                spritePalette[i] = reader.ReadInt32();
            for (int i = 0; i < 64; i++)
                spriteAbove[i] = reader.ReadBoolean();
            for (int i = 0; i < 256; i++)
                secondaryOAM[i] = reader.ReadByte();
        }
        private int[] vertFlipTable = { 7, 5, 3, 1, -1, -3, -5, -7};
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

        public uint[] colorChart = {
                            0xFF666666, 0xFF002A88, 0xFF1412A7, 0xFF3B00A4, 0xFF5C007E, 0xFF6E0040, 0xFF6C0700, 0xFF561D00, 
                            0xFF333500, 0xFF0C4800, 0xFF005200, 0xFF004F08, 0xFF00404D, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFFADADAD, 0xFF155FD9, 0xFF4240FF, 0xFF7527FE, 0xFFA01ACC, 0xFFB71E7B, 0xFFB53120, 0xFF994E00, 
                            0xFF6B6D00, 0xFF388700, 0xFF0D9300, 0xFF008F32, 0xFF007C8D, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFFFFFFFF, 0xFF64B0FF, 0xFF9290FF, 0xFFC676FF, 0xFFF26AFF, 0xFFFF6ECC, 0xFFFF8170, 0xFFEA9E22, 
                            0xFFBCBE00, 0xFF88D800, 0xFF5CE430, 0xFF45E082, 0xFF48CDDE, 0xFF4F4F4F, 0xFF000000, 0xFF000000, 
                            0xFFFFFFFF, 0xFFC0DFFF, 0xFFD3D2FF, 0xFFE8C8FF, 0xFFFAC2FF, 0xFFFFC4EA, 0xFFFFCCC5, 0xFFF7D8A5, 
                            0xFFE4E594, 0xFFCFEF96, 0xFFBDF4AB, 0xFFB3F3CC, 0xFFB5EBF2, 0xFFB8B8B8, 0xFF000000, 0xFF000000, 
                            0xFF704C4C, 0xFF002066, 0xFF160E7D, 0xFF41007B, 0xFF65005E, 0xFF790030, 0xFF770500, 0xFF5F1600, 
                            0xFF382800, 0xFF0D3600, 0xFF003E00, 0xFF003B06, 0xFF00303A, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFFBE8282, 0xFF1747A3, 0xFF4930BF, 0xFF811DBE, 0xFFB01499, 0xFFC9165C, 0xFFC72518, 0xFFA83A00, 
                            0xFF765200, 0xFF3E6500, 0xFF0E6E00, 0xFF006B26, 0xFF005D6A, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFFFFBFBF, 0xFF6E84BF, 0xFFA16CBF, 0xFFDA58BF, 0xFFFF50BF, 0xFFFF5299, 0xFFFF6154, 0xFFFF761A, 
                            0xFFCF8E00, 0xFF96A200, 0xFF65AB24, 0xFF4CA862, 0xFF4F9AA6, 0xFF573B3B, 0xFF000000, 0xFF000000, 
                            0xFFFFBFBF, 0xFFD3A7BF, 0xFFE89EBF, 0xFFFF96BF, 0xFFFF92BF, 0xFFFF93B0, 0xFFFF9994, 0xFFFFA27C, 
                            0xFFFBAC6F, 0xFFE4B370, 0xFFD0B780, 0xFFC5B699, 0xFFC7B0B6, 0xFFCA8A8A, 0xFF000000, 0xFF000000, 
                            0xFF4C704C, 0xFF002E66, 0xFF0F147D, 0xFF2C007B, 0xFF45005E, 0xFF520030, 0xFF510800, 0xFF402000, 
                            0xFF263A00, 0xFF094F00, 0xFF005A00, 0xFF005706, 0xFF00463A, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFF82BE82, 0xFF1069A3, 0xFF3246BF, 0xFF582BBE, 0xFF781D99, 0xFF89215C, 0xFF883618, 0xFF735600, 
                            0xFF507800, 0xFF2A9400, 0xFF0AA200, 0xFF009D26, 0xFF00886A, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFFBFFFBF, 0xFF4BC2BF, 0xFF6E9EBF, 0xFF9482BF, 0xFFB675BF, 0xFFBF7999, 0xFFBF8E54, 0xFFB0AE1A, 
                            0xFF8DD100, 0xFF66EE00, 0xFF45FB24, 0xFF34F662, 0xFF36E2A6, 0xFF3B573B, 0xFF000000, 0xFF000000, 
                            0xFFBFFFBF, 0xFF90F5BF, 0xFF9EE7BF, 0xFFAEDCBF, 0xFFBCD5BF, 0xFFBFD8B0, 0xFFBFE094, 0xFFB9EE7C, 
                            0xFFABFC6F, 0xFF9BFF70, 0xFF8EFF80, 0xFF86FF99, 0xFF88FFB6, 0xFF8ACA8A, 0xFF000000, 0xFF000000, 
                            0xFF575733, 0xFF002444, 0xFF110F54, 0xFF320052, 0xFF4E003F, 0xFF5E0020, 0xFF5C0600, 0xFF491900, 
                            0xFF2B2D00, 0xFF0A3D00, 0xFF004600, 0xFF004304, 0xFF003626, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFF939356, 0xFF12516C, 0xFF383680, 0xFF63217F, 0xFF881666, 0xFF9C1A3E, 0xFF9A2A10, 0xFF824200, 
                            0xFF5B5D00, 0xFF307300, 0xFF0B7D00, 0xFF007A19, 0xFF006946, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFFD9D980, 0xFF559680, 0xFF7C7A80, 0xFFA86480, 0xFFCE5A80, 0xFFD95E66, 0xFFD96E38, 0xFFC78611, 
                            0xFFA0A200, 0xFF74B800, 0xFF4EC218, 0xFF3BBE41, 0xFF3DAE6F, 0xFF434328, 0xFF000000, 0xFF000000, 
                            0xFFD9D980, 0xFFA3BE80, 0xFFB3B280, 0xFFC5AA80, 0xFFD5A580, 0xFFD9A775, 0xFFD9AD62, 0xFFD2B852, 
                            0xFFC2C34A, 0xFFB0CB4B, 0xFFA1CF56, 0xFF98CF66, 0xFF9AC879, 0xFF9C9C5C, 0xFF000000, 0xFF000000, 
                            0xFF4C4C70, 0xFF002096, 0xFF0F0EB8, 0xFF2C00B4, 0xFF45008B, 0xFF520046, 0xFF510500, 0xFF401600, 
                            0xFF262800, 0xFF093600, 0xFF003E00, 0xFF003B09, 0xFF003055, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFF8282BE, 0xFF1047EF, 0xFF3230FF, 0xFF581DFF, 0xFF7814E0, 0xFF891687, 0xFF882523, 0xFF733A00, 
                            0xFF505200, 0xFF2A6500, 0xFF0A6E00, 0xFF006B37, 0xFF005D9B, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFFBFBFFF, 0xFF4B84FF, 0xFF6E6CFF, 0xFF9458FF, 0xFFB650FF, 0xFFBF52E0, 0xFFBF617B, 0xFFB07625, 
                            0xFF8D8E00, 0xFF66A200, 0xFF45AB35, 0xFF34A88F, 0xFF369AF4, 0xFF3B3B57, 0xFF000000, 0xFF000000, 
                            0xFFBFBFFF, 0xFF90A7FF, 0xFF9E9EFF, 0xFFAE96FF, 0xFFBC92FF, 0xFFBF93FF, 0xFFBF99D9, 0xFFB9A2B6, 
                            0xFFABACA3, 0xFF9BB3A5, 0xFF8EB7BC, 0xFF86B6E0, 0xFF88B0FF, 0xFF8A8ACA, 0xFF000000, 0xFF000000, 
                            0xFF573357, 0xFF001574, 0xFF11098E, 0xFF32008B, 0xFF4E006B, 0xFF5E0036, 0xFF5C0400, 0xFF490E00, 
                            0xFF2B1A00, 0xFF0A2400, 0xFF002900, 0xFF002807, 0xFF002041, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFF935693, 0xFF1230B8, 0xFF3820D9, 0xFF6314D8, 0xFF880DAD, 0xFF9C0F69, 0xFF9A181B, 0xFF822700, 
                            0xFF5B3600, 0xFF304400, 0xFF0B4A00, 0xFF00482A, 0xFF003E78, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFFD980D9, 0xFF5558D9, 0xFF7C48D9, 0xFFA83BD9, 0xFFCE35D9, 0xFFD937AD, 0xFFD9405F, 0xFFC74F1D, 
                            0xFFA05F00, 0xFF746C00, 0xFF4E7229, 0xFF3B706E, 0xFF3D66BD, 0xFF432843, 0xFF000000, 0xFF000000, 
                            0xFFD980D9, 0xFFA370D9, 0xFFB369D9, 0xFFC564D9, 0xFFD561D9, 0xFFD962C7, 0xFFD966A7, 0xFFD26C8C, 
                            0xFFC2727E, 0xFFB07880, 0xFFA17A91, 0xFF987AAD, 0xFF9A76CE, 0xFF9C5C9C, 0xFF000000, 0xFF000000, 
                            0xFF335757, 0xFF002474, 0xFF0A0F8E, 0xFF1E008B, 0xFF2E006B, 0xFF370036, 0xFF360600, 0xFF2B1900, 
                            0xFF1A2D00, 0xFF063D00, 0xFF004600, 0xFF004307, 0xFF003641, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFF569393, 0xFF0A51B8, 0xFF2136D9, 0xFF3A21D8, 0xFF5016AD, 0xFF5C1A69, 0xFF5A2A1B, 0xFF4C4200, 
                            0xFF365D00, 0xFF1C7300, 0xFF067D00, 0xFF007A2A, 0xFF006978, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFF80D9D9, 0xFF3296D9, 0xFF497AD9, 0xFF6364D9, 0xFF795AD9, 0xFF805EAD, 0xFF806E5F, 0xFF75861D, 
                            0xFF5EA200, 0xFF44B800, 0xFF2EC229, 0xFF22BE6E, 0xFF24AEBD, 0xFF284343, 0xFF000000, 0xFF000000, 
                            0xFF80D9D9, 0xFF60BED9, 0xFF6AB3D9, 0xFF74AAD9, 0xFF7DA5D9, 0xFF80A7C7, 0xFF80ADA7, 0xFF7CB88C, 
                            0xFF72C37E, 0xFF68CB80, 0xFF5ECF91, 0xFF5ACFAD, 0xFF5AC8CE, 0xFF5C9C9C, 0xFF000000, 0xFF000000, 
                            0xFF3D3D3D, 0xFF001952, 0xFF0C0B64, 0xFF230062, 0xFF37004C, 0xFF420026, 0xFF410400, 0xFF341100, 
                            0xFF1F2000, 0xFF072B00, 0xFF003100, 0xFF002F05, 0xFF00262E, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFF686868, 0xFF0D3982, 0xFF282699, 0xFF461798, 0xFF60107A, 0xFF6E124A, 0xFF6D1D13, 0xFF5C2F00, 
                            0xFF404100, 0xFF225100, 0xFF085800, 0xFF00561E, 0xFF004A55, 0xFF000000, 0xFF000000, 0xFF000000, 
                            0xFF999999, 0xFF3C6A99, 0xFF585699, 0xFF774799, 0xFF914099, 0xFF99427A, 0xFF994D43, 0xFF8C5F14, 
                            0xFF717200, 0xFF528200, 0xFF37891D, 0xFF29864E, 0xFF2B7B85, 0xFF2F2F2F, 0xFF000000, 0xFF000000, 
                            0xFF999999, 0xFF738699, 0xFF7F7E99, 0xFF8B7899, 0xFF967499, 0xFF99768C, 0xFF997A76, 0xFF948263, 
                            0xFF898959, 0xFF7C8F5A, 0xFF719267, 0xFF6B927A, 0xFF6D8D91, 0xFF6E6E6E, 0xFF000000, 0xFF000000
		};
    }
}
