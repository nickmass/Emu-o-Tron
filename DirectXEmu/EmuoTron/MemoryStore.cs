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
        public int ramSwapOffset;
        public int hardwiredBanks;
        public int swapROM;
        public int swapRAM;
        public bool hardwired = false;
        private int openBusBank;
        public MemoryStore(int hardwiredBanks, int swapROM, int swapRAM, bool readOnly)
        {
            int totalBank = hardwiredBanks + 1 + swapROM + swapRAM;
            this.swapRAM = swapRAM;
            this.swapROM = swapROM;
            this.hardwiredBanks = hardwiredBanks;
            this.swapOffset = hardwiredBanks + 1;
            this.ramSwapOffset = swapOffset + swapROM;
            openBusBank = hardwiredBanks;
            this.banks = new byte[totalBank][];
            saveBanks = new bool[totalBank];
            for (int i = 0; i < totalBank; i++)
            {
                this.banks[i] = new byte[0x400];
                saveBanks[i] = false;
            }
            for (int i = 0; i < 0x40; i++)
            {
                this.readOnly[i] = readOnly;
                memMap[i] = openBusBank;
            }
            for (int i = 0; i < 0x400; i++)
            {
                banks[openBusBank][i] = 0xFF;
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
            bank = bank % swapROM;
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
        public void Swap1kRAM(ushort address, int bank, bool writeProtect)
        {
            bank = bank % swapRAM;
            bank += ramSwapOffset;
            memMap[address >> 0xA] = bank;
            readOnly[address >> 0xA] = writeProtect;
        }
        public void Swap2kRAM(ushort address, int bank, bool writeProtect)
        {
            bank *= 2;
            Swap1kRAM(address, bank, writeProtect);
            Swap1kRAM((ushort)(address + 0x400), bank + 1, writeProtect);
        }
        public void Swap4kRAM(ushort address, int bank, bool writeProtect)
        {
            bank *= 2;
            Swap2kRAM(address, bank, writeProtect);
            Swap2kRAM((ushort)(address + 0x800), bank + 1, writeProtect);
        }
        public void Swap8kRAM(ushort address, int bank, bool writeProtect)
        {
            bank *= 2;
            Swap4kRAM(address, bank, writeProtect);
            Swap4kRAM((ushort)(address + 0x1000), bank + 1, writeProtect);
        }
        public void Swap16kRAM(ushort address, int bank, bool writeProtect)
        {
            bank *= 2;
            Swap8kRAM(address, bank, writeProtect);
            Swap8kRAM((ushort)(address + 0x2000), bank + 1, writeProtect);
        }
        public void Swap32kRAM(ushort address, int bank, bool writeProtect)
        {
            bank *= 2;
            Swap16kRAM(address, bank, writeProtect);
            Swap16kRAM((ushort)(address + 0x4000), bank + 1, writeProtect);
        }
        public void HorizontalMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x0;
            memMap[0x9] = 0x0;
            memMap[0xA] = 0x1;
            memMap[0xB] = 0x1;
            memMap[0xC] = memMap[0x8];
            memMap[0xD] = memMap[0x9];
            memMap[0xE] = memMap[0xA];
            memMap[0xF] = memMap[0xB];
            readOnly[0x8] = false;
            readOnly[0x9] = false;
            readOnly[0xA] = false;
            readOnly[0xB] = false;
            readOnly[0xC] = readOnly[0x8];
            readOnly[0xD] = readOnly[0x9];
            readOnly[0xE] = readOnly[0xA];
            readOnly[0xF] = readOnly[0xB];
        }
        public void VerticalMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x0;
            memMap[0x9] = 0x1;
            memMap[0xA] = 0x0;
            memMap[0xB] = 0x1;
            memMap[0xC] = memMap[0x8];
            memMap[0xD] = memMap[0x9];
            memMap[0xE] = memMap[0xA];
            memMap[0xF] = memMap[0xB];
            readOnly[0x8] = false;
            readOnly[0x9] = false;
            readOnly[0xA] = false;
            readOnly[0xB] = false;
            readOnly[0xC] = readOnly[0x8];
            readOnly[0xD] = readOnly[0x9];
            readOnly[0xE] = readOnly[0xA];
            readOnly[0xF] = readOnly[0xB];
        }
        public void FourScreenMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x0;
            memMap[0x9] = 0x1;
            memMap[0xA] = 0x2;
            memMap[0xB] = 0x3;
            memMap[0xC] = memMap[0x8];
            memMap[0xD] = memMap[0x9];
            memMap[0xE] = memMap[0xA];
            memMap[0xF] = memMap[0xB];
            readOnly[0x8] = false;
            readOnly[0x9] = false;
            readOnly[0xA] = false;
            readOnly[0xB] = false;
            readOnly[0xC] = readOnly[0x8];
            readOnly[0xD] = readOnly[0x9];
            readOnly[0xE] = readOnly[0xA];
            readOnly[0xF] = readOnly[0xB];
        }
        public void ScreenOneMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x0;
            memMap[0x9] = 0x0;
            memMap[0xA] = 0x0;
            memMap[0xB] = 0x0;
            memMap[0xC] = memMap[0x8];
            memMap[0xD] = memMap[0x9];
            memMap[0xE] = memMap[0xA];
            memMap[0xF] = memMap[0xB];
            readOnly[0x8] = false;
            readOnly[0x9] = false;
            readOnly[0xA] = false;
            readOnly[0xB] = false;
            readOnly[0xC] = readOnly[0x8];
            readOnly[0xD] = readOnly[0x9];
            readOnly[0xE] = readOnly[0xA];
            readOnly[0xF] = readOnly[0xB];
        }
        public void ScreenTwoMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x1;
            memMap[0x9] = 0x1;
            memMap[0xA] = 0x1;
            memMap[0xB] = 0x1;
            memMap[0xC] = memMap[0x8];
            memMap[0xD] = memMap[0x9];
            memMap[0xE] = memMap[0xA];
            memMap[0xF] = memMap[0xB];
            readOnly[0x8] = false;
            readOnly[0x9] = false;
            readOnly[0xA] = false;
            readOnly[0xB] = false;
            readOnly[0xC] = readOnly[0x8];
            readOnly[0xD] = readOnly[0x9];
            readOnly[0xE] = readOnly[0xA];
            readOnly[0xF] = readOnly[0xB];
        }
        public void ScreenThreeMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x2;
            memMap[0x9] = 0x2;
            memMap[0xA] = 0x2;
            memMap[0xB] = 0x2;
            memMap[0xC] = memMap[0x8];
            memMap[0xD] = memMap[0x9];
            memMap[0xE] = memMap[0xA];
            memMap[0xF] = memMap[0xB];
            readOnly[0x8] = false;
            readOnly[0x9] = false;
            readOnly[0xA] = false;
            readOnly[0xB] = false;
            readOnly[0xC] = readOnly[0x8];
            readOnly[0xD] = readOnly[0x9];
            readOnly[0xE] = readOnly[0xA];
            readOnly[0xF] = readOnly[0xB];
        }
        public void ScreenFourMirroring()
        {
            if (hardwired)
                return;
            memMap[0x8] = 0x3;
            memMap[0x9] = 0x3;
            memMap[0xA] = 0x3;
            memMap[0xB] = 0x3;
            memMap[0xC] = memMap[0x8];
            memMap[0xD] = memMap[0x9];
            memMap[0xE] = memMap[0xA];
            memMap[0xF] = memMap[0xB];
            readOnly[0x8] = false;
            readOnly[0x9] = false;
            readOnly[0xA] = false;
            readOnly[0xB] = false;
            readOnly[0xC] = readOnly[0x8];
            readOnly[0xD] = readOnly[0x9];
            readOnly[0xE] = readOnly[0xA];
            readOnly[0xF] = readOnly[0xB];
        }
        public void CustomMirroring(int nameTable, int bank) // 0 1 2 3
        {
            nameTable &= 3;
            memMap[nameTable + 0x8] = (bank & 3);
            readOnly[nameTable + 0x8] = false;
            memMap[nameTable + 0xC] = memMap[nameTable + 0x8];
            readOnly[nameTable + 0x8] = readOnly[nameTable + 0x8];
        }
        public void ExternalROMMirroring(int nameTable, int bank)
        {
            nameTable &= 3;
            memMap[nameTable + 0x8] = swapOffset + (bank % swapROM);
            memMap[nameTable + 0xC] = memMap[nameTable + 0x8];
            readOnly[nameTable + 0x8] = true;
            readOnly[nameTable + 0xC] = readOnly[nameTable + 0x8];
        }
        public void ExternalRAMMirroring(int nameTable, int bank, bool writeProtect)
        {
            nameTable &= 3;
            memMap[nameTable + 0x8] = ramSwapOffset + (bank % swapRAM);
            memMap[nameTable + 0xC] = memMap[nameTable + 0x8];
            readOnly[nameTable + 0x8] = writeProtect;
            readOnly[nameTable + 0xC] = readOnly[nameTable + 0x8];
        }
        public void StateSave(BinaryWriter writer)
        {
            writer.Write(memMap.Length);
            for (int i = 0; i < memMap.Length; i++)
                writer.Write(memMap[i]);
            for (int i = 0; i < readOnly.Length; i++)
                writer.Write(readOnly[i]);
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
            for (int i = 0; i < readOnly.Length; i++)
                readOnly[i] = reader.ReadBoolean();
            for (int i = 0; i < saveBanks.Length; i++)
                saveBanks[i] = false;
            int saveLength = reader.ReadInt32();
            for (int i = 0; i < saveLength; i++)
            {
                string bbb = reader.ReadString();
                int bankNumber = reader.ReadInt32();
                saveBanks[bankNumber] = true;
                for (int j = 0; j < 0x400; j++)
                {
                    banks[bankNumber][j] = reader.ReadByte();
                }
            }
        }
    }
}
