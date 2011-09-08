using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class N163 : Channel
    {
        byte[] soundRam = new byte[128];
        byte soundRamAddr;
        bool autoIncrement;

        byte[] chanStartAddr = new byte[8];
        byte[] chanAddr = new byte[8];
        int[] chanFreq = new int[8];
        int[] chanClockTime = new int[8];
        int[] chanCounter = new int[8];
        int[] chanLength = new int[8];
        int[] chanLengthCounter = new int[8];
        byte[] chanVolume = new byte[8];
        byte enabledCount;

        private void UpdateFreq(int channel)
        {
            if (chanFreq[channel] == 0)
                chanClockTime[channel] = -1; //Never clocked
            else
                chanClockTime[channel] = (int)((0xF0000 * (enabledCount + 1)) / (chanFreq[channel] * 1.0));
        }
        public override void Write(byte value, ushort address)
        {
            switch (address)
            {
                case 0x4800:
                    soundRam[soundRamAddr] = value;
                    if (soundRamAddr >= 0x40)
                    {
                        int channel = (soundRamAddr >> 3) & 7;
                        switch (soundRamAddr & 7)
                        {
                            case 0:
                                chanFreq[channel] = (chanFreq[channel] & 0x3FF00) | value;
                                UpdateFreq(channel);
                                break;
                            case 2:
                                chanFreq[channel] = (chanFreq[channel] & 0x300FF) | (value << 8);
                                UpdateFreq(channel);
                                break;
                            case 4:
                                chanFreq[channel] = (chanFreq[channel] & 0x0FFFF) | ((value & 3) << 16);
                                chanLength[channel] = (4 * (8 - ((value >> 2) & 7)));//4-bit samples
                                UpdateFreq(channel);
                                break;
                            case 6:
                                chanStartAddr[channel] = value; //4-bit samples
                                chanAddr[channel] = chanStartAddr[channel];
                                chanLengthCounter[channel] = 0;
                                break;
                            case 7:
                                chanVolume[channel] = (byte)(value & 0xF);
                                if (channel == 7)
                                {
                                    enabledCount = (byte)((value >> 4) & 7);
                                    for (int i = 0; i < 8; i++)
                                        UpdateFreq(i);
                                }
                                break;
                        }
                    }
                    if (autoIncrement)
                    {
                        soundRamAddr++;
                        soundRamAddr &= 0x7F;
                    }
                    break;
                case 0xF800:
                    soundRamAddr = (byte)(value & 0x7F);
                    autoIncrement = (value & 0x80) != 0;
                    break;
            }
        }
        public override byte Read(byte value, ushort address)
        {
            if (address == 0x4800)
                value = soundRam[soundRamAddr];
            return value;
        }
        public override byte Cycle()
        {
            byte volume = 0;
            for (int i = 7; i >= 0 && i >= 7 - enabledCount; i--)
            {
                chanCounter[i]++;
                if (chanCounter[i] >= chanClockTime[i] && chanClockTime[i] >= 0)
                {
                    chanCounter[i] = 0;
                    chanLengthCounter[i]++;
                    chanAddr[i]++;
                    if (chanLengthCounter[i] >= chanLength[i])
                    {
                        chanAddr[i] = chanStartAddr[i];
                        chanLengthCounter[i] = 0;
                    }
                }
                if (chanVolume[i] != 0)
                {
                    if ((chanAddr[i] & 1) == 0)
                        volume += (byte)((soundRam[chanAddr[i] >> 1] & 0x0F) * (chanVolume[i] / 7.0)); //Really don't like this volume clac, but will have to do.
                    else
                        volume += (byte)(((soundRam[chanAddr[i] >> 1] >> 4) & 0x0F) * (chanVolume[i] / 7.0));
                }
            }
            return volume;
        }
        public override void StateSave(System.IO.BinaryWriter writer)
        {
            writer.Write(soundRam);
            writer.Write(soundRamAddr);
            writer.Write(autoIncrement);
            for (int i = 0; i < 8; i++)
            {
                writer.Write(chanStartAddr[i]);
                writer.Write(chanAddr[i]);
                writer.Write(chanFreq[i]);
                writer.Write(chanClockTime[i]);
                writer.Write(chanCounter[i]);
                writer.Write(chanLength[i]);
                writer.Write(chanLengthCounter[i]);
                writer.Write(chanVolume[i]);
            }
            writer.Write(enabledCount);
        }
        public override void StateLoad(System.IO.BinaryReader reader)
        {
            soundRam = reader.ReadBytes(128);
            soundRamAddr = reader.ReadByte();
            autoIncrement = reader.ReadBoolean();
            for (int i = 0; i < 8; i++)
            {
                chanStartAddr[i] = reader.ReadByte();
                chanAddr[i] = reader.ReadByte();
                chanFreq[i] = reader.ReadInt32();
                chanClockTime[i] = reader.ReadInt32();
                chanCounter[i] = reader.ReadInt32();
                chanLength[i] = reader.ReadInt32();
                chanLengthCounter[i] = reader.ReadInt32();
                chanVolume[i] = reader.ReadByte();
            }
            enabledCount = reader.ReadByte();

        }

    }
}
