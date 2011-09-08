using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class Noise : Channel
    {
        private bool enabled;
        private byte lengthCounter;
        private ushort[] periods;
        private ushort shiftReg = 1;
        private bool loop;
        private bool haltFlag;
        private byte envelope;
        private byte envelopeCounter;
        private int envelopeDivider;
        private bool constantVolume;
        private ushort timer;
        private bool startFlag;
        private int divider;
        private byte volume;

        public Noise(NESCore nes)
        {
            this.nes = nes;
            switch (nes.nesRegion)
            {
                default:
                case SystemType.NTSC:
                    periods = new ushort[] { 4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068 };
                    break;
                case SystemType.PAL:
                    periods = new ushort[] { 4, 7, 14, 30, 60, 88, 118, 148, 188, 236, 354, 472, 708, 944, 1890, 3778 };
                    break;
            }
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
                case 0: //Envelope
                    envelope = (byte)(value & 0xF);
                    envelopeDivider = envelope + 1;
                    constantVolume = ((value & 0x10) != 0);
                    haltFlag = (value & 0x20) != 0;
                    break;
                case 2: //Timer
                    timer = periods[value & 0xF];
                    divider = timer;
                    loop = (value & 0x80) != 0;
                    break;
                case 3: //Length Counter
                    if (enabled)
                        lengthCounter = nes.APU.lengthTable[(value >> 3)];
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
            LengthCounter();
        }
        public override void QuarterFrame()
        {
            Envelope();
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
        private void LengthCounter()
        {
            if (!haltFlag && lengthCounter != 0)
                lengthCounter--;
        }
        public override byte Cycle()
        {
            divider--;
            if (divider == 0)
            {
                if ((shiftReg & 1) == 1 || lengthCounter == 0)
                    volume = 0;
                else if (constantVolume)
                    volume = envelope;
                else
                    volume = envelopeCounter;
                byte feedback;
                if (loop)
                    feedback = (byte)((shiftReg & 1) ^ ((shiftReg >> 6) & 1));
                else
                    feedback = (byte)((shiftReg & 1) ^ ((shiftReg >> 1) & 1));
                shiftReg >>= 1;
                shiftReg = (ushort)(shiftReg | (feedback << 14));
                divider = timer;
            }
            return volume;
        }
        public override void StateSave(System.IO.BinaryWriter writer)
        {
            writer.Write(enabled);
            writer.Write(lengthCounter);
            writer.Write(shiftReg);
            writer.Write(loop);
            writer.Write(haltFlag);
            writer.Write(envelope);
            writer.Write(envelopeCounter);
            writer.Write(envelopeDivider);
            writer.Write(constantVolume);
            writer.Write(timer);
            writer.Write(startFlag);
            writer.Write(divider);
            writer.Write(volume);
        }
        public override void StateLoad(System.IO.BinaryReader reader)
        {
            enabled = reader.ReadBoolean();
            lengthCounter = reader.ReadByte();
            shiftReg = reader.ReadUInt16();
            loop = reader.ReadBoolean();
            haltFlag = reader.ReadBoolean();
            envelope = reader.ReadByte();
            envelopeCounter = reader.ReadByte();
            envelopeDivider = reader.ReadInt32();
            constantVolume = reader.ReadBoolean();
            timer = reader.ReadUInt16();
            startFlag = reader.ReadBoolean();
            divider = reader.ReadInt32();
            volume = reader.ReadByte();
        }
    }
}
