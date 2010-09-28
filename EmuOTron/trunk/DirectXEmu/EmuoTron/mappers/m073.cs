using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m073 : Mapper
    {
        private int irqReload;
        private bool irqMode;
        private bool irqEnable;
        private bool irqAckEnable;
        private int irqCounter;

        public m073(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap16kROM(0x8000, 0);
            nes.Memory.Swap16kROM(0xC000, (nes.rom.prgROM / 16) - 1);
            nes.PPU.PPUMemory.Swap8kRAM(0x0000, 0);
        }
        public override void Write(byte value, ushort address)
        {

            byte highAddr = (byte)(address >> 12);
            if (highAddr == 0xF)
                nes.Memory.Swap16kROM(0x8000, (value & 0xF) % (nes.rom.prgROM / 16));
            else if (highAddr == 0x8)
                irqReload = (value & 0xF) | (irqReload & 0xFFF0);
            else if (highAddr == 0x9)
                irqReload = ((value & 0xF) << 4) | (irqReload & 0xFF0F);
            else if (highAddr == 0xA)
                irqReload = ((value & 0xF) << 8) | (irqReload & 0xF0FF);
            else if (highAddr == 0xB)
                irqReload = ((value & 0xF) << 12) | (irqReload & 0x0FFF);
            else if (highAddr == 0xC)
            {
                interruptMapper = false;
                irqAckEnable = (value & 1) != 0;
                irqEnable = (value & 2) != 0;
                irqMode = (value & 4) != 0;
                if (irqEnable)
                {
                    irqCounter = irqReload;
                }
            }
            else if (highAddr == 0xD)
            {
                interruptMapper = false;
                irqEnable = irqAckEnable;
            }

        }
        public override byte Read(byte value, ushort address) { return value; }
        public override void IRQ(int cycles, int vblank)
        {
            if (irqEnable)
            {
                if (irqMode)
                {
                    for (int i = 0; i < cycles; i++)
                    {
                        if ((irqCounter & 0xFF) == 0xFF)
                        {
                            interruptMapper = true;
                            irqCounter = (irqCounter & 0xFF00) | (irqReload & 0xFF);
                        }
                        else
                        {
                            irqCounter = (irqCounter & 0xFF00) | ((irqCounter + 1) & 0xFF);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < cycles; i++)
                    {
                        if ((irqCounter & 0xFFFF) == 0xFFFF)
                        {
                            interruptMapper = true;
                            irqCounter = irqReload;
                        }
                        else
                        {
                            irqCounter = (irqCounter + 1) & 0xFFFF;
                        }
                    }
                }
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(irqReload);
            writer.Write(irqMode);
            writer.Write(irqEnable);
            writer.Write(irqAckEnable);
            writer.Write(irqCounter);
        }
        public override void StateLoad(BinaryReader reader)
        {
            irqReload = reader.ReadInt32();
            irqMode = reader.ReadBoolean();
            irqEnable = reader.ReadBoolean();
            irqAckEnable = reader.ReadBoolean();
            irqCounter = reader.ReadInt32();
        }
    }
}
