using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class FME7 : Channel
    {
        private byte reg;
        private int[] freq;
        private bool[] noiseEnabled;
        private bool[] enabled;
        private bool[] envelopeEnabled;
        private byte[] volume;
        private int[] timer;
        private int[] dutyCounter;

        private int noiseFreq;
        private int noiseTimer;
        private ushort noiseShift = 1;
        private bool noiseOutput;

        private int envInc = 1;
        private int envVolume;

        private bool envCont;
        private bool envAtt;
        private bool envAlt;
        private bool envHold;

        private int envFreq;
        private int envTimer;

        private bool[] dutyCycle = { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
        private byte[] sunsoftOut;

        public FME7()
        {
            freq = new int[3];
            timer = new int[3];
            dutyCounter = new int[3];
            enabled = new bool[3];
            noiseEnabled = new bool[3];
            volume = new byte[3];
            envelopeEnabled = new bool[3];
            sunsoftOut = new byte[16];
            double vol = 1.0;
            double step = Math.Pow(10, (3) / 20.0);
            for (int i = 0; i < 16; i++)
            {
                sunsoftOut[i] = (byte)(vol * 0.48); //0.48 is tweaked to keep the max volume * 3 under the 255 cap of the channel.
                vol *= step;
            }
        }
        public override void Power()
        {
            for (byte i = 0; i < 0x10; i++)
            {
                reg = i;
                Write(0, 0xE000);
            }
            reg = 0;
        }
        public override void Write(byte value, ushort address)
        {
            switch (address)
            {
                case 0xC000:
                    reg = (byte)(value & 0xF);
                    break;
                case 0xE000:
                    switch (reg)
                    {
                        case 0:
                            freq[0] = (freq[0] & 0xF00) | value;
                            break;
                        case 1:
                            freq[0] = (freq[0] & 0x0FF) | ((value & 0xF) << 8);
                            break;
                        case 2:
                            freq[1] = (freq[1] & 0xF00) | value;
                            break;
                        case 3:
                            freq[1] = (freq[1] & 0x0FF) | ((value & 0xF) << 8);
                            break;
                        case 4:
                            freq[2] = (freq[2] & 0xF00) | value;
                            break;
                        case 5:
                            freq[2] = (freq[2] & 0x0FF) | ((value & 0xF) << 8);
                            break;
                        case 6:
                            noiseFreq = (value & 0x1F) << 1; //Not sure about shifting this over, but I think I have to to match double the pulse duty length.
                            break;
                        case 7:
                            enabled[0] = ((value & 1) == 0);
                            enabled[1] = ((value & 2) == 0);
                            enabled[2] = ((value & 4) == 0);
                            noiseEnabled[0] = ((value & 8) == 0);
                            noiseEnabled[1] = ((value & 0x10) == 0);
                            noiseEnabled[2] = ((value & 0x20) == 0);
                            break;
                        case 8:
                            volume[0] = (byte)(value & 0xF);
                            envelopeEnabled[0] = (value & 0x10) != 0;
                            break;
                        case 9:
                            volume[1] = (byte)(value & 0xF);
                            envelopeEnabled[1] = (value & 0x10) != 0;
                            break;
                        case 0xA:
                            volume[2] = (byte)(value & 0xF);
                            envelopeEnabled[2] = (value & 0x10) != 0;
                            break;
                        case 0xB:
                            envFreq = value | (envFreq & 0xFF00);
                            break;
                        case 0xC:
                            envFreq = (value << 8) | (envFreq & 0x00FF);
                            break;
                        case 0xD:
                            envHold = (value & 1) != 0;
                            envAlt = (value & 2) != 0;
                            envAtt = (value & 4) != 0;
                            envCont = (value & 8) != 0;
                            envInc = envAlt ? -1 : 1;
                            break;
                            
                    }
                    break;
            }
        }
        public override byte Cycle()
        {
            byte volume = 0;
            envTimer++;
            if(envTimer >= envFreq << 1)  //Have NO idea how I should be shifting the envelope, this is my best guess.
            {
                envTimer = 0;
                if (envVolume + envInc > 31 || envVolume + envInc < 0)
                {
                    if (envCont)
                    {
                        if (envAlt)
                        {
                            if (envHold)
                            {
                                envVolume = envAtt ? 0 : 31;
                            }
                            else
                            {
                                envInc = envInc * -1;
                            }
                        }
                        else
                        {
                            if (!envHold)
                                envVolume = envAtt ? 0 : 31;
                        }
                        if (!envHold)
                            envVolume += envInc;
                    }
                    else
                    {
                        envVolume = 0;
                        envInc = -1;
                    }
                }
                else
                {
                    envVolume += envInc;
                }
            }
            noiseTimer++;
            if (noiseTimer >= noiseFreq)
            {
                noiseTimer = 0;
                noiseOutput = NoiseClock();
            }

            for (int i = 0; i < 3; i++)
            {
                timer[i]++;
                if (timer[i] >= freq[i])
                {
                    timer[i] = 0;
                    dutyCounter[i]++;
                }
                if (dutyCycle[dutyCounter[i] % 32] && (enabled[i] || (noiseOutput && noiseEnabled[i])))
                {
                    if (envelopeEnabled[i])//Don't know if envelope is linear or not, going to act like it is for simplicities sake.
                        volume += (byte)(envVolume << 1);
                    else
                        volume += sunsoftOut[this.volume[i]];
                }
            }
            return volume;
        }
        private bool NoiseClock()
        {
            ushort inBit = (ushort)((((noiseShift >> 3) & 0x01) ^ (noiseShift & 0x01)) << 15);
            bool result = (noiseShift & 1) == 1;
            noiseShift >>= 1;
            noiseShift |= inBit;
            return result;
        }
        public override void StateSave(System.IO.BinaryWriter writer)
        {
            writer.Write(reg);
            for (int i = 0; i < 3; i++)
            {
                writer.Write(freq[i]);
                writer.Write(enabled[i]);
                writer.Write(volume[i]);
                writer.Write(timer[i]);
                writer.Write(dutyCounter[i]);
            }
        }
        public override void StateLoad(System.IO.BinaryReader reader)
        {
            reg = reader.ReadByte();
            for (int i = 0; i < 3; i++)
            {
                freq[i] = reader.ReadInt32();
                enabled[i] = reader.ReadBoolean();
                volume[i] = reader.ReadByte();
                timer[i] = reader.ReadInt32();
                dutyCounter[i] = reader.ReadInt32();
            }

        }
    }
}
