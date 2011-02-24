using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    interface IAudio
    {
        void Create();
        void Reset();
        void MainLoop(int samples, bool reverse);
        void SetVolume(float volume);
        bool SyncToAudio();
        void Destroy();
    }
}
