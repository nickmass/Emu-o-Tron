// Runs Castlevania 3 (barely) and some demos but nothing else
// No vert split supported coded in at all and exram mode 1 is
// wrong

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
        int[] backgroundBanks = new int[8];
        int[] lastBanks = new int[8];
        byte[] prgBanks = new byte[5];
        bool[] prgRAMBanks = new bool[5];
        bool tallSprite = false;
        byte irqTarget = 0;
        bool irqEnabled = false;
        byte irqCounter = 0;
        bool irqInFrame = false;
        public m005(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            tallSprite = false;
            irqTarget = 0;
            irqEnabled = false;
            irqCounter = 0;
            irqInFrame = false;
            ramReadOnly = true;
            prgRAMBanks[0] = false;
            prgRAMBanks[1] = false;
            prgRAMBanks[2] = false;
            prgRAMBanks[3] = false;
            prgRAMBanks[4] = false;
            prgMode = 1;
            prgBanks[4] = (byte)((nes.rom.prgROM / 8)-1);
            PRGSync();
            SpriteSync();
            nes.Memory.SetReadOnly(0x5C00, 1, false);
            nes.Memory.banks[nes.Memory.memMap[0x5C00 >> 0xA]] = exRAM;
            nes.PPU.PPUMemory.banks[0xA] = exRAM;
            nes.PPU.PPUMemory.banks[0xB] = fillTable;
        }
        public override byte Read(byte value, ushort address)
        {
            switch (address)
            {
                case 0x5204:
                    value = 0;
                    if (interruptMapper)
                        value |= 0x80;
                    if (irqInFrame)
                        value |= 0x40;
                    interruptMapper = false;
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
                    value = (byte)(address - 0x5C00);//open bus
            }
            return value;
        }
        public override void Write(byte value, ushort address)
        {
            switch (address)
            {
                case 0x5100:
                    prgMode = value & 3;
                    PRGSync();
                    break;
                case 0x5101:
                    chrMode = value & 3;
                    break;
                case 0x5102:
                    ramProtect1 = (value & 3) == 2;
                    ramReadOnly = ramProtect1 && ramProtect2;
                    PRGSync();
                    break;
                case 0x5103:
                    ramProtect2 = (value & 3) == 1;
                    ramReadOnly = ramProtect1 && ramProtect2;
                    PRGSync();
                    break;
                case 0x5104:
                    exMode = (byte)(value & 3);
                    nes.Memory.SetReadOnly(0x5C00, 1, exMode == 3);
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
                    // Technically, $5113 should look something like:
                    // [.... .CPP]
                    //  C = Chip select
                    //  P = 8k PRG-RAM page on selected chip
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
                    spriteBanks[0] = lastBanks[0] = value | (highChr << 8);
                    break;
                case 0x5121:
                    spriteBanks[1] = lastBanks[1] = value | (highChr << 8);
                    break;
                case 0x5122:
                    spriteBanks[2] = lastBanks[2] = value | (highChr << 8);
                    break;
                case 0x5123:
                    spriteBanks[3] = lastBanks[3] = value | (highChr << 8);
                    break;
                case 0x5124:
                    spriteBanks[4] = lastBanks[4] = value | (highChr << 8);
                    break;
                case 0x5125:
                    spriteBanks[5] = lastBanks[5] = value | (highChr << 8);
                    break;
                case 0x5126:
                    spriteBanks[6] = lastBanks[6] = value | (highChr << 8);
                    break;
                case 0x5127:
                    spriteBanks[7] = lastBanks[7] = value | (highChr << 8);
                    break;
                case 0x5128:
                    backgroundBanks[0] = backgroundBanks[4] = lastBanks[0] = lastBanks[4] = value | (highChr << 8);
                    break;
                case 0x5129:
                    backgroundBanks[1] = backgroundBanks[5] = lastBanks[1] = lastBanks[5] = value | (highChr << 8);
                    break;
                case 0x512A:
                    backgroundBanks[2] = backgroundBanks[6] = lastBanks[2] = lastBanks[6] = value | (highChr << 8);
                    break;
                case 0x512B:
                    backgroundBanks[3] = backgroundBanks[7] = lastBanks[3] = lastBanks[7] = value | (highChr << 8);
                    break;
                case 0x5130:
                    highChr = (byte)(value & 0x3);
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
        public override void IRQ(int arg)
        {
            if (arg == 0)
            {
                if (irqCounter > 242)
                {
                    int i = 0;
                    i++;
                }
                if (irqInFrame)
                {
                    irqCounter++;
                    if (irqCounter == irqTarget)
                        interruptMapper = true;
                }
                else
                {
                    irqInFrame = true;
                    irqCounter = 0;
                    interruptMapper = false;
                }
            }
            else if (arg == 1)
            {
                irqInFrame = false;
            }
        }
        private void SpriteSync()
        {
            if (tallSprite)
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
            else
            {
                switch (chrMode)
                {
                    case 0:
                        nes.PPU.PPUMemory.Swap8kROM(0x0000, lastBanks[7] % (nes.rom.vROM / 8));
                        break;
                    case 1:
                        nes.PPU.PPUMemory.Swap4kROM(0x0000, lastBanks[3] % (nes.rom.vROM / 4));
                        nes.PPU.PPUMemory.Swap4kROM(0x1000, lastBanks[7] % (nes.rom.vROM / 4));
                        break;
                    case 2:
                        nes.PPU.PPUMemory.Swap2kROM(0x0000, lastBanks[1] % (nes.rom.vROM / 2));
                        nes.PPU.PPUMemory.Swap2kROM(0x0800, lastBanks[3] % (nes.rom.vROM / 2));
                        nes.PPU.PPUMemory.Swap2kROM(0x1000, lastBanks[5] % (nes.rom.vROM / 2));
                        nes.PPU.PPUMemory.Swap2kROM(0x1800, lastBanks[7] % (nes.rom.vROM / 2));
                        break;
                    case 3:
                        nes.PPU.PPUMemory.Swap1kROM(0x0000, lastBanks[0] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0400, lastBanks[1] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0800, lastBanks[2] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0C00, lastBanks[3] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1000, lastBanks[4] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1400, lastBanks[5] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1800, lastBanks[6] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1C00, lastBanks[7] % nes.rom.vROM);
                        break;
                }
            }
        }
        private void BackgroundSync()
        {
            if (tallSprite)
            {
                switch (chrMode)
                {
                    case 0:
                        nes.PPU.PPUMemory.Swap4kROM(0x0000, (backgroundBanks[7] * 2) % (nes.rom.vROM / 4));
                        nes.PPU.PPUMemory.Swap4kROM(0x1000, (backgroundBanks[7] * 2) % (nes.rom.vROM / 4));
                        break;
                    case 1:
                        nes.PPU.PPUMemory.Swap4kROM(0x0000, backgroundBanks[3] % (nes.rom.vROM / 4));
                        nes.PPU.PPUMemory.Swap4kROM(0x1000, backgroundBanks[7] % (nes.rom.vROM / 4));
                        break;
                    case 2:
                        nes.PPU.PPUMemory.Swap2kROM(0x0000, backgroundBanks[1] % (nes.rom.vROM / 2));
                        nes.PPU.PPUMemory.Swap2kROM(0x0800, backgroundBanks[3] % (nes.rom.vROM / 2));
                        nes.PPU.PPUMemory.Swap2kROM(0x1000, backgroundBanks[5] % (nes.rom.vROM / 2));
                        nes.PPU.PPUMemory.Swap2kROM(0x1800, backgroundBanks[7] % (nes.rom.vROM / 2));
                        break;
                    case 3:
                        nes.PPU.PPUMemory.Swap1kROM(0x0000, backgroundBanks[0] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0400, backgroundBanks[1] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0800, backgroundBanks[2] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0C00, backgroundBanks[3] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1000, backgroundBanks[4] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1400, backgroundBanks[5] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1800, backgroundBanks[6] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1C00, backgroundBanks[7] % nes.rom.vROM);
                        break;
                }
            }
            else
            {
                switch (chrMode)
                {
                    case 0:
                        nes.PPU.PPUMemory.Swap8kROM(0x0000, lastBanks[7] % (nes.rom.vROM / 8));
                        break;
                    case 1:
                        nes.PPU.PPUMemory.Swap4kROM(0x0000, lastBanks[3] % (nes.rom.vROM / 4));
                        nes.PPU.PPUMemory.Swap4kROM(0x1000, lastBanks[7] % (nes.rom.vROM / 4));
                        break;
                    case 2:
                        nes.PPU.PPUMemory.Swap2kROM(0x0000, lastBanks[1] % (nes.rom.vROM / 2));
                        nes.PPU.PPUMemory.Swap2kROM(0x0800, lastBanks[3] % (nes.rom.vROM / 2));
                        nes.PPU.PPUMemory.Swap2kROM(0x1000, lastBanks[5] % (nes.rom.vROM / 2));
                        nes.PPU.PPUMemory.Swap2kROM(0x1800, lastBanks[7] % (nes.rom.vROM / 2));
                        break;
                    case 3:
                        nes.PPU.PPUMemory.Swap1kROM(0x0000, lastBanks[0] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0400, lastBanks[1] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0800, lastBanks[2] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x0C00, lastBanks[3] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1000, lastBanks[4] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1400, lastBanks[5] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1800, lastBanks[6] % nes.rom.vROM);
                        nes.PPU.PPUMemory.Swap1kROM(0x1C00, lastBanks[7] % nes.rom.vROM);
                        break;
                }
            }
        }
        public void StartBackground(bool tallSprites)
        {
            this.tallSprite = tallSprites;
            BackgroundSync();
        }
        public void StartSprites(bool tallSprites)
        {
            this.tallSprite = tallSprites;
            SpriteSync();
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
            writer.Write(backgroundBanks[4]);
            writer.Write(backgroundBanks[5]);
            writer.Write(backgroundBanks[6]);
            writer.Write(backgroundBanks[7]);
            writer.Write(lastBanks[0]);
            writer.Write(lastBanks[1]);
            writer.Write(lastBanks[2]);
            writer.Write(lastBanks[3]);
            writer.Write(lastBanks[4]);
            writer.Write(lastBanks[5]);
            writer.Write(lastBanks[6]);
            writer.Write(lastBanks[7]);
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
            backgroundBanks[4] = reader.ReadInt32();
            backgroundBanks[5] = reader.ReadInt32();
            backgroundBanks[6] = reader.ReadInt32();
            backgroundBanks[7] = reader.ReadInt32();
            lastBanks[0] = reader.ReadInt32();
            lastBanks[1] = reader.ReadInt32();
            lastBanks[2] = reader.ReadInt32();
            lastBanks[3] = reader.ReadInt32();
            lastBanks[4] = reader.ReadInt32();
            lastBanks[5] = reader.ReadInt32();
            lastBanks[6] = reader.ReadInt32();
            lastBanks[7] = reader.ReadInt32();
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
        }
        private void PRGSync()
        {
            int swapBank;
            if (!ramReadOnly)
            {
                nes.Memory.Swap8kRAM(0x6000, (nes.rom.prgROM / 8) + prgBanks[0]);
                switch (prgMode)
                {
                    case 0:
                        swapBank = (prgBanks[4] % (nes.rom.prgROM / 8)) & 0xFC;
                        nes.Memory.Swap8kROM(0x8000, swapBank);
                        nes.Memory.Swap8kROM(0xA000, swapBank + 1);
                        nes.Memory.Swap8kROM(0xC000, swapBank + 2);
                        nes.Memory.Swap8kROM(0xE000, swapBank + 3);
                        break;
                    case 1:
                        swapBank = (prgBanks[2] % (nes.rom.prgROM / 8)) & 0xFE;
                        if (prgRAMBanks[2])
                        {
                            nes.Memory.Swap8kRAM(0x8000, (nes.rom.prgROM / 8) + swapBank);
                            nes.Memory.Swap8kRAM(0xA000, (nes.rom.prgROM / 8) + swapBank + 1);
                        }
                        else
                        {
                            nes.Memory.Swap8kROM(0x8000, swapBank);
                            nes.Memory.Swap8kROM(0xA000, swapBank + 1);
                        }
                        swapBank = (prgBanks[4] % (nes.rom.prgROM / 8)) & 0xFE;
                        nes.Memory.Swap8kROM(0xC000, swapBank);
                        nes.Memory.Swap8kROM(0xE000, swapBank + 1);
                        break;
                    case 2:
                        swapBank = (prgBanks[2] % (nes.rom.prgROM / 8)) & 0xFE;
                        if (prgRAMBanks[2])
                        {
                            nes.Memory.Swap8kRAM(0x8000, (nes.rom.prgROM / 8) + swapBank);
                            nes.Memory.Swap8kRAM(0xA000, (nes.rom.prgROM / 8) + swapBank + 1);
                        }
                        else
                        {
                            nes.Memory.Swap8kROM(0x8000, swapBank);
                            nes.Memory.Swap8kROM(0xA000, swapBank + 1);
                        }
                        if (prgRAMBanks[3])
                            nes.Memory.Swap8kRAM(0xC000, (nes.rom.prgROM / 8) + (prgBanks[3]));
                        else
                            nes.Memory.Swap8kROM(0xC000, prgBanks[3] % (nes.rom.prgROM / 8));
                        nes.Memory.Swap8kROM(0xE000, prgBanks[4] % (nes.rom.prgROM / 8));
                        break;
                    case 3:
                        if (prgRAMBanks[1])
                            nes.Memory.Swap8kRAM(0x8000, (nes.rom.prgROM / 8) + (prgBanks[1]));
                        else
                            nes.Memory.Swap8kROM(0x8000, prgBanks[1] % (nes.rom.prgROM / 8));
                        if (prgRAMBanks[2])
                            nes.Memory.Swap8kRAM(0xA000, (nes.rom.prgROM / 8) + (prgBanks[2]));
                        else
                            nes.Memory.Swap8kROM(0xA000, prgBanks[2] % (nes.rom.prgROM / 8));
                        if (prgRAMBanks[3])
                            nes.Memory.Swap8kRAM(0xC000, (nes.rom.prgROM / 8) + (prgBanks[3]));
                        else
                            nes.Memory.Swap8kROM(0xC000, prgBanks[3] % (nes.rom.prgROM / 8));
                        nes.Memory.Swap8kROM(0xE000, prgBanks[4] % (nes.rom.prgROM / 8));
                        break;
                }
            }
            else
            {
                nes.Memory.Swap8kROM(0x6000, (nes.rom.prgROM / 8) + prgBanks[0]);
                switch (prgMode)
                {
                    case 0:
                        swapBank = (prgBanks[4] % (nes.rom.prgROM / 8)) & 0xFC;
                        nes.Memory.Swap8kROM(0x8000, swapBank);
                        nes.Memory.Swap8kROM(0xA000, swapBank + 1);
                        nes.Memory.Swap8kROM(0xC000, swapBank + 2);
                        nes.Memory.Swap8kROM(0xE000, swapBank + 3);
                        break;
                    case 1:
                        swapBank = (prgBanks[2] % (nes.rom.prgROM / 8)) & 0xFE;
                        if (prgRAMBanks[2])
                        {
                            nes.Memory.Swap8kROM(0x8000, (nes.rom.prgROM / 8) + swapBank);
                            nes.Memory.Swap8kROM(0xA000, (nes.rom.prgROM / 8) + swapBank + 1);
                        }
                        else
                        {
                            nes.Memory.Swap8kROM(0x8000, swapBank);
                            nes.Memory.Swap8kROM(0xA000, swapBank + 1);
                        }
                        swapBank = (prgBanks[4] % (nes.rom.prgROM / 8)) & 0xFE;
                        nes.Memory.Swap8kROM(0xC000, swapBank);
                        nes.Memory.Swap8kROM(0xE000, swapBank + 1);
                        break;
                    case 2:
                        swapBank = (prgBanks[2] % (nes.rom.prgROM / 8)) & 0xFE;
                        if (prgRAMBanks[2])
                        {
                            nes.Memory.Swap8kROM(0x8000, (nes.rom.prgROM / 8) + swapBank);
                            nes.Memory.Swap8kROM(0xA000, (nes.rom.prgROM / 8) + swapBank + 1);
                        }
                        else
                        {
                            nes.Memory.Swap8kROM(0x8000, swapBank);
                            nes.Memory.Swap8kROM(0xA000, swapBank + 1);
                        }
                        if (prgRAMBanks[3])
                            nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) + (prgBanks[3]));
                        else
                            nes.Memory.Swap8kROM(0xC000, prgBanks[3] % (nes.rom.prgROM / 8));
                        nes.Memory.Swap8kROM(0xE000, prgBanks[4] % (nes.rom.prgROM / 8));
                        break;
                    case 3:
                        if (prgRAMBanks[1])
                            nes.Memory.Swap8kROM(0x8000, (nes.rom.prgROM / 8) + (prgBanks[1]));
                        else
                            nes.Memory.Swap8kROM(0x8000, prgBanks[1] % (nes.rom.prgROM / 8));
                        if (prgRAMBanks[2])
                            nes.Memory.Swap8kROM(0xA000, (nes.rom.prgROM / 8) + (prgBanks[2]));
                        else
                            nes.Memory.Swap8kROM(0xA000, prgBanks[2] % (nes.rom.prgROM / 8));
                        if (prgRAMBanks[3])
                            nes.Memory.Swap8kROM(0xC000, (nes.rom.prgROM / 8) + (prgBanks[3]));
                        else
                            nes.Memory.Swap8kROM(0xC000, prgBanks[3] % (nes.rom.prgROM / 8));
                        nes.Memory.Swap8kROM(0xE000, prgBanks[4] % (nes.rom.prgROM / 8));
                        break;
                }
            }
        }
    }
}
