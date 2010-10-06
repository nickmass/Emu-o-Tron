using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EmuoTron;

namespace DirectXEmu
{
    public partial class SoundConfig : Form
    {
        public SoundConfig(SoundVolume volume)
        {
            InitializeComponent();
            this.soundVolume.Value = (int)(volume.master * 100);
            this.pulse1Volume.Value = (int)(volume.pulse1 * 100);
            this.pulse2Volume.Value = (int)(volume.pulse2 * 100);
            this.triangleVolume.Value = (int)(volume.triangle * 100);
            this.noiseVolume.Value = (int)(volume.noise * 100);
            this.dmcVolume.Value = (int)(volume.dmc * 100);
        }
    }
}
