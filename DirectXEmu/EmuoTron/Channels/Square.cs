using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class Square : Channel
    {
        private bool negate;
        private bool sweep;

        private bool[][] dutyCycles = {new bool[] {false, true, false, false, false, false, false, false},
                                       new bool[] {false, true, true, false, false, false, false, false}, 
                                       new bool[] {false, true, true, true, true, false, false, false}, 
                                       new bool[] {true, false, false, true, true, true, true, true}};

        private bool enabled;
        private byte lengthCounter;
        private int dutySequencer;
        private byte duty;
        private bool haltFlag;
        private byte envelope;
        private byte envelopeCounter;
        private int envelopeDivider;
        private bool constantVolume;
        private ushort timer;
        private bool startFlag;
        private int divider;
        private bool sweepEnable;
        private byte sweepTimer;
        private byte sweepDivider;
        private bool sweepNegate;
        private bool sweepReload;
        private byte sweepShift;
        private bool sweepMute;
        private int freq;
        private byte volume;

        public Square(NESCore nes, bool sweep, bool negate)
        {
            this.nes = nes;
            this.negate = negate;
            this.sweep = sweep; //Allows for reuse as MMC5 square
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
                case 0: //Duty
                    envelope = (byte)(value & 0xF);
                    constantVolume = (value & 0x10) != 0;
                    haltFlag = (value & 0x20) != 0;
                    duty = (byte)(value >> 6);
                    break;
                case 1: //Sweep
                    if (sweep)
                    {
                        sweepShift = (byte)(value & 0x7);
                        sweepNegate = ((value & 0x8) != 0);
                        sweepTimer = (byte)((value >> 4) & 0x7);
                        sweepDivider = (byte)(sweepTimer + 1);
                        sweepEnable = ((value & 0x80) != 0);
                        sweepReload = true;
                    }
                    break;
                case 2: //Low Timer
                    timer = (ushort)((timer & 0x700) | value);
                    freq = (timer + 1) * 2;
                    divider = freq;
                    break;
                case 3: //Length Counter and High Timer
                    if (enabled)
                        lengthCounter = nes.APU.lengthTable[value >> 3];
                    timer = (ushort)((timer & 0x00FF) | ((value & 0x7) << 8));
                    freq = (timer + 1) * 2;
                    divider = freq;
                    dutySequencer = 0;
                    startFlag = true;
                    break;
                case 4:
                    enabled = (value != 0);
                    if (!enabled)
                        lengthCounter = 0;
                    break;
            }
        }
        public override void HalfFrame()
        {
            if(sweep)
                Sweep();
            LengthCounter();
        }
        public override void QuarterFrame()
        {
            Envelope();
        }
        public override byte Cycle()
        {
            divider--;
            if (divider == 0)
            {
                SweepMute();
                if (lengthCounter == 0 || sweepMute || !dutyCycles[duty][dutySequencer % 8])
                    volume = 0;
                else if (constantVolume)
                    volume = envelope;
                else
                    volume = envelopeCounter;
                dutySequencer++;
                divider = freq;
            }
            return volume;
        }
        private void Envelope()
        {
            if (startFlag)
            {
                startFlag = false;
                envelopeCounter = 0xF;
                envelopeDivider = envelope + 1;
            }
            else
            {
                envelopeDivider--;
                if (envelopeDivider == 0)
                {
                    if (envelopeCounter != 0)
                        envelopeCounter--;
                    else if (haltFlag)
                        envelopeCounter = 0xF;
                    envelopeDivider = envelope + 1;
                }
            }
        }
        private void SweepMute()
        {
            sweepMute = sweep && (timer < 8 || ((!sweepNegate) && (timer + (timer >> sweepShift)) > 0x7FF));
        }
        private void Sweep()
        {
            sweepDivider--;
            if (sweepDivider == 0)
            {
                int tmp = timer >> sweepShift;
                if (sweepNegate)
                {
                    if(negate)
                        tmp = ~tmp;
                    else
                        tmp = 0 - tmp;
                }
                tmp += timer;
                SweepMute();
                if (sweepEnable && sweepShift != 0 && !sweepMute)
                {
                    timer = (ushort)(tmp & 0x7FF);
                    freq = (timer + 1) * 2;
                }
                sweepDivider = (byte)(sweepTimer + 1);

            }
            if (sweepReload)
            {
                sweepReload = false;
                sweepDivider = (byte)(sweepTimer + 1);
            }
        }
        private void LengthCounter()
        {
            if (!haltFlag && lengthCounter != 0)
                lengthCounter--;
        }
        public override void StateSave(System.IO.BinaryWriter writer)
        {
            writer.Write(enabled);
            writer.Write(lengthCounter);
            writer.Write(dutySequencer);
            writer.Write(duty);
            writer.Write(haltFlag);
            writer.Write(envelope);
            writer.Write(envelopeCounter);
            writer.Write(envelopeDivider);
            writer.Write(constantVolume);
            writer.Write(timer);
            writer.Write(startFlag);
            writer.Write(divider);
            writer.Write(sweepEnable);
            writer.Write(sweepTimer);
            writer.Write(sweepDivider);
            writer.Write(sweepNegate);
            writer.Write(sweepReload);
            writer.Write(sweepShift);
            writer.Write(sweepMute);
            writer.Write(freq);
            writer.Write(volume);
        }
        public override void StateLoad(System.IO.BinaryReader reader)
        {
            enabled = reader.ReadBoolean();
            lengthCounter = reader.ReadByte();
            dutySequencer = reader.ReadInt32();
            duty = reader.ReadByte();
            haltFlag = reader.ReadBoolean();
            envelope = reader.ReadByte();
            envelopeCounter = reader.ReadByte();
            envelopeDivider = reader.ReadInt32();
            constantVolume = reader.ReadBoolean();
            timer = reader.ReadUInt16();
            startFlag = reader.ReadBoolean();
            divider = reader.ReadInt32();
            sweepEnable = reader.ReadBoolean();
            sweepTimer = reader.ReadByte();
            sweepDivider = reader.ReadByte();
            sweepNegate = reader.ReadBoolean();
            sweepReload = reader.ReadBoolean();
            sweepShift = reader.ReadByte();
            sweepMute = reader.ReadBoolean();
            freq = reader.ReadInt32();
            volume = reader.ReadByte();
        }
    }
}
