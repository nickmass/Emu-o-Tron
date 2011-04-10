using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;
using System.IO;


namespace DirectXEmu
{
    class OpenGLRenderer : IRenderer
    {

        private uint[,] screen;
        private uint[] scaledScreen;
        private Dictionary<char, int> charSheetSprites = new Dictionary<char, int>();
        private int charSize;
        private int sheetSize;
        private Control renderTarget;
        private IScaler imageScaler;
        private bool smoothOutput;
        private GLControl glControl;
        private int textureName;
        private int charTextureName;


        public OpenGLRenderer(Control renderTarget, IScaler imageScaler, uint[,] screen, bool smooth)
        {
            this.renderTarget = renderTarget;
            this.imageScaler = imageScaler;
            this.screen = screen;
            this.smoothOutput = smooth;
        }
        
        #region IRenderer Members

        public void Create()
        {
            Reset();
        }

        public void Reset()
        {
            if (glControl != null)
            {
                Destroy();
            }
            scaledScreen = new uint[imageScaler.ResizedX * imageScaler.ResizedY];
            glControl = new GLControl(new GraphicsMode());
            glControl.Location = Point.Empty;
            glControl.Size = renderTarget.Size;
            glControl.Visible = true;
            glControl.TabStop = false;
            glControl.Enabled = false;
            renderTarget.Controls.Add(glControl);
            renderTarget.FindForm().Show(); //Need this in here when running a rom from double click, may cause problems elsewhere?
            glControl.VSync = false;
            glControl.BringToFront();
            GL.ClearColor(Color.Black);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, renderTarget.Width, renderTarget.Height, 0, -1, 1); // Bottom-left corner pixel has coordinate (0, 0)
            GL.Viewport(0, 0, renderTarget.Width, renderTarget.Height); // Use all of the glControl painting area
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            textureName = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureName);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new int[] { (int)TextureWrapMode.Clamp });//Clamp to edges
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new int[] { (int)TextureWrapMode.Clamp });
            if (smoothOutput)
            {
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Linear });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMagFilter.Linear });
            }
            else
            {
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Nearest });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMagFilter.Nearest });
            }
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, new int[] { (int)TextureEnvMode.Modulate }); //Enables blending and lighting
            LoadCharSheet();
        }

        public void MainLoop(bool newScreen)
        {
            unsafe
            {
                GL.BindTexture(TextureTarget.Texture2D, textureName);
                fixed( uint* scaledPTR = scaledScreen)
                {
                    if (newScreen)
                    {
                        fixed (uint* screenPTR = screen)
                        {
                                imageScaler.PerformScale(screenPTR, scaledPTR);
                        }
                    }
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, imageScaler.ResizedX, imageScaler.ResizedY, 0, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)scaledPTR);
                }
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadIdentity();
                GL.Begin(BeginMode.Quads);
                GL.TexCoord2(0.0f, 0.0f);
                GL.Vertex3(0, 0, 1);
                GL.TexCoord2(1.0f, 0.0f);
                GL.Vertex3(renderTarget.Width, 0, 1);
                GL.TexCoord2(1.0f, 1.0f);
                GL.Vertex3(renderTarget.Width, renderTarget.Height, 1);
                GL.TexCoord2(0.0f, 1.0f);
                GL.Vertex3(0, renderTarget.Height, 1);
                GL.End();
                DrawMessageEvent(this, null);
                glControl.SwapBuffers();
            }
        }

        public void Destroy()
        {
            GL.DeleteTexture(textureName);
            GL.DeleteTexture(charTextureName);
            if (glControl != null)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                glControl.SwapBuffers();
                glControl.Visible = false;
                glControl.Dispose();
            }
        }

        public void SmoothOutput(bool smooth)
        {
            this.smoothOutput = smooth;
            GL.BindTexture(TextureTarget.Texture2D, textureName);
            if (smoothOutput)
            {
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Linear });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMagFilter.Linear });
            }
            else
            {
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Nearest });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMagFilter.Nearest });
            }
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
            GL.BindTexture(TextureTarget.Texture2D, charTextureName);
            GL.Begin(BeginMode.Quads);
            for (int i = 0; i < message.Length; i++)
            {
                if (charSheetSprites.ContainsKey(message[i]))
                {
                    int charNum = charSheetSprites[message[i]];

                    float texCharX = (charNum % charSize) * ((float)charSize / (float)sheetSize);
                    float texCharY = (charNum / charSize) * ((float)charSize / (float)sheetSize);
                    float texCharXEnd = ((charNum % charSize) + 1) * ((float)charSize / (float)sheetSize);
                    float texCharYEnd = ((charNum / charSize) + 1) * ((float)charSize / (float)sheetSize);

                    GL.TexCoord2(texCharX, texCharY);
                    GL.Vertex3(realXOffset + (i * charSize), realYOffset, 0);
                    GL.TexCoord2(texCharXEnd, texCharY);
                    GL.Vertex3(realXOffset + (i * charSize) + charSize, realYOffset, 0);
                    GL.TexCoord2(texCharXEnd, texCharYEnd);
                    GL.Vertex3(realXOffset + (i * charSize) + charSize, realYOffset + charSize, 0);
                    GL.TexCoord2(texCharX, texCharYEnd);
                    GL.Vertex3(realXOffset + (i * charSize), realYOffset + charSize, 0);
                }
            }
            GL.End();
        }
        private void LoadCharSheet()
        {
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            Stream file = thisExe.GetManifestResourceStream("DirectXEmu.images.charSheet.png");
            Bitmap charBitmap = (Bitmap)Bitmap.FromStream(file);
            file.Close();
            charTextureName = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, charTextureName);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new int[] { (int)TextureWrapMode.Clamp });//Clamp to edges
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new int[] { (int)TextureWrapMode.Clamp });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMagFilter.Nearest });
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, new int[] { (int)TextureEnvMode.Modulate }); //Enables blending and lighting
            unsafe
            {
                System.Drawing.Imaging.BitmapData bmd = charBitmap.LockBits(new Rectangle(0, 0, charBitmap.Width, charBitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmd.Width, bmd.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmd.Scan0);
                charBitmap.UnlockBits(bmd);
            }
            charSize = 16;
            sheetSize = charBitmap.Width;
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
        private static int Pow2RoundUp(int x) //Save this lovely code for later.
        {
            if (x < 0)
                return 0;
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }
        public event EventHandler DrawMessageEvent;

        #endregion
    }
}
