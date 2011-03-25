using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace DirectXEmu
{
    class ByteViewer : Panel
    {
        private byte[] oldData;
        private byte[] _data;
        private int fontHeight;
        private int fontWidth;
        public byte[] Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }
        public int StartAddress
        {
            get;
            set;
        }
        public int BytesPerLine
        {
            get;
            set;
        }
        public ByteViewer()
        {
            this.Paint += new PaintEventHandler(ByteViewer_Paint);
            Data = new byte[0x200];
            BytesPerLine = 0x10;
        }
        public void UpdateBytes()
        {
            if(oldData == null)
                oldData = new byte[Data.Length];
            Graphics gfx = this.CreateGraphics();
            fontWidth = TextRenderer.MeasureText("0x0000: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00", this.Font).Width / "0x0000: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00".Length;
            int byteStart = fontWidth * 7;
            int byteWidth = fontWidth * 3;//TextRenderer.MeasureText(gfx, "00", this.Font).Width;
            int addrWidth = fontWidth * 8;//TextRenderer.MeasureText(gfx, "0x0000:", this.Font).Width;
            for (int i = 0; i < Data.Length; i++)
            {
                if (Data[i] != oldData[i])
                {
                    oldData[i] = Data[i];
                    TextRenderer.DrawText(gfx, Data[i].ToString("X2"), this.Font, new Point(addrWidth + (byteWidth * (i % BytesPerLine)), this.fontHeight * (i / BytesPerLine)), this.ForeColor, this.BackColor);
                }
            }
        }
        public void ByteViewer_Paint(object sender, PaintEventArgs e)
        {
            if (oldData == null)
                oldData = new byte[Data.Length];
            this.fontHeight = TextRenderer.MeasureText(e.Graphics, "0", this.Font).Height;
            this.fontWidth = TextRenderer.MeasureText(e.Graphics, "0", this.Font).Width;
            string line = "";
            for (int i = 0; i < Data.Length; i++)
            {
                if (i % BytesPerLine == 0)
                {
                    if(i != 0)
                        TextRenderer.DrawText(e.Graphics, line, this.Font, new Point(0, this.fontHeight * ((i - 1) / BytesPerLine)), this.ForeColor);
                    line = "0x" + (i + StartAddress).ToString("X4") + ":";
                }
                oldData[i] = Data[i];
                line += " " + Data[i].ToString("X2");
            }
            TextRenderer.DrawText(e.Graphics, line, this.Font, new Point(0, this.fontHeight * ((Data.Length - 1) / BytesPerLine)), this.ForeColor);
        }
    }
}
