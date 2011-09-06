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
        private bool[] enabled;
        private byte[] volume;
        private int[] timer;
        private int[] dutyCounter;

        private bool[] dutyCycle = { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
        private int[] sunsoftOut;

        public FME7()
        {
            freq = new int[3];
            timer = new int[3];
            dutyCounter = new int[3];
            enabled = new bool[3];
            volume = new byte[3];
            sunsoftOut = new int[16];
            double vol = 1.0;
            double step = Math.Pow(10, (3) / 20.0);
            for (int i = 0; i < 16; i++)
            {
                sunsoftOut[i] = (int)(vol * 0.48); //0.48 is tweaked to keep the max volume * 3 under the 255 cap of the channel.
                vol *= step;
            }
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
                        case 7:
                            enabled[0] = ((value & 1) == 0);
                            enabled[1] = ((value & 2) == 0);
                            enabled[2] = ((value & 4) == 0);
                            break;
                        case 8:
                            volume[0] = (byte)(value & 0xF);
                            break;
                        case 9:
                            volume[1] = (byte)(value & 0xF);
                            break;
                        case 0xA:
                            volume[2] = (byte)(value & 0xF);
                            break;
                    }
                    break;
            }
        }
        public override int Cycle()
        {
            int volume = 0;
            for (int i = 0; i < 3; i++)
            {
                timer[i]++;
                if (timer[i] >= freq[i])
                {
                    timer[i] = 0;
                    dutyCounter[i]++;
                }
                if (dutyCycle[dutyCounter[i] % 32] && enabled[i])
                    volume += sunsoftOut[this.volume[i]];
            }
            return volume;
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
