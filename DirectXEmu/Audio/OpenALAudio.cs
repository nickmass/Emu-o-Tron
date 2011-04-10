using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK;
using System.Threading;

namespace DirectXEmu
{
    class OpenALAudio : IAudio
    {
        AudioContext AC;
        int sourceName;
        int sampleRate;
        short[] buffer;
        float volume;
        List<int> activeBuffers = new List<int>();

        bool tooSlow = false;
        int frame = 0;
        int frameDelay = 6; //Should also corrispond to the number of buffers created


        public OpenALAudio(int sampleRate, short[] buffer, float volume)
        {
            this.sampleRate = sampleRate;
            this.buffer = buffer;
            this.volume = volume;
        }

        #region IAudio Members

        public void Create()
        {
            Reset();
        }

        public void Reset()
        {
            AC = new AudioContext();
            sourceName = AL.GenSource();

            AL.Source(sourceName, ALSourcef.Gain, volume);
            Vector3 defaultDirection = new Vector3(0, 0, 0);
            Vector3 defaultPosition = new Vector3(0, 0, 0);
            Vector3 defaultVelocity = new Vector3(0, 0, 0);
            AL.Source(sourceName, ALSource3f.Position, ref defaultPosition);
            AL.Source(sourceName, ALSource3f.Velocity, ref defaultVelocity);
            AL.Source(sourceName, ALSource3f.Direction, ref defaultDirection);
            AL.Source(sourceName, ALSourcef.RolloffFactor, 0.0f);
            AL.Source(sourceName, ALSourceb.SourceRelative, true);
        }
        int processed = -1;
        public void MainLoop(int samples, bool reverse)
        {
            unsafe
            {
                if (reverse)
                {
                    short temp;
                    for (int i = 0; i < samples / 2; i++)
                    {
                        temp = buffer[i];
                        buffer[i] = buffer[samples - i];
                        buffer[samples - i] = temp;
                    }
                }
                int processedBuf = -1;
                if (processed > 0)
                {
                    AL.SourceUnqueueBuffers(sourceName, 1, ref processedBuf);
                }
                else
                {
                    processedBuf = AL.GenBuffer();
                    activeBuffers.Add(processedBuf);
                }
                
                fixed (short* bufferPtr = buffer)
                    AL.BufferData(processedBuf, ALFormat.Mono16, (IntPtr)bufferPtr, samples * 2, sampleRate);
                AL.SourceQueueBuffer(sourceName, processedBuf);

                if (AL.GetSourceState(sourceName) != ALSourceState.Playing)
                {
                    AL.SourcePlay(sourceName);
                    if (frame > frameDelay) //If the buffer runs out of stuff to play, frames are obviously being pushed too slow, but by the time that happens the audio will already be terrible so this isnt a good time to detect it.
                        tooSlow = true;
                }
                
            }
        }
        public void SetVolume(float volume)
        {
            AL.Source(sourceName, ALSourcef.Gain, volume);
        }
        private void CheckError(string msg)
        {
            ALError e = AL.GetError();
            if (e != ALError.NoError)
                System.Windows.Forms.MessageBox.Show(msg + AL.GetErrorString(e));
        }
        public bool SyncToAudio()//Need a better way of detecting slow frames, low priority
        {
            bool tooSlow = this.tooSlow;
            this.tooSlow = false;
            processed = -1;
            do
            {
                AL.GetSource(sourceName, ALGetSourcei.BuffersProcessed, out processed);
                Thread.Sleep(1);
            }
            while (processed < 1 && frame > frameDelay); //Second condition gives time for the set of buffers to be filled, plus a little extra for a margin of error.
            frame++;
            return tooSlow;
        }

        public void Destroy()
        {
            AL.SourceStop(sourceName);
            AL.Source(sourceName, ALSourcei.Buffer, 0);
            for (int i = 0; i < activeBuffers.Count; i++)
            {
                AL.DeleteBuffer(activeBuffers[i]);
            }
            AL.DeleteSource(sourceName);
            AC.Dispose();
        }

        #endregion
    }
}
