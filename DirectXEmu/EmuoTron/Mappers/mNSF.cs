using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class mNSF : Mapper
    {
        public int totalSongs;
        public int startSong;
        public int currentSong;
        public int initAddress;
        public int playAddress;
        public int loadAddress;

        public string songName;
        public string artist;
        public string copyright;

        private byte[] initialBanks;
        private byte[] banks = new byte[8];
        private bool bankSwitching;
        public double speed;
        public double counter;
        public bool overTime;
        public mNSF(NESCore nes, byte[] banks, int PBRATE)
        {
            this.nes = nes;
            this.cycleIRQ = true;
            this.speed = (nes.APU.CPUClock * 1.0) / (1000000.0 / (PBRATE * 1.0));
            for (int i = 0; i < 8; i++)
                if (banks[i] != 0)
                    bankSwitching = true;
            initialBanks = banks;
        }
        public override void Power()
        {
            if (bankSwitching)
            {
                for (int i = 0; i < 8; i++)
                    banks[i] = initialBanks[i];
                SyncPrg();
            }
            else
            {
                nes.Memory.Swap32kROM(0x8000, 0);
            }
            counter = speed;
        }
        public override void Write(byte value, ushort address)
        {
            switch (address)
            {
                case 0x5FF8:
                    banks[0] = value;
                    SyncPrg();
                    break;
                case 0x5FF9:
                    banks[1] = value;
                    SyncPrg();
                    break;
                case 0x5FFA:
                    banks[2] = value;
                    SyncPrg();
                    break;
                case 0x5FFB:
                    banks[3] = value;
                    SyncPrg();
                    break;
                case 0x5FFC:
                    banks[4] = value;
                    SyncPrg();
                    break;
                case 0x5FFD:
                    banks[5] = value;
                    SyncPrg();
                    break;
                case 0x5FFE:
                    banks[6] = value;
                    SyncPrg();
                    break;
                case 0x5FFF:
                    banks[7] = value;
                    SyncPrg();
                    break;
            }
        }
        private void SyncPrg()
        {
            if (bankSwitching)
            {
                nes.Memory.Swap4kROM(0x8000, banks[0] % 8);
                nes.Memory.Swap4kROM(0x9000, banks[1] % 8);
                nes.Memory.Swap4kROM(0xA000, banks[2] % 8);
                nes.Memory.Swap4kROM(0xB000, banks[3] % 8);
                nes.Memory.Swap4kROM(0xC000, banks[4] % 8);
                nes.Memory.Swap4kROM(0xD000, banks[5] % 8);
                nes.Memory.Swap4kROM(0xE000, banks[6] % 8);
                nes.Memory.Swap4kROM(0xF000, banks[7] % 8);
            }
        }
        public override void IRQ(int cycles)
        {
            counter -= cycles;
            if (counter <= 0)
            {
                overTime = true;
                counter += speed;
            }
        }
        public int IRQ(int cycles, int opCode)
        {
            if (opCode == OpInfo.InstrRTS)
            {
                if (nes.RegS == 0xFD)
                {
                    if (!overTime)
                        cycles = (int)(Math.Round(counter) + 1);
                    else
                        counter = speed + cycles;
                    nes.PPU.frameComplete = true;
                    nes.PushWordStack(playAddress - 1);
                    overTime = false;
                }
            }
            counter -= cycles;
            if (counter <= 0)
            {
                overTime = true && !nes.PPU.frameComplete;
                counter += speed;
            }
            return cycles;
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(speed);
            writer.Write(counter);
            writer.Write(bankSwitching);
            for (int i = 0; i < 8; i++)
            {
                writer.Write(banks[i]);
            }
            for (int i = 0; i < 8; i++)
            {
                writer.Write(initialBanks[i]);
            }
        }
        public override void StateLoad(BinaryReader reader)
        {
            speed = reader.ReadDouble();
            counter = reader.ReadDouble();
            bankSwitching = reader.ReadBoolean();
            for (int i = 0; i < 8; i++)
            {
                banks[i] = reader.ReadByte();
            }
            for (int i = 0; i < 8; i++)
            {
                initialBanks[i] = reader.ReadByte();
            }
        }
    }
}
