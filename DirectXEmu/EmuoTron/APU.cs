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

        public byte[] lengthTable = { 10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14, 12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30 };

        public int CPUClock;
        public double FPS;
        public double curFPS;

        public bool mute;
        public bool turbo;

        public int sampleRate;
        private double sampleRateDivider;
        private double sampleDivider;

        public SoundVolume volume;

        private int currentTime;
        private int lastUpdateCycle;
        public bool frameIRQ;
        private int frameCounter;
        private int timeToClock;
        private int modeZeroDelay;
        private int[] modeZeroFrameLengths;
        private int modeOneDelay;
        private int[] modeOneFrameLengths;
        private bool mode;
        private bool frameIRQInhibit;

        private double[] pulseTable = new double[32];
        private double[] tndTable = new double[204];
        private int[] pulseTableShort = new int[32];
        private int[] tndTableShort = new int[204];

        public short[] output;
        public int outputPtr;
        private int dmcOutputPtr;

        private  double aveSample;
        private Queue<double> rollingAve = new Queue<double>();
        private long rollingAveCount;
        private long rollingAveWindow;
        private double rollingAveTotal;

        int sampleTotal;
        int sampleCount;

        private Channels.Square square1;
        private Channels.Square square2;
        private Channels.Triangle triangle;
        private Channels.Noise noise;
        public Channels.DMC dmc;
        public Channels.Channel external;

        public bool interruptAPU
        {
            get
            {
                return frameIRQ || dmc.interrupt;
            }
        }

        public APU(NESCore nes, int sampleRate)
        {
            this.nes = nes;
            this.sampleRate = sampleRate;
            switch (nes.nesRegion)
            {
                default:
                case SystemType.NTSC:
                    CPUClock = 1789773;
                    FPS = (CPUClock * 3.0) / 89341.5;
                    modeZeroDelay = 7459;//http://nesdev.parodius.com/bbs/viewtopic.php?p=64281#64281
                    modeZeroFrameLengths = new int[] { 7456, 7458, 7458, 7458};
                    modeOneDelay = 1;
                    modeOneFrameLengths = new int[] { 7458, 7456, 7458, 7458, 7452};
                    break;
                case SystemType.PAL:
                    CPUClock = 1662607;
                    FPS = (CPUClock * 3.2) / 106392;
                    modeZeroDelay = 8315;
                    modeZeroFrameLengths = new int[] { 8314, 8312, 8314, 8314 };
                    modeOneDelay = 1;
                    modeOneFrameLengths = new int[] { 8314, 8314, 8312, 8314, 8312 };
                    break;
            }
            if (this.sampleRate == -1)
                this.sampleRate = CPUClock;
            rollingAveWindow = this.sampleRate / 10;
            SetFPS(FPS);
            output = new short[this.sampleRate]; //the buffers really don't need to be this large, but it should prevent overflows when the FPS is set exceptionally low.
            for (int i = 0; i < 32; i++)
            {
                pulseTable[i] = ((95.52 / (8128.0 / i + 100.0)));
                pulseTableShort[i] = (int)(pulseTable[i] * 0x7FFF); //Half the range for internal channels, half for external.
            }
            for (int i = 0; i < 204; i++)
            {
                tndTable[i] = ((163.67 / (24329.0 / i + 100.0)));
                tndTableShort[i] = (int)(tndTable[i] * 0x7FFF);
            }
            volume.square1 = 1;
            volume.square2 = 1;
            volume.triangle = 1;
            volume.noise = 1;
            volume.dmc = 1;
            volume.external = 1;
            square1 = new Channels.Square(nes, true, true);
            square2 = new Channels.Square(nes, true, false);
            triangle = new Channels.Triangle(nes);
            noise = new Channels.Noise(nes);
            dmc = new Channels.DMC(nes, CPUClock);
            external = new Channels.External();
        }
        public void SetFPS(double FPS)
        {
            curFPS = FPS;
            sampleDivider = (CPUClock / ((double)this.FPS)) / ((sampleRate * 1.0) / ((double)FPS));
        }
        public void Power()
        {
            square1.Power();
            square2.Power();
            triangle.Power();
            noise.Power();
            dmc.Power();
            external.Power();
            mode = false;
            frameIRQInhibit = false;
            frameCounter = 0;
            timeToClock = modeZeroDelay - 10;
            frameIRQ = false;
            if (nes.nsfPlayer)
            {
                Write(0x10, 0x4010);
                Write(0x0F, 0x4015);
            }
            Update();
            currentTime = 0;
            lastUpdateCycle = 0;
        }
        public void Reset()
        {
            Update();
            square1.Reset();
            square2.Reset();
            triangle.Reset();
            noise.Reset();
            dmc.Reset();
            external.Reset();
            frameCounter = 0;
            if (mode)
                timeToClock = modeOneDelay;
            else
                timeToClock = modeZeroDelay;
            if (currentTime % 2 == 0) //jitter, apu_test 4-jitter.nes
                timeToClock++;
            timeToClock -= 12;
            frameIRQ = false;
        }
        public byte Read()
        {
            byte nextByte = 0;
            nextByte |= square1.Read(0, 0);
            nextByte |= (byte)(square2.Read(0, 0) << 1);
            nextByte |= (byte)(triangle.Read(0, 0) << 2);
            nextByte |= (byte)(noise.Read(0, 0) << 3);
            nextByte |= dmc.Read(0,0);
            if (frameIRQ)
                nextByte |= 0x40;
            frameIRQ = false;
            return nextByte;
        }
        public void Write(byte value, ushort address)
        {
            if (address >= 0x4000 && address <= 0x4017)
            {
                Update();
                switch (address & 0xFFFC)
                {
                    case 0x4000:
                        square1.Write(value, (ushort)(address & 0x3));
                        break;
                    case 0x4004:
                        square2.Write(value, (ushort)(address & 0x3));
                        break;
                    case 0x4008:
                        triangle.Write(value, (ushort)(address & 0x3));
                        break;
                    case 0x400C:
                        noise.Write(value, (ushort)(address & 0x3));
                        break;
                    case 0x4010:
                        dmc.Write(value, (ushort)(address & 0x3));
                        break;
                    default:
                        switch (address)
                        {
                            case 0x4015:
                                square1.Write((byte)(value & 1), 4);
                                square2.Write((byte)(value & 2), 4);
                                triangle.Write((byte)(value & 4), 4);
                                noise.Write((byte)(value & 8), 4);
                                dmc.Write((byte)(value & 0x10), 4);
                                break;
                            case 0x4017://APU Frame rate/ IRQ control
                                mode = (value & 0x80) != 0;
                                frameIRQInhibit = (value & 0x40) != 0;
                                if (frameIRQInhibit)
                                    frameIRQ = false;
                                frameCounter = 0;
                                if (mode)
                                    timeToClock = modeOneDelay;
                                else
                                    timeToClock = modeZeroDelay;
                                if (currentTime % 2 == 1) //jitter, apu_test 4-jitter.nes
                                    timeToClock++;
                                break;
                        }
                        break;
                }
            }
        }
        public void ResetBuffer()
        {
            outputPtr = 0;
            dmc.ptr = 0;
            dmcOutputPtr = 0;
#if DEBUGGER
            nes.debug.APUFrameReset();
#endif
        }
        private void QuarterFrame()
        {
            square1.QuarterFrame();
            square2.QuarterFrame();
            triangle.QuarterFrame();
            noise.QuarterFrame();
            external.QuarterFrame();
        }
        private void HalfFrame()
        {
            QuarterFrame(); //Quarter must be done first.
            square1.HalfFrame();
            square2.HalfFrame();
            triangle.HalfFrame();
            noise.HalfFrame();
            external.HalfFrame();
        }
        public void AddCycles(int cycles)
        {
            currentTime += cycles;
            for (int i = 0; i < cycles; i++)
                dmc.Cycle();

            timeToClock -= cycles;
            if (timeToClock <= 0)
            {
                Update();
                if (!mode) //Mode 0
                {
                    int step = frameCounter % 4;
                    timeToClock += modeZeroFrameLengths[step];
                    if (step == 0)
                        QuarterFrame();
                    else if (step == 1)
                        HalfFrame();
                    else if (step == 2)
                        QuarterFrame();
                    else if (step == 3)
                    {
                        HalfFrame();
                        if (!frameIRQInhibit)
                            frameIRQ = true;
                    }
                }
                else
                {
                    int step = frameCounter % 5;
                    timeToClock += modeOneFrameLengths[step];
                    if (step == 0)
                        HalfFrame();
                    else if (step == 1)
                        QuarterFrame();
                    else if (step == 2)
                        HalfFrame();
                    else if (step == 3)
                        QuarterFrame();
                }
                frameCounter++;
            }
        }
        public void Update()
        {
            if (mute || turbo)
            {
                for (int updateCycle = lastUpdateCycle; updateCycle < currentTime; updateCycle++)
                {
                    sampleRateDivider--;
                    if (sampleRateDivider <= 0)
                    {
                        output[outputPtr] = 0;
                        outputPtr++;
                        sampleRateDivider += sampleDivider;
                    }
                }
                lastUpdateCycle = currentTime;
                return;
            }
            for (int updateCycle = lastUpdateCycle; updateCycle < currentTime; updateCycle++)
            {
                byte square1Volume = square1.Cycle();
                byte square2Volume = square2.Cycle();
                byte triangleVolume = triangle.Cycle();
                byte noiseVolume = noise.Cycle();
                byte dmcVolume = dmc.buffer[dmcOutputPtr++];
                byte externalVolume = external.Cycle();//treating external volume as 0-255
#if DEBUGGER
                nes.debug.APUCycle(square1Volume, square2Volume, triangleVolume, noiseVolume, dmcVolume, externalVolume);
#endif
                square1Volume = (byte)(square1Volume * volume.square1);
                square2Volume = (byte)(square2Volume * volume.square2);
                triangleVolume = (byte)(triangleVolume * volume.triangle);
                noiseVolume = (byte)(noiseVolume * volume.noise);
                dmcVolume = (byte)(dmcVolume * volume.dmc);
                externalVolume = (byte)(externalVolume * volume.external);
                sampleTotal += (short)((tndTableShort[(3 * triangleVolume) + (2 * noiseVolume) + dmcVolume] + pulseTableShort[square1Volume + square2Volume] + (externalVolume << 7)) ^ 0x8000);//just inserting external sound linearly.
                sampleCount++;
                sampleRateDivider--;
                if (sampleRateDivider <= 0) //&& outputPtr < output.Length)
                {
                    double sample = sampleTotal / (sampleCount * 1.0);
                    sampleRateDivider += sampleDivider;
                    rollingAveTotal += sample;
                    rollingAve.Enqueue(sample);
                    if (rollingAveCount == rollingAveWindow)
                        rollingAveTotal -= rollingAve.Dequeue();
                    else
                        rollingAveCount++;
                    aveSample = rollingAveTotal / (rollingAveCount * 1.0); //aveSample reduces clicks by keeping waveform centered around 0, size of window can be adjusted with rollingAveWindow.
                    output[outputPtr++] = (short)(Math.Max(Math.Min(sample - aveSample, short.MaxValue), short.MinValue));//I can't imagine clamping the audio could possibly sound worse then an overflow so this is how I shall do things (even though this will rarely even have an effect).
                    sampleTotal = 0;
                    sampleCount = 0;
                }
            }
            lastUpdateCycle = currentTime;
        }
        public void StateSave(BinaryWriter writer)
        {
            writer.Write(currentTime);
            writer.Write(lastUpdateCycle);
            writer.Write(frameIRQ);
            writer.Write(frameCounter);
            writer.Write(mode);
            writer.Write(frameIRQInhibit);
            writer.Write(timeToClock);
            square1.StateSave(writer);
            square2.StateSave(writer);
            triangle.StateSave(writer);
            noise.StateSave(writer);
            dmc.StateSave(writer);
            external.StateSave(writer);
        }
        public void StateLoad(BinaryReader reader)
        {
            currentTime = reader.ReadInt32();
            lastUpdateCycle = reader.ReadInt32();
            frameIRQ = reader.ReadBoolean();
            frameCounter = reader.ReadInt32();
            mode = reader.ReadBoolean();
            frameIRQInhibit = reader.ReadBoolean();
            timeToClock = reader.ReadInt32();
            square1.StateLoad(reader);
            square2.StateLoad(reader);
            triangle.StateLoad(reader);
            noise.StateLoad(reader);
            dmc.StateLoad(reader);
            external.StateLoad(reader);
        }
    }
}
