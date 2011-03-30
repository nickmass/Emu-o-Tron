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
        private float fontHeight;
        private float fontWidth;
        private SolidBrush textBrush;
        private SolidBrush testBrush;
        private SolidBrush backBrush;
        VScrollBar scrollBar;
        public byte[] Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                bool newData = false;
                if (oldData == null || oldData.Length != Data.Length)
                {
                    oldData = new byte[Data.Length];
                    scrollBar.Minimum = 0;
                    scrollBar.Maximum = (Data.Length - 1) / BytesPerLine;
                    newData = true;
                }
                Graphics gfx = this.CreateGraphics();
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                float byteWidth = fontWidth * 3;
                float addrWidth = fontWidth * 8;
                for (int i = 0; i + StartAddress < Data.Length && i < BytesPerPage; i++)
                {
                    if (Data[i + StartAddress] != oldData[i + StartAddress])
                    {
                        oldData[i + StartAddress] = Data[i + StartAddress];
                        float x = addrWidth + (byteWidth * (i % BytesPerLine));
                        float y = this.fontHeight * (i / BytesPerLine);
                        if (!newData)
                        {
                            gfx.FillRectangle(backBrush, x, y, byteWidth, fontHeight);
                            gfx.DrawString(Data[i + StartAddress].ToString("X2"), this.Font, this.testBrush, x, y);
                        }
                    }
                }
            }
        }
        public int StartAddress
        {
            get;
            set;
        }
        public int BytesPerPage
        {
            get;
            set;
        }
        public int LinesPerPage
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
            scrollBar = new VScrollBar();
            scrollBar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            scrollBar.Dock = DockStyle.Right;
            scrollBar.Visible = true;
            this.Controls.Add(scrollBar);
            this.MouseWheel += new MouseEventHandler(ByteViewer_MouseWheel);
            this.FontChanged += new EventHandler(ByteViewer_FontChanged);
            this.ForeColorChanged += new EventHandler(ByteViewer_ForeColorChanged);
            this.BackColorChanged += new EventHandler(ByteViewer_BackColorChanged);
            this.SizeChanged += new EventHandler(ByteViewer_SizeChanged);
            this.scrollBar.Scroll += new ScrollEventHandler(scrollBar_Scroll);
            this.ParentChanged += new EventHandler(ByteViewer_ParentChanged);
            ByteViewer_FontChanged(this, new EventArgs());
            ByteViewer_ForeColorChanged(this, new EventArgs());
            ByteViewer_BackColorChanged(this, new EventArgs());
            ByteViewer_SizeChanged(this, new EventArgs());
            BytesPerLine = 0x10;
        }

        void ByteViewer_ParentChanged(object sender, EventArgs e)
        {
            this.FindForm().MouseWheel += new MouseEventHandler(ByteViewer_MouseWheel);
        }

        void scrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            ScrollToAddress(e.NewValue * BytesPerLine);
        }

        void ByteViewer_SizeChanged(object sender, EventArgs e)
        {
            LinesPerPage = ((this.Size.Height - (this.Size.Height % FontHeight)) / (int)Math.Floor(this.fontHeight)) - 1;
            BytesPerPage = LinesPerPage * BytesPerLine;
        }
        void ScrollToAddress(int address)
        {
            if (address > Data.Length - BytesPerPage + 1)
                address = Data.Length - BytesPerPage + 1;
            if (address < 0)
                address = 0;
            address = address - (address % BytesPerLine);
            if (StartAddress != address)
            {
                StartAddress = address;
                DrawMemory();
            }
            scrollBar.Value = StartAddress / BytesPerLine;
        }
        void ByteViewer_BackColorChanged(object sender, EventArgs e)
        {
            backBrush = new SolidBrush(this.BackColor);
        }

        void ByteViewer_ForeColorChanged(object sender, EventArgs e)
        {
            textBrush = new SolidBrush(this.ForeColor);
            testBrush = new SolidBrush(Color.DarkRed);
        }

        void ByteViewer_FontChanged(object sender, EventArgs e)
        {
            Graphics gfx = this.CreateGraphics();
            gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            this.fontWidth = gfx.MeasureString("000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", this.Font).Width / "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000".Length;
            this.fontHeight = gfx.MeasureString("0", this.Font).Height;
        }

        void ByteViewer_MouseWheel(object sender, MouseEventArgs e)
        {
            ScrollToAddress(StartAddress - (SystemInformation.MouseWheelScrollLines * BytesPerLine * (e.Delta / 120)));
        }
        public void ByteViewer_Paint(object sender, PaintEventArgs e)
        {
            DrawMemory();
        }
        public void DrawMemory()
        {
            if (oldData == null || oldData.Length != Data.Length)
            {
                oldData = new byte[Data.Length];
                scrollBar.Minimum = 0;
                scrollBar.Maximum = (Data.Length - 1) / BytesPerLine;
            }
            Graphics gfx = this.CreateGraphics();
            gfx.FillRectangle(backBrush, 0, 0, this.Width, this.Height);
            gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            string line = "";
            int last = 0;
            for (int i = 0; i + StartAddress < Data.Length && i < BytesPerPage; i++)
            {
                if (i % BytesPerLine == 0)
                {
                    if (i != 0)
                        gfx.DrawString(line, this.Font, this.textBrush, 0, this.fontHeight * ((i - 1) / BytesPerLine));
                    line = "0x" + (i + StartAddress).ToString("X4") + ":";
                }
                oldData[i + StartAddress] = Data[i + StartAddress];
                line += " " + Data[i + StartAddress].ToString("X2");
                last = i;
            }
            gfx.DrawString(line, this.Font, this.textBrush, 0, this.fontHeight * ((last) / BytesPerLine));
        }
    }
}
