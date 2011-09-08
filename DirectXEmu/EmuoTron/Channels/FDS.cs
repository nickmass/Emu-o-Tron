using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class FDS : Channel
    {
        int[] modAdjustTable = new int[] { 0, 1, 2, 4, 0, -4, -2, -1 };
        byte[] waveform = new byte[64];
        byte[] modBuffer = new byte[64];
        bool writeMode;
        bool volumeEnvelopeEnabled;
        bool volumeEnvelopeMode;
        int volume;
        int volumeEnvelopeGain;
        int volumeEnvelopeSpeed;
        int mainFreq;
        bool envelopeEnabled;
        bool mainUnitEnabled;
        bool sweepEnvelopeEnabled;
        bool sweepEnvelopeMode;
        int sweepGain;
        int sweepSpeed;
        int sweepBias;
        int modUnitFreq;
        bool modUnitEnabled;
        int modUnitAddress;
        byte envelopeSpeed;
        int mod;
        int mainAddr;

        int volumeEnvelopeCounter;
        int sweepEnvelopeCounter;
        int modUnitCounter;
        int mainCounter;

        byte masterVolume; //x/30% volume

        public override void Power()
        {
            envelopeSpeed = 0xFF; //NSFs need this apparently.
        }

        public override void Write(byte value, ushort address)
        {
            if ((address & 0xFFC0) == 0x4040)
            {
                if (writeMode)
                {
                    waveform[address & 0x3F] = (byte)(value & 0x3F);
                }
            }
            else if (address >= 0x4080 && address <= 0x408A)
            {
                switch (address)
                {
                    case 0x4080:
                        volumeEnvelopeEnabled = (value & 0x80) == 0;
                        volumeEnvelopeMode = (value & 0x40) != 0;
                        if (volumeEnvelopeEnabled)
                        {
                            volumeEnvelopeSpeed = (value & 0x3F);
                        }
                        else
                        {
                            volume = Math.Min((value & 0x3F), 20);
                            volumeEnvelopeGain = (value & 0x3F);
                        }
                        break;
                    case 0x4082:
                        mainFreq = (mainFreq & 0xF00) | value;
                        break;
                    case 0x4083:
                        mainFreq = (mainFreq & 0x0FF) | ((value & 0xF) << 8);
                        mainUnitEnabled = (value & 0x80) == 0;
                        envelopeEnabled = (value & 0x40) == 0;
                        break;
                    case 0x4084:
                        sweepEnvelopeEnabled = (value & 0x80) == 0;
                        sweepEnvelopeMode = (value & 0x40) != 0;
                        if (sweepEnvelopeEnabled)
                        {
                            sweepSpeed = value & 0x3F;
                        }
                        else
                        {
                            sweepGain = value & 0x3F;
                        }
                        break;
                    case 0x4085:
                        sweepBias = value & 0x7F;
                        if ((sweepBias & 0x40) != 0)
                            sweepBias = (((~sweepBias) + 1) & 0x3F) * -1;
                        else
                            sweepBias = sweepBias & 0x3F;
                        modUnitAddress = 0;
                        break;
                    case 0x4086:
                        modUnitFreq = (modUnitFreq & 0xF00) | value;
                        break;
                    case 0x4087:
                        modUnitEnabled = (value & 0x80) == 0;
                        modUnitFreq = (modUnitFreq & 0x0FF) | ((value & 0xF) << 8);
                        break;
                    case 0x4088:
                        for (int i = 0; i < 31; i++)//Could use some tricks to optimize this loop out but I feel clarity should be the goal here.
                        {
                            modBuffer[i << 1] = modBuffer[(i + 1) << 1];
                            modBuffer[(i << 1) | 1] = modBuffer[((i + 1) << 1) | 1];
                        }
                        modBuffer[31 << 1] = (byte)(value & 0x7);
                        modBuffer[(31 << 1) | 1] = (byte)(value & 0x7);
                        break;
                    case 0x4089:
                        writeMode = (value & 0x80) != 0;
                        switch (value & 3)
                        {
                            case 0:
                                masterVolume = 30;
                                break;
                            case 1:
                                masterVolume = 20;
                                break;
                            case 2:
                                masterVolume = 15;
                                break;
                            case 3:
                                masterVolume = 12;
                                break;
                        }
                        break;
                    case 0x408A:
                        envelopeSpeed = value;
                        break;
                }
            }
        }

        public override byte Read(byte value, ushort address)
        {
            if ((address & 0xFFC0) == 0x4040)
            {
                if (writeMode)
                {
                    value = (byte)(waveform[address & 0x3F] | 0x40);
                }
            }
            else if (address == 0x4090)
            {
                value = (byte)(volumeEnvelopeGain | 0x40);
            }
            else if (address == 0x4092)
            {
                value = (byte)(sweepGain | 0x40);
            }
            return value;
        }

        public override byte Cycle()
        {
            byte outVolume = 0;
            if (volumeEnvelopeEnabled && envelopeSpeed != 0 && envelopeEnabled)
            {
                volumeEnvelopeCounter++;
                if (volumeEnvelopeCounter >= 8 * envelopeSpeed * (volumeEnvelopeSpeed + 1))
                {
                    volumeEnvelopeCounter = 0;
                    if (volumeEnvelopeMode && volumeEnvelopeGain < 0x20)
                    {
                        volumeEnvelopeGain++;
                    }
                    else if (!volumeEnvelopeMode && volumeEnvelopeGain > 0x0)
                    {
                        volumeEnvelopeGain--;
                    }
                    volume = Math.Min(volumeEnvelopeGain, 0x20);
                }
            }
            if (sweepEnvelopeEnabled && envelopeSpeed != 0 && envelopeEnabled)
            {
                sweepEnvelopeCounter++;
                if (sweepEnvelopeCounter >= 8 * envelopeSpeed * (sweepSpeed + 1))
                {
                    sweepEnvelopeCounter = 0;
                    if (sweepEnvelopeMode && sweepGain < 0x20)
                    {
                        sweepGain++;
                    }
                    else if (!volumeEnvelopeMode && sweepGain > 0x0)
                    {
                        sweepGain--;
                    }
                }
            }
            if (modUnitEnabled && modUnitFreq != 0)
            {
                modUnitCounter++;
                if (modUnitCounter >= 65536 / modUnitFreq)
                {
                    modUnitCounter = 0;
                    byte nextMod = modBuffer[modUnitAddress];
                    if (nextMod == 4)
                        sweepBias = 0;
                    else
                        sweepBias += modAdjustTable[nextMod];
                    if (sweepBias > 63)
                    {
                        sweepBias = (sweepBias - 63) - 64;
                    }
                    else if (sweepBias < -64)
                    {
                        sweepBias = 63 + (sweepBias + 64);
                    }
                    modUnitAddress++;
                    modUnitAddress &= 0x3F;
                    int temp = sweepBias * sweepGain;
                    if ((temp & 0x0F) != 0)
                    {
                        temp /= 16;
                        if (sweepBias < 0)
                            temp -= 1;
                        else
                            temp += 2;
                    }
                    else
                    {
                        temp /= 16;
                    }
                    if (temp > 193)
                        temp -= 258;
                    if (temp < -64)
                        temp += 256;
                    mod = mainFreq * temp / 64;
                }
            }
            else
            {
                mod = 0;
            }
            if (mainUnitEnabled && mainFreq != 0 && !writeMode && mainFreq + mod > 0)
            {
                mainCounter++;
                if (mainCounter >= 65536 / (mainFreq + mod))
                {
                    mainCounter = 0;
                    mainAddr++;
                    mainAddr &= 0x3F;
                }
                outVolume = (byte)((waveform[mainAddr] << 1) * (volume / 20.0) * (masterVolume / 30.0)); //waveform could be shifted over 2, but that is just far too loud.
            }
            return outVolume;
        }

        public override void StateSave(System.IO.BinaryWriter writer)
        {
            writer.Write(waveform);
            writer.Write(modBuffer);
            writer.Write(writeMode);
            writer.Write(volumeEnvelopeEnabled);
            writer.Write(volumeEnvelopeMode);
            writer.Write(volume);
            writer.Write(volumeEnvelopeGain);
            writer.Write(volumeEnvelopeSpeed);
            writer.Write(mainFreq);
            writer.Write(envelopeEnabled);
            writer.Write(mainUnitEnabled);
            writer.Write(sweepEnvelopeEnabled);
            writer.Write(sweepEnvelopeMode);
            writer.Write(sweepGain);
            writer.Write(sweepSpeed);
            writer.Write(sweepBias);
            writer.Write(modUnitFreq);
            writer.Write(modUnitEnabled);
            writer.Write(modUnitAddress);
            writer.Write(envelopeSpeed);
            writer.Write(mod);
            writer.Write(mainAddr);
            writer.Write(volumeEnvelopeCounter);
            writer.Write(sweepEnvelopeCounter);
            writer.Write(modUnitCounter);
            writer.Write(mainCounter);
            writer.Write(masterVolume);
        }

        public override void StateLoad(System.IO.BinaryReader reader)
        {
            waveform = reader.ReadBytes(64);
            modBuffer = reader.ReadBytes(64);
            writeMode = reader.ReadBoolean();
            volumeEnvelopeEnabled = reader.ReadBoolean();
            volumeEnvelopeMode = reader.ReadBoolean();
            volume = reader.ReadInt32();
            volumeEnvelopeGain = reader.ReadInt32();
            volumeEnvelopeSpeed = reader.ReadInt32();
            mainFreq = reader.ReadInt32();
            envelopeEnabled = reader.ReadBoolean();
            mainUnitEnabled = reader.ReadBoolean();
            sweepEnvelopeEnabled = reader.ReadBoolean();
            sweepEnvelopeMode = reader.ReadBoolean();
            sweepGain = reader.ReadInt32();
            sweepSpeed = reader.ReadInt32();
            sweepBias = reader.ReadInt32();
            modUnitFreq = reader.ReadInt32();
            modUnitEnabled = reader.ReadBoolean();
            modUnitAddress = reader.ReadInt32();
            envelopeSpeed = reader.ReadByte();
            mod = reader.ReadInt32();
            mainAddr = reader.ReadInt32();
            volumeEnvelopeCounter = reader.ReadInt32();
            sweepEnvelopeCounter = reader.ReadInt32();
            modUnitCounter = reader.ReadInt32();
            mainCounter = reader.ReadInt32();
            masterVolume = reader.ReadByte();
        }
    }
}
