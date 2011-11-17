using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace DirectXEmu
{
    public partial class PaletteConfig : Form
    {
        EmuConfig config;
        uint[] palette;
        Graphics panelGFX;
        Bitmap buffer;
        bool change = true;
        public PaletteConfig(EmuConfig config, uint[] palette)
        {
            this.config = config;
            this.palette = palette;
            InitializeComponent();
            trkGamma.Value = (int)(double.Parse(config["gamma"]) * 10);
            trkSat.Value = (int)(double.Parse(config["saturation"]) * 10);
            trkHue.Value = (int)(double.Parse(config["hue"]) * 10);
            trkBrightness.Value = (int)(double.Parse(config["brightness"]) * 10);
            NTSCFilter.gamma = (trkGamma.Value / 10.0);
            NTSCFilter.sat = (trkSat.Value / 10.0);
            NTSCFilter.hue = (trkHue.Value / 10.0);
            NTSCFilter.brightness = (trkBrightness.Value / 10.0);
            txtPath.Text = config["palette"];
            openPalette.InitialDirectory = config["paletteDir"];
            panelGFX = panel1.CreateGraphics();
            buffer = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            if (config["simulatedPalette"] == "1")
            {
                radInternal.Checked = true;
                radExternal.Checked = false;
            }
            else
            {
                radInternal.Checked = false;
                radExternal.Checked = true;
                LoadExternal();
                UpdateSwatches();
            }
            btnDefaults.Enabled = tmrPalUpdate.Enabled = trkHue.Enabled = trkSat.Enabled = trkBrightness.Enabled = trkGamma.Enabled = radInternal.Checked;
            change = true;
            txtPath.Enabled = btnBrowse.Enabled = radExternal.Checked;
        }

        private unsafe void UpdateSwatches()
        {
            System.Drawing.Imaging.BitmapData bmd = buffer.LockBits(new Rectangle(0,0, 256, 256), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            uint* ptr = (uint*)bmd.Scan0;
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    int ySwap = (y / 32);
                    switch (ySwap)
                    {
                        case 0:
                            ySwap = 0;
                            break;
                        case 1:
                            ySwap = 2;
                            break;
                        case 2:
                            ySwap = 4;
                            break;
                        case 3:
                            ySwap = 6;
                            break;
                        case 4:
                            ySwap = 1;
                            break;
                        case 5:
                            ySwap = 3;
                            break;
                        case 6:
                            ySwap = 5;
                            break;
                        case 7:
                            ySwap = 7;
                            break;
                    }

                    int pal = (x / 32) + (ySwap * 8);


                    ptr[(y * 256) + x] = palette[pal];
                }
            }
            buffer.UnlockBits(bmd);
            panelGFX.DrawImageUnscaled(buffer, Point.Empty);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (radInternal.Checked)
            {
                config["simulatedPalette"] = "1";
                config["gamma"] = (trkGamma.Value / 10.0).ToString();
                config["saturation"] = (trkSat.Value / 10.0).ToString();
                config["hue"] = (trkHue.Value / 10.0).ToString();
                config["brightness"] = (trkBrightness.Value / 10.0).ToString();
            }
            else
            {
                config["simulatedPalette"] = "0";
                config["palette"] = txtPath.Text;
            }
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radInternal_CheckedChanged(object sender, EventArgs e)
        {
            btnDefaults.Enabled = tmrPalUpdate.Enabled = trkHue.Enabled = trkSat.Enabled = trkBrightness.Enabled = trkGamma.Enabled = radInternal.Checked;
            change = true;
        }

        private void radExternal_CheckedChanged(object sender, EventArgs e)
        {
            txtPath.Enabled = btnBrowse.Enabled = radExternal.Checked;
            if (radExternal.Checked)
            {
                LoadExternal();
                UpdateSwatches();
            }
        }

        private void trkGamma_Scroll(object sender, EventArgs e)
        {
            NTSCFilter.gamma = (trkGamma.Value / 10.0);
            change = true;
        }

        private void trkSat_Scroll(object sender, EventArgs e)
        {
            NTSCFilter.sat = (trkSat.Value / 10.0);
            change = true;
        }

        private void trkHue_Scroll(object sender, EventArgs e)
        {
            NTSCFilter.hue = (trkHue.Value / 10.0);
            change = true;
        }

        private void tmrPalUpdate_Tick(object sender, EventArgs e)
        {
            if (!change)
                return;
            for (int i = 0; i < 0x200; i++)
                palette[i] = NTSCFilter.NESToRGB(i);
            UpdateSwatches();
            change = false;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (openPalette.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = openPalette.FileName;
                LoadExternal();
                UpdateSwatches();
            }
        }
        private void LoadExternal()
        {
            FileStream palFile = File.OpenRead(txtPath.Text);
            for (int i = 0; i < 0x40; i++)
                palette[i] = (uint)((0xFF << 24) | (palFile.ReadByte() << 16) | (palFile.ReadByte() << 8) | palFile.ReadByte());
            if (palFile.Length > 0x40 * 3)
            {
                uint[] vsColor = new uint[0x200];
                for (int i = 0; palFile.Position < palFile.Length; i++)
                {
                    vsColor[i] = palette[palFile.ReadByte()];
                }
                palette = vsColor;
            }
            palFile.Close(); //not doing emph in here... yet.
            CreateEmphasisTables();
        }

        private void CreateEmphasisTables()
        {
            byte finalRed;
            byte finalGreen;
            byte finalBlue;
            double emphasis = 0.1;
            double demphasis = 0.25;
            for (int i = 1; i < 0x08; i++)
            {
                double blue = 1;
                double green = 1;
                double red = 1;
                if ((i & 0x01) != 0)
                {
                    red += emphasis;
                    green -= demphasis;
                    blue -= demphasis;
                }
                if ((i & 0x02) != 0)
                {
                    green += emphasis;
                    red -= demphasis;
                    blue -= demphasis;
                }
                if ((i & 0x04) != 0)
                {
                    blue += emphasis;
                    green -= demphasis;
                    red -= demphasis;
                }
                red = (red < 0) ? 0 : red;
                green = (green < 0) ? 0 : green;
                blue = (blue < 0) ? 0 : blue;
                for (int j = 0; j < 0x40; j++)
                {
                    finalRed = Math.Round(((palette[j] >> 16) & 0xFF) * red) > 0xFF ? (byte)0xFF : (byte)Math.Round(((palette[j] >> 16) & 0xFF) * red);
                    finalGreen = Math.Round(((palette[j] >> 8) & 0xFF) * green) > 0xFF ? (byte)0xFF : (byte)Math.Round(((palette[j] >> 8) & 0xFF) * green);
                    finalBlue = Math.Round(((palette[j]) & 0xFF) * blue) > 0xFF ? (byte)0xFF : (byte)Math.Round(((palette[j]) & 0xFF) * blue);
                    palette[j | (i << 6)] = (uint)((0xFF << 24) | (finalRed << 16) | (finalGreen << 8) | finalBlue);
                }
            }
        }

        private void btnDefaults_Click(object sender, EventArgs e)
        {
            config["gamma"] = config.defaults["gamma"];
            config["saturation"] = config.defaults["saturation"];
            config["hue"] = config.defaults["hue"];
            config["brightness"] = config.defaults["brightness"];
            trkGamma.Value = (int)(double.Parse(config["gamma"]) * 10);
            trkSat.Value = (int)(double.Parse(config["saturation"]) * 10);
            trkHue.Value = (int)(double.Parse(config["hue"]) * 10);
            trkBrightness.Value = (int)(double.Parse(config["brightness"]) * 10);
            NTSCFilter.gamma = (trkGamma.Value / 10.0);
            NTSCFilter.sat = (trkSat.Value / 10.0);
            NTSCFilter.hue = (trkHue.Value / 10.0);
            NTSCFilter.brightness = (trkBrightness.Value / 10.0);
            change = true;
        }

        private void PaletteConfig_Paint(object sender, PaintEventArgs e)
        {
            UpdateSwatches();
        }

        private void trkBrightness_Scroll(object sender, EventArgs e)
        {
            NTSCFilter.brightness = (trkBrightness.Value / 10.0);
            change = true;
        }
    }
}
