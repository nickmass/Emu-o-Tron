#if !NO_DX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SlimDX.XAudio2;
using SlimDX.Multimedia;
using System.Threading;

namespace DirectXEmu
{
    class XA2Audio : IAudio
    {
        XAudio2 device;
        MasteringVoice mVoice;
        SourceVoice sVoice;
        AudioBuffer audioBuffer;
        BinaryWriter audioWriter;
        WaveFormat audioFormat;
        float volume;
        short[] buffer;

        public XA2Audio(int sampleRate, short[] buffer, float volume)
        {
            this.buffer = buffer;
            this.volume = volume;
            audioFormat = new WaveFormat();
            audioFormat.BitsPerSample = 16;
            audioFormat.Channels = 1;
            audioFormat.SamplesPerSecond = sampleRate;
            audioFormat.BlockAlignment = (short)(audioFormat.BitsPerSample * audioFormat.Channels / 8);
            audioFormat.AverageBytesPerSecond = (audioFormat.BitsPerSample / 8) * audioFormat.SamplesPerSecond;
            audioFormat.FormatTag = WaveFormatTag.Pcm;
        }

        public void Create()
        {
            device = new XAudio2();
            mVoice = new MasteringVoice(device);
            Reset();
        }

        public void Reset()
        {
            if (sVoice != null)
            {
                sVoice.Stop();
                sVoice.Dispose();
            }
            if(audioWriter != null)
                audioWriter.Close();
            if(audioBuffer != null)
                audioBuffer.Dispose();
            sVoice = new SourceVoice(device, audioFormat, VoiceFlags.None);
            audioBuffer = new AudioBuffer();
            audioBuffer.AudioData = new MemoryStream();
            audioWriter = new BinaryWriter(audioBuffer.AudioData);
            mVoice.Volume = volume;
            sVoice.Start();
        }
        public void SetVolume(float volume)
        {
            this.volume = volume;
            mVoice.Volume = volume;
        }
        public void MainLoop(int samples, bool reverse)
        {
            audioWriter.BaseStream.SetLength(0);
            if (reverse)
            {
                for (int i = samples - 1; i >= 0; i--)
                    audioWriter.Write(buffer[i]);
            }
            else
            {
                for (int i = 0; i < samples; i++)
                    audioWriter.Write(buffer[i]);
            }
            audioWriter.BaseStream.Position = 0;
            audioBuffer.AudioBytes = samples * (audioFormat.BitsPerSample / 8);
            sVoice.SubmitSourceBuffer(audioBuffer);
        }
        public bool SyncToAudio()
        {
            bool tooSlow = false;
            if (sVoice.State.BuffersQueued <= 1)
                tooSlow = true;
            else
            {
                while (sVoice.State.BuffersQueued > 1) //Keep this set as 1 or prepare for clicking
                    Thread.Sleep(1);
            }
            return tooSlow;
        }
        public void Destroy()
        {
            if(sVoice != null)
                sVoice.Dispose();
            if(audioWriter != null)
                audioWriter.Close();
            if(audioBuffer != null)
                audioBuffer.Dispose();
            if(mVoice != null)
                mVoice.Dispose();
            if(device != null)
                device.Dispose();
        }
    }
}
#endif