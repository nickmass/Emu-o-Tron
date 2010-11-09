
// Extremely basic, pretty much only supports prg switching and poorly at that,
// my current model really can't hand the way MMC5 does RAM and how tied into
// the PPU it is, FillMode, Horz Split, Seperate sprite and background tables,
// EXRAM all seem impossible. The sound channels are even more impossible but
// At least they arent really required. Also the multiplication reg will be an
// issues as currently mappers don't have or have needed a read handler.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m005 : Mapper
    {
        private int prgMode;
        private bool ramProtect1;
        private bool ramProtect2;
        private byte fillModeTile;
        private byte fillModePalatte;
        bool ramReadOnly;
        public m005(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {
            switch (address)
            {
                case 0x5100:
                    prgMode = value & 3;
                    break;
                case 0x5102:
                    if ((value & 3) == 2)
                        ramProtect1 = true;
                    ramReadOnly = ramProtect1 && ramProtect2;
                    nes.Memory.SetReadOnly(0x6000, 32, ramReadOnly);
                    break;
                case 0x5103:
                    if ((value & 3) == 1)
                        ramProtect2 = true;
                    ramReadOnly = ramProtect1 && ramProtect2;
                    nes.Memory.SetReadOnly(0x6000, 32, ramReadOnly);
                    break;
                case 0x5113:
                    // Technically, $5113 should look something like:
                    // [.... .CPP]
                    //  C = Chip select
                    //  P = 8k PRG-RAM page on selected chip
                    //Memory.Swap8kRAM(0x6000, value % (numPRGRom * 2));
                    break;
                case 0x5114:
                    if (prgMode == 3)
                    {
                        if ((value & 0x80) == 0)
                            nes.Memory.Swap8kRAM(0x8000, (value & 0x7f) % (nes.rom.prgROM / 8));
                        else
                            nes.Memory.Swap8kROM(0x8000, (value & 0x7f) % (nes.rom.prgROM / 8));
                    }
                    break;
                case 0x5115:
                    if (prgMode == 3)
                    {
                        if ((value & 0x80) == 0)
                            nes.Memory.Swap8kRAM(0xA000, (value & 0x7f) % (nes.rom.prgROM / 8));
                        else
                            nes.Memory.Swap8kROM(0xA000, (value & 0x7f) % (nes.rom.prgROM / 8));
                    }
                    else if (prgMode == 2 || prgMode == 1)
                    {
                        int swapBank = ((value & 0x7f) % (nes.rom.prgROM / 8)) & 0xFE;
                        if ((value & 0x80) == 0)
                        {
                            nes.Memory.Swap8kRAM(0x8000, swapBank);
                            nes.Memory.Swap8kRAM(0xA000, swapBank + 1);
                        }
                        else
                        {
                            nes.Memory.Swap8kROM(0x8000, swapBank);
                            nes.Memory.Swap8kROM(0xA000, swapBank + 1);
                        }
                    }
                    break;
                case 0x5116:
                    if (prgMode == 3 || prgMode == 2)
                    {
                        if ((value & 0x80) == 0)
                            nes.Memory.Swap8kRAM(0xC000, (value & 0x7f) % (nes.rom.prgROM / 8));
                        else
                            nes.Memory.Swap8kROM(0xC000, (value & 0x7f) % (nes.rom.prgROM / 8));
                    }
                    break;
                case 0x5117:
                    if (prgMode == 3 || prgMode == 2)
                    {
                        if ((value & 0x80) == 0)
                            nes.Memory.Swap8kRAM(0xE000, (value & 0x7f) % (nes.rom.prgROM / 8));
                        else
                            nes.Memory.Swap8kROM(0xE000, (value & 0x7f) % (nes.rom.prgROM / 8));
                    }
                    else if (prgMode == 1)
                    {
                        int swapBank = ((value & 0x7f) % (nes.rom.prgROM / 8)) & 0xFE;
                        if ((value & 0x80) == 0)
                        {
                            nes.Memory.Swap8kRAM(0xC000, swapBank);
                            nes.Memory.Swap8kRAM(0xE000, swapBank + 1);
                        }
                        else
                        {
                            nes.Memory.Swap8kROM(0xC000, swapBank);
                            nes.Memory.Swap8kROM(0xE000, swapBank + 1);
                        }

                    }
                    else if (prgMode == 0)
                    {
                        int swapBank = ((value & 0x7f) % (nes.rom.prgROM / 8)) & 0xFC;
                        nes.Memory.Swap8kROM(0x8000, swapBank);
                        nes.Memory.Swap8kROM(0xA000, swapBank + 1);
                        nes.Memory.Swap8kROM(0xC000, swapBank + 2);
                        nes.Memory.Swap8kROM(0xE000, swapBank + 3);
                    }
                    break;
                case 0x5105:
                    nes.PPU.PPUMemory.CustomMirroring(0, value & 3);
                    nes.PPU.PPUMemory.CustomMirroring(1, (value >> 2) & 3);
                    nes.PPU.PPUMemory.CustomMirroring(2, (value >> 4) & 3);
                    nes.PPU.PPUMemory.CustomMirroring(3, (value >> 6) & 3); //Doesnt have fill mode
                    break;
                case 0x5106:
                    fillModeTile = value;
                    break;
                case 0x5107:
                    fillModePalatte = (byte)(value & 3);
                    break;
            }
        }
    }
}
