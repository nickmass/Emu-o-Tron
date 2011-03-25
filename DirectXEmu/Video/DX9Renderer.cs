#if !NO_DX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.Multimedia;

namespace DirectXEmu
{
    class DX9Renderer : IRenderer
    {
        private uint[,] screen;
        private Control renderTarget;
        private IScaler imageScaler;
        private Device device;
        private Direct3D d3d;
        private Texture texture;
        private Dictionary<char, int> charSheetSprites = new Dictionary<char, int>();
        private Texture charSheet;
        private int charSize;
        private PresentParameters pps = new PresentParameters();
        private Sprite messageSprite;
        private VertexBuffer vertexBuffer;
        private bool smoothOutput = false;

        public event EventHandler DrawMessageEvent;

        public DX9Renderer(Control renderTarget, IScaler imageScaler, uint[,] screen, bool smooth)
        {
            this.renderTarget = renderTarget;
            this.imageScaler = imageScaler;
            this.screen = screen;
            this.smoothOutput = smooth;
        }
        public void SmoothOutput(bool smooth)
        {
            smoothOutput = smooth;
        }
        public void Create()
        {
            d3d = new Direct3D();
            pps.Windowed = true;
            pps.PresentationInterval = PresentInterval.Immediate;
            Reset();
        }
        public void ChangeScaler(IScaler imageScaler)
        {
            this.imageScaler = imageScaler;
            Reset();
        }
        public void Reset()
        {
            pps.BackBufferWidth = renderTarget.Width;
            pps.BackBufferHeight = renderTarget.Height;
            if (device != null)
                device.Dispose();
            if (texture != null)
                texture.Dispose();
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            device = new SlimDX.Direct3D9.Device(d3d, 0, SlimDX.Direct3D9.DeviceType.Hardware, renderTarget.Handle, CreateFlags.HardwareVertexProcessing, pps);
            texture = new Texture(device, imageScaler.ResizedX, imageScaler.ResizedY, 0, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            LoadCharSheet();
            vertexBuffer = new VertexBuffer(device, 6 * Marshal.SizeOf(typeof(Vertex)), Usage.WriteOnly, VertexFormat.PositionRhw | VertexFormat.Texture1, Pool.Managed);

            DataStream stream = vertexBuffer.Lock(0, 0, LockFlags.None);
            Vertex[] vertexData = new Vertex[6];

            vertexData[0].PositionRhw = new Vector4((float)renderTarget.Width + 0f, (float)renderTarget.Height, 0f, 1f);
            vertexData[0].Texture1 = new Vector2(1f, 1f);

            vertexData[1].PositionRhw = new Vector4(0f, (float)renderTarget.Height, 0f, 1f);
            vertexData[1].Texture1 = new Vector2(0f, 1f);

            vertexData[2].PositionRhw = new Vector4(0f, 0f, 0f, 1f);
            vertexData[2].Texture1 = new Vector2(0f, 0f);

            vertexData[3].PositionRhw = new Vector4(0f, 0f, 0f, 1f);
            vertexData[3].Texture1 = new Vector2(0f, 0f);

            vertexData[4].PositionRhw = new Vector4((float)renderTarget.Width + 0f, 0f, 0f, 1f);
            vertexData[4].Texture1 = new Vector2(1f, 0f);

            vertexData[5].PositionRhw = new Vector4((float)renderTarget.Width + 0f, (float)renderTarget.Height, 0f, 1f);
            vertexData[5].Texture1 = new Vector2(1f, 1f);

            stream.WriteRange(vertexData);
            vertexBuffer.Unlock();

            device.VertexFormat = VertexFormat.PositionRhw | VertexFormat.Texture1;
            device.SetStreamSource(0, vertexBuffer, 0, Marshal.SizeOf(typeof(Vertex)));
            if (smoothOutput)
                device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear);
            else
                device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Point);
            device.SetSamplerState(0, SamplerState.AddressU, TextureAddress.Clamp);
            device.SetSamplerState(0, SamplerState.AddressV, TextureAddress.Clamp);
            device.SetSamplerState(0, SamplerState.AddressW, TextureAddress.Clamp);
            device.SetTexture(0, texture);
        }

        public void MainLoop(bool newScreen)
        {
            if (device != null)
            {
                try
                {
                    if (newScreen)
                    {
                        unsafe
                        {
                            DataRectangle drt = texture.LockRectangle(0, LockFlags.Discard);
                            fixed (uint* screenPTR = screen)
                            {
                                imageScaler.PerformScale(screenPTR, (uint*)drt.Data.DataPointer);
                            }
                            texture.UnlockRectangle(0);
                        }
                    }
                    device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
                    device.BeginScene();
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
                    DrawMessageEvent(this, null);
                    device.EndScene();
                    device.Present();
                }
                catch (Direct3D9Exception e)
                {
                    if (e.ResultCode == SlimDX.Direct3D9.ResultCode.DeviceLost)
                        Create();
                }
                catch
                {
                    throw;
                }
            }
        }

        public void Destroy()
        {
            if (texture != null)
                texture.Dispose();
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            if (messageSprite != null)
                messageSprite.Dispose();
            if (charSheet != null)
                charSheet.Dispose();
            if (device != null)
                device.Dispose();
            if (d3d != null)
                d3d.Dispose();
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
            messageSprite.Begin(SpriteFlags.AlphaBlend);
            for (int i = 0; i < message.Length; i++)
            {
                if (charSheetSprites.ContainsKey(message[i]))
                {
                    int charNum = charSheetSprites[message[i]];
                    int charX = (charNum % charSize) * charSize;
                    int charY = (charNum / charSize) * charSize;
                    Rectangle rect = new Rectangle(charX, charY, charSize, charSize);
                    messageSprite.Transform = Matrix.Translation(realXOffset + (i * charSize), realYOffset, 0);
                    messageSprite.Draw(charSheet, rect, new SlimDX.Color4(Color.White));
                }
            }
            messageSprite.End();
        }
        private void LoadCharSheet()
        {
            if (messageSprite != null)
                messageSprite.Dispose();
            if (charSheet != null)
                charSheet.Dispose();
            messageSprite = new Sprite(device);
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            Stream file = thisExe.GetManifestResourceStream("DirectXEmu.images.charSheet.png");
            charSheet = Texture.FromStream(device, file);
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
        private struct Vertex
        {
            public Vector4 PositionRhw;
            public Vector2 Texture1;
        }
    }
}

#endif