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
    public partial class NameTablePreview : Form
    {
        byte[][,] nameTables;
        int generateLine;
        Bitmap nameTableBitmap;
        Color[] colorChart;
        public NameTablePreview(Color[] colorChart, int generateLine)
        {
            this.generateLine = generateLine;
            this.colorChart = colorChart;
            this.nameTableBitmap = new Bitmap(512, 480, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            InitializeComponent();
            this.txtScanline.Text = generateLine.ToString();
        }
        public int UpdateNameTables(byte[][,] nameTables)
        {
            this.nameTables = nameTables;
            UpdateTables();
            return generateLine;
        }
        private unsafe void UpdateTables()
        {
            if (this.nameTables != null)
            {
                System.Drawing.Imaging.BitmapData bmd = nameTableBitmap.LockBits(new Rectangle(0, 0, 512, 480), System.Drawing.Imaging.ImageLockMode.ReadWrite, nameTableBitmap.PixelFormat);
                for (int t = 0; t < 4; t++)
                {
                    int tx = 0;
                    int ty = 0;
                    if (t == 1)
                        tx = 256;
                    if (t == 2)
                        ty = 240;
                    if (t == 3)
                    {
                        tx = 256;
                        ty = 240;
                    }
                    for (int y = 0; y < 240; y++)
                    {
                        int* row = (int*)bmd.Scan0 + ((y + ty) * (bmd.Stride/4));
                        for (int x = 0; x < 256; x++)
                        {
                            row[(x + tx)] = this.colorChart[nameTables[t][x, y] & 0x3F].ToArgb();
                            if ((nameTables[t][x, y] & 0x80) != 0 && chkScrollLines.Checked)
                                row[(x + tx)] ^= -1;
                        }
                    }
                }
                nameTableBitmap.UnlockBits(bmd);
                this.nameTableView.Image = nameTableBitmap;
            }
        }
        private void txtScanline_TextChanged(object sender, EventArgs e)
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
    }
}
