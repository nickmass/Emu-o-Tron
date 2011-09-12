using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class Triangle : Channel
    {
        private byte[] sequence = { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        private byte sequenceCounter;
        private bool enabled;
        private bool controlFlag;
        private bool haltFlag;
        private byte linearCounterReload;
        private byte linearCounter;
        private byte lengthCounter;
        private ushort timer;
        private int divider;
        private int freq;
        private byte volume;

        public Triangle(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Power()
        {
            Write(0, 0);
            Write(0, 1);
            Write(0, 2);
            Write(0, 3);
            Write(0, 4);
        }
        public override void Reset()
        {
            Write(0, 4);
        }
        public override byte Read(byte value, ushort address)
        {
            if (lengthCounter != 0)
                return 1;
            else
                return 0;
        }
        public override void Write(byte value, ushort reg)
        {
            switch (reg)
            {
                case 0: //Linear Counter
                    controlFlag = (value & 0x80) != 0;
                    linearCounterReload = (byte)(value & 0x7F);
                    break;
                case 2: //Low Timer
                    timer = (ushort)((timer & 0x0700) | value);
                    freq = (timer + 1);
                    divider = freq;
                    break;
                case 3: //Length Counter and High Timer
                    if (enabled)
                        lengthCounter = nes.APU.lengthTable[(value >> 3)];
                    timer = (ushort)((timer & 0x00FF) | ((value & 0x7) << 8));
                    freq = (timer + 1);
                    divider = freq;
                    haltFlag = true;
                    break;
                case 4:
                    enabled = (value != 0);
                    if (!enabled)
                        lengthCounter = 0;
                    break;
            }
        }
        public override void QuarterFrame()
        {
            LinearCounter();
        }
        public override void HalfFrame()
        {
            LengthCounter();
        }
        public override byte Cycle()
        {
            divider--;
            if (divider == 0)
            {
                if (timer <= 1) //Filter ultra-high freq.
                    volume = 7;
                else
                    volume = sequence[sequenceCounter % 32];
                if (lengthCounter != 0 && linearCounter != 0)
                    sequenceCounter++;
                divider = freq;
            }
            return volume;
        }
        private void LinearCounter()
        {
            if (haltFlag)
                linearCounter = linearCounterReload;
            else if (linearCounter != 0)
                linearCounter--;
            if (!controlFlag)
                haltFlag = false;
        }
        private void LengthCounter()
        {
            if (!haltFlag && lengthCounter != 0)
                lengthCounter--;
        }
        public override void StateSave(System.IO.BinaryWriter writer)
        {
            writer.Write(sequenceCounter);
            writer.Write(enabled);
            writer.Write(controlFlag);
            writer.Write(haltFlag);
            writer.Write(linearCounterReload);
            writer.Write(linearCounter);
            writer.Write(lengthCounter);
            writer.Write(timer);
            writer.Write(divider);
            writer.Write(freq);
            writer.Write(volume);
        }
        public override void StateLoad(System.IO.BinaryReader reader)
        {
            sequenceCounter = reader.ReadByte();
            enabled = reader.ReadBoolean();
            controlFlag = reader.ReadBoolean();
            haltFlag = reader.ReadBoolean();
            linearCounterReload = reader.ReadByte();
            linearCounter = reader.ReadByte();
            lengthCounter = reader.ReadByte();
            timer = reader.ReadUInt16();
            divider = reader.ReadInt32();
            freq = reader.ReadInt32();
            volume = reader.ReadByte();
        }
    }
}
