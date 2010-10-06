using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron
{
    public class MemoryStore
    {
        public byte[][] banks;
        public int[] memMap = new int[0x40];
        bool[] readOnly = new bool[0x40];
        public bool[] saveBanks;
        public int swapOffset;
        public bool hardwired = false;
        public MemoryStore(int banks, bool readOnly)
        {
            this.banks = new byte[banks][];
            saveBanks = new bool[banks];
            for (int i = 0; i < banks; i++)
            {
                this.banks[i] = new byte[0x400];
                saveBanks[i] = false;
            }
            for (int i = 0; i < 0x40; i++)
            {
                this.readOnly[i] = readOnly;
                memMap[i] = i;
            }
        }
        public byte this[int address]
        {
            get
            {
                return banks[memMap[address >> 0xA]][address & 0x3FF];
            }
            set
            {
                if(!readOnly[address >> 0xA])
                {
                    saveBanks[memMap[address >> 0xA]] = true;
                    banks[memMap[address >> 0xA]][address & 0x3FF] = value;
                }
            }
        }
        public void ForceValue(int address, byte value)
        {
            banks[memMap[address >> 0xA]][address & 0x3FF] = value;
        }
        public void SetReadOnly(ushort address, int kb, bool readOnly)
        {
            for (int i = 0; i < kb; i++)
                this.readOnly[(address >> 0xA) + i] = readOnly;
        }
        public void Swap1kROM(ushort address, int bank)
        {
            bank += swapOffset;
            memMap[address >> 0xA] = bank;
            readOnly[address >> 0xA] = true;
        }
        public void Swap2kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap1kROM(address, bank);
            Swap1kROM((ushort)(address + 0x400), bank + 1);
        }
        public void Swap4kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap2kROM(address, bank);
            Swap2kROM((ushort)(address + 0x800), bank + 1);
        }
        public void Swap8kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap4kROM(address, bank);
            Swap4kROM((ushort)(address + 0x1000), bank + 1);
        }
        public void Swap16kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap8kROM(address, bank);
            Swap8kROM((ushort)(address + 0x2000), bank + 1);
        }
        public void Swap32kROM(ushort address, int bank)
        {
            bank *= 2;
            Swap16kROM(address, bank);
            Swap16kROM((ushort)(address + 0x4000), bank + 1);
        }
        public void Swap1kRAM(ushort address, int bank)
        {
            bank += swapOffset;
            memMap[address >> 0xA] = bank;
            readOnly[address >> 0xA] = false;
        }
        public void Swap2kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap1kRAM(address, bank);
            Swap1kRAM((ushort)(address + 0x400), bank + 1);
        }
        public void Swap4kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap2kRAM(address, bank);
            Swap2kRAM((ushort)(address + 0x800), bank + 1);
        }
        public void Swap8kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap4kRAM(address, bank);
            Swap4kRAM((ushort)(address + 0x1000), bank + 1);
        }
        public void Swap16kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap8kRAM(address, bank);
            Swap8kRAM((ushort)(address + 0x2000), bank + 1);
        }
        public void Swap32kRAM(ushort address, int bank)
        {
            bank *= 2;
            Swap16kRAM(address, bank);
            Swap16kRAM((ushort)(address + 0x4000), bank + 1);
        }
        public void HorizontalMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x8;
            memMap[0x9] = 0x8;
            memMap[0xA] = 0x9;
            memMap[0xB] = 0x9;
        }
        public void VerticalMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x8;
            memMap[0x9] = 0x9;
            memMap[0xA] = 0x8;
            memMap[0xB] = 0x9;
        }
        public void FourScreenMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x8;
            memMap[0x9] = 0x9;
            memMap[0xA] = 0xA;
            memMap[0xB] = 0xB;
        }
        public void ScreenOneMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x8;
            memMap[0x9] = 0x8;
            memMap[0xA] = 0x8;
            memMap[0xB] = 0x8;
        }
        public void ScreenTwoMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x9;
            memMap[0x9] = 0x9;
            memMap[0xA] = 0x9;
            memMap[0xB] = 0x9;
        }
        public void ScreenThreeMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0xA;
            memMap[0x9] = 0xA;
            memMap[0xA] = 0xA;
            memMap[0xB] = 0xA;
        }
        public void ScreenFourMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0xB;
            memMap[0x9] = 0xB;
            memMap[0xA] = 0xB;
            memMap[0xB] = 0xB;
        }
        public void CustomMirroring(int nameTable, int bank) // 0 1 2 3
        {
            memMap[nameTable + 0x8] = bank + 0x8;
        }
        public void StateSave(BinaryWriter writer)
        {
            writer.Write(memMap.Length);
            for (int i = 0; i < memMap.Length; i++)
                writer.Write(memMap[i]);
            int changedBanks = 0;
            for (int i = 0; i < saveBanks.Length; i++)
                if (saveBanks[i])
                    changedBanks++;
            writer.Write(changedBanks);
            for (int i = 0; i < saveBanks.Length; i++)
            {
                if (saveBanks[i])
                {
                    writer.Write("BANK");
                    writer.Write(i);
                    writer.Write(readOnly[i]);
                    for (int j = 0; j < 0x400; j++)
                    {
                        writer.Write(banks[i][j]);
                    }
                }
            }
        }

        public void StateLoad(BinaryReader reader)
        {
            int memLength = reader.ReadInt32();
            for (int i = 0; i < memLength; i++)
                memMap[i] = reader.ReadInt32();

            for (int i = 0; i < saveBanks.Length; i++)
                saveBanks[i] = false;
            int saveLength = reader.ReadInt32();
            for (int i = 0; i < saveLength; i++)
            {
                string bbb = reader.ReadString();
                int bankNumber = reader.ReadInt32();
                saveBanks[bankNumber] = true;
                readOnly[bankNumber] = reader.ReadBoolean();
                for (int j = 0; j < 0x400; j++)
                {
                    banks[bankNumber][j] = reader.ReadByte();
                }
            }
        }
    }
}
