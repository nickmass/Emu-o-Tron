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

        int volume;
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

        public DXSAudio(int sampleRate, short[] buffer, float volume, IntPtr handle)
        {
            this.buffer = buffer;
            this.volume = (int)Math.Floor(volume * 100);
            this.handle = handle;
            audioFormat = new WaveFormat();
            audioFormat.BitsPerSample = 16;
            audioFormat.Channels = 1;
            audioFormat.SamplesPerSecond = sampleRate;
            audioFormat.BlockAlignment = (short)(audioFormat.BitsPerSample * audioFormat.Channels / 8);
            audioFormat.AverageBytesPerSecond = (audioFormat.BitsPerSample / 8) * audioFormat.SamplesPerSecond;
            audioFormat.FormatTag = WaveFormatTag.Pcm;
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
            sBufferDescription.Flags = BufferFlags.GlobalFocus | BufferFlags.ControlVolume;
            sBufferDescription.Format = audioFormat;
            sBufferDescription.SizeInBytes = audioFormat.AverageBytesPerSecond * 3;
            sBuffer = new SecondarySoundBuffer(device, sBufferDescription);
            //sBuffer.Volume = volume;
            sBuffer.Play(0, PlayFlags.Looping);
        }

        public void MainLoop(int samples, bool reverse)
        {
            sBuffer.Write(buffer, 0, samples, lastWritePos, LockFlags.FromWriteCursor);
            lastLastWritePos = lastWritePos;
            lastWritePos = sBuffer.CurrentWritePosition;
            lastSamples = samples;
        }

        public void SetVolume(float volume) //Volume is measured in DB, don't bother handling it now.
        {
            //sBuffer.Volume = (int)Math.Floor(volume * 100);
        }

        public bool SyncToAudio() //Not syncing correctly :(
        {
            bool tooSlow = true;
            if (lastLastWritePos > lastWritePos && (sBuffer.CurrentPlayPosition > lastWritePos))//I think the buffer loops around to the other side, this should catch that
            {
                while ((lastLastWritePos + (lastSamples * 2)) - sBuffer.CurrentPlayPosition > (lastSamples * 2))
                {
                    Thread.Sleep(1);
                    tooSlow = false;
                }

            }
            else
            {
                while (lastWritePos - sBuffer.CurrentPlayPosition > (lastSamples * 2)) //Im assuming those positions are in bytes?
                {
                    Thread.Sleep(1);
                    tooSlow = false;
                }
            }
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