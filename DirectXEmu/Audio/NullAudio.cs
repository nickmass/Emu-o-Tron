using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DirectXEmu
{
    class NullAudio : IAudio
    {
        private int samplesPerMS;
        private int lastSamples;
        private int lastFrame;
        private int thisFrame;
        double remainder = 0;
        public NullAudio(int sampleRate)
        {
            samplesPerMS = sampleRate / 1000;
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

        public bool SyncToAudio()//This has some timing issues but I can't see it right now, tends to run ~10 FPS too fast in both PAL and NTSC
        {
            double preciseSleep = ((lastSamples * 1.0) / (samplesPerMS * 1.0));
            remainder += preciseSleep - Math.Floor(preciseSleep);
            lastFrame = thisFrame;
            thisFrame = Environment.TickCount;
            int sleepTime = (lastSamples / samplesPerMS) - (thisFrame - lastFrame) + (int)Math.Floor(remainder);
            remainder -= Math.Floor(remainder);
            if (sleepTime > 0 && sleepTime < 1000) //Second case is just to prevent my bad code from locking up the CPU somehow
                Thread.Sleep(sleepTime);
            else if (sleepTime < 0)
                return true;
            return false;
        }

        public void Destroy()
        {
        }
    }
}
