using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron
{
    public class APU
    {
        NESCore nes;

        public int CPUClock;
        public int FPS;
        public int curFPS;

        public bool mute;
        public bool turbo;

        public int sampleRate;
        private double sampleRateDivider;
        private double sampleDivider;

        public SoundVolume volume;

        private int cycles;
        private int lastUpdateCycle;
        private int lastCycleClock;
        public bool frameIRQ;
        private int frameCounter;
        private int timeToClock;
        private int modeZeroDelay;
        private int[] modeZeroFrameLengths;
        private int modeOneDelay;
        private int[] modeOneFrameLengths;
        private bool mode;
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
        private bool pulse1SweepMute;
        private int pulse1Freq;


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
        private bool pulse2SweepMute;
        private int pulse2Freq;

        private bool[][] dutyCycles = {new bool[] {false, true, false, false, false, false, false, false},
                                       new bool[] {false, true, true, false, false, false, false, false}, 
                                       new bool[] {false, true, true, true, true, false, false, false}, 
                                       new bool[] {true, false, false, true, true, true, true, true}};

        private byte[] lengthTable = { 10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14, 12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30 };

        private bool noiseEnable;
        private byte noiseLengthCounter;
        private ushort[] noisePeriods;

        private ushort noiseShiftReg = 1;
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
        private byte triangleSequenceCounter;
        private bool triangleEnable;
        private bool triangleControlFlag;
        private bool triangleHaltFlag;
        private byte triangleLinearCounterReload;
        private byte triangleLinearCounter;
        private byte triangleLengthCounter;
        private ushort triangleTimer;
        private int triangleDivider;
        private int triangleFreq;

        public bool dmcInterrupt;
        private bool dmcInterruptEnable;
        private bool dmcLoop;
        private int dmcRate;
        private int dmcDivider;
        private int dmcSampleAddress;
        private int dmcSampleCurrentAddress;
        private int dmcSampleLength;
        private byte dmcDeltaCounter;
        private int dmcBytesRemaining;
        private byte dmcSampleBuffer;
        private bool dmcSampleBufferEmpty;
        private int dmcShiftCount;
        private byte dmcShiftReg;
        private int[] dmcRates;
        private double dmcSampleRateDivider;

        private double[] pulseTable = new double[32];
        private double[] tndTable = new double[204];
        private int[] pulseTableShort = new int[32];
        private int[] tndTableShort = new int[204];

        public short[] output;
        public int outputPtr;
        private byte[] dmcBuffer;
        private int dmcPtr;
        private int dmcOutputPtr;

        byte pulse1Volume;
        byte pulse2Volume;
        byte triangleVolume;
        byte noiseVolume;
        int dmcVolume;

        double aveSample;
        long aveSampleCount;

        int sampleTotal;
        int sampleCount;

        int dmcSampleTotal;
        int dmcSampleCount;

        public APU(NESCore nes, int sampleRate)
        {
            this.nes = nes;
            this.sampleRate = sampleRate;
            switch (nes.nesRegion)
            {
                default:
                case SystemType.NTSC:
                    CPUClock = 1789773;
                    FPS = 60;
                    modeZeroDelay = 7459;//http://nesdev.parodius.com/bbs/viewtopic.php?p=64281#64281
                    modeZeroFrameLengths = new int[] { 7456, 7458, 7458, 7458};
                    modeOneDelay = 1;
                    modeOneFrameLengths = new int[] { 7458, 7456, 7458, 7458, 7452};
                    noisePeriods = new ushort[] { 4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068 };
                    dmcRates = new int[] { 428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54 };
                    break;
                case SystemType.PAL:
                    CPUClock = 1662607;
                    FPS = 50;
                    modeZeroDelay = 8315;
                    modeZeroFrameLengths = new int[] { 8314, 8312, 8314, 8314 };
                    modeOneDelay = 1;
                    modeOneFrameLengths = new int[] { 8314, 8314, 8312, 8314, 8312 };
                    noisePeriods = new ushort[] { 4, 7, 14, 30, 60, 88, 118, 148, 188, 236, 354, 472, 708, 944, 1890, 3778 };
                    dmcRates = new int[] { 398, 354, 316, 298, 276, 236, 210, 198, 176, 148, 132, 118, 98, 78, 66, 50 };
                    break;
            }
            if (this.sampleRate == -1)
                this.sampleRate = CPUClock;
            SetFPS(FPS);
            output = new short[this.sampleRate]; //the buffers really don't need to be this large, but it should prevent overflows when the FPS is set exceptionally low.
            dmcBuffer = new byte[CPUClock];
            for (int i = 0; i < 32; i++)
            {
                pulseTable[i] = ((95.52 / (8128.0 / i + 100)));
                pulseTableShort[i] = (int)(pulseTable[i] * 32767);
            }
            for (int i = 0; i < 204; i++)
            {
                tndTable[i] = ((163.67 / (24329.0 / i + 100)));
                tndTableShort[i] = (int)(tndTable[i] * 32767);
            }
            volume.triangle = 1;
            volume.pulse1 = 1;
            volume.pulse2 = 1;
            volume.noise = 1;
            volume.dmc = 1;
        }
        public void Power() //4017_written.nes gives different results on initial load vs. power emulation, MUST fix.
        {
            frameIRQ = false;
            dmcSampleBufferEmpty = true;
            Write(00, 0x4000); //Start-up values
            Write(00, 0x4001);
            Write(00, 0x4002);
            Write(00, 0x4003);
            Write(00, 0x4004);
            Write(00, 0x4005);
            Write(00, 0x4006);
            Write(00, 0x4007);
            Write(00, 0x4008);
            Write(00, 0x4009);
            Write(00, 0x400A);
            Write(00, 0x400B);
            Write(00, 0x400C);
            Write(00, 0x400D);
            Write(00, 0x400E);
            Write(00, 0x400F);
            Write(00, 0x4010);
            Write(00, 0x4011);
            Write(00, 0x4012);
            Write(00, 0x4013);
            Write(00, 0x4015);
            Write(00, 0x4017);
            timeToClock = modeZeroDelay - 12;
            pulse1EnvelopeCounter = 0xF;
            pulse1EnvelopeDivider = pulse1Envelope + 1;
            pulse2EnvelopeCounter = 0xF;
            pulse2EnvelopeDivider = pulse2Envelope + 1;
            noiseEnvelopeCounter = 0xF;
            noiseEnvelopeDivider = noiseEnvelope + 1;
            if (nes.nsfPlayer)
            {
                Write(00, 0x4000); //Start-up values
                Write(00, 0x4001);
                Write(00, 0x4002);
                Write(00, 0x4003);
                Write(00, 0x4004);
                Write(00, 0x4005);
                Write(00, 0x4006);
                Write(00, 0x4007);
                Write(00, 0x4008);
                Write(00, 0x4009);
                Write(00, 0x400A);
                Write(00, 0x400B);
                Write(00, 0x400C);
                Write(00, 0x400D);
                Write(00, 0x400E);
                Write(00, 0x400F);
                Write(0x10, 0x4010);
                Write(00, 0x4011);
                Write(00, 0x4012);
                Write(00, 0x4013);
                Write(0x0F, 0x4015);
            }
        }
        public void Reset()
        {
            Write(00, 0x4015);
            Update();
            if (frameIRQInhibit)
                frameIRQ = false;
            frameCounter = 0;
            lastCycleClock = cycles;
            if (mode)
                timeToClock = modeOneDelay;
            else
                timeToClock = modeZeroDelay;
            if (cycles % 2 == 0) //jitter, apu_test 4-jitter.nes
                timeToClock++;
            timeToClock -= 12;
            frameIRQ = false;
        }
        public void SetFPS(int FPS)
        {
            curFPS = FPS;
            sampleDivider = (CPUClock / ((double)this.FPS)) / ((sampleRate * 1.0) / ((double)FPS));
        }
        public byte Read()
        {
            byte nextByte = 0;
            if (pulse1LengthCounter != 0)
                nextByte |= 0x1;
            if (pulse2LengthCounter != 0)
                nextByte |= 0x2;
            if (triangleLengthCounter != 0)
                nextByte |= 0x4;
            if (noiseLengthCounter != 0)
                nextByte |= 0x8;
            if (dmcBytesRemaining != 0)
                nextByte |= 0x10;
            if (frameIRQ)
                nextByte |= 0x40;
            if (dmcInterrupt)
                nextByte |= 0x80;
            frameIRQ = false;
            return nextByte;
        }
        public void Write(byte value, ushort address)
        {
            switch (address)
            {
                case 0x4000: //Pulse 1 Duty
                    Update();
                    pulse1Envelope = (byte)(value & 0xF);
                    pulse1ConstantVolume = (value & 0x10) != 0;
                    pulse1HaltFlag = (value & 0x20) != 0;
                    pulse1Duty = (byte)(value >> 6);
                    break;
                case 0x4001: //Pulse 1 Sweep
                    Update();
                    pulse1SweepShift = (byte)(value & 0x7);
                    pulse1SweepNegate = ((value & 0x8) != 0);
                    pulse1SweepTimer = (byte)((value >> 4) & 0x7);
                    pulse1SweepDivider = (byte)(pulse1SweepTimer + 1);
                    pulse1SweepEnable = ((value & 0x80) != 0);
                    pulse1SweepReload = true;
                    break;
                case 0x4002: //Pulse 1 Low Timer
                    Update();
                    pulse1Timer = (ushort)((pulse1Timer & 0x700) | value);
                    pulse1Freq = (pulse1Timer + 1) * 2;
                    pulse1Divider = pulse1Freq;
                    break;
                case 0x4003: //Pulse 1 Length Counter and High Timer
                    Update();
                    if (pulse1Enable)
                        pulse1LengthCounter = lengthTable[value >> 3];
                    pulse1Timer = (ushort)((pulse1Timer & 0x00FF) | ((value & 0x7) << 8));
                    pulse1Freq = (pulse1Timer + 1) * 2;
                    pulse1Divider = pulse1Freq;
                    pulse1DutySequencer = 0;
                    pulse1StartFlag = true;
                    break;
                case 0x4004: //Pulse 2 Duty
                    Update();
                    pulse2Envelope = (byte)(value & 0xF);
                    pulse2ConstantVolume = (value & 0x10) != 0;
                    pulse2HaltFlag = (value & 0x20) != 0;
                    pulse2Duty = (byte)(value >> 6);
                    break;
                case 0x4005: //Pulse 2 Sweep
                    Update();
                    pulse2SweepShift = (byte)(value & 0x7);
                    pulse2SweepNegate = ((value & 0x8) != 0);
                    pulse2SweepTimer = (byte)((value >> 4) & 0x7);
                    pulse2SweepDivider = (byte)(pulse2SweepTimer + 1);
                    pulse2SweepEnable = ((value & 0x80) != 0);
                    pulse2SweepReload = true;
                    break;
                case 0x4006: //Pulse 2 Low Timer
                    Update();
                    pulse2Timer = (ushort)((pulse2Timer & 0x700) | value);
                    pulse2Freq = (pulse2Timer + 1) * 2;
                    pulse2Divider = pulse2Freq;
                    break;
                case 0x4007: //Pulse 2 Length Counter and High Timer
                    Update();
                    if (pulse2Enable)
                        pulse2LengthCounter = lengthTable[(value >> 3)];
                    pulse2Timer = (ushort)((pulse2Timer & 0x00FF) | ((value & 0x7) << 8));
                    pulse2Freq = (pulse2Timer + 1) * 2;
                    pulse2Divider = pulse2Freq;
                    pulse2DutySequencer = 0;
                    pulse2StartFlag = true;
                    break;
                case 0x4008: //Triangle Linear Counter
                    Update();
                    triangleControlFlag = (value & 0x80) != 0;
                    triangleLinearCounterReload = (byte)(value & 0x7F);
                    break;
                case 0x400A: //Triangle Low Timer
                    Update();
                    triangleTimer = (ushort)((triangleTimer & 0x0700) | value);
                    triangleFreq = (triangleTimer + 1);
                    triangleDivider = triangleFreq;
                    break;
                case 0x400B: //Triangle Length Counter and High Timer
                    Update();
                    if (triangleEnable)
                        triangleLengthCounter = lengthTable[(value >> 3)];
                    triangleTimer = (ushort)((triangleTimer & 0x00FF) | ((value & 0x7) << 8));
                    triangleFreq = (triangleTimer + 1);
                    triangleDivider = triangleFreq;
                    triangleHaltFlag = true;
                    break;
                case 0x400C: //Noise Envelope
                    Update();
                    noiseEnvelope = (byte)(value & 0xF);
                    noiseEnvelopeDivider = noiseEnvelope + 1;
                    noiseConstantVolume = ((value & 0x10) != 0);
                    noiseHaltFlag = (value & 0x20) != 0;
                    break;
                case 0x400E: //Noise Timer
                    Update();
                    noiseTimer = noisePeriods[value & 0xF];
                    noiseDivider = noiseTimer;
                    noiseLoop = (value & 0x80) != 0;
                    break;
                case 0x400F: //Noise Length Counter
                    Update();
                    if (noiseEnable)
                        noiseLengthCounter = lengthTable[(value >> 3)];
                    noiseStartFlag = true;
                    break;
                case 0x4010: //DMC Flags and Freq
                    Update();
                    dmcRate = dmcRates[value & 0xF];
                    dmcDivider = dmcRate;
                    dmcLoop = (value & 0x40) != 0;
                    dmcInterruptEnable = (value & 0x80) != 0;
                    if (!dmcInterruptEnable)
                        dmcInterrupt = false;
                    break;
                case 0x4011: //DMC Direct Load
                    Update();
                    dmcDeltaCounter = (byte)(value & 0x7F);
                    break;
                case 0x4012: //DMC Sample Address
                    Update();
                    dmcSampleAddress = 0xC000 | (value << 6);
                    break;
                case 0x4013: //DMC Sample Length
                    Update();
                    dmcSampleLength = (value << 4) | 1;
                    break;
                case 0x4015:
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
                    if ((value & 0x10) == 0)
                        dmcBytesRemaining = 0;
                    else if (dmcBytesRemaining == 0)
                    {
                        dmcSampleCurrentAddress = dmcSampleAddress;
                        dmcBytesRemaining = dmcSampleLength;
                    }
                    dmcInterrupt = false;
                    break;
                case 0x4017://APU Frame rate/ IRQ control
                    Update();
                    mode = (value & 0x80) != 0;
                    frameIRQInhibit = (value & 0x40) != 0;
                    if (frameIRQInhibit)
                        frameIRQ = false;
                    frameCounter = 0;
                    lastCycleClock = cycles;
                    if (mode)
                        timeToClock = modeOneDelay;
                    else
                        timeToClock = modeZeroDelay;
                    if (cycles % 2 == 0) //jitter, apu_test 4-jitter.nes
                        timeToClock++;
                    break;
            }
        }
        public void ResetBuffer()
        {
            outputPtr = 0;
            dmcPtr = 0;
            dmcOutputPtr = 0;
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
        public void TriangleLength()
        {
            if (!triangleHaltFlag && triangleLengthCounter != 0)
                triangleLengthCounter--;
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
                    else if (pulse1HaltFlag)
                        pulse1EnvelopeCounter = 0xF;
                    pulse1EnvelopeDivider = pulse1Envelope + 1;
                }
            }
        }
        public void PulseSweepMute()
        {
            pulse1SweepMute = pulse1Timer < 8 || ((!pulse1SweepNegate) && (pulse1Timer + (pulse1Timer >> pulse1SweepShift)) > 0x7FF);
            pulse2SweepMute = pulse2Timer < 8 || ((!pulse2SweepNegate) && (pulse2Timer + (pulse2Timer >> pulse2SweepShift)) > 0x7FF);
        }
        public void Pulse1Sweep()
        {
            pulse1SweepDivider--;
            if (pulse1SweepDivider == 0)
            {
                int tmp = pulse1Timer >> pulse1SweepShift;
                if (pulse1SweepNegate)
                    tmp = ~tmp;
                tmp += pulse1Timer;
                if (pulse1SweepEnable && pulse1SweepShift != 0 && !pulse1SweepMute)
                {
                    pulse1Timer = (ushort)(tmp & 0x7FF);
                    pulse1Freq = (pulse1Timer + 1) * 2;
                }
                pulse1SweepDivider = (byte)(pulse1SweepTimer + 1);

            }
            if (pulse1SweepReload)
            {
                pulse1SweepReload = false;
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
                    else if (pulse2HaltFlag)
                        pulse2EnvelopeCounter = 0xF;
                    pulse2EnvelopeDivider = pulse2Envelope + 1;
                }
            }
        }
        public void Pulse2Sweep()
        {
            pulse2SweepDivider--;
            if (pulse2SweepDivider == 0)
            {
                int tmp = pulse2Timer >> pulse2SweepShift;
                if (pulse2SweepNegate)
                    tmp = 0 - tmp;
                tmp += pulse2Timer;
                if (pulse2SweepEnable && pulse2SweepShift != 0 && !pulse2SweepMute)
                {
                    pulse2Timer = (ushort)(tmp & 0x7FF);
                    pulse2Freq = (pulse2Timer + 1) * 2;
                }
                pulse2SweepDivider = (byte)(pulse2SweepTimer + 1);
            }
            if (pulse2SweepReload)
            {
                pulse2SweepReload = false;
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
                    else if (noiseHaltFlag)
                        noiseEnvelopeCounter = 0xF;
                    noiseEnvelopeDivider = noiseEnvelope + 1;
                }
            }
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
            for (int i = 0; i < cycles; i++ )
                DMCOutput();
            if (this.cycles - lastCycleClock >= timeToClock)
            {
                lastCycleClock += timeToClock;
                Update();
                if (!mode) //Mode 0
                {
                    timeToClock = modeZeroFrameLengths[frameCounter % 4];
                    int step = frameCounter % 4;
                    frameCounter++;
                    if (step == 0) //Envelopes + Triangle Linear Counter
                    {
                        Pulse1Envelope();
                        Pulse2Envelope();
                        NoiseEnvelope();
                        TriangleLinear();
                    }
                    else if (step == 1) //Envelopes + Triangle Linear Counter + Length Counters + Sweep Units
                    {
                        PulseSweepMute();
                        Pulse1Sweep();
                        Pulse2Sweep();
                        Pulse1Envelope();
                        Pulse2Envelope();
                        NoiseEnvelope();
                        TriangleLinear();
                        Pulse1Length();
                        Pulse2Length();
                        NoiseLength();
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
                        PulseSweepMute();
                        Pulse1Sweep();
                        Pulse2Sweep();
                        Pulse1Envelope();
                        Pulse2Envelope();
                        NoiseEnvelope();
                        TriangleLinear();
                        Pulse1Length();
                        Pulse2Length();
                        NoiseLength();
                        TriangleLength();
                        if (!frameIRQInhibit)
                            frameIRQ = true;

                    }
                }
                else //Mode 1
                {
                    timeToClock = modeOneFrameLengths[frameCounter % 5];
                    int step = frameCounter % 5;
                    frameCounter++;
                    if (step == 0)//Envelopes + Triangle Linear Counter + Length Counters + Sweep Units
                    {
                        PulseSweepMute();
                        Pulse1Sweep();
                        Pulse2Sweep();
                        Pulse1Envelope();
                        Pulse2Envelope();
                        NoiseEnvelope();
                        TriangleLinear();
                        Pulse1Length();
                        Pulse2Length();
                        NoiseLength();
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
                        PulseSweepMute();
                        Pulse1Sweep();
                        Pulse2Sweep();
                        Pulse1Envelope();
                        Pulse2Envelope();
                        NoiseEnvelope();
                        TriangleLinear();
                        Pulse1Length();
                        Pulse2Length();
                        NoiseLength();
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
        private bool dmcDelay;
        private void DMCOutput()
        {
            if (dmcSampleBufferEmpty && !dmcDelay)
            {
                if (dmcBytesRemaining != 0)
                {
                    dmcSampleBuffer = nes.Memory[dmcSampleCurrentAddress];
                    dmcSampleBufferEmpty = false;
                    dmcDelay = true;
                    nes.AddCycles(4);
                    dmcDelay = false;
                    dmcSampleCurrentAddress++;
                    if (dmcSampleCurrentAddress > 0xFFFF)
                        dmcSampleCurrentAddress = 0x8000;
                    dmcBytesRemaining--;
                    if (dmcBytesRemaining == 0)
                    {
                        if (dmcLoop)
                        {
                            dmcBytesRemaining = dmcSampleLength;
                            dmcSampleCurrentAddress = dmcSampleAddress;
                        }
                        else if (dmcInterruptEnable)
                            dmcInterrupt = true;
                    }
                }
            }
            dmcDivider--;
            if (dmcDivider == 0)
            {
                if (dmcShiftCount > 0)
                {
                    if ((dmcShiftReg & 1) != 0 && dmcDeltaCounter > 1)
                        dmcDeltaCounter -= 2;
                    else if ((dmcShiftReg & 1) == 0 && dmcDeltaCounter < 126)
                        dmcDeltaCounter += 2;
                    dmcShiftReg >>= 1;
                    dmcShiftCount--;
                }
                else if (!dmcSampleBufferEmpty)
                {
                    dmcShiftReg = dmcSampleBuffer;
                    dmcSampleBufferEmpty = true;
                    dmcShiftCount = 8;
                    if ((dmcShiftReg & 1) != 0 && dmcDeltaCounter > 1)
                        dmcDeltaCounter -= 2;
                    else if ((dmcShiftReg & 1) == 0 && dmcDeltaCounter < 126)
                        dmcDeltaCounter += 2;
                    dmcShiftReg >>= 1;
                    dmcShiftCount--;
                }
                dmcDivider = dmcRate;
            }
            dmcBuffer[dmcPtr++] = dmcDeltaCounter;
        }
        public void Update()
        {
            if (mute || turbo)
            {
                for (int updateCycle = lastUpdateCycle; updateCycle < cycles; updateCycle++)
                {
                    sampleRateDivider--;
                    if (sampleRateDivider <= 0)
                    {
                        output[outputPtr] = 0;
                        outputPtr++;
                        sampleRateDivider += sampleDivider;
                    }
                }
                lastUpdateCycle = cycles;
                return;
            }
            PulseSweepMute();
            for (int updateCycle = lastUpdateCycle; updateCycle < cycles; updateCycle++)
            {
                triangleDivider--;
                if (triangleDivider == 0)
                {
                    if (triangleTimer <= 1) //Filter ultra-high freq. channel
                        triangleVolume = 7;
                    else
                        triangleVolume = triangleSequence[triangleSequenceCounter % 32];
                    if (triangleLengthCounter != 0 && triangleLinearCounter != 0)
                        triangleSequenceCounter++;
                    triangleDivider = triangleFreq;
                }
                pulse1Divider--;
                if (pulse1Divider == 0)
                {
                    if (pulse1LengthCounter == 0 || pulse1SweepMute || !dutyCycles[pulse1Duty][pulse1DutySequencer % 8])
                        pulse1Volume = 0;
                    else if (pulse1ConstantVolume)
                        pulse1Volume = pulse1Envelope;
                    else
                        pulse1Volume = pulse1EnvelopeCounter;
                    pulse1DutySequencer++;
                    pulse1Divider = pulse1Freq;
                }
                pulse2Divider--;
                if (pulse2Divider == 0)
                {
                    if (pulse2LengthCounter == 0 || pulse2SweepMute || !dutyCycles[pulse2Duty][pulse2DutySequencer % 8])
                        pulse2Volume = 0;
                    else if (pulse2ConstantVolume)
                        pulse2Volume = pulse2Envelope;
                    else
                        pulse2Volume = pulse2EnvelopeCounter;
                    pulse2DutySequencer++;
                    pulse2Divider = pulse2Freq;
                }
                noiseDivider--;
                if (noiseDivider == 0)
                {
                    if ((noiseShiftReg & 1) == 1 || noiseLengthCounter == 0)
                        noiseVolume = 0;
                    else if (noiseConstantVolume)
                        noiseVolume = noiseEnvelope;
                    else
                        noiseVolume = noiseEnvelopeCounter;
                    NoiseClock();
                    noiseDivider = noiseTimer;
                }
                triangleVolume = (byte)(triangleVolume * volume.triangle);
                pulse1Volume = (byte)(pulse1Volume * volume.pulse1);
                pulse2Volume = (byte)(pulse2Volume * volume.pulse2);
                noiseVolume = (byte)(noiseVolume * volume.noise);
                dmcVolume = (byte)(dmcBuffer[dmcOutputPtr++] * volume.dmc);
                sampleTotal += (short)(tndTableShort[(3 * triangleVolume) + (2 * noiseVolume) + dmcVolume] + pulseTableShort[pulse1Volume + pulse2Volume]);
                sampleCount++;
                sampleRateDivider--;
                if (sampleRateDivider <= 0) //&& outputPtr < output.Length)
                {
                    output[outputPtr++] = (short)((sampleTotal / (sampleCount * 1.0)) - aveSample);
                    sampleRateDivider += sampleDivider;
                    aveSample += (output[outputPtr] / ((++aveSampleCount) * 1.0)); //Attempt at centering waveform to reduce clicks, this is probably a horrible thing to do but I really cant hear any negative effects.
                    sampleTotal = 0;
                    sampleCount = 0;
                }
            }
            lastUpdateCycle = cycles;
        }
        public void StateSave(BinaryWriter writer)
        {
            writer.Write(cycles);
            writer.Write(lastUpdateCycle);
            writer.Write(lastCycleClock);
            writer.Write(frameIRQ);
            writer.Write(frameCounter);
            writer.Write(mode);
            writer.Write(frameIRQInhibit);
            writer.Write(pulse1Enable);
            writer.Write(pulse1LengthCounter);
            writer.Write(pulse1DutySequencer);
            writer.Write(pulse1Duty);
            writer.Write(pulse1HaltFlag);
            writer.Write(pulse1Envelope);
            writer.Write(pulse1EnvelopeCounter);
            writer.Write(pulse1EnvelopeDivider);
            writer.Write(pulse1ConstantVolume);
            writer.Write(pulse1Timer);
            writer.Write(pulse1StartFlag);
            writer.Write(pulse1EnvelopeLoop);
            writer.Write(pulse1Divider);
            writer.Write(pulse1SweepEnable);
            writer.Write(pulse1SweepTimer);
            writer.Write(pulse1SweepDivider);
            writer.Write(pulse1SweepNegate);
            writer.Write(pulse1SweepReload);
            writer.Write(pulse1SweepShift);
            writer.Write(pulse1SweepMute);
            writer.Write(pulse1Freq);
            writer.Write(pulse2Enable);
            writer.Write(pulse2LengthCounter);
            writer.Write(pulse2DutySequencer);
            writer.Write(pulse2Duty);
            writer.Write(pulse2HaltFlag);
            writer.Write(pulse2Envelope);
            writer.Write(pulse2EnvelopeCounter);
            writer.Write(pulse2EnvelopeDivider);
            writer.Write(pulse2ConstantVolume);
            writer.Write(pulse2Timer);
            writer.Write(pulse2StartFlag);
            writer.Write(pulse2EnvelopeLoop);
            writer.Write(pulse2Divider);
            writer.Write(pulse2SweepEnable);
            writer.Write(pulse2SweepTimer);
            writer.Write(pulse2SweepDivider);
            writer.Write(pulse2SweepNegate);
            writer.Write(pulse2SweepReload);
            writer.Write(pulse2SweepShift);
            writer.Write(pulse2SweepMute);
            writer.Write(pulse2Freq);
            writer.Write(noiseEnable);
            writer.Write(noiseLengthCounter);
            writer.Write(noiseShiftReg);
            writer.Write(noiseLoop);
            writer.Write(noiseHaltFlag);
            writer.Write(noiseEnvelope);
            writer.Write(noiseEnvelopeCounter);
            writer.Write(noiseEnvelopeDivider);
            writer.Write(noiseConstantVolume);
            writer.Write(noiseTimer);
            writer.Write(noiseStartFlag);
            writer.Write(noiseEnvelopeLoop);
            writer.Write(noiseDivider);
            writer.Write(triangleSequenceCounter);
            writer.Write(triangleEnable);
            writer.Write(triangleControlFlag);
            writer.Write(triangleHaltFlag);
            writer.Write(triangleLinearCounterReload);
            writer.Write(triangleLinearCounter);
            writer.Write(triangleLengthCounter);
            writer.Write(triangleTimer);
            writer.Write(triangleDivider);
            writer.Write(triangleFreq);
            writer.Write(dmcInterrupt);
            writer.Write(dmcInterruptEnable);
            writer.Write(dmcLoop);
            writer.Write(dmcRate);
            writer.Write(dmcDivider);
            writer.Write(dmcSampleAddress);
            writer.Write(dmcSampleCurrentAddress);
            writer.Write(dmcSampleLength);
            writer.Write(dmcDeltaCounter);
            writer.Write(dmcBytesRemaining);
            writer.Write(dmcSampleBuffer);
            writer.Write(dmcSampleBufferEmpty);
            writer.Write(dmcShiftCount);
            writer.Write(dmcShiftReg);
            writer.Write(timeToClock);
        }
        public void StateLoad(BinaryReader reader)
        {
            cycles = reader.ReadInt32();
            lastUpdateCycle = reader.ReadInt32();
            lastCycleClock = reader.ReadInt32();
            frameIRQ = reader.ReadBoolean();
            frameCounter = reader.ReadInt32();
            mode = reader.ReadBoolean();
            frameIRQInhibit = reader.ReadBoolean();
            pulse1Enable = reader.ReadBoolean();
            pulse1LengthCounter = reader.ReadByte();
            pulse1DutySequencer = reader.ReadInt32();
            pulse1Duty = reader.ReadByte();
            pulse1HaltFlag = reader.ReadBoolean();
            pulse1Envelope = reader.ReadByte();
            pulse1EnvelopeCounter = reader.ReadByte();
            pulse1EnvelopeDivider = reader.ReadInt32();
            pulse1ConstantVolume = reader.ReadBoolean();
            pulse1Timer = reader.ReadUInt16();
            pulse1StartFlag = reader.ReadBoolean();
            pulse1EnvelopeLoop = reader.ReadBoolean();
            pulse1Divider = reader.ReadInt32();
            pulse1SweepEnable = reader.ReadBoolean();
            pulse1SweepTimer = reader.ReadByte();
            pulse1SweepDivider = reader.ReadByte();
            pulse1SweepNegate = reader.ReadBoolean();
            pulse1SweepReload = reader.ReadBoolean();
            pulse1SweepShift = reader.ReadByte();
            pulse1SweepMute = reader.ReadBoolean();
            pulse1Freq = reader.ReadInt32();
            pulse2Enable = reader.ReadBoolean();
            pulse2LengthCounter = reader.ReadByte();
            pulse2DutySequencer = reader.ReadInt32();
            pulse2Duty = reader.ReadByte();
            pulse2HaltFlag = reader.ReadBoolean();
            pulse2Envelope = reader.ReadByte();
            pulse2EnvelopeCounter = reader.ReadByte();
            pulse2EnvelopeDivider = reader.ReadInt32();
            pulse2ConstantVolume = reader.ReadBoolean();
            pulse2Timer = reader.ReadUInt16();
            pulse2StartFlag = reader.ReadBoolean();
            pulse2EnvelopeLoop = reader.ReadBoolean();
            pulse2Divider = reader.ReadInt32();
            pulse2SweepEnable = reader.ReadBoolean();
            pulse2SweepTimer = reader.ReadByte();
            pulse2SweepDivider = reader.ReadByte();
            pulse2SweepNegate = reader.ReadBoolean();
            pulse2SweepReload = reader.ReadBoolean();
            pulse2SweepShift = reader.ReadByte();
            pulse2SweepMute = reader.ReadBoolean();
            pulse2Freq = reader.ReadInt32();
            noiseEnable = reader.ReadBoolean();
            noiseLengthCounter = reader.ReadByte();
            noiseShiftReg = reader.ReadUInt16();
            noiseLoop = reader.ReadBoolean();
            noiseHaltFlag = reader.ReadBoolean();
            noiseEnvelope = reader.ReadByte();
            noiseEnvelopeCounter = reader.ReadByte();
            noiseEnvelopeDivider = reader.ReadInt32();
            noiseConstantVolume = reader.ReadBoolean();
            noiseTimer = reader.ReadUInt16();
            noiseStartFlag = reader.ReadBoolean();
            noiseEnvelopeLoop = reader.ReadBoolean();
            noiseDivider = reader.ReadInt32();
            triangleSequenceCounter = reader.ReadByte();
            triangleEnable = reader.ReadBoolean();
            triangleControlFlag = reader.ReadBoolean();
            triangleHaltFlag = reader.ReadBoolean();
            triangleLinearCounterReload = reader.ReadByte();
            triangleLinearCounter = reader.ReadByte();
            triangleLengthCounter = reader.ReadByte();
            triangleTimer = reader.ReadUInt16();
            triangleDivider = reader.ReadInt32();
            triangleFreq = reader.ReadInt32();
            dmcInterrupt = reader.ReadBoolean();
            dmcInterruptEnable = reader.ReadBoolean();
            dmcLoop = reader.ReadBoolean();
            dmcRate = reader.ReadInt32();
            dmcDivider = reader.ReadInt32();
            dmcSampleAddress = reader.ReadInt32();
            dmcSampleCurrentAddress = reader.ReadInt32();
            dmcSampleLength = reader.ReadInt32();
            dmcDeltaCounter = reader.ReadByte();
            dmcBytesRemaining = reader.ReadInt32();
            dmcSampleBuffer = reader.ReadByte();
            dmcSampleBufferEmpty = reader.ReadBoolean();
            dmcShiftCount = reader.ReadInt32();
            dmcShiftReg = reader.ReadByte();
            timeToClock = reader.ReadInt32();
        }
    }
}
