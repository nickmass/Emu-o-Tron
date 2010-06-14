using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    class APU
    {
        private int CPUClock = 1789773; //NTSC
                             //1662607 PAL

        private int cycles;
        private int lastUpdateCycle;
        private int lastCycleClock;
        private MemoryStore Memory;
        public bool frameIRQ;
        private int frameCounter;
        private int[] frameLengths = { 7457, 7458 }; //to create the true average of 7457.5 cycles per 240hz
        private bool mode; //flase = mode 0, true = mode 1, mode 0 is 4 clock cycle, mode 1 is 5 clock cycle
        //In mode 0, the interrupt flag is set every 29830 CPU cycles which is slightly slower than the 29780.5 CPU cycles per NTSC PPU frame.
        private bool frameIRQInhibit;

        private bool pulse1Enable;
        private byte pulse1LengthCounter;
        private int pulse1DutySequencer;
        private byte pulse1Duty;
        private bool pulse1HaltFlag;
        private byte pulse1Envelope;
        private byte pulse1EnvelopeCounter;
        private int pulse1EnvelopeClock;
        private bool pulse1ConstantVolume;
        private ushort pulse1Timer;
        private bool pulse1StartFlag;

        private bool pulse2Enable;
        private byte pulse2LengthCounter;
        private int pulse2DutySequencer;
        private int pulse2Duty;
        private bool pulse2HaltFlag;

        private bool[][] dutyCycles = {new bool[] {false, true, false, false, false, false, false, false},
                                       new bool[] {false, true, true, false, false, false, false, false}, 
                                       new bool[] {false, true, true, true, true, false, false, false}, 
                                       new bool[] {true, false, false, true, true, true, true, true}}; 

        private bool noiseEnable;
        private byte noiseLengthCounter;
        private ushort[] noisePeriods = { 4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068 }; //This is ntsc only
                                        //{4, 7, 14, 30, 60, 88, 118, 148, 188, 236, 354, 472, 708,  944, 1890, 3778} PAL
                                        //NTSC Freq ~= 1.79MHz/n+1
        private ushort noiseShiftReg = 1; //Set to 1 on power up suppposed to be 15 bits wide
        private bool noiseLoop;
        private byte[] triangleSequence = {15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15};
        private byte triangleSequenceCounter = 0;
        private bool triangleEnable;
        private bool triangleControlFlag;
        private bool triangleHaltFlag;
        private byte triangleLinearCounterReload;
        private byte triangleLinearCounter;
        private byte triangleLengthCounter;
        private ushort triangleTimer;

        private double[] pulseTable = new double[32];
        private double[] tndTable = new double[204];
        //output = pulse_out + tnd_out
        //pulse_out = pulse_table [pulse1 + pulse2]
        //tnd_out = tnd_table [3 * triangle + 2 * noise + dmc]

        //public double[] output = new double[5000]; //CPUCLOCK / 41 / 60 = 728
        //public byte[] output = new byte[5000];
        public short[] output = new short[750* 60];
        public byte[] outBytes = new byte[1500 * 60];
        public int outputPtr = 0;


        public APU(MemoryStore Memory)
        {
            this.Memory = Memory;
            for (int i = 0; i < 32; i++)
                pulseTable[i] = 95.52 / (8128.0 / i + 100);
            for (int i = 0; i < 204; i++)
                tndTable[i] = 163.67 / (24329.0 / i + 100);
        }
        public byte Read(byte value, ushort address)
        {
            byte nextByte = value;
            if (address == 0x4015)//Length Counter enable and status
            {
                value = 0;
                if (pulse1LengthCounter != 0)
                    value |= 0x1;
                if (pulse2LengthCounter != 0)
                    value |= 0x2;
                if (triangleLengthCounter != 0)
                    value |= 0x4;
                if (noiseLengthCounter != 0)
                    value |= 0x8;
                if (frameIRQ)
                    value |= 0x40;
                //DMC Interrupt Flag, DMC bytes remaining
                frameIRQ = false;
            }
            return nextByte;
        }
        public void Write(byte value, ushort address)
        {
            if (address == 0x4000) //Pulse 1 Duty
            {
                Update();
                pulse1Envelope = (byte)(value & 0xF);
                pulse1ConstantVolume = (value & 0x10) != 0;
                pulse1HaltFlag = (value & 0x20) != 0;
                pulse1Duty = (byte)(value >> 6);

            }
            else if (address == 0x4002) //Pulse 1 Low Timer
            {
                Update();
                pulse1Timer = (ushort)((pulse1Timer & 0xFF00) + value);

            }
            else if (address == 0x4003)//Pulse 1 Length Counter and High Timer
            {
                Update();
                if (pulse1Enable)
                    pulse1LengthCounter = (byte)(value >> 3);
                pulse1Timer = (ushort)((pulse1Timer & 0x00FF) + ((value & 0x7) << 8));
                pulse1DutySequencer = 0;
                pulse1EnvelopeCounter = 0xF;
                pulse1StartFlag = true;
            }
            else if (address == 0x4008) //Triangle Linear Counter
            {
                Update();
                triangleControlFlag = (value & 0x80) != 0;
                triangleLinearCounterReload = (byte)(value & 0x7F);
            }
            else if (address == 0x400A) //Triangle Low Timer
            {
                Update();
                triangleTimer = (ushort)((triangleTimer & 0xFF00) + value);
            }
            else if (address == 0x400B)//Triangle Length Counter and High Timer
            {
                Update();
                if (triangleEnable)
                    triangleLengthCounter = (byte)(value >> 3);
                triangleTimer = (ushort)((triangleTimer & 0x00FF) + ((value & 0x7) << 8));
                triangleHaltFlag = true;
            }
            else if (address == 0x4015)
            {
                Update();
                pulse1Enable = (value & 0x1) != 0;
                if (!pulse1Enable)
                    pulse1LengthCounter = 0;
                pulse2Enable = (value & 0x2) != 0;
                if (!pulse2Enable)
                    pulse2LengthCounter = 0;
                triangleEnable = (value & 0x4) != 0;
                if (!triangleEnable)
                    triangleLengthCounter = 0;
                noiseEnable = (value & 0x8) != 0;
                if (!noiseEnable)
                    noiseLengthCounter = 0;
                //Clear DMC Interrupt, Set DMC bytes remaining
            }
            else if (address == 0x4017)//APU Frame rate/ IRQ control
            {
                frameCounter = 0;
                frameIRQInhibit = (value & 0x40) != 0;
                if (frameIRQInhibit)
                    frameIRQ = false;
                mode = (value & 0x80) != 0;
            }
        }
        public void ResetBuffer()
        {
            outputPtr = 0;
        }
        public void TriangleLinear()
        {
            if (triangleHaltFlag)
                triangleLinearCounter = triangleLinearCounterReload;
            else if (triangleLinearCounter != 0)
                triangleLinearCounter--;
            if (!triangleControlFlag)
                triangleHaltFlag = false;
        }
        public void Pulse1Envelope()
        {
            if (pulse1StartFlag)
            {
                pulse1StartFlag = false;
                pulse1EnvelopeCounter = 0xF;
                pulse1EnvelopeClock = 0;
            }
            if (pulse1EnvelopeClock % (pulse1Envelope + 1) == 0)
            {
                if (pulse1EnvelopeCounter != 0)
                    pulse1EnvelopeCounter--;
                else if (pulse1HaltFlag)
                    pulse1EnvelopeCounter = 0xF;
            }
            pulse1EnvelopeClock++;
        }
        public void TriangleLength()
        {
            if (triangleEnable && !triangleHaltFlag && triangleLengthCounter != 0)
                triangleLengthCounter--;
        }
        public void Pulse1Length()
        {
            if (pulse1Enable && !pulse1HaltFlag && pulse1LengthCounter != 0)
                pulse1LengthCounter--;
        }
        public void AddCycles(int cycles)
        {
            this.cycles += cycles;
            if (cycles - lastCycleClock > frameLengths[frameCounter % 2])
            {
                lastCycleClock += frameLengths[frameCounter % 2];
                frameCounter++;
                Update();
                if (!mode) //Mode 0
                {
                    int step = frameCounter % 4;
                    if (step == 0) //Envelopes + Triangle Linear Counter
                    {
                        Pulse1Envelope();
                        TriangleLinear();
                    }
                    else if (step == 1) //Envelopes + Triangle Linear Counter + Length Counters + Sweep Units
                    {
                        Pulse1Envelope();
                        TriangleLinear();
                        TriangleLength();
                    }
                    else if (step == 2) //Envelopes + Triangle Linear Counter
                    {
                        Pulse1Envelope();
                        TriangleLinear();
                    }
                    else if (step == 3) //Envelopes + Triangle Linear Counter + Length Counters + Sweep Units + Interrupt
                    {
                        Pulse1Envelope();
                        TriangleLinear();
                        TriangleLength();
                        if (!frameIRQInhibit)
                            frameIRQ = true;

                    }
                }
                else //Mode 1
                {
                    int step = frameCounter % 5;
                    if (step == 0)//Envelopes + Triangle Linear Counter + Length Counters + Sweep Units
                    {
                        Pulse1Envelope();
                        TriangleLinear();
                        TriangleLength();
                    }
                    else if (step == 1)//Envelopes + Triangle Linear Counter
                    {
                        Pulse1Envelope();
                        TriangleLinear();
                    }
                    else if (step == 2)//Envelopes + Triangle Linear Counter + Length Counters + Sweep Units
                    {
                        Pulse1Envelope();
                        TriangleLinear();
                        TriangleLength();
                    }
                    else if (step == 3)//Envelopes + Triangle Linear Counter
                    {
                        Pulse1Envelope();
                        TriangleLinear();
                    }
                }
            }
        }
        private void NoiseClock()
        {
            byte feedback;
            if(noiseLoop)
                feedback = (byte)((noiseShiftReg & 1) ^ ((noiseShiftReg >> 6) & 1));
            else
                feedback = (byte)((noiseShiftReg & 1) ^ ((noiseShiftReg >> 1) & 1));
            noiseShiftReg >>= 1;
            noiseShiftReg = (ushort)(noiseShiftReg | (feedback << 14));
        }
        public void Update()
        {
            int triangleClockRate = CPUClock / (CPUClock / (32 * (triangleTimer + 1)));
            int pulse1CockRate = CPUClock / (CPUClock / (16 * (pulse1Timer + 1)));
            byte pulse1Volume = 0;
            if (pulse1LengthCounter != 0 && dutyCycles[pulse1Duty][pulse1DutySequencer % 8])
            {
                if (pulse1ConstantVolume)
                    pulse1Volume = pulse1Envelope;
                else
                    pulse1Volume = pulse1EnvelopeCounter;
            }
            byte triangleVolume = 0;
            if((triangleLengthCounter != 0 || triangleLinearCounter != 0))
                triangleVolume = triangleSequence[triangleSequenceCounter % 32];
            for (int updateCycle = lastUpdateCycle; updateCycle < cycles; updateCycle++)
            {
                if ((triangleLengthCounter != 0 || triangleLinearCounter != 0) && updateCycle % triangleClockRate == 0)
                {
                    triangleSequenceCounter++;
                    triangleVolume = triangleSequence[triangleSequenceCounter % 32];
                }
                if (updateCycle % pulse1CockRate == 0)
                {
                    if ((pulse1LengthCounter != 0 && dutyCycles[pulse1Duty][pulse1DutySequencer % 8]))
                    {
                        if (pulse1ConstantVolume)
                            pulse1Volume = pulse1Envelope;
                        else
                            pulse1Volume = pulse1EnvelopeCounter;
                    }
                    pulse1DutySequencer++;
                }
                if (updateCycle % 41 == 0)
                {
                    output[outputPtr] = (short)((((tndTable[3 * triangleVolume + 2 * 0 + 0] + pulseTable[0 + 0]) - 0.0) * 1.0) * (short.MaxValue - 1));
                    byte[]  tmp = BitConverter.GetBytes(output[outputPtr]);
                    outBytes[outputPtr * 2] = tmp[0];
                    outBytes[(outputPtr * 2) + 1] = tmp[1];
                    outputPtr++;
                }
            }
            lastUpdateCycle = cycles;
        }
    }
}
