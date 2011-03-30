using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace DirectXEmu
{
    class GDIRenderer : IRenderer
    {
        private uint[,] screen;
        private Control renderTarget;
        private IScaler imageScaler;
        private Bitmap texture;
        private BufferedGraphics renderGfx;
        private BufferedGraphicsContext bufferContex;
        private Point[] renderSize;
        private Bitmap charSheet;
        private Dictionary<char, int> charSheetSprites = new Dictionary<char, int>();
        private int charSize;
        private bool smoothOutput;

        public GDIRenderer(Control renderTarget, IScaler imageScaler, uint[,] screen, bool smooth)
        {
            this.renderTarget = renderTarget;
            this.imageScaler = imageScaler;
            this.screen = screen;
            this.smoothOutput = smooth;
        }
        public void Create()
        {
            LoadCharSheet();
            Reset();
        }

        public void SmoothOutput(bool smooth)
        {
            smoothOutput = smooth;
        }
        public void Reset()
        {
            bufferContex = BufferedGraphicsManager.Current;
            bufferContex.MaximumBuffer = renderTarget.Size;
            renderGfx = bufferContex.Allocate(renderTarget.CreateGraphics(), new Rectangle(0, 0, renderTarget.Width, renderTarget.Height));
            if (smoothOutput)
                renderGfx.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            else
                renderGfx.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            renderSize = new Point[3];
            renderSize[0] = new Point(0, 0);
            renderSize[1] = new Point(renderTarget.Width, 0);
            renderSize[2] = new Point(0, renderTarget.Height);
            texture = new Bitmap(imageScaler.ResizedX, imageScaler.ResizedY, PixelFormat.Format32bppArgb);
        }

        public void MainLoop(bool newScreen)
        {
            unsafe
            {
                if (newScreen)
                {
                    BitmapData bmd = texture.LockBits(new Rectangle(Point.Empty, texture.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    fixed (uint* screenPTR = screen)
                        imageScaler.PerformScale(screenPTR, (uint*)bmd.Scan0);
                    texture.UnlockBits(bmd);
                }
                renderGfx.Graphics.DrawImage(texture, renderSize);
                DrawMessageEvent(this, null);
                renderGfx.Render();
            }
        }

        public void Destroy()
        {
        }

        public void ChangeScaler(IScaler imageScaler)
        {
            this.imageScaler = imageScaler;
            Reset();
        }

        public void DrawMessage(string message, Anchor anchor, int xOffset, int yOffset)
        {
            message = message.ToLower();
            int realXOffset;
            int realYOffset;
            switch (anchor)
            {
                default:
                case Anchor.TopLeft:
                    realXOffset = (xOffset * charSize) + (charSize / 2);
                    realYOffset = (yOffset * charSize) + (charSize / 2);
                    break;
                case Anchor.BottomLeft:
                    realXOffset = (xOffset * charSize) + (charSize / 2);
                    realYOffset = renderTarget.Height - ((yOffset + 1) * charSize) - (charSize / 2);
                    break;
                case Anchor.TopRight:
                    realXOffset = renderTarget.Width - (message.Length * charSize) - (charSize / 2);
                    realYOffset = (yOffset * charSize) + (charSize / 2);
                    break;
                case Anchor.BottomRight:
                    realXOffset = renderTarget.Width - (message.Length * charSize) - (charSize / 2);
                    realYOffset = renderTarget.Height - ((yOffset + 1) * charSize) - (charSize / 2);
                    break;
            }

            for (int i = 0; i < message.Length; i++)
            {
                if (charSheetSprites.ContainsKey(message[i]))
                {
                    int charNum = charSheetSprites[message[i]];
                    int charX = (charNum % charSize) * charSize;
                    int charY = (charNum / charSize) * charSize;
                    renderGfx.Graphics.DrawImage(charSheet, new Rectangle(realXOffset + (i * charSize), realYOffset, charSize, charSize), new Rectangle(charX, charY, charSize, charSize), GraphicsUnit.Pixel);
                }
            }
        }

        private void LoadCharSheet()
        {
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            Stream file = thisExe.GetManifestResourceStream("DirectXEmu.images.charSheet.png");
            charSheet = (Bitmap)Bitmap.FromStream(file);
            file.Close();
            charSize = 16;
            charSheetSprites[' '] = 0;
            charSheetSprites['0'] = 1;
            charSheetSprites['1'] = 2;
            charSheetSprites['2'] = 3;
            charSheetSprites['3'] = 4;
            charSheetSprites['4'] = 5;
            charSheetSprites['5'] = 6;
            charSheetSprites['6'] = 7;
            charSheetSprites['7'] = 8;
            charSheetSprites['8'] = 9;
            charSheetSprites['9'] = 10;
            charSheetSprites['a'] = 11;
            charSheetSprites['b'] = 12;
            charSheetSprites['c'] = 13;
            charSheetSprites['d'] = 14;
            charSheetSprites['e'] = 15;
            charSheetSprites['f'] = 16;
            charSheetSprites['g'] = 17;
            charSheetSprites['h'] = 18;
            charSheetSprites['i'] = 19;
            charSheetSprites['j'] = 20;
            charSheetSprites['k'] = 21;
            charSheetSprites['l'] = 22;
            charSheetSprites['m'] = 23;
            charSheetSprites['n'] = 24;
            charSheetSprites['o'] = 25;
            charSheetSprites['p'] = 26;
            charSheetSprites['q'] = 27;
            charSheetSprites['r'] = 28;
            charSheetSprites['s'] = 29;
            charSheetSprites['t'] = 30;
            charSheetSprites['u'] = 31;
            charSheetSprites['v'] = 32;
            charSheetSprites['w'] = 33;
            charSheetSprites['x'] = 34;
            charSheetSprites['y'] = 35;
            charSheetSprites['z'] = 36;
            charSheetSprites['='] = 37;
            charSheetSprites['<'] = 38;
            charSheetSprites['^'] = 39;
            charSheetSprites['>'] = 40;
            charSheetSprites['_'] = 41;
            charSheetSprites['*'] = 42;
            charSheetSprites['&'] = 43;
            charSheetSprites['$'] = 44;
            charSheetSprites['.'] = 45;
            charSheetSprites[':'] = 46;
        }

        public event EventHandler DrawMessageEvent;
    }
}
