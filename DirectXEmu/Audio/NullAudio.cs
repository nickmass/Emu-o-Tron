using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DirectXEmu
{
    class NullAudio : IAudio
    {
        private double samplesPerMS;
        private int lastSamples;
        private int thisFrame;
        private double remainder = 0;

        public NullAudio(int sampleRate)
        {
            samplesPerMS = sampleRate / 1000.0;
        }
        public void Create()
        {
        }

        public void Reset()
        {
        }

        public void MainLoop(int samples, bool reverse)
        {
            lastSamples = samples;
        }

        public void SetVolume(float volume)
        {
        }

        public bool SyncToAudio()
        {
            bool tooSlow = false;
            double preciseSleep = lastSamples / samplesPerMS;
            remainder += preciseSleep - Math.Floor(preciseSleep);
            int lastFrame = Environment.TickCount - thisFrame;
            int sleepTime = (int)Math.Floor(preciseSleep) - lastFrame + (int)Math.Floor(remainder);
            remainder -= Math.Floor(remainder);
            if (sleepTime > 0 && sleepTime < 1000) //Second case is just to prevent my bad code from locking up the CPU somehow
                Thread.Sleep(sleepTime);
            else if (sleepTime < 0)
                tooSlow = true;
            thisFrame = Environment.TickCount;
            return tooSlow;
        }

        public void Destroy()
        {
        }
    }
}
