// Vert split VERY primitive

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m005 : Mapper
    {
        private int prgMode;
        private bool ramProtect1;
        private bool ramProtect2;
        private byte fillModeTile;
        private byte fillModePalatte;
        bool ramReadOnly;
        byte multiplicand;
        byte multiplier;
        byte[] exRAM = new byte[0x400];
        byte[] fillTable = new byte[0x400];
        byte exMode;
        byte highChr;
        int chrMode;
        int[] spriteBanks = new int[8];
        int[] backgroundBanks = new int[4];
        byte[] prgBanks = new byte[5];
        bool[] prgRAMBanks = new bool[5];
        bool tallSprite = false;
        byte irqTarget = 0;
        bool irqEnabled = false;
        byte irqCounter = 0;
        bool irqInFrame = false;
        bool interrupt;
        bool inBackground;
        bool lastWriteBackground;
        int nextAttrReadAddr;
        public override bool interruptMapper
        {
            get
            {
                return (irqEnabled && interrupt) || ((Channels.MMC5)nes.APU.external).interrupt;
            }
        }
        public m005(NESCore nes)
        {
            this.nes = nes;
            nes.APU.external = new Channels.MMC5(nes);
        }
        public override void Power()
        {
            tallSprite = false;
            irqTarget = 0;
            irqEnabled = false;
            irqCounter = 0;
            irqInFrame = false;
            ramReadOnly = true;
            prgRAMBanks[0] = true;
            prgRAMBanks[1] = false;
            prgRAMBanks[2] = false;
            prgRAMBanks[3] = false;
            prgRAMBanks[4] = false;
            prgMode = 0x3;
            prgBanks[0] = 0;
            prgBanks[1] = 0;
            prgBanks[2] = 0;
            prgBanks[3] = 0;
            prgBanks[4] = 0xFF;
            spriteBanks[0] = 0;
            spriteBanks[1] = 1;
            spriteBanks[2] = 2;
            spriteBanks[3] = 3;
            spriteBanks[4] = 4;
            spriteBanks[5] = 5;
            spriteBanks[6] = 6;
            spriteBanks[7] = 7;
            backgroundBanks[0] = 0;
            backgroundBanks[1] = 1;
            backgroundBanks[2] = 2;
            backgroundBanks[3] = 3;
            PRGSync();
            CHRSync();
            nes.PPU.PPUMemory.banks[0xA] = exRAM;
            nes.PPU.PPUMemory.banks[0xB] = fillTable;
        }
        public override byte Read(byte value, ushort address)
        {
            nes.APU.external.Read(value, address);
            switch (address)
            {
                case 0x5204:
                    value = 0;
                    if (interrupt)
                        value |= 0x80;
                    if (irqInFrame)
                        value |= 0x40;
                    interrupt = false;
                    break;
                case 0x5205:
                    value = (byte)((multiplicand * multiplier) & 0xFF);
                    break;
                case 0x5206:
                    value = (byte)((multiplicand * multiplier) >> 8);
                    break;

            }
            if (address >> 0xA == 0x5C00 >> 0xA)
            {
                if (exMode == 0 || exMode == 1)
                    value = (byte)(address & 0x3FF);//open bus
                else
                    value = exRAM[address & 0x3FF];
            }
            return value;
        }
        public override void Write(byte value, ushort address)
        {
            nes.APU.external.Write(value, address);

            if (address >> 0xA == 0x5C00 >> 0xA)
            {
                if (exMode == 2 || (irqInFrame && (exMode == 0 || exMode == 1)))
                    exRAM[address & 0x3FF] = value;
            }
            switch (address)
            {
                case 0x5100:
                    prgMode = value & 3;
                    PRGSync();
                    break;
                case 0x5101:
                    chrMode = value & 3;
                    CHRSync();
                    break;
                case 0x5102:
                    ramProtect1 = (value & 3) != 2;
                    ramReadOnly = ramProtect1 && ramProtect2;
                    PRGSync();
                    break;
                case 0x5103:
                    ramProtect2 = (value & 3) != 1;
                    ramReadOnly = ramProtect1 && ramProtect2;
                    PRGSync();
                    break;
                case 0x5104:
                    exMode = (byte)(value & 3);
                    break;
                case 0x5105:
                    nes.PPU.PPUMemory.CustomMirroring(0, value & 0x3);
                    nes.PPU.PPUMemory.CustomMirroring(1, (value >> 2) & 0x3);
                    nes.PPU.PPUMemory.CustomMirroring(2, (value >> 4) & 0x3);
                    nes.PPU.PPUMemory.CustomMirroring(3, (value >> 6) & 0x3);
                    break;
                case 0x5106:
                    if (value != fillModeTile)
                    {
                        fillModeTile = value;
                        for (int i = 0; i < 0x3C0; i++)
                            fillTable[i] = value;
                    }
                    break;
                case 0x5107:
                    if ((value & 0x3) != fillModePalatte)
                    {
                        fillModePalatte = (byte)(value & 0x3);
                        byte attr = (byte)(fillModePalatte | (fillModePalatte << 2) | (fillModePalatte << 4) | (fillModePalatte << 6));
                        for (int i = 0x3C0; i < 0x40; i++)
                            fillTable[i] = attr;
                    }
                    break;
                case 0x5113:
                    prgRAMBanks[0] = true;
                    prgBanks[0] = (byte)(value & 7);
                    PRGSync();
                    break;
                case 0x5114:
                    if ((value & 0x80) == 0)
                    {
                        prgRAMBanks[1] = true;
                        prgBanks[1] = (byte)(value & 7);
                    }
                    else
                    {
                        prgRAMBanks[1] = false;
                        prgBanks[1] = (byte)(value & 0x7f);
                    }
                    PRGSync();
                    break;
                case 0x5115:
                    if ((value & 0x80) == 0)
                    {
                        prgRAMBanks[2] = true;
                        prgBanks[2] = (byte)(value & 7);
                    }
                    else
                    {
                        prgRAMBanks[2] = false;
                        prgBanks[2] = (byte)(value & 0x7f);
                    }
                    PRGSync();
                    break;
                case 0x5116:
                    if ((value & 0x80) == 0)
                    {
                        prgRAMBanks[3] = true;
                        prgBanks[3] = (byte)(value & 7);
                    }
                    else
                    {
                        prgRAMBanks[3] = false;
                        prgBanks[3] = (byte)(value & 0x7f);
                    }
                    PRGSync();
                    break;
                case 0x5117:
                    prgRAMBanks[4] = false;
                    prgBanks[4] = (byte)(value & 0x7f);
                    PRGSync();
                    break;
                case 0x5120:
                case 0x5121:
                case 0x5122:
                case 0x5123:
                case 0x5124:
                case 0x5125:
                case 0x5126:
                case 0x5127:
                    lastWriteBackground = false;
                    spriteBanks[address & 7] = value | (highChr << 8);
                    CHRSync();
                    break;
                case 0x5128:
                case 0x5129:
                case 0x512A:
                case 0x512B:
                    lastWriteBackground = true;
                    backgroundBanks[address & 3] = value | (highChr << 8);
                    CHRSync();
                    break;
                case 0x5130:
                    highChr = (byte)(value & 0x3);
                    break;
                case 0x5200:
                    splitEnable = (value & 0x80) != 0;
                    splitRightSide = (value & 0x40) != 0;
                    splitTile = (byte)(value & 0x1F);
                    break;
                case 0x5201:
                    splitYScroll = value;
                    break;
                case 0x5202:
                    splitChrBank = value;
                    break;
                case 0x5203:
                    irqTarget = value;
                    break;
                case 0x5204:
                    irqEnabled = ((value & 0x80) != 0);
                    break;
                case 0x5205:
                    multiplicand = value;
                    break;
                case 0x5206:
                    multiplier = value;
                    break;
            }
        }
        bool splitEnable;
        bool splitRightSide;
        byte splitTile;
        byte splitYScroll;
        byte splitChrBank;
        int splitTileCounter;
        bool inSplit;
        public byte PPURead(int address, byte value, AccessType access, bool tallSprites)
        {
            inSplit = false;
            if (splitEnable && (exMode == 0 || exMode == 1))
            {
                if (access == AccessType.nameTable)
                    splitTileCounter++;
                if ((splitTileCounter < splitTile && !splitRightSide) || (splitTileCounter >= splitTile &&splitRightSide))
                {
                    inSplit = true;
                }
            }
            if (access == AccessType.spriteTile)
            {
                StartSprites(tallSprites);
                value = nes.PPU.PPUMemory[address];
                splitTileCounter = -1;
            }
            else if (inSplit)
            {
                if (access == AccessType.nameTable || access == AccessType.attrTable)
                {
                    value = exRAM[address & 0x3FF];
                    nes.PPU.PPUMemory.Swap4kROM(0x0000, splitChrBank % (nes.rom.vROM / 4));
                    nes.PPU.PPUMemory.Swap4kROM(0x1000, splitChrBank % (nes.rom.vROM / 4));
                }
            }
            else
            {
                if (exMode != 1)
                {
                    if (access == AccessType.bgTile)
                    {
                        StartBackground(tallSprites);
                        value = nes.PPU.PPUMemory[address];
                    }
                }
                else
                {
                    if (access == AccessType.nameTable)
                    {
                        nextAttrReadAddr = address & 0x3FF;//I could just do the math here and not do the actual bank swap, but that would really be almost duplicating the MemoryStore math.
                        nes.PPU.PPUMemory.Swap4kROM(0x0000, ((exRAM[address & 0x3FF] & 0x3F) | highChr << 6) % (nes.rom.vROM / 4));
                        nes.PPU.PPUMemory.Swap4kROM(0x1000, ((exRAM[address & 0x3FF] & 0x3F) | highChr << 6) % (nes.rom.vROM / 4));
                    }
                    else if (access == AccessType.attrTable)//This relies on the Attr read for a tile always coming directly after that tiles nametable read, otherwise I would need a lookup table.
                    {
                        int attr = (exRAM[nextAttrReadAddr] >> 6) & 3;
                        value = (byte)(attr | (attr << 2) | (attr << 4) | (attr << 6));
                    }
                }
            }
            return value;
        }
        public override void IRQ(int arg)
        {
            if (arg == 0)
            {
                if (irqInFrame)
                {
                    irqCounter++;
                    if (irqCounter == irqTarget)
                        interrupt = true;
                }
                else
                {
                    irqInFrame = true;
                    irqCounter = 0;
                    interrupt = false;
                }
            }
            else if (arg == 1)
            {
                irqInFrame = false;
            }
        }
        private void CHRSync()
        {
            if (inBackground)
                BackgroundSync();
            else
                SpriteSync();
        }
        private void SwapBackground()
        {
            switch (chrMode)
            {
                case 0:
                    nes.PPU.PPUMemory.Swap4kROM(0x0000, (backgroundBanks[3] * 2) % (nes.rom.vROM / 4));
                    nes.PPU.PPUMemory.Swap4kROM(0x1000, (backgroundBanks[3] * 2) % (nes.rom.vROM / 4));
                    break;
                case 1:
                    nes.PPU.PPUMemory.Swap4kROM(0x0000, backgroundBanks[3] % (nes.rom.vROM / 4));
                    nes.PPU.PPUMemory.Swap4kROM(0x1000, backgroundBanks[3] % (nes.rom.vROM / 4));
                    break;
                case 2:
                    nes.PPU.PPUMemory.Swap2kROM(0x0000, backgroundBanks[1] % (nes.rom.vROM / 2));
                    nes.PPU.PPUMemory.Swap2kROM(0x0800, backgroundBanks[3] % (nes.rom.vROM / 2));
                    nes.PPU.PPUMemory.Swap2kROM(0x1000, backgroundBanks[1] % (nes.rom.vROM / 2));
                    nes.PPU.PPUMemory.Swap2kROM(0x1800, backgroundBanks[3] % (nes.rom.vROM / 2));
                    break;
                case 3:
                    nes.PPU.PPUMemory.Swap1kROM(0x0000, backgroundBanks[0] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x0400, backgroundBanks[1] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x0800, backgroundBanks[2] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x0C00, backgroundBanks[3] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x1000, backgroundBanks[0] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x1400, backgroundBanks[1] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x1800, backgroundBanks[2] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x1C00, backgroundBanks[3] % nes.rom.vROM);
                    break;
            }
        }
        private void SwapSprites()
        {
            switch (chrMode)
            {
                case 0:
                    nes.PPU.PPUMemory.Swap8kROM(0x0000, spriteBanks[7] % (nes.rom.vROM / 8));
                    break;
                case 1:
                    nes.PPU.PPUMemory.Swap4kROM(0x0000, spriteBanks[3] % (nes.rom.vROM / 4));
                    nes.PPU.PPUMemory.Swap4kROM(0x1000, spriteBanks[7] % (nes.rom.vROM / 4));
                    break;
                case 2:
                    nes.PPU.PPUMemory.Swap2kROM(0x0000, spriteBanks[1] % (nes.rom.vROM / 2));
                    nes.PPU.PPUMemory.Swap2kROM(0x0800, spriteBanks[3] % (nes.rom.vROM / 2));
                    nes.PPU.PPUMemory.Swap2kROM(0x1000, spriteBanks[5] % (nes.rom.vROM / 2));
                    nes.PPU.PPUMemory.Swap2kROM(0x1800, spriteBanks[7] % (nes.rom.vROM / 2));
                    break;
                case 3:
                    nes.PPU.PPUMemory.Swap1kROM(0x0000, spriteBanks[0] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x0400, spriteBanks[1] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x0800, spriteBanks[2] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x0C00, spriteBanks[3] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x1000, spriteBanks[4] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x1400, spriteBanks[5] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x1800, spriteBanks[6] % nes.rom.vROM);
                    nes.PPU.PPUMemory.Swap1kROM(0x1C00, spriteBanks[7] % nes.rom.vROM);
                    break;
            }
        }
        private void SpriteSync()
        {
            if (tallSprite || !lastWriteBackground)
            {
                SwapSprites();
            }
            else
            {
                SwapBackground();
            }
        }
        private void BackgroundSync()
        {
            if (tallSprite || lastWriteBackground)
            {
                SwapBackground();
            }
            else
            {
                SwapSprites();
            }
        }
        public void StartBackground(bool tallSprites)
        {
            inBackground = true;
            this.tallSprite = tallSprites;
            CHRSync();
        }
        public void StartSprites(bool tallSprites)
        {
            inBackground = false;
            this.tallSprite = tallSprites;
            CHRSync();
        }
        private void SwapPrg(ushort addr, int size, int bank, bool ramBank, bool ramProtect)
        {
            if (ramBank)
            {
                if (ramProtect)
                {
                    switch (size)
                    {
                        case 32:
                            nes.Memory.Swap32kROM(addr, (bank >> 2) + (nes.rom.prgROM / 32)); //The addition here moves the bank into the 64kb I appended to the end of the memory store for mmc5.
                            break;
                        case 16:
                            nes.Memory.Swap16kROM(addr, (bank >> 1) + (nes.rom.prgROM / 16));
                            break;
                        case 8:
                            nes.Memory.Swap8kROM(addr, bank + (nes.rom.prgROM / 8));
                            break;
                    }
                }
                else
                {
                    switch (size)
                    {
                        case 32:
                            nes.Memory.Swap32kRAM(addr, (bank >> 2) + (nes.rom.prgROM / 32));
                            break;
                        case 16:
                            nes.Memory.Swap16kRAM(addr, (bank >> 1) + (nes.rom.prgROM / 16));
                            break;
                        case 8:
                            nes.Memory.Swap8kRAM(addr, bank + (nes.rom.prgROM / 8));
                            break;
                    }
                }
            }
            else
            {
                switch (size)
                {

                    case 32:
                        nes.Memory.Swap32kROM(addr, (bank >> 2) % (nes.rom.prgROM / 32));
                        break;
                    case 16:
                        nes.Memory.Swap16kROM(addr, (bank >> 1) % (nes.rom.prgROM / 16));
                        break;
                    case 8:
                        nes.Memory.Swap8kROM(addr, bank % (nes.rom.prgROM / 8));
                        break;
                }
            }
        }
        private void PRGSync()
        {
            SwapPrg(0x6000, 8, prgBanks[0], prgRAMBanks[0], ramReadOnly);
            switch (prgMode)
            {
                case 0:
                    SwapPrg(0x8000, 32, prgBanks[4], prgRAMBanks[4], ramReadOnly);
                    break;
                case 1:
                    SwapPrg(0x8000, 16, prgBanks[2], prgRAMBanks[2], ramReadOnly);
                    SwapPrg(0xC000, 16, prgBanks[4], prgRAMBanks[4], ramReadOnly);
                    break;
                case 2:
                    SwapPrg(0x8000, 16, prgBanks[2], prgRAMBanks[2], ramReadOnly);
                    SwapPrg(0xC000, 8, prgBanks[3], prgRAMBanks[3], ramReadOnly);
                    SwapPrg(0xE000, 8, prgBanks[4], prgRAMBanks[4], ramReadOnly);
                    break;
                case 3:
                    SwapPrg(0x8000, 8, prgBanks[1], prgRAMBanks[1], ramReadOnly);
                    SwapPrg(0xA000, 8, prgBanks[2], prgRAMBanks[2], ramReadOnly);
                    SwapPrg(0xC000, 8, prgBanks[3], prgRAMBanks[3], ramReadOnly);
                    SwapPrg(0xE000, 8, prgBanks[4], prgRAMBanks[4], ramReadOnly);
                    break;
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(prgMode);
            writer.Write(ramProtect1);
            writer.Write(ramProtect2);
            writer.Write(fillModeTile);
            writer.Write(fillModePalatte);
            writer.Write(ramReadOnly);
            writer.Write(multiplicand);
            writer.Write(multiplier);
            writer.Write(exMode);
            writer.Write(highChr);
            writer.Write(chrMode);
            writer.Write(spriteBanks[0]);
            writer.Write(spriteBanks[1]);
            writer.Write(spriteBanks[2]);
            writer.Write(spriteBanks[3]);
            writer.Write(spriteBanks[4]);
            writer.Write(spriteBanks[5]);
            writer.Write(spriteBanks[6]);
            writer.Write(spriteBanks[7]);
            writer.Write(backgroundBanks[0]);
            writer.Write(backgroundBanks[1]);
            writer.Write(backgroundBanks[2]);
            writer.Write(backgroundBanks[3]);
            writer.Write(prgBanks[0]);
            writer.Write(prgBanks[1]);
            writer.Write(prgBanks[2]);
            writer.Write(prgBanks[3]);
            writer.Write(prgBanks[4]);
            writer.Write(prgRAMBanks[0]);
            writer.Write(prgRAMBanks[1]);
            writer.Write(prgRAMBanks[2]);
            writer.Write(prgRAMBanks[3]);
            writer.Write(prgRAMBanks[4]);
            writer.Write(tallSprite);
            writer.Write(irqTarget);
            writer.Write(irqEnabled);
            writer.Write(irqCounter);
            writer.Write(irqInFrame);
            writer.Write(inBackground);
            writer.Write(lastWriteBackground);
            writer.Write(nextAttrReadAddr);
        }
        public override void StateLoad(BinaryReader reader)
        {
            prgMode = reader.ReadInt32();
            ramProtect1 = reader.ReadBoolean();
            ramProtect2 = reader.ReadBoolean();
            fillModeTile = reader.ReadByte();
            fillModePalatte = reader.ReadByte();
            ramReadOnly = reader.ReadBoolean();
            multiplicand = reader.ReadByte();
            multiplier = reader.ReadByte();
            exMode = reader.ReadByte();
            highChr = reader.ReadByte();
            chrMode = reader.ReadInt32();
            spriteBanks[0] = reader.ReadInt32();
            spriteBanks[1] = reader.ReadInt32();
            spriteBanks[2] = reader.ReadInt32();
            spriteBanks[3] = reader.ReadInt32();
            spriteBanks[4] = reader.ReadInt32();
            spriteBanks[5] = reader.ReadInt32();
            spriteBanks[6] = reader.ReadInt32();
            spriteBanks[7] = reader.ReadInt32();
            backgroundBanks[0] = reader.ReadInt32();
            backgroundBanks[1] = reader.ReadInt32();
            backgroundBanks[2] = reader.ReadInt32();
            backgroundBanks[3] = reader.ReadInt32();
            prgBanks[0] = reader.ReadByte();
            prgBanks[1] = reader.ReadByte();
            prgBanks[2] = reader.ReadByte();
            prgBanks[3] = reader.ReadByte();
            prgBanks[4] = reader.ReadByte();
            prgRAMBanks[0] = reader.ReadBoolean();
            prgRAMBanks[1] = reader.ReadBoolean();
            prgRAMBanks[2] = reader.ReadBoolean();
            prgRAMBanks[3] = reader.ReadBoolean();
            prgRAMBanks[4] = reader.ReadBoolean();
            tallSprite = reader.ReadBoolean();
            irqTarget = reader.ReadByte();
            irqEnabled = reader.ReadBoolean();
            irqCounter = reader.ReadByte();
            irqInFrame = reader.ReadBoolean();
            inBackground = reader.ReadBoolean();
            lastWriteBackground = reader.ReadBoolean();
            nextAttrReadAddr = reader.ReadInt32();
        }
    }
}
