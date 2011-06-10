
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m185 : Mapper
    {
        private byte[] disabledChrBank = new byte[0x400];
        private byte[][] chrBanks = new byte[8][];
        private bool chrEnabled = true;
        public m185(NESCore nes)
        {
            this.nes = nes;
            for (int i = 0; i < 0x400; i++)
            {
                disabledChrBank[i] = 0x12;
            }
            for (int i = 0; i < 8; i++)
            {
                chrBanks[i] = nes.PPU.PPUMemory.banks[nes.PPU.PPUMemory.swapOffset + i];
            }
        }
        public override void Power()
        {
            for (int i = 0; i < 8; i++)
                nes.PPU.PPUMemory.banks[nes.PPU.PPUMemory.swapOffset + i] = chrBanks[i];
            nes.Memory.Swap32kROM(0x8000, 0);
            nes.PPU.PPUMemory.Swap8kROM(0x0000, 0);
        }

        public override void Write(byte value, ushort address)
        {
            if (address >= 0x8000)
            {
                if (chrEnabled = ((value & 0x0F) != 0 && value != 0x13))
                {//chr enabled
                    for (int i = 0; i < 8; i++)
                        nes.PPU.PPUMemory.banks[nes.PPU.PPUMemory.swapOffset + i] = chrBanks[i];
                }
                else
                {//chr disabled
                    for (int i = 0; i < 8; i++)
                        nes.PPU.PPUMemory.banks[nes.PPU.PPUMemory.swapOffset + i] = disabledChrBank;
                }
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(chrEnabled);
        }
        public override void StateLoad(BinaryReader reader)
        {
            chrEnabled = reader.ReadBoolean();
            if (chrEnabled)
            {
                for (int i = 0; i < 8; i++)
                    nes.PPU.PPUMemory.banks[nes.PPU.PPUMemory.swapOffset + i] = chrBanks[i];
            }
            else
            {
                for (int i = 0; i < 8; i++)
                    nes.PPU.PPUMemory.banks[nes.PPU.PPUMemory.swapOffset + i] = disabledChrBank;
            }
        }
    }
}
