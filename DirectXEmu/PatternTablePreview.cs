using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DirectXEmu
{
    public partial class PatternTablePreview : Form
    {
        byte[][,] patternTables;
        public int generateLine;
        Bitmap patternTableBitmap;
        Panel[] palPanels = new Panel[32];
        public byte[][] palette;
        Color[] colorChart;
        private int selectedPal;
        public PatternTablePreview(Color[] colorChart, int generateLine)
        {
            this.selectedPal = 0;
            this.generateLine = generateLine;
            this.colorChart = colorChart;
            this.patternTableBitmap = new Bitmap(512, 256, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            for (int i = 0; i < 32; i++)
            {
                palPanels[i] = new Panel();
                palPanels[i].Size = new Size(32, 32);
                palPanels[i].Location = new Point((((i % 16)) * 32) + 12, ((i / 16) * 32) + 304);
                palPanels[i].Tag = i / 4;
                palPanels[i].Click += new EventHandler(PatternTablePreview_Click);
                this.Controls.Add(palPanels[i]);
            }
            InitializeComponent();
            this.txtScanline.Text = generateLine.ToString();
        }

        void PatternTablePreview_Click(object sender, EventArgs e)
        {
            this.selectedPal = (int)(((Panel)sender).Tag);
        }
        public void UpdatePatternTables(byte[][,] patternTables, byte[][] palette)
        {
            this.patternTables = patternTables;
            this.palette = palette;
            UpdateTables();
        }

        private void txtScanLine_TextChanged(object sender, EventArgs e)
        {

            for (int i = 0; i < txtScanline.Text.Length; i++)
            {
                if (txtScanline.Text[i] != '0' &&
                    txtScanline.Text[i] != '1' &&
                    txtScanline.Text[i] != '2' &&
                    txtScanline.Text[i] != '3' &&
                    txtScanline.Text[i] != '4' &&
                    txtScanline.Text[i] != '5' &&
                    txtScanline.Text[i] != '6' &&
                    txtScanline.Text[i] != '7' &&
                    txtScanline.Text[i] != '8' &&
                    txtScanline.Text[i] != '9')
                {
                    txtScanline.Text = txtScanline.Text.Remove(i, 1);
                    i--;
                    txtScanline.SelectionStart = txtScanline.Text.Length;
                    txtScanline.SelectionLength = 0;
                }
            }
            if (this.txtScanline.Text == "")
                this.generateLine = 0;
            else if (Convert.ToInt32(this.txtScanline.Text) > 260)
                this.generateLine = 260;
            else
                this.generateLine = Convert.ToInt32(this.txtScanline.Text);
        }
        private unsafe void UpdateTables()
        {
            if (this.patternTables != null)
            {
                for (int pal = 0; pal < 8; pal++)
                {
                    for (int index = 0; index < 4; index++)
                    {
                        palPanels[(pal * 4) + index].BackColor = this.colorChart[this.palette[pal][index]];
                    }
                }
                Color col0 = this.colorChart[this.palette[selectedPal][0]];
                Color col1 = this.colorChart[this.palette[selectedPal][1]];
                Color col2 = this.colorChart[this.palette[selectedPal][2]];
                Color col3 = this.colorChart[this.palette[selectedPal][3]];
                System.Drawing.Imaging.BitmapData bmd = patternTableBitmap.LockBits(new Rectangle(0, 0, 512, 256), System.Drawing.Imaging.ImageLockMode.ReadWrite, patternTableBitmap.PixelFormat);
                for (int t = 0; t < 2; t++)
                {
                    int tx = 0;
                    if (t == 1)
                        tx = 256;
                    for (int y = 0; y < 256; y++)
                    {
                        byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);
                        for (int x = 0; x < 256; x++)
                        {
                            switch (patternTables[t][x / 2, y / 2])
                            {
                                case 0:
                                    row[(x + tx) * 3] = col0.B;
                                    row[((x + tx) * 3) + 1] = col0.G;
                                    row[((x + tx) * 3) + 2] = col0.R;
                                    break;
                                case 1:
                                    row[(x + tx) * 3] = col1.B;
                                    row[((x + tx) * 3) + 1] = col1.G;
                                    row[((x + tx) * 3) + 2] = col1.R;
                                    break;
                                case 2:
                                    row[(x + tx) * 3] = col2.B;
                                    row[((x + tx) * 3) + 1] = col2.G;
                                    row[((x + tx) * 3) + 2] = col2.R;
                                    break;
                                case 3:
                                    row[(x + tx) * 3] = col3.B;
                                    row[((x + tx) * 3) + 1] = col3.G;
                                    row[((x + tx) * 3) + 2] = col3.R;
                                    break;
                            }
                        }
                    }
                }
                patternTableBitmap.UnlockBits(bmd);
                this.patternTableViewer.Image = patternTableBitmap;
            }
        }
    }
}
