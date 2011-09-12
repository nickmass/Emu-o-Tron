using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    public class DMC : Channel
    {
        public bool interrupt;
        private bool interruptEnable;
        private bool loop;
        private int rate;
        private int divider;
        private int sampleAddress;
        public int sampleCurrentAddress;
        private int sampleLength;
        private byte deltaCounter;
        private int bytesRemaining;
        private byte sampleBuffer;
        private bool sampleBufferEmpty;
        private int shiftCount;
        private byte shiftReg;
        private int[] rates;
        public byte[] buffer;
        public int ptr;
        private bool silenced;

        public bool fetching;
        public int idleCycles;
        public bool reading;

        public DMC(NESCore nes, int bufferSize)
        {
            this.nes = nes;
            switch (nes.nesRegion)
            {
                default:
                case SystemType.NTSC:
                    rates = new int[] { 428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54 };
                    break;
                case SystemType.PAL:
                    rates = new int[] { 398, 354, 316, 298, 276, 236, 210, 198, 176, 148, 132, 118, 98, 78, 66, 50 };
                    break;
            }
            buffer = new byte[bufferSize];
        }
        public override void Power()
        {
            Write(0, 0);
            Write(0, 1);
            Write(0, 2);
            Write(0, 3);
            Write(0, 4);
            fetching = false;
            idleCycles = 0;
            reading = false;
            shiftReg = 0;
            shiftCount = 0;
        }
        public override void Reset()
        {
            Write(0, 4);
        }
        public override byte Read(byte value, ushort address)
        {
            byte nextByte = 0;
            if (bytesRemaining != 0)
                nextByte |= 0x10;
            if (interrupt)
                nextByte |= 0x80;
            return nextByte;
        }
        public override void Write(byte value, ushort reg)
        {
            switch (reg)
            {
                case 0: //DMC Flags and Freq
                    rate = rates[value & 0xF];
                    divider = rate;
                    loop = (value & 0x40) != 0;
                    interruptEnable = (value & 0x80) != 0;
                    if (!interruptEnable)
                        interrupt = false;
                    break;
                case 1: //DMC Direct Load
                    deltaCounter = (byte)(value & 0x7F);
                    break;
                case 2: //DMC Sample Address
                    sampleAddress = 0xC000 | (value << 6);
                    break;
                case 3: //DMC Sample Length
                    sampleLength = (value << 4) | 1;
                    break;
                case 4:
                    if (value == 0)
                        bytesRemaining = 0;
                    else if (bytesRemaining == 0)
                    {
                        sampleCurrentAddress = sampleAddress;
                        bytesRemaining = sampleLength;
                    }
                    interrupt = false;
                    break;
            }
        }
        public override byte Cycle()
        {
            divider--;
            if (divider == 0)
            {
                divider = rate;
                if (!silenced)
                {
                    if ((shiftReg & 1) == 0 && deltaCounter > 1)
                        deltaCounter -= 2;
                    else if ((shiftReg & 1) != 0 && deltaCounter < 126)
                        deltaCounter += 2;
                    shiftReg >>= 1;
                    shiftCount--;
                }
                if (shiftCount <= 0)
                {
                    if (!sampleBufferEmpty)
                    {
                        shiftReg = sampleBuffer;
                        sampleBufferEmpty = true;
                        silenced = false;
                        shiftCount = 8;
                    }
                    else
                    {
                        silenced = true;
                    }
                }
            }
            if (sampleBufferEmpty && !fetching)
            {
                if (bytesRemaining != 0)
                {
                    fetching = true;
                    idleCycles = 3;
                }
            }
            buffer[ptr++] = deltaCounter;
            return deltaCounter;
        }
        public void Fetch(bool write, int nextSample)
        {
            if (write && idleCycles > 0)
                idleCycles--;
            else if (!write)
            {
                if (idleCycles > 0)
                    idleCycles--;
                else
                {
                    fetching = false;
                    sampleBuffer = (byte)(nextSample & 0xFF);
                    sampleBufferEmpty = false;
                    sampleCurrentAddress++;
                    if (sampleCurrentAddress > 0xFFFF)
                        sampleCurrentAddress = 0x8000;
                    bytesRemaining--;
                    if (bytesRemaining == 0)
                    {
                        if (loop)
                        {
                            bytesRemaining = sampleLength;
                            sampleCurrentAddress = sampleAddress;
                        }
                        else if (interruptEnable)
                        {
                            interrupt = true;
                        }
                    }
                }
            }
        }
        public override void StateSave(System.IO.BinaryWriter writer)
        {
            writer.Write(interrupt);
            writer.Write(interruptEnable);
            writer.Write(loop);
            writer.Write(rate);
            writer.Write(divider);
            writer.Write(sampleAddress);
            writer.Write(sampleCurrentAddress);
            writer.Write(sampleLength);
            writer.Write(deltaCounter);
            writer.Write(bytesRemaining);
            writer.Write(sampleBuffer);
            writer.Write(sampleBufferEmpty);
            writer.Write(shiftCount);
            writer.Write(shiftReg);
            writer.Write(silenced);
            writer.Write(fetching);
            writer.Write(idleCycles);
            writer.Write(reading);
        }
        public override void StateLoad(System.IO.BinaryReader reader)
        {
            interrupt = reader.ReadBoolean();
            interruptEnable = reader.ReadBoolean();
            loop = reader.ReadBoolean();
            rate = reader.ReadInt32();
            divider = reader.ReadInt32();
            sampleAddress = reader.ReadInt32();
            sampleCurrentAddress = reader.ReadInt32();
            sampleLength = reader.ReadInt32();
            deltaCounter = reader.ReadByte();
            bytesRemaining = reader.ReadInt32();
            sampleBuffer = reader.ReadByte();
            sampleBufferEmpty = reader.ReadBoolean();
            shiftCount = reader.ReadInt32();
            shiftReg = reader.ReadByte();
            silenced = reader.ReadBoolean();
            fetching = reader.ReadBoolean();
            idleCycles = reader.ReadInt32();
            reading = reader.ReadBoolean();
        }
    }
}
