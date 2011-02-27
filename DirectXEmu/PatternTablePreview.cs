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
        Panel highlightTop;
        Panel highlightLeft;
        Panel highlightRight;
        Panel highlightBottom;
        public byte[][] palette;
        uint[] colorChart;
        private int selectedPal;
        public PatternTablePreview(uint[] colorChart, int generateLine)
        {
            this.selectedPal = 0;
            this.generateLine = generateLine;
            this.colorChart = colorChart;
            this.patternTableBitmap = new Bitmap(512, 256, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            highlightTop = new Panel();
            highlightTop.Size = new Size(128, 2);
            highlightTop.BackColor = Color.Red;
            highlightLeft = new Panel();
            highlightLeft.Size = new Size(2, 32);
            highlightLeft.BackColor = Color.Red;
            highlightRight = new Panel();
            highlightRight.Size = new Size(2, 32);
            highlightRight.BackColor = Color.Red;
            highlightBottom = new Panel();
            highlightBottom.Size = new Size(128, 2);
            highlightBottom.BackColor = Color.Red;
            this.Controls.Add(highlightTop);
            this.Controls.Add(highlightLeft);
            this.Controls.Add(highlightRight);
            this.Controls.Add(highlightBottom);
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
            highlightTop.Top = palPanels[0].Top;
            highlightTop.Left = palPanels[0].Left;
            highlightLeft.Top = palPanels[0].Top;
            highlightLeft.Left = palPanels[0].Left;
            highlightRight.Top = palPanels[0].Top;
            highlightRight.Left = palPanels[0].Left + 128 - 2;
            highlightBottom.Top = palPanels[0].Top + 32 - 2;
            highlightBottom.Left = palPanels[0].Left;
            this.txtScanline.Text = generateLine.ToString();
            //this.TransparencyKey = Color.Red;
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
                        palPanels[(pal * 4) + index].BackColor = Color.FromArgb((int)this.colorChart[this.palette[pal][index]]);
                        if (pal == selectedPal && index == 0 && !testPal.Checked)
                        {
                            highlightTop.Visible = highlightLeft.Visible = highlightRight.Visible = highlightBottom.Visible = true;
                            highlightTop.Top = palPanels[(pal * 4) + index].Top;
                            highlightTop.Left = palPanels[(pal * 4) + index].Left;
                            highlightLeft.Top = palPanels[(pal * 4) + index].Top;
                            highlightLeft.Left = palPanels[(pal * 4) + index].Left;
                            highlightRight.Top = palPanels[(pal * 4) + index].Top;
                            highlightRight.Left = palPanels[(pal * 4) + index].Left + 128 - 2;
                            highlightBottom.Top = palPanels[(pal * 4) + index].Top + 32 - 2;
                            highlightBottom.Left = palPanels[(pal * 4) + index].Left;
                        }
                    }
                }
                uint col0;
                uint col1;
                uint col2;
                uint col3;
                if (testPal.Checked)
                {

                    highlightTop.Visible = highlightLeft.Visible = highlightRight.Visible = highlightBottom.Visible = false;
                    col0 = this.colorChart[0x0F];
                    col1 = this.colorChart[0x36];
                    col2 = this.colorChart[0x26];
                    col3 = this.colorChart[0x06];
                }
                else
                {
                    col0 = this.colorChart[this.palette[selectedPal][0]];
                    col1 = this.colorChart[this.palette[selectedPal][1]];
                    col2 = this.colorChart[this.palette[selectedPal][2]];
                    col3 = this.colorChart[this.palette[selectedPal][3]];
                }
                System.Drawing.Imaging.BitmapData bmd = patternTableBitmap.LockBits(new Rectangle(0, 0, 512, 256), System.Drawing.Imaging.ImageLockMode.ReadWrite, patternTableBitmap.PixelFormat);
                for (int t = 0; t < 2; t++)
                {
                    int tx = 0;
                    if (t == 1)
                        tx = 256;
                    for (int y = 0; y < 256; y++)
                    {
                        uint* row = (uint*)bmd.Scan0 + (y * (bmd.Stride/4));
                        for (int x = 0; x < 256; x++)
                        {
                            switch (patternTables[t][x / 2, y / 2])
                            {
                                case 0:
                                    row[(x + tx)] = col0;
                                    break;
                                case 1:
                                    row[(x + tx)] = col1;
                                    break;
                                case 2:
                                    row[(x + tx)] = col2;
                                    break;
                                case 3:
                                    row[(x + tx)] = col3;
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
