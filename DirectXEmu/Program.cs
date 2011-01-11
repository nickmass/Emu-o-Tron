using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using SlimDX;
using SlimDX.DirectInput;
using SlimDX.Direct3D9;
using SlimDX.XAudio2;
using SlimDX.Multimedia;
using SlimDX.XInput;
using SlimDX.Windows;
using EmuoTron;
using NetPlay;

namespace DirectXEmu
{
    enum SystemState
    {
        Playing,
        Empty,
        Paused,
        SystemPause
    }
    public struct AutoFire
    {
        public bool on;
        public int freq;
        public int count;
    }
    struct VertexPositionRhwTexture
    {
        public Vector4 PositionRhw;
        public Vector2 Texture1;
    }
    public partial class Program : Form
    {
        string appPath = "";
        string romPath = "";
        SlimDX.Direct3D9.Device device;
        Keyboard dKeyboard;
        Mouse dMouse;
        DirectInput dInput;
        Direct3D d3d;
        XAudio2 dAudio;
        MasteringVoice mVoice;
        SourceVoice sVoice;
        WaveFormat audioFormat;
        AudioBuffer audioBuffer = new AudioBuffer();
        BinaryWriter audioWriter;
        PresentParameters pps = new PresentParameters();
        Sprite messageSprite;
        Texture texture;
        Dictionary<char, int> charSheetSprites = new Dictionary<char, int>();
        Texture charSheet;
        int charSize;
        NESCore cpu;
        EmuoTron.Controller player1;
        AutoFire player1A;
        AutoFire player1B;
        EmuoTron.Controller player2;
        AutoFire player2A;
        AutoFire player2B;
        bool controlStrobe = false;
        Color[] colorChart = new Color[0x200];
        public int frame = 0;
        public int frameSkipper = 1;
        int maxFrameSkip = 10;
        GameGenie[] gameGenieCodes = new GameGenie[0xFF];
        int gameGenieCodeCount = 0;
        string message = "";
        bool storeState;
        int messageDuration = 0;
        bool loadState;
        bool closed = false;
        StreamReader fm2File;
        bool playMovie;
        Keybinds keyBindings;
        SystemState state;

        int generateLine = 0;
        bool generateNameTables = false;
        int nameTableUpdate = 10;

        int generatePatternLine = 0;
        bool generatePatternTables = false;
        int patternTableUpdate = 3;

        NameTablePreview nameTablePreview;
        PatternTablePreview patternTablePreview;
        MemoryViewer memoryViewer;
        Debugger debugger;

        EmuConfig config;

        SoundVolume volume;
        SoundConfig soundConfig;

        int frames = 0;
        int lastTickCount = 0;
        int lastFrameRate = 0;
        private OpenFileDialog openPaletteDialog;
        private OpenFileDialog openMovieDialog;
        private ToolStripMenuItem closeToolStripMenuItem;
        bool showFPS;
        bool showInput;

        Scaler imageScaler;
        Bitmap frameBuffer;

        int quickSaveSlot = 0;
        SaveState[] saveSlots;

        SaveState[] saveBuffer;
        int saveBufferCounter = 0;
        int saveBufferAvaliable = 0;
        int saveBufferFreq;
        int saveBufferSeconds;
        bool saveSafeRewind = false;
        bool rewinding = false;
        bool rewindingEnabled;

        private SlimDX.XInput.Controller x360Controller;
        private bool enableController = true;
        private bool buttonDown = false;
        private int vibTimer = 0;

        uint CRC;
        VertexBuffer vertexBuffer;
        private bool fullScreen = false;
        private Point smallLocation;
        private Size smallSize;
        int memoryViewerMem = 0;
        FileStream wavFile;
        BinaryWriter wavWriter;
        bool wavRecord;
        int wavSamples;

        byte[] movie = new byte[60 * 60 * 60 * 12];//twelve hours should be enough
        int moviePtr = 0;

        [STAThread]
        static void Main(string[] args)
        {
            Process thisProc = Process.GetCurrentProcess();
            thisProc.PriorityClass = ProcessPriorityClass.AboveNormal;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Program prg;
            if (args.Length == 0)
                prg = new Program();
            else
                prg = new Program(args[0]);
            MessagePump.Run(prg, new MainLoop(prg.Run));
        }
        public Program(string arg = "")
        {
            this.romPath = arg;
            InitializeComponent();
            this.InitializeCPU();
            this.InitializeDirect3D();
#if !DEBUG
            this.openWithFXCEUToolStripMenuItem.Dispose();
            this.toolStripSeparator3.Dispose();
#endif
        }
        public unsafe void Run()
        {
            if (state == SystemState.Playing && !cpu.debug.debugInterrupt)
            {
                debugger.smartUpdate = false;
                this.RunCPU();
                if (cpu.debug.pendingError)
                {
                    cpu.debug.pendingError = false;
                    message = cpu.debug.errorMessage;
                    messageDuration = 90;
                }
            }
            else
            {
                if (cpu != null && cpu.debug.debugInterrupt && (!debugger.updated || !debugger.smartUpdate))
                {
                    debugger.smartUpdate = true;
                    message = "Breakpoint";
                    messageDuration = 1;
                    debugger.UpdateDebug();
                }
                Thread.Sleep(16);
            }
            if (state != SystemState.SystemPause && this.frame++ % this.frameSkipper == 0)
            {
                if (state != SystemState.Paused)
                    this.messageDuration--;
                else
                    this.frame--;
                if (cpu != null && cpu.debug.debugInterrupt)
                    messageDuration = 1;
                this.Render(); // Keep rendering until the program terminates
            }
        }
        private unsafe void RunCPU()
        {
            if (this.storeState)
            {
                this.storeState = false;
                saveSlots[quickSaveSlot] = cpu.StateSave();
                Directory.CreateDirectory(config["savestateDir"]);
                Stream stream = new FileStream(Path.Combine(config["savestateDir"], cpu.rom.fileName + ".s" + quickSaveSlot.ToString("D2")), FileMode.Create, FileAccess.Write, FileShare.None);
                saveSlots[quickSaveSlot].stateStream.WriteTo(stream);
                stream.Close();
                this.message = "State " + quickSaveSlot.ToString() + " Saved";
                this.messageDuration = 90;
            }
            if (this.loadState)
            {
                this.loadState = false;
                if (saveSlots[quickSaveSlot].isStored)
                {
                    cpu.StateLoad(saveSlots[quickSaveSlot]);
                    this.message = "State " + quickSaveSlot.ToString() + " Loaded";
                }
                else
                {
                    message = "Empty Save Slot";
                }
                messageDuration = 90;
            }
            HandleKeyboard();
            HandleMouse();
            if (x360Controller.IsConnected)
                player1 = HandleGamepad(player1);
            if (controlStrobe)
            {
                if (player1.a)
                    player1.a = (frame % 2) == 1;
                if (player1.b)
                    player1.b = (frame % 2) == 1;
                if (player1.select)
                    player1.select = (frame % 2) == 1;
                if (player1.start)
                    player1.start = (frame % 2) == 1;
                if (player1.left)
                    player1.left = (frame % 2) == 1;
                if (player1.right)
                    player1.right = (frame % 2) == 1;
                if (player1.up)
                    player1.up = (frame % 2) == 1;
                if (player1.down)
                    player1.down = (frame % 2) == 1;
            }
            if (player1A.on)
                player1.a = player1A.count++ % player1A.freq == 0;
            if (player1B.on)
                player1.b = player1B.count++ % player1B.freq == 0;
            if (player2A.on)
                player2.a = player2A.count++ % player2A.freq == 0;
            if (player2B.on)
                player2.b = player2B.count++ % player2B.freq == 0;
            if (playMovie)
            {
                playMovie = !Fm2Reader();
                if (!playMovie)
                {
                    this.playMovieToolStripMenuItem.Text = "Play Movie";
                    message = "Playback Ended";
                    messageDuration = 90;
                }
            }
            if (rewindingEnabled)
            {
                if (rewinding)
                {

                    if (frame % ((saveBufferFreq == 1 ? 2 : saveBufferFreq) / 2) == 0)
                    {
                        if (saveBufferAvaliable != 0)
                        {
                            saveBufferAvaliable--;
                            saveBufferCounter--;
                            if (saveBufferCounter < 0)
                                saveBufferCounter = ((60 / saveBufferFreq) * saveBufferSeconds) - 1;

                        }
                        saveSafeRewind = true;
                    }
                    if (saveSafeRewind)
                    {
                        cpu.StateLoad(saveBuffer[saveBufferCounter]);
                        moviePtr = saveBuffer[saveBufferCounter].frame; 
                    }
                }
                else
                {
                    saveSafeRewind = false;
                    if (frame % saveBufferFreq == 0)
                    {
                        saveBuffer[saveBufferCounter] = cpu.StateSave();
                        saveBuffer[saveBufferCounter].frame = moviePtr;
                        saveBufferCounter++;
                        if (saveBufferCounter >= ((60 / saveBufferFreq) * saveBufferSeconds))
                            saveBufferCounter = 0;
                        if (saveBufferAvaliable != ((60 / saveBufferFreq) * saveBufferSeconds))
                            saveBufferAvaliable++;
                    }

                }
            }
            movie[moviePtr] = PlayerToByte(player1);
            moviePtr++;
            UpdateFramerate();
            if (netPlay)
            {
                netClient.SendInput(PlayerToByte(this.player1));
                cpu.Start(ByteToPlayer(netClient.player1), ByteToPlayer(netClient.player2), (this.frame % this.frameSkipper != 0));
            }
            else
            {
                cpu.Start(player1, player2, (this.frame % this.frameSkipper != 0));
            }
            if (memoryViewerMem == 1)
                memoryViewer.updateMemory(cpu.Memory, cpu.MirrorMap);
            else if (memoryViewerMem == 2)
                memoryViewer.updateMemory(cpu.PPU.PPUMemory, cpu.PPU.PPUMirrorMap);


            if (frame % cpu.APU.frameBuffer == 0)
            {
                if (frameSkipper == 1 && !cpu.debug.debugInterrupt)
                {
                    if (wavRecord)
                    {
                        for (int i = 0; i < cpu.APU.outputPtr; i++)
                            wavWriter.Write(cpu.APU.output[i]);
                        wavSamples += cpu.APU.outputPtr;
                    }
                    cpu.APU.volume = volume;
                    audioWriter.BaseStream.SetLength(0);
                    if (rewinding)
                    {
                        for (int i = cpu.APU.outputPtr - 1; i >= 0; i--)
                            audioWriter.Write(cpu.APU.output[i]);
                    }
                    else
                    {
                        for (int i = 0; i < cpu.APU.outputPtr; i++)
                            audioWriter.Write(cpu.APU.output[i]);
                    }
                    audioWriter.BaseStream.Position = 0;
                    audioBuffer.AudioBytes = cpu.APU.outputPtr * (audioFormat.BitsPerSample / 8);

                    if (sVoice.State.BuffersQueued <= 1)// this in theory will reduce skipping, setting to zero should reduce some skipping, while setting it to one should reduce it completely but often seems to just make everything sound very depressing :P
                    {
                        if (cpu.APU.curFPS > 0)
                            cpu.APU.curFPS--;
                    }
                    else if (cpu.APU.curFPS < cpu.APU.FPS)
                        cpu.APU.curFPS++;
                    cpu.APU.SetFPS(cpu.APU.curFPS);

                    if (wavRecord) //Willing to put up with a few clicks and pops if it means the wav output sounds perfect
                        cpu.APU.SetFPS(cpu.APU.FPS);


                    while (sVoice.State.BuffersQueued > 1) //Keep this set as 1 or prepare for clicking
                    {
                        Thread.Sleep(1);
                    }
                    sVoice.SubmitSourceBuffer(audioBuffer);
                }
                cpu.APU.ResetBuffer();
                
            }
            if (this.generatePatternTables && this.frame % this.patternTableUpdate == 0)
            {
                this.patternTablePreview.UpdatePatternTables(cpu.PPU.patternTables, cpu.PPU.patternTablesPalette);
                cpu.PPU.generatePatternLine = this.patternTablePreview.generateLine;
                cpu.PPU.generatePatternTables = true;
            }
            if (this.generateNameTables && this.frame % this.nameTableUpdate == 0)
            {
                cpu.PPU.generateLine = this.nameTablePreview.UpdateNameTables(cpu.PPU.nameTables);
                cpu.PPU.generateNameTables = true;
            }
            if (this.frame % this.frameSkipper == 0)
            {
                BitmapData frameBMD = frameBuffer.LockBits(new Rectangle(0, 0, frameBuffer.Width, frameBuffer.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int* framePixels = (int*)frameBMD.Scan0;
                for (int i = 0, y = 0; y < 240; y++)
                    for (int x = 0; x < 256; x++, i++)
                        framePixels[i] = this.colorChart[cpu.PPU.screen[x, y]].ToArgb();
                frameBuffer.UnlockBits(frameBMD);
                if(config["showDebug"] == "1")
                    frameBuffer.SetPixel(player2.x, player2.y, Color.Magenta);
            }
        }
        private void Render()
        {
            if (device == null) // If the device is empty don't bother rendering
            {
                return;
            }
            try
            {
                if (state == SystemState.Playing)
                {
                    unsafe
                    {
                        BitmapData frameBMD = frameBuffer.LockBits(new Rectangle(0, 0, frameBuffer.Width, frameBuffer.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                        DataRectangle drt = texture.LockRectangle(0, LockFlags.None);
                        imageScaler.PerformScale((int*)frameBMD.Scan0, (int*)drt.Data.DataPointer);
                        frameBuffer.UnlockBits(frameBMD);
                        texture.UnlockRectangle(0);

                    }
                }
                device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
                device.BeginScene();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
                if (this.messageDuration > 0)
                    DrawString(this.message, 4, 4);
                if (this.showFPS)
                    DrawString(lastFrameRate.ToString(), this.surfaceControl.Width - ((charSize * lastFrameRate.ToString().Length) + 4), 4);
                if (config["showDebug"] == "1")
                {
                    DrawString(frame.ToString(), this.surfaceControl.Width - ((charSize * frame.ToString().Length) + 4), 24);
                    if (cpu != null)
                    {
                        CRC = GetScreenCRC(cpu.PPU.screen);
                        DrawString(CRC.ToString("X8"), this.surfaceControl.Width - ((charSize * CRC.ToString("X8").Length) + 4), 44);
                    }
                }
                if (this.showInput)
                {
                    string inputString = "";
                    if (this.player1.up)
                        inputString += "^";
                    else
                        inputString += " ";
                    if (this.player1.down)
                        inputString += "_";
                    else
                        inputString += " ";
                    if (this.player1.left)
                        inputString += "<";
                    else
                        inputString += " ";
                    if (this.player1.right)
                        inputString += ">";
                    else
                        inputString += " ";
                    if (this.player1.start)
                        inputString += "*&";
                    else
                        inputString += "  ";
                    if (this.player1.select)
                        inputString += "*$";
                    else
                        inputString += "  ";
                    if (this.player1.a)
                        inputString += "A";
                    else
                        inputString += " ";
                    if (this.player1.b)
                        inputString += "B";
                    else
                        inputString += " ";
                    inputString += " ";
                    if (this.player2.up)
                        inputString += "^";
                    else
                        inputString += " ";
                    if (this.player2.down)
                        inputString += "_";
                    else
                        inputString += " ";
                    if (this.player2.left)
                        inputString += "<";
                    else
                        inputString += " ";
                    if (this.player2.right)
                        inputString += ">";
                    else
                        inputString += " ";
                    if (this.player2.start)
                        inputString += "*&";
                    else
                        inputString += "  ";
                    if (this.player2.select)
                        inputString += "*$";
                    else
                        inputString += "  ";
                    if (this.player2.a)
                        inputString += "A";
                    else
                        inputString += " ";
                    if (this.player2.b)
                        inputString += "B";
                    else
                        inputString += " ";
                    DrawString(inputString, 4, this.surfaceControl.Height - (charSize + 4));
                }
                if (netPlay)
                {
                    if (netClient.pendingMessage != 0)
                    {
                        DrawString(netClient.message, 4, (charSize + 4));
                        netClient.pendingMessage--;
                    }
                }
                device.EndScene();
                device.Present();
            }
            catch (Direct3D9Exception e)
            {
                if (!this.closed)
                {
                    if (e.ResultCode == SlimDX.Direct3D9.ResultCode.DeviceLost)
                    {
                        InitializeDirect3D();
                    }
#if DEBUG
                    else
                        MessageBox.Show(e.Message, "D3D ON CLOSING");
#endif
                }
#if DEBUG
                else
                    MessageBox.Show(e.Message, "D3D");
#endif
            }
        }
        public bool InitializeDirect3D()
        {
            try
            {
                d3d = new Direct3D();
                pps.Windowed = true;
                pps.PresentationInterval = PresentInterval.Immediate;
                pps.BackBufferWidth = this.surfaceControl.Height;
                pps.BackBufferHeight = this.surfaceControl.Width;
                device = new SlimDX.Direct3D9.Device(d3d, 0, SlimDX.Direct3D9.DeviceType.Hardware, this.surfaceControl.Handle, CreateFlags.HardwareVertexProcessing, pps);
                texture = new Texture(device, this.imageScaler.xSize, this.imageScaler.ySize, 0, Usage.Dynamic, Format.A8R8G8B8, Pool.Default);
                CreateScreenBuffer();
                LoadCharSheet();
                dInput = new DirectInput();
                dKeyboard = new Keyboard(dInput);
                dKeyboard.SetCooperativeLevel(this, CooperativeLevel.Foreground | CooperativeLevel.Nonexclusive);
                dKeyboard.Acquire();
                dMouse = new Mouse(dInput);
                dMouse.SetCooperativeLevel(this, CooperativeLevel.Foreground | CooperativeLevel.Nonexclusive);
                dMouse.Acquire();
                Program_Resize(this, new EventArgs());
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error"); // Handle all the exceptions
                return false;
            }
        }
        public void InitializeCPU()
        {
            this.appPath = Path.GetDirectoryName(Application.ExecutablePath);
            Directory.SetCurrentDirectory(appPath);
            config = new EmuConfig("Emu-o-Tron.cfg");
            this.LoadKeys();
            if (!File.Exists(this.config["palette"]))
                this.config["palette"] = config.defaults["palette"];
            FileStream palFile = File.OpenRead(this.config["palette"]);
            for (int i = 0; i < 0x40; i++)
                this.colorChart[i] = Color.FromArgb(palFile.ReadByte(), palFile.ReadByte(), palFile.ReadByte());
            if (palFile.Length > 0x40 * 3) //shitty hack for vs palette because im LAZY
            {
                Color[] vsColor = new Color[0x200];
                for (int i = 0; palFile.Position < palFile.Length; i++)
                {
                    vsColor[i] = colorChart[palFile.ReadByte()];
                }
                colorChart = vsColor;
            }
            palFile.Close();
            CreateEmphasisTables();
            if (this.config["showFPS"] == "1")
            {
                this.showFPSToolStripMenuItem.Checked = true;
                this.showFPS = true;
            }
            else
            {
                this.showFPSToolStripMenuItem.Checked = false;
                this.showFPS = false;
            }
            if (this.config["showInput"] == "1")
            {
                this.showInputToolStripMenuItem.Checked = true;
                this.showInput = true;
            }
            else
            {
                this.showInputToolStripMenuItem.Checked = false;
                this.showInput = false;
            }
            this.spritesToolStripMenuItem.Checked = (config["displaySprites"] == "1");
            this.backgroundToolStripMenuItem.Checked = (config["displayBG"] == "1");
            this.spriteLimitToolStripMenuItem.Checked = (config["disableSpriteLimit"] == "1");
            this.openPaletteDialog.InitialDirectory = Path.GetFullPath(this.config["paletteDir"]);
            this.openMovieDialog.InitialDirectory = Path.GetFullPath(this.config["movieDir"]);
            this.saveMovie.InitialDirectory = Path.GetFullPath(this.config["movieDir"]);
            this.openFile.InitialDirectory = Path.GetFullPath(this.config["romPath1"]);
            this.enableSoundToolStripMenuItem.Checked = (config["sound"] == "1");
            this.nTSCToolStripMenuItem.Checked = (SystemType)Convert.ToInt32(config["region"]) == SystemType.NTSC;
            this.pALToolStripMenuItem.Checked = (SystemType)Convert.ToInt32(config["region"]) == SystemType.PAL;
            for(int i = 1; i <= 5; i++)
                if(this.config["romPath" + i.ToString()] != "")
                    this.openFile.CustomPlaces.Add(this.config["romPath" + i.ToString()]);
            this.LoadRecentFiles();
            this.Width = Convert.ToInt32(this.config["width"]);
            this.Height = Convert.ToInt32(this.config["height"]);
            switch (this.config["scaler"].ToLower())
            {
                default:
                case "sizable":
                case "sizeable":
                    sizeableToolStripMenuItem.Checked = true;
                    this.imageScaler = new Sizeable();
                    break;
                case "1x":
                    xToolStripMenuItem.Checked = true;
                    this.imageScaler = new NearestNeighbor1x();
                    break;
                case "2x":
                    xToolStripMenuItem1.Checked = true;
                    this.imageScaler = new NearestNeighbor2x();
                    break;
                case "scale2x":
                    scale2xToolStripMenuItem.Checked = true;
                    this.imageScaler = new Scale2x();
                    break;
                case "scale3x":
                    scale3xToolStripMenuItem.Checked = true;
                    this.imageScaler = new Scale3x();
                    break;
                case "fill":
                    fillToolStripMenuItem.Checked = true;
                    this.imageScaler = new Fill();
                    break;
                case "tv":
                    tVAspectToolStripMenuItem.Checked = true;
                    this.imageScaler = new TVAspect();
                    break;
            }
            frameBuffer = new Bitmap(256, 240);
            PrepareScaler();
            rewindingEnabled = this.config["rewindEnabled"] == "1" ? true : false;
            saveBufferFreq = Convert.ToInt32(this.config["rewindBufferFreq"]);
            saveBufferSeconds = Convert.ToInt32(this.config["rewindBufferSeconds"]);
            saveBuffer = new SaveState[(60 / saveBufferFreq) * saveBufferSeconds];
            x360Controller = new SlimDX.XInput.Controller(UserIndex.One);
            player1A.freq = 2;
            player1A.count = 1;
            player1B.freq = 2;
            player1B.count = 1;
            player2A.freq = 2;
            player2A.count = 1;
            player2B.freq = 2;
            player2B.count = 1;
            state = SystemState.Empty;
            surfaceControl.Visible = false;

            audioFormat = new WaveFormat();
            audioFormat.BitsPerSample = 16;
            audioFormat.Channels = 1;
            audioFormat.SamplesPerSecond = Convert.ToInt32(this.config["sampleRate"]);
            audioFormat.BlockAlignment = (short)(audioFormat.BitsPerSample * audioFormat.Channels / 8);
            audioFormat.AverageBytesPerSecond = (audioFormat.BitsPerSample / 8) * audioFormat.SamplesPerSecond;
            audioFormat.FormatTag = WaveFormatTag.Pcm;

            dAudio = new XAudio2();
            mVoice = new MasteringVoice(dAudio);
            sVoice = new SourceVoice(dAudio, audioFormat, VoiceFlags.None);
            sVoice.Start();
            if (config["sound"] == "1")
                mVoice.Volume = Convert.ToInt32(config["volume"]) / 100f;
            else
                mVoice.Volume = 0;
            audioBuffer = new AudioBuffer();
            audioBuffer.AudioData = new MemoryStream();
            audioWriter = new BinaryWriter(audioBuffer.AudioData);
            volume.master = Convert.ToInt32(config["volume"]) / 100f;
            volume.pulse1 = 1;
            volume.pulse2 = 1;
            volume.triangle = 1;
            volume.noise = 1;
            volume.dmc = 1;
            if (this.romPath != "")
                this.OpenFile(romPath);
        }
        void soundVolume_ValueChanged(object sender, EventArgs e)
        {
            config["volume"] = soundConfig.soundVolume.Value.ToString();
            mVoice.Volume = soundConfig.soundVolume.Value / 100f;
            volume.master = mVoice.Volume;
            volume.pulse1 = soundConfig.pulse1Volume.Value / 100f;
            volume.pulse2 = soundConfig.pulse2Volume.Value / 100f;
            volume.triangle = soundConfig.triangleVolume.Value / 100f;
            volume.noise = soundConfig.noiseVolume.Value / 100f;
            volume.dmc = soundConfig.dmcVolume.Value / 100f;
        }
        private void LoadSaveStateFiles()
        {
            saveSlots = new SaveState[10];
            for (int i = 0; i < 10; i++)
            {
                if (File.Exists(Path.Combine(config["savestateDir"], cpu.rom.fileName + ".s" + i.ToString("D2"))))
                {
                    FileStream stream = new FileStream(Path.Combine(config["savestateDir"], cpu.rom.fileName + ".s" + i.ToString("D2")), FileMode.Open, FileAccess.Read, FileShare.Read);
                    SaveState newstate = new SaveState();
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, (int)stream.Length);
                    newstate.stateStream = new MemoryStream(buffer);
                    newstate.isStored = true;
                    saveSlots[i] = newstate;
                    stream.Close();
                }

            }
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
                    finalRed = Math.Round(colorChart[j].R * red) > 0xFF ? (byte)0xFF : (byte)Math.Round(colorChart[j].R * red);
                    finalGreen = Math.Round(colorChart[j].G * green) > 0xFF ? (byte)0xFF : (byte)Math.Round(colorChart[j].G * green);
                    finalBlue = Math.Round(colorChart[j].B * blue) > 0xFF ? (byte)0xFF : (byte)Math.Round(colorChart[j].B * blue);
                    colorChart[j | (i << 6)] = Color.FromArgb(finalRed, finalGreen, finalBlue);
                }
            }
        }
        private void LoadRecentFiles()
        {
            this.recentFileMenu1.Text = "Recent Files";
            this.recentFileMenu1.Enabled = false;
            this.recentFileMenu2.Visible = false;
            this.recentFileMenu2.Enabled = false;
            this.recentFileMenu3.Visible = false;
            this.recentFileMenu3.Enabled = false;
            this.recentFileMenu4.Visible = false;
            this.recentFileMenu4.Enabled = false;
            this.recentFileMenu5.Visible = false;
            this.recentFileMenu5.Enabled = false;
            if (this.config["recentFile1"] != "")
            {
                this.recentFileMenu1.Text = Path.GetFileName(this.config["recentFile1"]);
                this.recentFileMenu1.Enabled = true;
            }
            if (this.config["recentFile2"] != "")
            {
                this.recentFileMenu2.Text = Path.GetFileName(this.config["recentFile2"]);
                this.recentFileMenu2.Visible = true;
                this.recentFileMenu2.Enabled = true;
            }
            if (this.config["recentFile3"] != "")
            {
                this.recentFileMenu3.Text = Path.GetFileName(this.config["recentFile3"]);
                this.recentFileMenu3.Visible = true;
                this.recentFileMenu3.Enabled = true;
            }
            if (this.config["recentFile4"] != "")
            {
                this.recentFileMenu4.Text = Path.GetFileName(this.config["recentFile4"]);
                this.recentFileMenu4.Visible = true;
                this.recentFileMenu4.Enabled = true;
            }
            if (this.config["recentFile5"] != "")
            {
                this.recentFileMenu5.Text = Path.GetFileName(this.config["recentFile5"]);
                this.recentFileMenu5.Visible = true;
                this.recentFileMenu5.Enabled = true;
            }
        }
        private void AddRecentFile(string fileName)
        {
            this.recentFileMenu1.Text = "Recent Files";
            this.recentFileMenu1.Enabled = false;
            this.recentFileMenu2.Visible = false;
            this.recentFileMenu2.Enabled = false;
            this.recentFileMenu3.Visible = false;
            this.recentFileMenu3.Enabled = false;
            this.recentFileMenu4.Visible = false;
            this.recentFileMenu4.Enabled = false;
            this.recentFileMenu5.Visible = false;
            this.recentFileMenu5.Enabled = false;
            if (fileName == this.config["recentFile1"])
            {
                this.config["recentFile5"] = this.config["recentFile5"];
                this.config["recentFile4"] = this.config["recentFile4"];
                this.config["recentFile3"] = this.config["recentFile3"];
                this.config["recentFile2"] = this.config["recentFile2"];
                this.config["recentFile1"] = fileName;
            }
            else if (fileName == this.config["recentFile2"])
            {
                this.config["recentFile5"] = this.config["recentFile5"];
                this.config["recentFile4"] = this.config["recentFile4"];
                this.config["recentFile3"] = this.config["recentFile3"];
                this.config["recentFile2"] = this.config["recentFile1"];
                this.config["recentFile1"] = fileName;
            }
            else if (fileName == this.config["recentFile3"])
            {
                this.config["recentFile5"] = this.config["recentFile5"];
                this.config["recentFile4"] = this.config["recentFile4"];
                this.config["recentFile3"] = this.config["recentFile2"];
                this.config["recentFile2"] = this.config["recentFile1"];
                this.config["recentFile1"] = fileName;
            }
            else if (fileName == this.config["recentFile4"])
            {
                this.config["recentFile5"] = this.config["recentFile5"];
                this.config["recentFile4"] = this.config["recentFile3"];
                this.config["recentFile3"] = this.config["recentFile2"];
                this.config["recentFile2"] = this.config["recentFile1"];
                this.config["recentFile1"] = fileName;
            }
            else
            {
                this.config["recentFile5"] = this.config["recentFile4"];
                this.config["recentFile4"] = this.config["recentFile3"];
                this.config["recentFile3"] = this.config["recentFile2"];
                this.config["recentFile2"] = this.config["recentFile1"];
                this.config["recentFile1"] = fileName;
            }
            if (this.config["recentFile1"] != "")
            {
                this.recentFileMenu1.Text = Path.GetFileName(this.config["recentFile1"]);
                this.recentFileMenu1.Enabled = true;
            }
            if (this.config["recentFile2"] != "")
            {
                this.recentFileMenu2.Text = Path.GetFileName(this.config["recentFile2"]);
                this.recentFileMenu2.Visible = true;
                this.recentFileMenu2.Enabled = true;
            }
            if (this.config["recentFile3"] != "")
            {
                this.recentFileMenu3.Text = Path.GetFileName(this.config["recentFile3"]);
                this.recentFileMenu3.Visible = true;
                this.recentFileMenu3.Enabled = true;
            }
            if (this.config["recentFile4"] != "")
            {
                this.recentFileMenu4.Text = Path.GetFileName(this.config["recentFile4"]);
                this.recentFileMenu4.Visible = true;
                this.recentFileMenu4.Enabled = true;
            }
            if (this.config["recentFile5"] != "")
            {
                this.recentFileMenu5.Text = Path.GetFileName(this.config["recentFile5"]);
                this.recentFileMenu5.Visible = true;
                this.recentFileMenu5.Enabled = true;
            }
        }
        private void recentFileMenu1_Click(object sender, EventArgs e)
        {
            this.OpenFile(this.config["recentFile1"], false);
        }

        private void recentFileMenu2_Click(object sender, EventArgs e)
        {
            this.OpenFile(this.config["recentFile2"], false);
        }

        private void recentFileMenu3_Click(object sender, EventArgs e)
        {
            this.OpenFile(this.config["recentFile3"], false);
        }

        private void recentFileMenu4_Click(object sender, EventArgs e)
        {
            this.OpenFile(this.config["recentFile4"], false);
        }

        private void recentFileMenu5_Click(object sender, EventArgs e)
        {
            this.OpenFile(this.config["recentFile5"], false);
        }
        private void UpdateFramerate()
        {
            frames++;
            if (Math.Abs(Environment.TickCount - lastTickCount) > 1000)
            {
                lastFrameRate = frames * 1000 / Math.Abs(Environment.TickCount - lastTickCount);
                lastTickCount = Environment.TickCount;
                frames = 0;
            }
        }
        private void nameTablePreviewForm_Closed(object sender, FormClosedEventArgs e)
        {
            this.generateNameTables = false;
        }
        private void patternTablePreviewForm_Closed(object sender, FormClosedEventArgs e)
        {
            this.generatePatternTables = false;
        }
        private EmuoTron.Controller HandleGamepad(EmuoTron.Controller input)
        {
            Gamepad x360State = x360Controller.GetState().Gamepad;
            Vibration vib = new Vibration();
            if ((x360State.Buttons & GamepadButtonFlags.LeftThumb) != 0)
                buttonDown = true;
            else
            {
                if (buttonDown)
                {
                    enableController = !enableController;
                    buttonDown = false;
                    vibTimer = 30;
                }
            }
            if (vibTimer > 0)
            {
                vib.LeftMotorSpeed = 0xA000;
                vib.RightMotorSpeed = 0xA000;
                vibTimer--;
            }
            else
            {
                vib.LeftMotorSpeed = 0x00;
                vib.RightMotorSpeed = 0x00;
            }
            x360Controller.SetVibration(vib);
            if (enableController)
            {
                if ((x360State.Buttons & GamepadButtonFlags.A) != 0)
                    input.a = true;
                if ((x360State.Buttons & GamepadButtonFlags.B) != 0)
                    input.b = true;
                if ((x360State.Buttons & GamepadButtonFlags.X) != 0)
                    input.b = true;
                if ((x360State.Buttons & GamepadButtonFlags.Y) != 0)
                    input.a = true;
                if ((x360State.Buttons & GamepadButtonFlags.DPadUp) != 0)
                    input.up = true;
                if ((x360State.Buttons & GamepadButtonFlags.DPadDown) != 0)
                    input.down = true;
                if ((x360State.Buttons & GamepadButtonFlags.DPadLeft) != 0)
                    input.left = true;
                if ((x360State.Buttons & GamepadButtonFlags.DPadRight) != 0)
                    input.right = true;
                if ((x360State.Buttons & GamepadButtonFlags.Start) != 0)
                    input.start = true;
                if ((x360State.Buttons & GamepadButtonFlags.Back) != 0)
                    input.select = true;
                if ((x360State.Buttons & GamepadButtonFlags.LeftShoulder) != 0)
                    this.loadState = true;
                if ((x360State.Buttons & GamepadButtonFlags.RightShoulder) != 0)
                    this.storeState = true;
                if (x360State.LeftTrigger > 100)
                    rewinding = true;
                if (x360State.RightTrigger > 100)
                    frameSkipper = this.maxFrameSkip;
                if (x360State.LeftThumbX < -15000)
                    input.left = true;
                if (x360State.LeftThumbX > 15000)
                    input.right = true;
                if (x360State.LeftThumbY > 15000)
                    input.up = true;
                if (x360State.LeftThumbY < -15000)
                    input.down = true;
            }
            return input;
            
        }
        private void HandleMouse()
        {
            if (dMouse.Acquire().IsFailure)
                return;
            if (dMouse.Poll().IsFailure)
                return;
            try
            {
                MouseState mouseState = dMouse.GetCurrentState();
                player2.triggerPulled = player1.triggerPulled = mouseState.IsPressed(0);
                Point tmpPoint = Cursor.Position;
                tmpPoint.X -= this.Location.X;
                tmpPoint.Y -= this.Location.Y;
                int borderWidth = (Width - ClientSize.Width) / 2;
                int titlebarHeight = (Height - ClientSize.Height) - borderWidth;
                tmpPoint.X -= borderWidth;
                tmpPoint.Y -= titlebarHeight;
                tmpPoint.X -= surfaceControl.Location.X;
                tmpPoint.Y -= surfaceControl.Location.Y;
                tmpPoint.X = (int)((frameBuffer.Width * tmpPoint.X) / (surfaceControl.Width * 1.0));
                tmpPoint.Y = (int)((frameBuffer.Height * tmpPoint.Y) / (surfaceControl.Height * 1.0));
                if (tmpPoint.X < 0)
                {
                    tmpPoint.X = 0;
                }
                else if (tmpPoint.X >= this.frameBuffer.Width)
                {
                    tmpPoint.X = this.frameBuffer.Width - 1;
                }
                if (tmpPoint.Y < 0)
                {
                    tmpPoint.Y = 0;
                }
                else if (tmpPoint.Y >= this.frameBuffer.Height)
                {
                    tmpPoint.Y = this.frameBuffer.Height - 1;
                }
                player2.x = player1.x = (byte)tmpPoint.X;
                player2.y = player1.y = (byte)tmpPoint.Y;
                
            }
            catch
            {
                dMouse.Acquire();
            }
        }
        private void HandleKeyboard()
        {
            if (dKeyboard.Acquire().IsFailure)
                return;
            if (dKeyboard.Poll().IsFailure)
                return;
            try
            {
                KeyboardState keyState = dKeyboard.GetCurrentState();
                controlStrobe = keyState.IsPressed(Key.Q);
                player1.up = keyState.IsPressed(keyBindings.Player1Up);
                player1.down = keyState.IsPressed(keyBindings.Player1Down);
                player1.left = keyState.IsPressed(keyBindings.Player1Left);
                player1.right = keyState.IsPressed(keyBindings.Player1Right);
                player1.start = keyState.IsPressed(keyBindings.Player1Start);
                player1.select = keyState.IsPressed(keyBindings.Player1Select);
                player1.a = keyState.IsPressed(keyBindings.Player1A);
                player1.b = keyState.IsPressed(keyBindings.Player1B);
                player1A.on = keyState.IsPressed(keyBindings.Player1TurboA);
                if (!player1A.on)
                    player1A.count = 1;
                player1B.on = keyState.IsPressed(keyBindings.Player1TurboB);
                if (!player1B.on)
                    player1B.count = 1;
                player2.up = keyState.IsPressed(keyBindings.Player2Up);
                player2.down = keyState.IsPressed(keyBindings.Player2Down);
                player2.left = keyState.IsPressed(keyBindings.Player2Left);
                player2.right = keyState.IsPressed(keyBindings.Player2Right);
                player2.start = keyState.IsPressed(keyBindings.Player2Start);
                player2.select = keyState.IsPressed(keyBindings.Player2Select);
                player2.a = keyState.IsPressed(keyBindings.Player2A);
                player2.b = keyState.IsPressed(keyBindings.Player2B);
                player2A.on = keyState.IsPressed(keyBindings.Player2TurboA);
                if (!player2A.on)
                    player2A.count = 1;
                player2B.on = keyState.IsPressed(keyBindings.Player2TurboB);
                if (!player2B.on)
                    player2B.count = 1;
                rewinding = keyState.IsPressed(keyBindings.Rewind);
                if (keyState.IsPressed(keyBindings.FastForward))
                    frameSkipper = maxFrameSkip;
                else
                    frameSkipper = 1;
                player1.coin = keyState.IsPressed(Key.F2);
                player2.coin = keyState.IsPressed(Key.F3);
            }
            catch
            {
                dKeyboard.Acquire();
            }
        }
        private uint GetScreenCRC(ushort[,] scanlines)
        {
            uint crc = 0xFFFFFFFF;
            for (int y = 0; y < 240; y++)
                for (int x = 0; x < 256; x++)
                    crc = CRC32.crc32_adjust(crc, (byte)(scanlines[x, y] & 0x3F));
            crc ^= 0xFFFFFFFF;
            return crc;
        }
        private void LoadCharSheet()
        {
            messageSprite = new Sprite(device);
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            Stream file = thisExe.GetManifestResourceStream("DirectXEmu.images.charSheet.png");
            if (charSheet != null)
                charSheet.Dispose();
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
            charSheetSprites[':'] = 45;
        }
        private void DrawString(string str, int x, int y)
        {
            str = str.ToLower();
            messageSprite.Begin(SpriteFlags.AlphaBlend);
            for (int i = 0; i < str.Length; i++)
            {
                if (charSheetSprites.ContainsKey(str[i]))
                {
                    int charNum = charSheetSprites[str[i]];
                    int charX = (charNum % charSize) * charSize;
                    int charY = (charNum / charSize) * charSize;
                    Rectangle rect = new Rectangle(charX, charY, charSize, charSize);
                    messageSprite.Transform = Matrix.Translation(x + (i * charSize), y, 0);
                    messageSprite.Draw(charSheet, rect, new SlimDX.Color4(Color.White));
                }
            }
            messageSprite.End();
        }
        private void CreateScreenBuffer()
        {
            if(vertexBuffer != null)
                vertexBuffer.Dispose();
            vertexBuffer = new VertexBuffer(device, 6 * Marshal.SizeOf(typeof(VertexPositionRhwTexture)), Usage.WriteOnly, VertexFormat.PositionRhw | VertexFormat.Texture1, Pool.Managed);

            DataStream stream = vertexBuffer.Lock(0, 0, LockFlags.None);
            VertexPositionRhwTexture[] vertexData = new VertexPositionRhwTexture[6];

            vertexData[0].PositionRhw = new Vector4((float)device.Viewport.Width + 1f, (float)device.Viewport.Height, 0f, 1f);
            vertexData[0].Texture1 = new Vector2(1f, 1f);

            vertexData[1].PositionRhw = new Vector4(0f, (float)device.Viewport.Height, 0f, 1f);
            vertexData[1].Texture1 = new Vector2(0f, 1f);

            vertexData[2].PositionRhw = new Vector4(0f, 0f, 0f, 1f);
            vertexData[2].Texture1 = new Vector2(0f, 0f);

            vertexData[3].PositionRhw = new Vector4(0f, 0f, 0f, 1f);
            vertexData[3].Texture1 = new Vector2(0f, 0f);

            vertexData[4].PositionRhw = new Vector4((float)device.Viewport.Width + 1f, 0f, 0f, 1f);
            vertexData[4].Texture1 = new Vector2(1f, 0f);

            vertexData[5].PositionRhw = new Vector4((float)device.Viewport.Width + 1f, (float)device.Viewport.Height, 0f, 1f);
            vertexData[5].Texture1 = new Vector2(1f, 1f);

            stream.WriteRange(vertexData);
            vertexBuffer.Unlock();
            device.SetRenderState(RenderState.CullMode, Cull.Counterclockwise);
            device.VertexFormat = VertexFormat.PositionRhw | VertexFormat.Texture1;
            device.SetStreamSource(0, vertexBuffer, 0, Marshal.SizeOf(typeof(VertexPositionRhwTexture)));
            device.SetTexture(0, texture);
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.openFile.ShowDialog() == DialogResult.OK)
            {
                this.OpenFile(this.openFile.FileName);
                this.openFile.InitialDirectory = Path.GetDirectoryName(this.openFile.FileName);
            }
        }
        private void EmuWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == keyBindings.SaveState)
            {
                this.storeState = true;
            }
            else if (e.KeyCode == keyBindings.LoadState)
            {
                this.loadState = true;
            }
            else if (e.KeyCode == keyBindings.Pause)
            {
                if (state == SystemState.Playing)
                    state = SystemState.Paused;
                else if (state == SystemState.Paused)
                    state = SystemState.Playing;
                this.message = "Paused";
                this.messageDuration = 1;
            }
            else if (e.KeyCode == keyBindings.Reset)
            {
                this.SaveGame();
                moviePtr = 0;
                this.cpu.Reset();
                this.LoadGame();
                this.message = "Reset";
                this.messageDuration = 90;
            }
            else if (e.KeyCode == keyBindings.Power)
            {
                this.SaveGame();
                moviePtr = 0;
                this.cpu.Power();
                this.LoadGame();
                this.message = "Power";
                this.messageDuration = 90;
            }
            else if (e.KeyCode == Keys.F11) //TO-DO: turn into real keybind
            {
                ToggleFullScreen();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                if (fullScreen)
                {
                    ToggleFullScreen();
                }
            }
            else if (e.KeyValue == 18) //This sucks dick too but I'm not sure how best to do it
            {
                if (fullScreen)
                {
                    this.menuStrip.Visible = !this.menuStrip.Visible;
                }
            }
            else if (e.KeyCode == Keys.OemOpenBrackets)
            {
                quickSaveSlot--;
                if (quickSaveSlot < 0)
                    quickSaveSlot = 9;
                this.message = "Save Slot " + quickSaveSlot.ToString();
                this.messageDuration = 90;

            }
            else if (e.KeyCode == Keys.OemCloseBrackets)
            {
                quickSaveSlot++;
                if (quickSaveSlot > 9)
                    quickSaveSlot = 0;
                this.message = "Save Slot " + quickSaveSlot.ToString();
                this.messageDuration = 90;

            }
        }
        private void ToggleFullScreen()
        {
            if (fullScreen)
            {
                this.menuStrip.Show();
                this.Size = this.smallSize;
                this.Location = this.smallLocation;
                this.fullScreen = false;
            }
            else
            {
                this.menuStrip.Hide();
                this.smallLocation = this.Location;
                this.smallSize = this.Size;
                this.fullScreen = true;
            }
            PrepareScaler();
        }
        private void ResetDevice()
        {
            try
            {
                if (device != null)
                {
                    texture.Dispose();
                    messageSprite.Dispose();
                    device.Reset(pps);
                    if (texture != null)
                        texture.Dispose();
                    texture = new Texture(device, this.imageScaler.xSize, this.imageScaler.ySize, 0, Usage.Dynamic, Format.A8R8G8B8, Pool.Default);
                    LoadCharSheet();
                    CreateScreenBuffer();
                }
            }
            catch
            {
            }
        }
        private void openWithFCEUXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process FCEXU = new Process();
            FCEXU.StartInfo.FileName = this.config["previewEmu"];
            FCEXU.StartInfo.Arguments = "\"" + this.romPath + "\"";
            FCEXU.Start();
        }
        private bool Fm2Reader()
        {
            String line = " ";
            while (line[0] != '|')
            {
                line = fm2File.ReadLine();
                if (fm2File.EndOfStream)
                {
                    fm2File.Close();
                    return true;
                }
            }
            player1.right = line[3] != '.';
            player1.left = line[4] != '.';
            player1.down = line[5] != '.';
            player1.up = line[6] != '.';
            player1.start = line[7] != '.';
            player1.select = line[8] != '.';
            player1.b = line[9] != '.';
            player1.a = line[10] != '.';
            player2.right = line[12] != '.';
            player2.left = line[13] != '.';
            player2.down = line[14] != '.';
            player2.up = line[15] != '.';
            player2.start = line[16] != '.';
            player2.select = line[17] != '.';
            player2.b = line[18] != '.';
            player2.a = line[19] != '.';
            return fm2File.EndOfStream;

        }

        private void openMovieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.openMovieDialog.ShowDialog() == DialogResult.OK)
            {
                this.playMovieToolStripMenuItem.Enabled = true;
            }
        }

        private void playMovieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.playMovie)
            {
                this.playMovie = false;
                this.fm2File.Close();
                this.playMovieToolStripMenuItem.Text = "Play Movie";
            }
            else
            {
                this.fm2File = File.OpenText(this.openMovieDialog.FileName);
                cpu.Power();
                this.playMovie = true;
                this.playMovieToolStripMenuItem.Text = "Stop Movie";
            }
        }
        private void ShowLog()
        {
            if (this.cpu != null)
            {
                File.WriteAllText("log.txt", this.cpu.debug.logBuilder.ToString());
                Process log = new Process();
                log.StartInfo.FileName = this.config["logReader"];
                log.StartInfo.Arguments = "log.txt";
                log.Start();
            }
        }

        private void loadPaletteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.openPaletteDialog.ShowDialog() == DialogResult.OK)
            {
                FileStream palFile = File.OpenRead(this.openPaletteDialog.FileName);
                this.config["palette"] = this.openPaletteDialog.FileName;
                for (int i = 0; i < 0x40; i++)
                    this.colorChart[i] = Color.FromArgb(palFile.ReadByte(), palFile.ReadByte(), palFile.ReadByte());
                if (palFile.Length > 0x40 * 3) //shitty hack for vs palette because im LAZY
                {
                    Color[] vsColor = new Color[0x200];
                    for (int i = 0; palFile.Position < palFile.Length; i++)
                    {
                        vsColor[i] = colorChart[palFile.ReadByte()];
                    }
                    colorChart = vsColor;
                }
                palFile.Close();
                CreateEmphasisTables();
            }
        }

        private void enableLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.cpu.debug.logging = !this.cpu.debug.logging;
            enableLoggingToolStripMenuItem.Checked = this.cpu.debug.logging;
        }

        private void openLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowLog();
        }

        private void gameGenieCodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GameGenieWindow codeWindow = new GameGenieWindow();
            codeWindow.AddCodes(this.gameGenieCodes, this.gameGenieCodeCount);
            codeWindow.ShowDialog();
            GameGenie[] tmp = codeWindow.GetCodes();
            for (int i = 0; i < tmp.Length; i++)
            {
                this.gameGenieCodes[i] = tmp[i];
            }
            this.gameGenieCodeCount = tmp.Length;
            if (this.cpu != null)
            {
                this.cpu.gameGenieCodeNum = this.gameGenieCodeCount;
                this.cpu.gameGenieCodes = this.gameGenieCodes;
            }
        }

        private void Program_Resize(object sender, EventArgs e)
        {
            PrepareScaler();
            this.config["width"] = this.Width.ToString();
            this.config["height"] = this.Height.ToString();
        }

        private void aboutEmuoTronToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var about = new About();
            about.ShowDialog();
        }

        private void Program_DragDrop(object sender, DragEventArgs e)
        {
            this.OpenFile(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
        }

        private void Program_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
                e.Effect = DragDropEffects.All;
        }
        private void OpenFile(string fileName)
        {
            this.OpenFile(fileName, true);
        }
        private string ExtractFile(string fileName)
        {
            SystemState old = state;
            state = SystemState.SystemPause;
            string ext = Path.GetExtension(fileName).ToLower();
            if (ext == ".7z" || ext == ".zip" || ext == ".rar")
            {
                string sevenZ = this.config["7z"];
                if(IntPtr.Size == 8)
                    sevenZ = this.config["7z64"]; //replace this with installer logic maybe?
                SevenZipFormat Format = new SevenZipFormat(sevenZ);
                KnownSevenZipFormat fileType;
                switch (ext)
                {
                    default:
                    case ".7z":
                        fileType = KnownSevenZipFormat.SevenZip;
                        break;
                    case ".zip":
                        fileType = KnownSevenZipFormat.Zip;
                        break;
                    case ".rar":
                        fileType = KnownSevenZipFormat.Rar;
                        break;
                }
                IInArchive Archive = Format.CreateInArchive(SevenZipFormat.GetClassIdFromKnownFormat(fileType));
                try
                {
                    InStreamWrapper ArchiveStream = new InStreamWrapper(File.OpenRead(fileName));
                    ulong checkPos = 32 * 1024;
                    Archive.Open(ArchiveStream, ref checkPos, null);
                    uint count = Archive.GetNumberOfItems();
                    string[] archiveContent = new string[count];
                    for (uint i = 0; i < count; i++)
                    {
                        PropVariant name = new PropVariant();
                        Archive.GetProperty(i, ItemPropId.kpidPath, ref name);
                        archiveContent[i] = name.GetObject().ToString();
                    }
                    if (count == 1)
                    {
                        fileName = Path.Combine(this.config["tmpDir"], Path.GetFileNameWithoutExtension(archiveContent[0]) + ".tmp");
                        Archive.Extract(new uint[] { 0 }, 1, 0, new ArchiveCallback(0, fileName));
                    }
                    else
                    {
                        ArchiveViewer viewer = new ArchiveViewer(archiveContent);
                        if (viewer.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            fileName = Path.Combine(this.config["tmpDir"], Path.GetFileNameWithoutExtension(archiveContent[viewer.selectedFile]) + ".tmp");
                            Archive.Extract(new uint[] { (uint)viewer.selectedFile }, 1, 0, new ArchiveCallback((uint)viewer.selectedFile, fileName));
                        }
                        else
                        {
                            fileName = "";
                        }
                    }
                    ArchiveStream.Dispose();
                }
                finally
                {
                    Archive.Close();
                    Marshal.ReleaseComObject(Archive);
                }
                Format.Dispose();
            }
            state = old;
            return fileName;
        }
        private void OpenFile(string fileName, bool addToRecent)
        {
            try
            {
                this.SaveGame();
                string extractedFileName = this.ExtractFile(fileName);
                if (Path.GetExtension(extractedFileName).ToLower() == ".ips" || Path.GetExtension(extractedFileName).ToLower() == ".ups")
                {
                    SystemState old = state;
                    state = SystemState.SystemPause;
                    MessageBox.Show("Select file to apply patch to.","Patch ROM");
                    if (this.openFile.ShowDialog() == DialogResult.OK)
                    {
                        string prePatch = this.ExtractFile(openFile.FileName);
                        if (prePatch != "")
                            if(Path.GetExtension(extractedFileName).ToLower() == ".ips")
                                extractedFileName = IPSPatch(prePatch, extractedFileName);
                            else if (Path.GetExtension(extractedFileName).ToLower() == ".ups")
                                extractedFileName = UPSPatch(prePatch, extractedFileName);
                        else
                            extractedFileName = "";
                    }
                    else
                        extractedFileName = "";
                    state = old;
                }
                if (extractedFileName != "")
                {
                    this.romPath = extractedFileName;
                    this.StartEmu();
                    if (addToRecent)
                        this.AddRecentFile(fileName);
                }
            }
            catch (Exception e)
            {
                if (e.Message == "Invalid File")
                {
                    SystemState old = state;
                    state = SystemState.SystemPause;
                    if (this.openFile.ShowDialog() == DialogResult.OK)
                    {
                        state = old;
                        this.OpenFile(this.openFile.FileName);

                    }
                }
                else
                    throw (e);
            }
        }
        private void StartEmu()
        {
            bool logState = false;
            if(this.cpu != null)
                logState = this.cpu.debug.logging;
            try
            {
                if (Path.GetExtension(romPath).ToLower() == ".fds")
                    this.cpu = new NESCore((SystemType)Convert.ToInt32(config["region"]), config["fdsBios"], this.romPath, this.appPath, audioFormat.SamplesPerSecond, 1);
                else
                    this.cpu = new NESCore((SystemType)Convert.ToInt32(config["region"]), this.romPath, this.appPath, audioFormat.SamplesPerSecond, 1);
            }
            catch (Exception e)
            {
                if (e.Message == "Invalid File")
                {
                    if (MessageBox.Show("File appears to be invalid. Attempt load anyway?", "Error", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        if (Path.GetExtension(romPath).ToLower() == ".fds")
                            this.cpu = new NESCore((SystemType)Convert.ToInt32(config["region"]), config["fdsBios"], this.romPath, this.appPath, audioFormat.SamplesPerSecond, 1, true);
                        else
                            this.cpu = new NESCore((SystemType)Convert.ToInt32(config["region"]), this.romPath, this.appPath, audioFormat.SamplesPerSecond, 1, true);
                    else
                        throw (e);
                }
                else if (e.Message == "FDS BIOS not found.")
                {
                    MessageBox.Show("FDS BIOS image not found.");
                    throw (e);
                }
                else
                    throw (e);
            }
            this.frame = 0;
            moviePtr = 0;
            this.saveBufferAvaliable = 0;
            this.cpu.debug.logging = logState;
            this.cpu.PPU.displayBG = (config["displayBG"] == "1");
            this.cpu.PPU.displaySprites = (config["displaySprites"] == "1");
            this.cpu.PPU.enforceSpriteLimit = !(config["disableSpriteLimit"] == "1");
            this.cpu.APU.mute = !(config["sound"] == "1");
            this.LoadGame();
            this.LoadSaveStateFiles();
            this.cpu.gameGenieCodeNum = this.gameGenieCodeCount;
            this.cpu.gameGenieCodes = this.gameGenieCodes;
            this.Text = this.cpu.rom.fileName + " - Emu-o-Tron";
            this.state = SystemState.Playing;
            this.surfaceControl.Visible = true;
            if (this.cpu.rom.vsUnisystem)
            {
                this.state = SystemState.SystemPause;
                DipDialog DipDiag = new DipDialog();
                DipDiag.FormClosing += new FormClosingEventHandler(DipDiag_FormClosing);
                DipDiag.ShowDialog();
            }
            if (debugger != null)
                debugger.Close();
            debugger = new Debugger(cpu.debug);
            debugger.UpdateDebug();
            ejectDiskToolStripMenuItem.DropDownItems.Clear();
            ejectDiskToolStripMenuItem.Text = "Eject Disk";
            ejectDiskToolStripMenuItem.Visible = (cpu.GetSideCount() != 0);
            cpu.SetControllers((ControllerType)Enum.Parse(typeof(ControllerType), config["portOne"]), (ControllerType)Enum.Parse(typeof(ControllerType), config["portTwo"]), (config["fourScore"] == "1"));
        }
        void DipDiag_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.cpu.dip1 = ((DipDialog)sender).dip1.Checked;
            this.cpu.dip2 = ((DipDialog)sender).dip2.Checked;
            this.cpu.dip3 = ((DipDialog)sender).dip3.Checked;
            this.cpu.dip4 = ((DipDialog)sender).dip4.Checked;
            this.cpu.dip5 = ((DipDialog)sender).dip5.Checked;
            this.cpu.dip6 = ((DipDialog)sender).dip6.Checked;
            this.cpu.dip7 = ((DipDialog)sender).dip7.Checked;
            this.cpu.dip8 = ((DipDialog)sender).dip8.Checked;
            this.state = SystemState.Playing;
            
        }
        
        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this.ActiveControl, this.config["helpFile"]);
        }

        private void nameTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!this.generateNameTables)
            {
                this.nameTablePreview = new NameTablePreview(this.colorChart, this.generateLine);
                this.nameTablePreview.FormClosed += new FormClosedEventHandler(this.nameTablePreviewForm_Closed);
                this.generateNameTables = true;
                nameTablePreview.Show();
            }
            else
                this.nameTablePreview.Activate();
        }

        private void patternTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!this.generatePatternTables)
            {
                this.patternTablePreview = new PatternTablePreview(this.colorChart, this.generatePatternLine);
                this.patternTablePreview.FormClosed += new FormClosedEventHandler(this.patternTablePreviewForm_Closed);
                this.generatePatternTables = true;
                patternTablePreview.Show();
            }
            else
                this.patternTablePreview.Activate();
        }
        private void memoryViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (memoryViewerMem == 0)
            {
                this.memoryViewer = new MemoryViewer();
                this.memoryViewer.FormClosed += new FormClosedEventHandler(memoryViewer_FormClosed);
                this.memoryViewer.SetMax(0x10000);
                this.memoryViewerMem = 1;
                this.memoryViewer.Show();
            }
            else
            {
                this.memoryViewer.SetMax(0x10000);
                this.memoryViewerMem = 1;
                this.memoryViewer.Activate();
            }

        }
        private void pPUMemoryViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (memoryViewerMem == 0)
            {
                this.memoryViewer = new MemoryViewer();
                this.memoryViewer.FormClosed += new FormClosedEventHandler(memoryViewer_FormClosed);
                this.memoryViewer.SetMax(0x4000);
                this.memoryViewerMem = 2;
                this.memoryViewer.Show();
            }
            else
            {
                this.memoryViewer.SetMax(0x4000);
                this.memoryViewerMem = 2;
                this.memoryViewer.Activate();
            }

        }
        void memoryViewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.memoryViewerMem = 0;
        }
        private void showFPSToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            this.showFPS = !this.showFPS;
            if (this.showFPS)
                this.config["showFPS"] = "1";
            else
                this.config["showFPS"] = "0";
        }


        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.Close();
            this.SaveGame();
            this.Text = "Emu-o-Tron";
            this.romPath = "";
            state = SystemState.Empty;
            surfaceControl.Visible = false;
        }

        private void SaveGame()
        {
            Directory.CreateDirectory(this.config["sramDir"]);
            if(this.cpu != null)
                if (this.cpu.rom.sRAM)
                    File.WriteAllBytes(Path.Combine(this.config["sramDir"], this.cpu.rom.fileName + ".sav"), this.cpu.GetSRAM());
        }
        private void LoadGame()
        {
            Directory.CreateDirectory(this.config["sramDir"]);
            if (this.cpu != null)
            {
                if (this.cpu.rom.sRAM)
                {
                    if (File.Exists(Path.Combine(this.config["sramDir"], this.cpu.rom.fileName + ".sav")))
                        this.cpu.SetSRAM(File.ReadAllBytes(Path.Combine(this.config["sramDir"], this.cpu.rom.fileName + ".sav")));
                }
            }
        }

        private void Program_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.SaveGame();
            this.SaveKeys();
            this.config.Save();
            if (Directory.Exists(this.config["tmpDir"]))
            {
                string[] tmpFiles = Directory.GetFiles(this.config["tmpDir"]);
                for (int i = 0; i < tmpFiles.Length; i++)
                {
                    if (Path.GetExtension(tmpFiles[i]) == ".tmp")
                        File.Delete(tmpFiles[i]);
                }
            }
            if (netPlay)
            {
                netClient.Close();
                if (netPlayServer)
                {
                    netServer.Close();
                }
            }
            this.closed = true;
            audioWriter.Close();
            if (wavRecord)
                wavWriter.Close();
            dAudio.Dispose();
            dMouse.Dispose();
            dKeyboard.Dispose();
            dInput.Dispose();
            texture.Dispose();
            charSheet.Dispose();
            messageSprite.Dispose();
            vertexBuffer.Dispose();
            d3d.Dispose();
            device.Dispose();
        }

        private void romInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.cpu != null)
            {
                RomInfoBox romInfoBox = new RomInfoBox(cpu.debug.romInfo.ToString());
                romInfoBox.Show();
            }
            else
            {
                RomInfoBox romInfoBox = new RomInfoBox("");
                romInfoBox.Show();
            }
        }
        private void LoadKeys()
        {
            keyBindings.Player1Up = (Key)Enum.Parse(typeof(Key), this.config["player1Up"]);
            keyBindings.Player1Down = (Key)Enum.Parse(typeof(Key), this.config["player1Down"]);
            keyBindings.Player1Left = (Key)Enum.Parse(typeof(Key), this.config["player1Left"]);
            keyBindings.Player1Right = (Key)Enum.Parse(typeof(Key), this.config["player1Right"]);
            keyBindings.Player1Start = (Key)Enum.Parse(typeof(Key), this.config["player1Start"]);
            keyBindings.Player1Select = (Key)Enum.Parse(typeof(Key), this.config["player1Select"]);
            keyBindings.Player1A = (Key)Enum.Parse(typeof(Key), this.config["player1A"]);
            keyBindings.Player1B = (Key)Enum.Parse(typeof(Key), this.config["player1B"]);
            keyBindings.Player1TurboA = (Key)Enum.Parse(typeof(Key), this.config["player1TurboA"]);
            keyBindings.Player1TurboB = (Key)Enum.Parse(typeof(Key), this.config["player1TurboB"]);
            keyBindings.Player2Up = (Key)Enum.Parse(typeof(Key), this.config["player2Up"]);
            keyBindings.Player2Down = (Key)Enum.Parse(typeof(Key), this.config["player2Down"]);
            keyBindings.Player2Left = (Key)Enum.Parse(typeof(Key), this.config["player2Left"]);
            keyBindings.Player2Right = (Key)Enum.Parse(typeof(Key), this.config["player2Right"]);
            keyBindings.Player2Start = (Key)Enum.Parse(typeof(Key), this.config["player2Start"]);
            keyBindings.Player2Select = (Key)Enum.Parse(typeof(Key), this.config["player2Select"]);
            keyBindings.Player2A = (Key)Enum.Parse(typeof(Key), this.config["player2A"]);
            keyBindings.Player2B = (Key)Enum.Parse(typeof(Key), this.config["player2B"]);
            keyBindings.Player2TurboA = (Key)Enum.Parse(typeof(Key), this.config["player2TurboA"]);
            keyBindings.Player2TurboB = (Key)Enum.Parse(typeof(Key), this.config["player2TurboB"]);
            keyBindings.LoadState = (Keys)Enum.Parse(typeof(Keys), this.config["loadState"]);
            keyBindings.SaveState = (Keys)Enum.Parse(typeof(Keys), this.config["saveState"]);
            keyBindings.Rewind = (Key)Enum.Parse(typeof(Key), this.config["rewind"]);
            keyBindings.FastForward = (Key)Enum.Parse(typeof(Key), this.config["fastForward"]);
            keyBindings.Pause = (Keys)Enum.Parse(typeof(Keys), this.config["pause"]);
            keyBindings.Power = (Keys)Enum.Parse(typeof(Keys), this.config["power"]);
            keyBindings.Reset = (Keys)Enum.Parse(typeof(Keys), this.config["restart"]);
        }
        private void SaveKeys()
        {
            this.config["player1Up"] = keyBindings.Player1Up.ToString();
            this.config["player1Down"] = keyBindings.Player1Down.ToString();
            this.config["player1Left"] = keyBindings.Player1Left.ToString();
            this.config["player1Right"] = keyBindings.Player1Right.ToString();
            this.config["player1Start"] = keyBindings.Player1Start.ToString();
            this.config["player1Select"] = keyBindings.Player1Select.ToString();
            this.config["player1A"] = keyBindings.Player1A.ToString();
            this.config["player1B"] = keyBindings.Player1B.ToString();
            this.config["player1TurboA"] = keyBindings.Player1TurboA.ToString();
            this.config["player1TurboB"] = keyBindings.Player1TurboB.ToString();
            this.config["player2Up"] = keyBindings.Player2Up.ToString();
            this.config["player2Down"] = keyBindings.Player2Down.ToString();
            this.config["player2Left"] = keyBindings.Player2Left.ToString();
            this.config["player2Right"] = keyBindings.Player2Right.ToString();
            this.config["player2Start"] = keyBindings.Player2Start.ToString();
            this.config["player2Select"] = keyBindings.Player2Select.ToString();
            this.config["player2A"] = keyBindings.Player2A.ToString();
            this.config["player2B"] = keyBindings.Player2B.ToString();
            this.config["player2TurboA"] = keyBindings.Player2TurboA.ToString();
            this.config["player2TurboB"] = keyBindings.Player2TurboB.ToString();
            this.config["loadState"] = keyBindings.LoadState.ToString();
            this.config["saveState"] = keyBindings.SaveState.ToString();
            this.config["rewind"] = keyBindings.Rewind.ToString();
            this.config["fastForward"] = keyBindings.FastForward.ToString();
            this.config["pause"] = keyBindings.Pause.ToString();
            this.config["power"] = keyBindings.Power.ToString();
            this.config["restart"] = keyBindings.Reset.ToString();
        }
        private void keyBindingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SystemState old = state;
            state = SystemState.SystemPause;
            Keybind keyBindWindow = new Keybind(keyBindings, (ControllerType)Enum.Parse(typeof(ControllerType), config["portOne"]), (ControllerType)Enum.Parse(typeof(ControllerType), config["portTwo"]), (config["fourScore"] == "1"));
            if (keyBindWindow.ShowDialog() == DialogResult.OK)
            {
                keyBindings = keyBindWindow.keys;
                config["portOne"] = keyBindWindow.portOne.ToString();
                config["portTwo"] = keyBindWindow.portTwo.ToString();
                config["fourScore"] = keyBindWindow.fourScore ? "1" : "0";
                if (cpu != null)
                {
                    cpu.SetControllers(keyBindWindow.portOne, keyBindWindow.portTwo, keyBindWindow.fourScore);
                }
            }
            state = old;
        }

        private void spritesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cpu != null)
                cpu.PPU.displaySprites = !cpu.PPU.displaySprites;
            this.spritesToolStripMenuItem.Checked = !this.spritesToolStripMenuItem.Checked;
            config["displaySprites"] = this.spritesToolStripMenuItem.Checked ? "1" : "0";
        }

        private void backgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cpu != null)
                cpu.PPU.displayBG = !cpu.PPU.displayBG;
            this.backgroundToolStripMenuItem.Checked = !this.backgroundToolStripMenuItem.Checked;
            config["displayBG"] = this.backgroundToolStripMenuItem.Checked ? "1" : "0";
        }

        private void spriteLimitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cpu != null)
                cpu.PPU.enforceSpriteLimit = !cpu.PPU.enforceSpriteLimit;
            this.spriteLimitToolStripMenuItem.Checked = !this.spriteLimitToolStripMenuItem.Checked;
            config["disableSpriteLimit"] = this.spriteLimitToolStripMenuItem.Checked ? "1" : "0";
        }

        private void showInputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showInput = !showInput;
            showInputToolStripMenuItem.Checked = showInput;
            config["showInput"] = showInput ? "1" : "0";
        }

        private void sizeableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            sizeableToolStripMenuItem.Checked = true;
            imageScaler = new Sizeable();
            PrepareScaler();
            config["scaler"] = "sizeable";
        }

        private void fillToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            fillToolStripMenuItem.Checked = true;
            imageScaler = new Fill();
            PrepareScaler();
            config["scaler"] = "fill";
        }

        private void xToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            xToolStripMenuItem.Checked = true;
            imageScaler = new NearestNeighbor1x();
            PrepareScaler();
            config["scaler"] = "1x";
        }

        private void xToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            xToolStripMenuItem1.Checked = true;
            imageScaler = new NearestNeighbor2x();
            PrepareScaler();
            config["scaler"] = "2x";
        }

        private void scale2xToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            scale2xToolStripMenuItem.Checked = true;
            imageScaler = new Scale2x();
            PrepareScaler();
            config["scaler"] = "scale2x";
        }

        private void scale3xToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            scale3xToolStripMenuItem.Checked = true;
            imageScaler = new Scale3x();
            PrepareScaler();
            config["scaler"] = "scale3x";
        }

        private void tVAspectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            tVAspectToolStripMenuItem.Checked = true;
            imageScaler = new TVAspect();
            PrepareScaler();
            config["scaler"] = "tv";
        }
        private void PrepareScaler()
        {
            int oldWidth = Width;
            int oldHeight = Height;
            if (fullScreen)
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.Location = new Point(0, 0);
                this.Size = SystemInformation.PrimaryMonitorSize;
                if (this.imageScaler.maintainAspectRatio)
                {
                    int height = this.Height;
                    int width = this.Width;
                    if (height / imageScaler.arY > width / imageScaler.arX)
                    {
                        this.surfaceControl.Width = width;
                        this.surfaceControl.Height = (int)(width * (imageScaler.arY / imageScaler.arX));
                        this.surfaceControl.Location = new Point(0, (int)(((height - (width * (imageScaler.arY / imageScaler.arX))) / 2.0)));
                    }
                    else
                    {
                        this.surfaceControl.Height = height;
                        this.surfaceControl.Width = (int)(height * (imageScaler.arX / imageScaler.arY));
                        this.surfaceControl.Location = new Point((int)((width - (height * (imageScaler.arX / imageScaler.arY))) / 2.0), 0);
                    }
                }
                else
                {
                    this.surfaceControl.Width = this.Width;
                    this.surfaceControl.Height = this.Height;
                    this.surfaceControl.Location = new Point(0, 0);
                }
            }
            else if (imageScaler.maintainAspectRatio)
            {
                if (imageScaler.resizeable)
                {
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.MaximizeBox = true;
                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.Fixed3D;
                    this.MaximizeBox = false;
                    int borderWidth = (Width - ClientSize.Width);
                    int titlebarHeight = (Height - ClientSize.Height);
                    this.Width = borderWidth + this.imageScaler.xSize;
                    this.Height = titlebarHeight + this.menuStrip.Height + this.imageScaler.ySize;
                }
                int height = ClientSize.Height - this.menuStrip.Height;
                int width = ClientSize.Width;
                if (height / imageScaler.arY > width / imageScaler.arX)
                {
                    this.surfaceControl.Width = width;
                    this.surfaceControl.Height = (int)(width * (imageScaler.arY / imageScaler.arX));
                    this.surfaceControl.Location = new Point(0, (int)(((height - (width * (imageScaler.arY / imageScaler.arX))) / 2.0)) + this.menuStrip.Height);
                }
                else
                {
                    this.surfaceControl.Height = height;
                    this.surfaceControl.Width = (int)(height * (imageScaler.arX / imageScaler.arY));
                    this.surfaceControl.Location = new Point((int)((width - (height * (imageScaler.arX / imageScaler.arY))) / 2.0), this.menuStrip.Height);
                }
            }
            else
            {
                if (imageScaler.resizeable)
                {
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.MaximizeBox = true;
                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.Fixed3D;
                    this.MaximizeBox = false;
                    int borderWidth = (Width - ClientSize.Width);
                    int titlebarHeight = (Height - ClientSize.Height);
                    this.Width = borderWidth + this.imageScaler.xSize;
                    this.Height = titlebarHeight + this.menuStrip.Height + this.imageScaler.ySize;
                }
                this.surfaceControl.Width = ClientSize.Width;
                this.surfaceControl.Height = ClientSize.Height - this.menuStrip.Height;
                this.surfaceControl.Location = new Point(0, this.menuStrip.Height);
            }
            if (oldWidth != Width || oldHeight != Height || pps.BackBufferHeight != this.surfaceControl.Height || pps.BackBufferWidth != this.surfaceControl.Width)
            {
                pps.BackBufferHeight = this.surfaceControl.Height;
                pps.BackBufferWidth = this.surfaceControl.Width;
                ResetDevice();
            }
        }
        private string IPSPatch(string rom, string patch)
        {
            string fileName = Path.Combine(this.config["tmpDir"], Path.GetFileNameWithoutExtension(rom) + ".ips.tmp");
            FileStream inStream = File.OpenRead(rom);
            byte[] file = new byte[0x100FFFF];
            FileStream ipsStream = File.OpenRead(patch);
            FileStream outStream = File.Create(fileName);
            int i = 0;
            while (inStream.Position < inStream.Length)
            {
                file[i] = (byte)inStream.ReadByte();
                i++;
            }
            if (ipsStream.ReadByte() != 'P' ||
                ipsStream.ReadByte() != 'A' ||
                ipsStream.ReadByte() != 'T' ||
                ipsStream.ReadByte() != 'C' ||
                ipsStream.ReadByte() != 'H')
                throw new Exception("Invlaid Patch File");
            int maxOffset = 0;
            bool reading = true;
            while (reading)
            {
                int offset = (ipsStream.ReadByte() << 16) | (ipsStream.ReadByte() << 8) | ipsStream.ReadByte();
                if (offset == (('E' << 16) | ('O' << 8) | 'F'))
                    reading = false;
                else
                {
                    int size = (ipsStream.ReadByte() << 8) | ipsStream.ReadByte();
                    if (size == 0)
                    {
                        int RLEsize = (ipsStream.ReadByte() << 8) | ipsStream.ReadByte();
                        byte value = (byte)ipsStream.ReadByte();
                        for (int j = 0; j < RLEsize; j++)
                            file[offset + j] = value;
                        if (offset + RLEsize > maxOffset)
                            maxOffset = offset + RLEsize;

                    }
                    else
                    {
                        for (int j = 0; j < size; j++)
                            file[offset + j] = (byte)ipsStream.ReadByte();
                        if (offset + size > maxOffset)
                            maxOffset = offset + size;
                    }
                }
            }
            if (inStream.Position < inStream.Length)
            {
                maxOffset = (ipsStream.ReadByte() << 16) | (ipsStream.ReadByte() << 8) | ipsStream.ReadByte();
            }
            outStream.Write(file, 0, maxOffset);
            inStream.Close();
            ipsStream.Close();
            outStream.Close();
            return fileName;
        }
        private string UPSPatch(string rom, string patch)
        {
            string fileName = Path.Combine(this.config["tmpDir"], Path.GetFileNameWithoutExtension(rom) + ".ups.tmp");
            FileStream inStream = File.OpenRead(rom);
            FileStream patchStream = File.OpenRead(patch);
            FileStream outStream = File.Create(fileName);
            if (patchStream.ReadByte() != 'U' ||
                patchStream.ReadByte() != 'P' ||
                patchStream.ReadByte() != 'S' ||
                patchStream.ReadByte() != '1')
                throw new Exception("Invlaid Patch File");
            long patchXSize = UPSDecode(patchStream);
            long patchYSize = UPSDecode(patchStream);
            long inputSize = inStream.Length;
            long outputSize = 0;
            if (inputSize == patchXSize)
                outputSize = patchYSize;
            if (inputSize == patchYSize)
                outputSize = patchXSize;
            long relative = 0;
            while (patchStream.Position < patchStream.Length - 12)
            {
                long offset = relative;
                relative += UPSDecode(patchStream);
                while (offset < relative)
                {
                    byte x;
                    if (inStream.Position < inputSize)
                        x = (byte)inStream.ReadByte();
                    else
                        x = 0;
                    if (outStream.Position < outputSize)
                        outStream.WriteByte(x);
                    offset++;
                }
                while (true)
                {
                    byte x;
                    byte y;
                    if (inStream.Position < inputSize)
                        x = (byte)inStream.ReadByte();
                    else
                        x = 0;
                    y = (byte)patchStream.ReadByte();
                    if (outStream.Position < outputSize)
                        outStream.WriteByte((byte)(x ^ y));
                    if (y == 0)
                        break;

                }
            }
            inStream.Close();
            patchStream.Close();
            outStream.Close();
            return fileName;
        }
        private long UPSDecode(FileStream patchStream)
        {
            long offset = 0;
            long shift = 1;
            while (true)
            {
                byte x = (byte)patchStream.ReadByte();
                offset += (x & 0x7f) * shift;
                if (x >= 0x80)
                    break;
                shift <<= 7;
                offset += shift;
            }
            return offset;
        }
        private void recordWAVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (state != SystemState.Empty)
            {
                recordDialog.FileName = cpu.rom.fileName;
                SystemState old = state;
                state = SystemState.SystemPause;
                if (recordDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!File.Exists(recordDialog.FileName))
                    {
                        wavSamples = 0;
                        if (wavRecord)
                            wavFile.Close();
                        wavFile = File.Create(recordDialog.FileName);
                        wavWriter = new BinaryWriter(wavFile);
                        for (int i = 0; i < 44; i++)
                            wavFile.WriteByte(0);
                        wavRecord = true;
                        stopWAVToolStripMenuItem.Enabled = true;
                    }
                }
                state = old;
            }
        }
        private void stopWAVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wavRecord = false;
            wavFile.Seek(0, SeekOrigin.Begin);
            int Subchunk2Size = wavSamples * audioFormat.Channels * (audioFormat.BitsPerSample / 8);
            wavFile.WriteByte((byte)'R');
            wavFile.WriteByte((byte)'I');
            wavFile.WriteByte((byte)'F');
            wavFile.WriteByte((byte)'F');
            wavFile.Write(BitConverter.GetBytes(Subchunk2Size + 36), 0, 4);
            wavFile.WriteByte((byte)'W');
            wavFile.WriteByte((byte)'A');
            wavFile.WriteByte((byte)'V');
            wavFile.WriteByte((byte)'E');
            wavFile.WriteByte((byte)'f');
            wavFile.WriteByte((byte)'m');
            wavFile.WriteByte((byte)'t');
            wavFile.WriteByte((byte)' ');
            wavFile.Write(BitConverter.GetBytes((int)16), 0, 4);
            wavFile.Write(BitConverter.GetBytes((short)audioFormat.FormatTag), 0, 2);
            wavFile.Write(BitConverter.GetBytes(audioFormat.Channels), 0, 2);
            wavFile.Write(BitConverter.GetBytes(audioFormat.SamplesPerSecond), 0, 4);
            wavFile.Write(BitConverter.GetBytes(audioFormat.AverageBytesPerSecond), 0, 4);
            wavFile.Write(BitConverter.GetBytes(audioFormat.BlockAlignment), 0, 2);
            wavFile.Write(BitConverter.GetBytes(audioFormat.BitsPerSample), 0, 2);
            wavFile.WriteByte((byte)'d');
            wavFile.WriteByte((byte)'a');
            wavFile.WriteByte((byte)'t');
            wavFile.WriteByte((byte)'a');
            wavFile.Write(BitConverter.GetBytes(Subchunk2Size), 0, 4);
            wavFile.Close();


        }
        private void enableSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableSoundToolStripMenuItem.Checked = !enableSoundToolStripMenuItem.Checked;
            if (cpu != null)
                cpu.APU.mute = !enableSoundToolStripMenuItem.Checked;
            config["sound"] = enableSoundToolStripMenuItem.Checked ? "1" : "0";
            if (enableSoundToolStripMenuItem.Checked)
                mVoice.Volume = Convert.ToInt32(config["volume"]) / 100f;
            else
                mVoice.Volume = 0;
        }

        private void soundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            soundConfig = new SoundConfig(volume);
            soundConfig.soundVolume.ValueChanged += new EventHandler(soundVolume_ValueChanged);
            soundConfig.pulse1Volume.ValueChanged += new EventHandler(soundVolume_ValueChanged);
            soundConfig.pulse2Volume.ValueChanged += new EventHandler(soundVolume_ValueChanged);
            soundConfig.triangleVolume.ValueChanged += new EventHandler(soundVolume_ValueChanged);
            soundConfig.noiseVolume.ValueChanged += new EventHandler(soundVolume_ValueChanged);
            soundConfig.dmcVolume.ValueChanged += new EventHandler(soundVolume_ValueChanged);
            soundConfig.Show();
        }

        private void testConsoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Console con = new Console();
            con.Show();
        }

        private void nTSCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in regionToolStripMenuItem.DropDownItems)
                item.Checked = false;
            nTSCToolStripMenuItem.Checked = true;
            config["region"] = ((int)SystemType.NTSC).ToString();
            if (state != SystemState.Empty)
            {
                this.SaveGame();
                this.StartEmu();
            }
        }

        private void pALToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in regionToolStripMenuItem.DropDownItems)
                item.Checked = false;
            pALToolStripMenuItem.Checked = true;
            config["region"] = ((int)SystemType.PAL).ToString();
            if (state != SystemState.Empty)
            {
                this.SaveGame();
                this.StartEmu();
            }
        }
        bool netPlay;
        bool netPlayServer;
        NetPlayServer netServer;
        NetPlayClient netClient;
        private void startGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (netPlay)
                netClient.Close();
            if (netPlayServer)
                netServer.Close();
            NetPlayConnect NPC = new NetPlayConnect("127.0.0.1");
            if (NPC.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                netServer = new NetPlayServer(Convert.ToInt32(config["serverPort"]));
                netClient = new NetPlayClient(NPC.ip, Convert.ToInt32(config["serverPort"]), NPC.nick);
                Thread.Sleep(100);
                netPlay = true;
                netPlayServer = true;
            }
        }

        private void joinGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (netPlay)
                netClient.Close();
            if (netPlayServer)
                netServer.Close();
            NetPlayConnect NPC = new NetPlayConnect();
            if (NPC.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                netClient = new NetPlayClient(NPC.ip, Convert.ToInt32(config["serverPort"]), NPC.nick);
                Thread.Sleep(100);
                netPlay = true;
            }
        }
        public static byte PlayerToByte(EmuoTron.Controller player)
        {
            byte input = 0;
            if (player.a)
                input |= 1;
            input <<= 1;
            if (player.b)
                input |= 1;
            input <<= 1;
            if (player.start)
                input |= 1;
            input <<= 1;
            if (player.select)
                input |= 1;
            input <<= 1;
            if (player.left)
                input |= 1;
            input <<= 1;
            if (player.right)
                input |= 1;
            input <<= 1;
            if (player.up)
                input |= 1;
            input <<= 1;
            if (player.down)
                input |= 1;
            return input;
        }
        public static EmuoTron.Controller ByteToPlayer(byte input)
        {
            EmuoTron.Controller player = new EmuoTron.Controller();
            player.down = ((input & 1) == 1);
            input >>= 1;
            player.up = ((input & 1) == 1);
            input >>= 1;
            player.right = ((input & 1) == 1);
            input >>= 1;
            player.left = ((input & 1) == 1);
            input >>= 1;
            player.select = ((input & 1) == 1);
            input >>= 1;
            player.start = ((input & 1) == 1);
            input >>= 1;
            player.b = ((input & 1) == 1);
            input >>= 1;
            player.a = ((input & 1) == 1);
            return player;
        }

        private void debuggerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cpu != null)
                debugger.Show();
        }

        private void cheatFinderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cpu != null)
            {
                CheatFinder cheatFinder = new CheatFinder(cpu.debug);
                cheatFinder.Show();
            }
        }
        void DiskSide_Click(object sender, EventArgs e)
        {
            if (!cpu.GetEjectDisk())
            {
                cpu.SetDiskSide((int)((ToolStripMenuItem)sender).Tag);
                ejectDiskToolStripMenuItem.DropDownItems.Clear();
                ejectDiskToolStripMenuItem.Text = "Eject Disk";
                cpu.EjectDisk(true);
            }
            else
                MessageBox.Show("Must eject the disk prior to switching sides.");
        }
        private void ejectDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cpu != null)
            {
                if (cpu.GetEjectDisk())
                {
                    ejectDiskToolStripMenuItem.DropDownItems.Clear();
                    cpu.EjectDisk(false);
                    ejectDiskToolStripMenuItem.Text = "Insert Disk";
                    if (cpu.GetSideCount() != 0)
                    {
                        ToolStripMenuItem[] diskSides = new ToolStripMenuItem[cpu.GetSideCount()];
                        for (int i = 0; i < cpu.GetSideCount(); i++)
                        {
                            diskSides[i] = new ToolStripMenuItem("Disk " + ((i / 2) + 1).ToString() + " Side " + (i % 2 == 0 ? "A" : "B"));
                            diskSides[i].Tag = (i);
                            diskSides[i].Click += new EventHandler(DiskSide_Click);
                        }
                        ejectDiskToolStripMenuItem.DropDownItems.AddRange(diskSides);
                    }
                }
            }
        }

        private void saveMovieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveMovie.FileName = cpu.rom.fileName.ToString() + ".fm2";
            if (saveMovie.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileStream mov = File.Create(saveMovie.FileName);
                for (int i = 0; i < moviePtr; i++)
                {
                    EmuoTron.Controller inp = ByteToPlayer(movie[i]);
                    mov.WriteByte((byte)'|');
                    mov.WriteByte((byte)'0');
                    mov.WriteByte((byte)'|');
                    mov.WriteByte((byte)(inp.right ? 'R' : '.'));
                    mov.WriteByte((byte)(inp.left ? 'L' : '.'));
                    mov.WriteByte((byte)(inp.down ? 'D' : '.'));
                    mov.WriteByte((byte)(inp.up ? 'U' : '.'));
                    mov.WriteByte((byte)(inp.start ? 'T' : '.'));
                    mov.WriteByte((byte)(inp.select ? 'S' : '.'));
                    mov.WriteByte((byte)(inp.b ? 'B' : '.'));
                    mov.WriteByte((byte)(inp.a ? 'A' : '.'));
                    mov.WriteByte((byte)'|');
                    mov.WriteByte((byte)'.');
                    mov.WriteByte((byte)'.');
                    mov.WriteByte((byte)'.');
                    mov.WriteByte((byte)'.');
                    mov.WriteByte((byte)'.');
                    mov.WriteByte((byte)'.');
                    mov.WriteByte((byte)'.');
                    mov.WriteByte((byte)'.');
                    mov.WriteByte((byte)'|');
                    mov.WriteByte((byte)'|');
                    mov.WriteByte((byte)'\n');
                }
                mov.Close();
            }
        }
    }
}
