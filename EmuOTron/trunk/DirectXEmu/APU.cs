﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace DirectXEmu
{
    class APU
    {
        public int CPUClock = 1789773; //NTSC
                             //1662607 PAL
        public int divider = 41;

        public bool mute = true;

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
        private int pulse1EnvelopeDivider;
        private bool pulse1ConstantVolume;
        private ushort pulse1Timer;
        private bool pulse1StartFlag;
        private bool pulse1EnvelopeLoop;
        private int pulse1Divider;
        private bool pulse1SweepEnable;
        private byte pulse1SweepTimer;
        private byte pulse1SweepDivider;
        private bool pulse1SweepNegate;
        private bool pulse1SweepReload;
        private byte pulse1SweepShift;



        private bool pulse2Enable;
        private byte pulse2LengthCounter;
        private int pulse2DutySequencer;
        private byte pulse2Duty;
        private bool pulse2HaltFlag;
        private byte pulse2Envelope;
        private byte pulse2EnvelopeCounter;
        private int pulse2EnvelopeDivider;
        private bool pulse2ConstantVolume;
        private ushort pulse2Timer;
        private bool pulse2StartFlag;
        private bool pulse2EnvelopeLoop;
        private int pulse2Divider;
        private bool pulse2SweepEnable;
        private byte pulse2SweepTimer;
        private byte pulse2SweepDivider;
        private bool pulse2SweepNegate;
        private bool pulse2SweepReload;
        private byte pulse2SweepShift;

        private bool[][] dutyCycles = {new bool[] {false, true, false, false, false, false, false, false},
                                       new bool[] {false, true, true, false, false, false, false, false}, 
                                       new bool[] {false, true, true, true, true, false, false, false}, 
                                       new bool[] {true, false, false, true, true, true, true, true}};

        private byte[] lengthTable = { 10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14, 12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30 };

        private bool noiseEnable;
        private byte noiseLengthCounter;
        private ushort[] noisePeriods = { 4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068 }; //This is ntsc only
                                        //{4, 7, 14, 30, 60, 88, 118, 148, 188, 236, 354, 472, 708,  944, 1890, 3778} PAL
                                        //NTSC Freq ~= 1.79MHz/n+1
        private ushort noiseShiftReg = 1; //Set to 1 on power up suppposed to be 15 bits wide
        private bool noiseLoop;
        private bool noiseHaltFlag;
        private byte noiseEnvelope;
        private byte noiseEnvelopeCounter;
        private int noiseEnvelopeDivider;
        private bool noiseConstantVolume;
        private ushort noiseTimer;
        private bool noiseStartFlag;
        private bool noiseEnvelopeLoop;
        private int noiseDivider;

        private byte[] triangleSequence = {15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15};
        private byte triangleSequenceCounter = 0;
        private bool triangleEnable;
        private bool triangleControlFlag;
        private bool triangleHaltFlag;
        private byte triangleLinearCounterReload;
        private byte triangleLinearCounter;
        private byte triangleLengthCounter;
        private ushort triangleTimer;
        private int triangleDivider;

        private float[] pulseTable = new float[32];
        private float[] tndTable = new float[204];

        public float[] output;
        public byte[] outBytes;
        public int outputPtr = 0;


        public APU(MemoryStore Memory)
        {
            output = new float[1789773 / divider];
            outBytes = new byte[1789773 / divider * 4];
            this.Memory = Memory;
            for (int i = 0; i < 32; i++)
                pulseTable[i] = ((95.52f / (8128.0f / i + 100f)));
            for (int i = 0; i < 204; i++)
                tndTable[i] = ((163.67f / (24329.0f / i + 100f)));
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
                pulse1EnvelopeLoop = pulse1HaltFlag;
                pulse1Duty = (byte)(value >> 6);

            }
            else if (address == 0x4001) //Pulse 1 Sweep
            {
                Update();
                pulse1SweepShift = (byte)(value & 0x7);
                pulse1SweepNegate = ((value & 0x8) != 0);
                pulse1SweepTimer = (byte)((value >> 4) & 0x7);
                pulse1SweepDivider = (byte)(pulse1SweepTimer + 1);
                pulse1SweepEnable = ((value & 0x80) != 0);
                pulse1SweepReload = true;

            }
            else if (address == 0x4002) //Pulse 1 Low Timer
            {
                Update();
                pulse1Timer = (ushort)((pulse1Timer & 0xFF00) + value);
                pulse1Divider = pulse1Timer + 1;

            }
            else if (address == 0x4003)//Pulse 1 Length Counter and High Timer
            {
                Update();
                if (pulse1Enable)
                    pulse1LengthCounter = lengthTable[(byte)(value >> 3)];
                pulse1Timer = (ushort)((pulse1Timer & 0x00FF) + ((value & 0x7) << 8));
                pulse1Divider = pulse1Timer + 1;
                pulse1DutySequencer = 0;
                pulse1StartFlag = true;
                pulse1HaltFlag = true;
            }
            else if (address == 0x4004) //Pulse 2 Duty
            {
                Update();
                pulse2Envelope = (byte)(value & 0xF);
                pulse2ConstantVolume = (value & 0x10) != 0;
                pulse2HaltFlag = (value & 0x20) != 0;
                pulse2EnvelopeLoop = pulse2HaltFlag;
                pulse2Duty = (byte)(value >> 6);

            }
            else if (address == 0x4005) //Pulse 2 Sweep
            {
                Update();
                pulse2SweepShift = (byte)(value & 0x7);
                pulse2SweepNegate = ((value & 0x8) != 0);
                pulse2SweepTimer = (byte)((value >> 4) & 0x7);
                pulse2SweepDivider = (byte)(pulse2SweepTimer + 1);
                pulse2SweepEnable = ((value & 0x80) != 0);
                pulse2SweepReload = true;

            }
            else if (address == 0x4006) //Pulse 2 Low Timer
            {
                Update();
                pulse2Timer = (ushort)((pulse2Timer & 0xFF00) + value);
                pulse2Divider = pulse2Timer + 1;

            }
            else if (address == 0x4007)//Pulse 2 Length Counter and High Timer
            {
                Update();
                if (pulse2Enable)
                    pulse2LengthCounter = lengthTable[(byte)(value >> 3)];
                pulse2Timer = (ushort)((pulse2Timer & 0x00FF) + ((value & 0x7) << 8));
                pulse2Divider = pulse2Timer + 1;
                pulse2DutySequencer = 0;
                pulse2StartFlag = true;
                pulse2HaltFlag = true;
            }
            else if (address == 0x400C) //Noise Envelope
            {
                Update();
                noiseEnvelope = (byte)(value & 0xF);
                noiseEnvelopeDivider = noiseEnvelope + 1;
                noiseConstantVolume = ((value & 0x10) != 0);
                noiseHaltFlag = (value & 0x20) != 0;
                noiseEnvelopeLoop = noiseHaltFlag;
            }
            else if (address == 0x400E) //Noise Timer
            {
                Update();
                noiseTimer = noisePeriods[value & 0xF];
                noiseDivider = noiseTimer;
                noiseLoop = (value & 0x80) != 0;
            }
            else if (address == 0x400F)//Noise Length Counter
            {
                Update();
                if (noiseEnable)
                    noiseLengthCounter = lengthTable[(byte)(value >> 3)];
                noiseStartFlag = true;
                noiseHaltFlag = true;
            }
            else if (address == 0x4008) //Triangle Linear Counter
            {
                Update();
                triangleControlFlag = (value & 0x80) != 0;
                triangleHaltFlag = triangleControlFlag;
                triangleLinearCounterReload = (byte)(value & 0x7F);
            }
            else if (address == 0x400A) //Triangle Low Timer
            {
                Update();
                triangleTimer = (ushort)((triangleTimer & 0xFF00) + value);
                triangleDivider = triangleTimer + 1;
            }
            else if (address == 0x400B)//Triangle Length Counter and High Timer
            {
                Update();
                if (triangleEnable)
                    triangleLengthCounter = lengthTable[(byte)(value >> 3)];
                triangleTimer = (ushort)((triangleTimer & 0x00FF) + ((value & 0x7) << 8));
                triangleDivider = triangleTimer + 1;
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
                Update();
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
                pulse1EnvelopeDivider = pulse1Envelope + 1;
            }
            else
            {
                pulse1EnvelopeDivider--;
                if (pulse1EnvelopeDivider == 0)
                {
                    if (pulse1EnvelopeCounter != 0)
                        pulse1EnvelopeCounter--;
                    else if (pulse1EnvelopeLoop)
                        pulse1EnvelopeCounter = 0xF;
                    pulse1EnvelopeDivider = pulse1Envelope + 1;
                }
            }
        }
        public void Pulse1Sweep()
        {
            pulse1SweepDivider--;
            if (pulse1SweepReload)
            {
                pulse1SweepReload = false;
                pulse1SweepDivider = (byte)(pulse1SweepTimer + 1);
            }
            if (pulse1SweepDivider == 0)
            {
                int tmp = pulse1Timer >> pulse1SweepShift;
                if (pulse1SweepNegate)
                    tmp = (~tmp) & 0x7FF;
                tmp += pulse1Timer;
                if (pulse1SweepEnable && pulse1SweepShift != 0 && pulse1Timer >= 8 && tmp <= 0x7FF)
                {
                    pulse1Timer = (ushort)(tmp & 0x7FF);
                }
                pulse1SweepDivider = (byte)(pulse1SweepTimer + 1);

            }
        }
        public void Pulse2Envelope()
        {
            if (pulse2StartFlag)
            {
                pulse2StartFlag = false;
                pulse2EnvelopeCounter = 0xF;
                pulse2EnvelopeDivider = pulse2Envelope + 1;
            }
            else
            {
                pulse2EnvelopeDivider--;
                if (pulse2EnvelopeDivider == 0)
                {
                    if (pulse2EnvelopeCounter != 0)
                        pulse2EnvelopeCounter--;
                    else if (pulse2EnvelopeLoop)
                        pulse2EnvelopeCounter = 0xF;
                    pulse2EnvelopeDivider = pulse2Envelope + 1;
                }
            }
        }
        public void Pulse2Sweep()
        {
            pulse2SweepDivider--;
            if (pulse2SweepReload)
            {
                pulse2SweepReload = false;
                pulse2SweepDivider = (byte)(pulse2SweepTimer + 1);
            }
            if (pulse2SweepDivider == 0)
            {
                int tmp = pulse2Timer >> pulse2SweepShift;
                if (pulse2SweepNegate)
                    tmp = ((~tmp)+1) & 0x7FF;
                tmp += pulse2Timer;
                if (pulse2SweepEnable && pulse2SweepShift != 0 && pulse2Timer >= 8 && tmp <= 0x7FF)
                {
                    pulse2Timer = (ushort)(tmp & 0x7FF);
                }
                pulse2SweepDivider = (byte)(pulse2SweepTimer + 1);

            }
        }
        public void NoiseEnvelope()
        {
            if (noiseStartFlag)
            {
                noiseStartFlag = false;
                noiseEnvelopeCounter = 0xF;
                noiseEnvelopeDivider = noiseEnvelope + 1;
            }
            else
            {
                noiseEnvelopeDivider--;
                if (noiseEnvelopeDivider == 0)
                {
                    if (noiseEnvelopeCounter != 0)
                        noiseEnvelopeCounter--;
                    else if (noiseEnvelopeLoop)
                        noiseEnvelopeCounter = 0xF;
                    noiseEnvelopeDivider = noiseEnvelope + 1;
                }
            }
        }
        public void TriangleLength()
        {
            if (!triangleHaltFlag && triangleLengthCounter != 0)
                triangleLengthCounter--;
        }
        public void Pulse1Length()
        {
            if (!pulse1HaltFlag && pulse1LengthCounter != 0)
                pulse1LengthCounter--;
        }
        public void Pulse2Length()
        {
            if (!pulse2HaltFlag && pulse2LengthCounter != 0)
                pulse2LengthCounter--;
        }
        public void NoiseLength()
        {
            if (!noiseHaltFlag && noiseLengthCounter != 0)
                noiseLengthCounter--;
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
                        Pulse2Envelope();
                        NoiseEnvelope();
                        TriangleLinear();
                    }
                    else if (step == 1) //Envelopes + Triangle Linear Counter + Length Counters + Sweep Units
                    {
                        Pulse1Envelope();
                        Pulse1Length();
                        Pulse1Sweep();
                        Pulse2Envelope();
                        Pulse2Length();
                        Pulse2Sweep();
                        NoiseEnvelope();
                        NoiseLength();
                        TriangleLinear();
                        TriangleLength();
                    }
                    else if (step == 2) //Envelopes + Triangle Linear Counter
                    {
                        Pulse1Envelope();
                        Pulse2Envelope();
                        NoiseEnvelope();
                        TriangleLinear();
                    }
                    else if (step == 3) //Envelopes + Triangle Linear Counter + Length Counters + Sweep Units + Interrupt
                    {
                        Pulse1Envelope();
                        Pulse1Length();
                        Pulse1Sweep();
                        Pulse2Envelope();
                        Pulse2Length();
                        Pulse2Sweep();
                        NoiseEnvelope();
                        NoiseLength();
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
                        Pulse1Length();
                        Pulse1Sweep();
                        Pulse2Envelope();
                        Pulse2Length();
                        Pulse2Sweep();
                        NoiseEnvelope();
                        NoiseLength();
                        TriangleLinear();
                        TriangleLength();
                    }
                    else if (step == 1)//Envelopes + Triangle Linear Counter
                    {
                        Pulse1Envelope();
                        Pulse2Envelope();
                        NoiseEnvelope();
                        TriangleLinear();
                    }
                    else if (step == 2)//Envelopes + Triangle Linear Counter + Length Counters + Sweep Units
                    {
                        Pulse1Envelope();
                        Pulse1Length();
                        Pulse1Sweep();
                        Pulse2Envelope();
                        Pulse2Length();
                        Pulse2Sweep();
                        NoiseEnvelope();
                        NoiseLength();
                        TriangleLinear();
                        TriangleLength();
                    }
                    else if (step == 3)//Envelopes + Triangle Linear Counter
                    {
                        Pulse1Envelope();
                        Pulse2Envelope();
                        NoiseEnvelope();
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
            if (mute)
            {

                for (int updateCycle = lastUpdateCycle; updateCycle < cycles; updateCycle++)
                {
                    if (updateCycle % divider == 0)
                    {
                        outBytes[(outputPtr * 4) + 0] = 0;
                        outBytes[(outputPtr * 4) + 1] = 0;
                        outBytes[(outputPtr * 4) + 2] = 0;
                        outBytes[(outputPtr * 4) + 3] = 0;
                        outputPtr++;
                    }
                }
                lastUpdateCycle = cycles;
                return;
            }
            byte pulse1Volume = 0;
            if (pulse1LengthCounter == 0)
                pulse1Volume = 0;
            else if(!dutyCycles[pulse1Duty][pulse1DutySequencer % 8])
                pulse1Volume = 0;
            else if (pulse1ConstantVolume)
                pulse1Volume = pulse1Envelope;
            else
                pulse1Volume = pulse1EnvelopeCounter;

            byte pulse2Volume = 0;
            if (pulse2LengthCounter == 0)
                pulse2Volume = 0;
            else if (!dutyCycles[pulse2Duty][pulse2DutySequencer % 8])
                pulse2Volume = 0;
            else if (pulse2ConstantVolume)
                pulse2Volume = pulse2Envelope;
            else
                pulse2Volume = pulse2EnvelopeCounter;
            byte noiseVolume = 0;
            if ((noiseShiftReg & 1) == 0)
                noiseVolume = 0;
            else if (noiseLengthCounter == 0)
                noiseVolume = 0;
            else if (noiseConstantVolume)
                noiseVolume = noiseEnvelope;
            else
                noiseVolume = noiseEnvelopeCounter;

            byte triangleVolume = 0;
            if((triangleLengthCounter != 0 || triangleLinearCounter != 0))
                triangleVolume = triangleSequence[triangleSequenceCounter % 32];
            byte dmcVolume = 0;
            for (int updateCycle = lastUpdateCycle; updateCycle < cycles; updateCycle++)
            {
                triangleDivider--;
                if (triangleDivider == 0 && (triangleLengthCounter != 0 || triangleLinearCounter != 0))
                {
                    triangleSequenceCounter++;
                    triangleVolume = triangleSequence[triangleSequenceCounter % 32];
                    triangleDivider = triangleTimer + 1;
                }
                pulse1Divider--;
                if (pulse1Divider == 0)
                {
                    if (pulse1LengthCounter == 0)
                        pulse1Volume = 0;
                    else if (!dutyCycles[pulse1Duty][pulse1DutySequencer % 8])
                        pulse1Volume = 0;
                    else if (pulse1ConstantVolume)
                        pulse1Volume = pulse1Envelope;
                    else
                        pulse1Volume = pulse1EnvelopeCounter;
                    pulse1DutySequencer++;
                    pulse1Divider = pulse1Timer + 1;
                }
                pulse2Divider--;
                if (pulse2Divider == 0)
                {
                    if (pulse2LengthCounter == 0)
                        pulse2Volume = 0;
                    else if (!dutyCycles[pulse2Duty][pulse2DutySequencer % 8])
                        pulse2Volume = 0;
                    else if (pulse2ConstantVolume)
                        pulse2Volume = pulse2Envelope;
                    else
                        pulse2Volume = pulse2EnvelopeCounter;
                    pulse2DutySequencer++;
                    pulse2Divider = pulse2Timer + 1;
                }
                noiseDivider--;
                if (noiseDivider == 0)
                {
                    NoiseClock();
                    if ((noiseShiftReg & 1) == 0)
                        noiseVolume = 0;
                    else if (noiseLengthCounter == 0)
                        noiseVolume = 0;
                    else if (noiseConstantVolume)
                        noiseVolume = noiseEnvelope;
                    else
                        noiseVolume = noiseEnvelopeCounter;
                    noiseDivider = noiseTimer;
                }
                if (updateCycle % divider == 0)
                {
                    //pulse1Volume = 8;
                    //pulse2Volume = 8;
                    //triangleVolume = 8;
                    //noiseVolume = 8;
                    dmcVolume = 8;
                    
                    output[outputPtr] = ((tndTable[(3 * triangleVolume) + (2 * noiseVolume) + dmcVolume] + pulseTable[pulse1Volume + pulse2Volume]) - 0.5f) * 2;
                    byte[] tmp = BitConverter.GetBytes(output[outputPtr]);
                    outBytes[(outputPtr * 4) + 0] = tmp[0];
                    outBytes[(outputPtr * 4) + 1] = tmp[1];
                    outBytes[(outputPtr * 4) + 2] = tmp[2];
                    outBytes[(outputPtr * 4) + 3] = tmp[3];
                    outputPtr++;
                }
            }
            lastUpdateCycle = cycles;
        }
    }
}