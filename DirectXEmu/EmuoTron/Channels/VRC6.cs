using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class VRC6 : Channel
    {
        private byte[] regAddr = new byte[4];

        private bool[][] dutyCycles = {
            new bool[] {true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
            new bool[] {true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
            new bool[] {true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false },
            new bool[] {true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false },
            new bool[] {true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false },
            new bool[] {true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false },
            new bool[] {true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false },
            new bool[] {true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false },
        };


        bool square1Digitized;
        bool square2Digitized;
        byte square1Volume;
        byte square2Volume;
        byte square1Duty;
        byte square2Duty;
        int square1DutyCounter;
        int square2DutyCounter;
        int square1Freq;
        int square2Freq;
        int square1Counter;
        int square2Counter;
        bool square1Enable;
        bool square2Enable;

        byte sawAccumRate;
        byte sawAccum;
        int sawAccumCounter;
        int sawCounter;
        int sawFreq;
        bool sawOddClock;
        bool sawEnable;

        public VRC6(byte reg1, byte reg2, byte reg3, byte reg4)
        {
            regAddr[0] = reg1;
            regAddr[1] = reg2;
            regAddr[2] = reg3;
            regAddr[3] = reg4;
        }
        public override void Power()
        {
            square1Digitized = false;
            square2Digitized = false;
            square1Volume = 0;
            square2Volume = 0;
            square1Duty = 0;
            square2Duty = 0;
            square1DutyCounter = 0;
            square2DutyCounter = 0;
            square1Freq = 0;
            square2Freq = 0;
            square1Counter = 0;
            square2Counter = 0;
            square1Enable = false;
            square2Enable = false;
            sawEnable = false;
            sawAccumRate = 0;
            sawAccum = 0;
            sawAccumCounter = 0;
            sawCounter = 0;
            sawFreq = 0;
            sawOddClock = false;
            
        }
        public override void Write(byte value, ushort address)
        {
            byte highAddr = (byte)(address >> 8);
            byte lowAddr = (byte)(address & 0xFF);
            if (highAddr == 0x90 && lowAddr == regAddr[0])
            {
                square1Digitized = (value & 0x80) != 0;
                square1Duty = (byte)(value >> 4 & 7);
                square1Volume = (byte)(value & 0xF);
            }
            else if (highAddr == 0xA0 && lowAddr == regAddr[0])
            {
                square2Digitized = (value & 0x80) != 0;
                square2Duty = (byte)(value >> 4 & 7);
                square2Volume = (byte)(value & 0xF);
            }
            else if (highAddr == 0x90 && lowAddr == regAddr[1])
            {
                square1Freq = (square1Freq & 0xF00) | value;
            }
            else if (highAddr == 0xA0 && lowAddr == regAddr[1])
            {
                square2Freq = (square2Freq & 0xF00) | value;
            }
            else if (highAddr == 0x90 && lowAddr == regAddr[2])
            {
                square1Freq = (square1Freq & 0x0FF) | ((value & 0xF) << 8);
                square1Enable = (value & 0x80) != 0;
            }
            else if (highAddr == 0xA0 && lowAddr == regAddr[2])
            {
                square2Freq = (square2Freq & 0x0FF) | ((value & 0xF) << 8);
                square2Enable = (value & 0x80) != 0;
            }
            else if (highAddr == 0xB0 && lowAddr == regAddr[0])
            {
                sawAccumRate = (byte)(value & 0x3F);
            }
            else if (highAddr == 0xB0 && lowAddr == regAddr[1])
            {
                sawFreq = (sawFreq & 0xF00) | value;
            }
            else if (highAddr == 0xB0 && lowAddr == regAddr[2])
            {
                sawFreq = (sawFreq & 0x0FF) | ((value & 0xF) << 8);
                sawEnable = (value & 0x80) != 0;
            }

        }
        public override byte Cycle()
        {
            byte volume = 0;
            square1Counter++;
            if (square1Counter >= square1Freq)
            {
                square1Counter = 0;
                square1DutyCounter++;
            }
            if(square1Enable && (dutyCycles[square1Duty][square1DutyCounter % 16] || square1Digitized))
            {
                volume += square1Volume;
            }
            square2Counter++;
            if (square2Counter >= square2Freq)
            {
                square2Counter = 0;
                square2DutyCounter++;
            }
            if (square2Enable && (dutyCycles[square2Duty][square2DutyCounter % 16] || square2Digitized))
            {
                volume += square2Volume;
            }
            sawCounter++;
            if (sawCounter >= sawFreq)
            {
                sawCounter = 0;
                sawOddClock = !sawOddClock;
                if (sawOddClock)
                {
                    sawAccum = (byte)((sawAccum + sawAccumRate) & 0xFF);
                    sawAccumCounter++;
                    if (sawAccumCounter % 7 == 0)
                    {
                        sawAccum = 0;
                    }
                }
            }
            if (sawEnable)
                volume += (byte)((sawAccum >> 3) & 0x1F);
            volume *= 3; //Could go to * 4 but that seems kinda overpowering, I really need an actual famicom and some carts to decide : (
            return volume;
        }
        public override void StateSave(System.IO.BinaryWriter writer)
        {
            writer.Write(square1Digitized);
            writer.Write(square1Volume);
            writer.Write(square1Duty);
            writer.Write(square1DutyCounter);
            writer.Write(square1Freq);
            writer.Write(square1Counter);
            writer.Write(square1Enable);
            writer.Write(square2Digitized);
            writer.Write(square2Volume);
            writer.Write(square2Duty);
            writer.Write(square2DutyCounter);
            writer.Write(square2Freq);
            writer.Write(square2Counter);
            writer.Write(square2Enable);
            writer.Write(sawAccumRate);
            writer.Write(sawAccum);
            writer.Write(sawAccumCounter);
            writer.Write(sawCounter);
            writer.Write(sawFreq);
            writer.Write(sawOddClock);
            writer.Write(sawEnable);
        }
        public override void StateLoad(System.IO.BinaryReader reader)
        {
            square1Digitized = reader.ReadBoolean();
            square1Volume = reader.ReadByte();
            square1Duty = reader.ReadByte();
            square1DutyCounter = reader.ReadInt32();
            square1Freq = reader.ReadInt32();
            square1Counter = reader.ReadInt32();
            square1Enable = reader.ReadBoolean();
            square2Digitized = reader.ReadBoolean();
            square2Volume = reader.ReadByte();
            square2Duty = reader.ReadByte();
            square2DutyCounter = reader.ReadInt32();
            square2Freq = reader.ReadInt32();
            square2Counter = reader.ReadInt32();
            square2Enable = reader.ReadBoolean();
            sawAccumRate = reader.ReadByte();
            sawAccum = reader.ReadByte();
            sawAccumCounter = reader.ReadInt32();
            sawCounter = reader.ReadInt32();
            sawFreq = reader.ReadInt32();
            sawOddClock = reader.ReadBoolean();
            sawEnable = reader.ReadBoolean();

        }
    }
}
