#if !NO_DX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.DirectSound;
using SlimDX.Multimedia;
using System.Threading;

namespace DirectXEmu
{
    class DXSAudio : IAudio
    {

        WaveFormat audioFormat;

        float volume;
        IntPtr handle;
        short[] buffer;
        DirectSound device;
        PrimarySoundBuffer pBuffer;
        SecondarySoundBuffer sBuffer;
        SoundBufferDescription pBufferDescription;
        SoundBufferDescription sBufferDescription;
        int lastSamples;
        int lastWritePos;
        int lastLastWritePos;

        int bufferSize;

        private double samplesPerMS;
        private int thisFrame;
        private double remainder = 0;

        public DXSAudio(int sampleRate, short[] buffer, float volume, IntPtr handle)
        {
            this.buffer = buffer;
            this.volume = volume;
            this.handle = handle;
            audioFormat = new WaveFormat();
            audioFormat.BitsPerSample = 16;
            audioFormat.Channels = 1;
            audioFormat.SamplesPerSecond = sampleRate;
            audioFormat.BlockAlignment = (short)(audioFormat.BitsPerSample * audioFormat.Channels / 8);
            audioFormat.AverageBytesPerSecond = audioFormat.BlockAlignment * audioFormat.SamplesPerSecond;
            audioFormat.FormatTag = WaveFormatTag.Pcm;


            samplesPerMS = sampleRate / 1000.0;
        }
        #region IAudio Members

        public void Create()
        {
            device = new DirectSound();
            device.SetCooperativeLevel(handle, CooperativeLevel.Priority);
            pBufferDescription.Flags = BufferFlags.PrimaryBuffer;
            pBuffer = new PrimarySoundBuffer(device, pBufferDescription);
            pBuffer.Format = audioFormat;
            Reset();
        }

        public void Reset()
        {
            bufferSize = audioFormat.AverageBytesPerSecond / 10;
            sBufferDescription.Flags = BufferFlags.GlobalFocus | BufferFlags.ControlVolume | BufferFlags.GetCurrentPosition2;
            sBufferDescription.Format = audioFormat;
            sBufferDescription.SizeInBytes = bufferSize * 2;
            sBuffer = new SecondarySoundBuffer(device, sBufferDescription);
            sBuffer.Volume = FloatToDB(volume);
        }
        int bufferOffset = 0;
        bool flip;
        public void MainLoop(int samples, bool reverse)//Write cursor is basically useless, need to keep track of own write position and hope it lines up with playback. With proper Sync algo it should be acceptable.
        {
            short[] smallBuf = new short[samples];
            for (int i = 0; i < samples; i++)
            {
                smallBuf[i] = buffer[i];
            }
            if (bufferOffset + samples + 1 > bufferSize)
            {
                int spaceLeft = bufferSize - (bufferOffset + 1);
                sBuffer.Write(smallBuf, 0, spaceLeft, bufferOffset * 2, LockFlags.None);
                sBuffer.Write(smallBuf, spaceLeft, samples - spaceLeft, 0, LockFlags.None);
                bufferOffset = spaceLeft;
                flip = true;
            }
            else
            {
                sBuffer.Write(smallBuf, 0, samples, bufferOffset * 2, LockFlags.None);
                bufferOffset += samples;
                flip = false;
            }
            if(sBuffer.Status != (BufferStatus.Looping | BufferStatus.Playing))
                sBuffer.Play(0, PlayFlags.Looping);
            lastLastWritePos = lastWritePos;
            lastWritePos = sBuffer.CurrentWritePosition;
            lastSamples = samples;
        }

        public void SetVolume(float volume)
        {
            sBuffer.Volume = FloatToDB(volume);
        }
        private int FloatToDB(float volume)
        {
            if (volume <= 0.25)
            {
                return (int)((volume * 2.8 * 10000) - 10000);
            }
            else
            {
                double range = -1 * ((10000 * 0.7) - 10000);
                double offset = ((volume - 0.25) * (4 / 3)) * range;
                return (int)(offset - range);
            }
        }
        public bool SyncToAudio() //Not syncing correctly :( stealing null audio sync
        {
            bool tooSlow = false;
            double preciseSleep = lastSamples / samplesPerMS;
            remainder += preciseSleep - Math.Floor(preciseSleep);
            int lastFrame = Environment.TickCount - thisFrame;
            int sleepTime = (int)Math.Floor(preciseSleep) - lastFrame + (int)Math.Floor(remainder);
            remainder -= Math.Floor(remainder);
            if (sleepTime > 0 && sleepTime < 50) //Second case is just to prevent my bad code from locking things up
                Thread.Sleep(sleepTime);
            else if (sleepTime < 0)
                tooSlow = true;
            thisFrame = Environment.TickCount;
            return tooSlow;

        }

        public void Destroy()
        {
            if (sBuffer != null)
                sBuffer.Dispose();
            if (pBuffer != null)
                pBuffer.Dispose();
            if (device != null)
                device.Dispose();
        }

        #endregion
    }
}
#endif