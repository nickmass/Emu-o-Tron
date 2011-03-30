#if !NO_DX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SlimDX;
using SlimDX.Direct3D10;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using SlimDX.Multimedia;
using System.Windows.Forms;
using System.Runtime.InteropServices;



namespace DirectXEmu
{
    class DX10Renderer : IRenderer
    {
        private struct Vertex
        {
            public Vector3 pos;
            public Vector2 texCoord;
        }
        private uint[,] screen;
        private Control renderTarget;
        private IScaler imageScaler;
        private bool smoothOutput = false;
        private SlimDX.Direct3D10.Device device;
        private SlimDX.Direct3D10.Buffer vertexBuffer;
        private Texture2D texture;
        private RenderTargetView renderTargetView;
        private SwapChain swapChain;
        private Viewport viewport;
        private EffectPass pass;
        private Effect effect;
        private ShaderResourceView textureView;
        private InputLayout inputLayout;

        private Dictionary<char, int> charSheetSprites = new Dictionary<char, int>();
        private Texture2D charSheet;
        private int charSize;
        private Sprite messageSprite;
        private ShaderResourceView spriteView;
        BlendStateDescription bsd;

        #region IRenderer Members
        public DX10Renderer(Control renderTarget, IScaler imageScaler, uint[,] screen, bool smooth)
        {
            this.renderTarget = renderTarget;
            this.imageScaler = imageScaler;
            this.screen = screen;
            this.smoothOutput = smooth;
        }

        public void Create()
        {
            Reset();
        }

        public void Reset()
        {
            Destroy();

            using (Factory factory = new Factory())
            {
                SlimDX.Direct3D10.Device.CreateWithSwapChain(factory.GetAdapter(0), DriverType.Hardware, DeviceCreationFlags.None, new SwapChainDescription
                {
                    BufferCount = 2,
                    IsWindowed = true,
                    OutputHandle = renderTarget.Handle,
                    Usage = Usage.RenderTargetOutput,
                    SampleDescription = new SampleDescription(1, 0),
                    ModeDescription = new ModeDescription(renderTarget.Width, renderTarget.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm)
                }, out device, out swapChain);
            }

            using (Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0))
            {
                renderTargetView = new RenderTargetView(device, backBuffer);
            }
            device.OutputMerger.SetTargets(renderTargetView);

            bsd = new BlendStateDescription();
            bsd.AlphaBlendOperation = BlendOperation.Add;
            bsd.BlendOperation = BlendOperation.Add;
            bsd.SourceBlend = BlendOption.SourceAlpha;
            bsd.DestinationBlend = BlendOption.InverseSourceAlpha;
            bsd.SourceAlphaBlend = BlendOption.SourceAlpha;
            bsd.DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            bsd.SetWriteMask(0, ColorWriteMaskFlags.All);
            bsd.IsAlphaToCoverageEnabled = true;
            bsd.SetBlendEnable(0, true);

            device.OutputMerger.BlendSampleMask = -1;
            device.OutputMerger.BlendFactor = new Color4(0, 0, 0, 0);
            device.OutputMerger.BlendState = BlendState.FromDescription(device, bsd);

            viewport = new Viewport(0, 0, renderTarget.Width, renderTarget.Height);
            device.Rasterizer.SetViewports(viewport);

            BufferDescription bd = new BufferDescription(4 * Marshal.SizeOf(typeof(Vertex)), ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None);
            vertexBuffer = new SlimDX.Direct3D10.Buffer(device, bd);

            Vertex[] vertexData = new Vertex[4];
            vertexData[0].pos = new Vector3(-1, -1, 0);
            vertexData[0].texCoord = new Vector2(0, 1);
            vertexData[1].pos = new Vector3(-1, 1, 0);
            vertexData[1].texCoord = new Vector2(0, 0);
            vertexData[2].pos = new Vector3(1, -1, 0);
            vertexData[2].texCoord = new Vector2(1, 1);
            vertexData[3].pos = new Vector3(1, 1, 0);
            vertexData[3].texCoord = new Vector2(1, 0);

            DataStream vbStream = vertexBuffer.Map(MapMode.WriteDiscard, SlimDX.Direct3D10.MapFlags.None);
            vbStream.WriteRange(vertexData);
            vertexBuffer.Unmap();

            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            Stream file = thisExe.GetManifestResourceStream("DirectXEmu.Video.Shaders.SimpleRender.fx");
            effect = Effect.FromStream(device, file, "fx_4_0");
            file.Close();
            
            EffectTechnique technique;
            if(smoothOutput)
                technique = effect.GetTechniqueByName("RenderSmooth");
            else
                technique = effect.GetTechniqueByName("Render");

            pass = technique.GetPassByIndex(0);

            ShaderSignature signature = pass.Description.Signature;
            inputLayout = new InputLayout(device, signature, new[] {
				new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32_Float, 0, 0),
				new InputElement("TEXCOORD", 0, SlimDX.DXGI.Format.R32G32_Float, 12, 0)
			});

            texture = new Texture2D(device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                Format = Format.R8G8B8A8_UNorm,
                Height = imageScaler.ResizedY,
                Width = imageScaler.ResizedX,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Dynamic
            });

            textureView = new ShaderResourceView(device, texture);
            effect.GetVariableByName("tex2d").AsResource().SetResource(textureView);


            LoadCharSheet();
        }

        public void MainLoop(bool newScreen)
        {
            if (device != null)
            {
                //if (newScreen) //Texture seems to be destroyed after being displayed once
                {
                    unsafe
                    {
                        DataRectangle drt = texture.Map(0, MapMode.WriteDiscard, SlimDX.Direct3D10.MapFlags.None);
                        fixed (uint* screenPTR = screen)
                        {
                            imageScaler.PerformScale(screenPTR, (uint*)drt.Data.DataPointer);
                        }
                        texture.Unmap(0);
                    }
                }
                device.ClearRenderTargetView(renderTargetView, new Color4(0.0f, 0.0f, 0.0f));
                device.InputAssembler.SetInputLayout(inputLayout);
                device.InputAssembler.SetPrimitiveTopology(SlimDX.Direct3D10.PrimitiveTopology.TriangleStrip);
                device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Marshal.SizeOf(typeof(Vertex)), 0));
                pass.Apply();
                device.Draw(4, 0);
                DrawMessageEvent(this, null);
                swapChain.Present(0, PresentFlags.None);
            }
        }

        public void Destroy()
        {
            if (device != null)
            {
                if (device.OutputMerger.BlendState != null)
                    device.OutputMerger.BlendState.Dispose();
                device.Dispose();
            }
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            if (texture != null)
                texture.Dispose();
            if (renderTargetView != null)
                renderTargetView.Dispose();
            if (swapChain != null)
                swapChain.Dispose();
            if (effect != null)
                effect.Dispose();
            if (inputLayout != null)
                inputLayout.Dispose();
            if (textureView != null)
                textureView.Dispose();
            if (messageSprite != null)
                messageSprite.Dispose();
            if (charSheet != null)
                charSheet.Dispose();
            if (spriteView != null)
                spriteView.Dispose();
        }

        public void SmoothOutput(bool smooth)
        {
            this.smoothOutput = smooth;
            EffectTechnique technique;
            if (smoothOutput)
                technique = effect.GetTechniqueByName("RenderSmooth");
            else
                technique = effect.GetTechniqueByName("Render");

            pass = technique.GetPassByIndex(0);
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
            float scale = 64f / (float)renderTarget.Width;
            float xf = (((((float)realXOffset / (float)renderTarget.Width) * 2f) - 1) * 2) + (0.5f * scale);
            float yf = -((((((float)realYOffset / (float)renderTarget.Height) * 2f) - 1) * 2) + (0.5f * scale));

            messageSprite.Begin(SpriteFlags.None);

            SpriteInstance[] chars = new SpriteInstance[message.Length];
            for (int i = 0; i < message.Length; i++)
            {
                int charNum = 0;
                if (charSheetSprites.ContainsKey(message[i]))
                {
                    charNum = charSheetSprites[message[i]];
                }
                float charX = ((charNum % 16) * 0.0625f);
                float charY = ((charNum / 16) * 0.0625f);
                chars[i] = new SpriteInstance(spriteView, new Vector2(charX, charY), new Vector2(0.0625f, 0.0625f));
                chars[i].Transform = Matrix.Add(Matrix.Translation(xf + (i * scale), yf, 0), Matrix.Scaling(scale - 1, scale - 1, 0));
            }
            messageSprite.DrawBuffered(chars);
            messageSprite.End();
        }
        private void LoadCharSheet()
        {
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            Stream file = thisExe.GetManifestResourceStream("DirectXEmu.images.charSheet.png");
            charSheet = Texture2D.FromStream(device, file, (int)file.Length);
            file.Close();
            messageSprite = new Sprite(device, 0);
            spriteView = new ShaderResourceView(device, charSheet);
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

        #endregion
    }
}
#endif