using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using SlimDX;
using SlimDX.DirectInput;
using SlimDX.Direct3D9;
using SlimDX.XAudio2;
using SlimDX.Multimedia;
using SlimDX.XInput;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
namespace DirectXEmu
{
    enum SystemState
    {
        Playing,
        Empty,
        Paused,
        SystemPause
    }
    struct Controller
    {
        public bool up;
        public bool down;
        public bool start;
        public bool select;
        public bool left;
        public bool right;
        public bool a;
        public bool b;
        public AutoFire aTurbo;
        public AutoFire bTurbo;
    }
    struct Zapper
    {
        public bool connected;
        public bool triggerPulled;
        public bool lightDetected;
    }
    struct AutoFire
    {
        public bool on;
        public int freq;
        public int count;
    }
    class Program : Form
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
        PresentParameters pps = new PresentParameters();
        Sprite messageSprite;
        Texture texture;
        NESCore cpu;
        Thread thread;
        Controller player1;
        Controller player2;
        Zapper player1Zap;
        Zapper player2Zap;
        Color[][] colorChart = new Color[0x8][];
        public int frame = 0;
        public int frameSkipper = 1;
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

        EmuConfig config;

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
        Bitmap scaledFrameBuffer;

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
        private ToolStripMenuItem romInfoToolStripMenuItem;
        private ToolStripMenuItem keyBindingsToolStripMenuItem;

        private SlimDX.XInput.Controller x360Controller;
        private bool enableController = true;
        private bool buttonDown = false;
        private int vibTimer = 0;
        private ToolStripMenuItem displayToolStripMenuItem;
        private ToolStripMenuItem spritesToolStripMenuItem;
        private ToolStripMenuItem backgroundToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator5;

        bool reinitializeD3D = false;
        private ToolStripMenuItem showInputToolStripMenuItem;
        private ToolStripMenuItem videoModeToolStripMenuItem;
        private ToolStripMenuItem sizeableToolStripMenuItem;
        private ToolStripMenuItem fillToolStripMenuItem;
        private ToolStripMenuItem xToolStripMenuItem;
        private ToolStripMenuItem xToolStripMenuItem1;
        private ToolStripMenuItem scale2xToolStripMenuItem;
        private ToolStripMenuItem scale3xToolStripMenuItem;
        private ToolStripMenuItem spriteLimitToolStripMenuItem;
        int maxFrameSkip = 10;
        private ToolStripMenuItem recordToolStripMenuItem;
        private ToolStripMenuItem recordWAVToolStripMenuItem;
        private ToolStripMenuItem stopWAVToolStripMenuItem;
        private SaveFileDialog recordDialog;
        private ToolStripMenuItem enableSoundToolStripMenuItem;

        bool controlStrobe = false;
        public Program()
        {
            InitializeComponent();
            this.InitializeCPU();
            this.InitializeDirect3D();
            thread = new Thread(new ThreadStart(Run));
            thread.Start();
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
#if !DEBUG
            this.logToolStripMenuItem.Dispose();
            this.openWithFXCEUToolStripMenuItem.Dispose();
            this.toolStripSeparator3.Dispose();
#endif
        }
        public Program(string arg)
        {
            this.romPath = arg;
            InitializeComponent();
            this.InitializeCPU();
            this.InitializeDirect3D();
            thread = new Thread(new ThreadStart(Run));
            thread.Start();
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
#if !DEBUG
            this.logToolStripMenuItem.Dispose();
            this.openWithFXCEUToolStripMenuItem.Dispose();
            this.toolStripSeparator3.Dispose();
#endif
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (reinitializeD3D)
            {
                InitializeDirect3D();
                reinitializeD3D = false;
            }

        }
        public void Run()
        {
            while (!this.closed) // This is our message loop
            {
                if (state == SystemState.Playing)
                {
                    this.RunCPU();
                }
                if (this.frame++ % this.frameSkipper == 0 && state != SystemState.SystemPause)
                {
                    this.Render(); // Keep rendering until the program terminates
                    UpdateFramerate();
                    if (state != SystemState.Paused)
                        this.messageDuration--;
                }
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
                dMouse.Acquire();
                audioFormat = new WaveFormat();
                audioFormat.BitsPerSample = 32;
                audioFormat.Channels = 1;
                audioFormat.SamplesPerSecond = 1789773 / 41;
                audioFormat.BlockAlignment = (short)(audioFormat.BitsPerSample * audioFormat.Channels / 8);
                audioFormat.AverageBytesPerSecond = (audioFormat.BitsPerSample / 8) * audioFormat.SamplesPerSecond;
                audioFormat.FormatTag = WaveFormatTag.IeeeFloat;
                dAudio = new XAudio2();
                mVoice = new MasteringVoice(dAudio);
                FilterParameters filter = new FilterParameters();
                filter.Frequency = 20000f;
                filter.Type = FilterType.HighPassFilter;
                audioBuffer = new AudioBuffer();
                audioBuffer.AudioData = new MemoryStream();
                this.Program_Resize(this, new EventArgs());
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
            config = new EmuConfig(Path.Combine(this.appPath,  "Emu-o-Tron.cfg"));
            this.config.replacements["{APP-PATH}"] = this.appPath;
            this.LoadKeys();
            FileStream palFile = File.OpenRead(this.config["palette"]);
            for (int i = 0; i < 0x08; i++)
                this.colorChart[i] = new Color[0x100];
            for (int i = 0; palFile.Position < palFile.Length; i++)
                this.colorChart[0][i] = Color.FromArgb(palFile.ReadByte(), palFile.ReadByte(), palFile.ReadByte());
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
            this.openPaletteDialog.InitialDirectory = this.config["paletteDir"];
            this.openMovieDialog.InitialDirectory = this.config["movieDir"];
            this.openFile.InitialDirectory = this.config["romPath1"];
            this.enableSoundToolStripMenuItem.Checked = (config["sound"] == "1");
            for(int i = 1; i <= 5; i++)
                if(this.config["romPath" + i.ToString()] != "")
                    this.openFile.CustomPlaces.Add(this.config["romPath" + i.ToString()]);
            this.LoadRecentFiles();
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
            }
            this.frameBuffer = new Bitmap(256, 240);
            this.scaledFrameBuffer = new Bitmap(this.imageScaler.xSize, this.imageScaler.ySize);
            if (this.imageScaler.resizeable)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.MaximizeBox = true;
                this.Width = Convert.ToInt32(this.config["width"]);
                this.Height = Convert.ToInt32(this.config["height"]);
                
                pps.BackBufferHeight = this.surfaceControl.Height;
                pps.BackBufferWidth = this.surfaceControl.Width;
                ResetDevice();
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Fixed3D;
                this.MaximizeBox = false;
                this.Width = (this.Width - this.insideSize.Width) + this.imageScaler.xSize;
                this.Height = (this.Height - (this.insideSize.Height - this.menuStrip.Height)) + this.imageScaler.ySize;
            }
            rewindingEnabled = this.config["rewindEnabled"] == "1" ? true : false;
            saveBufferFreq = Convert.ToInt32(this.config["rewindBufferFreq"]);
            saveBufferSeconds = Convert.ToInt32(this.config["rewindBufferSeconds"]);
            saveBuffer = new SaveState[(60 / saveBufferFreq) * saveBufferSeconds];
            x360Controller = new SlimDX.XInput.Controller(UserIndex.One);
            player1.aTurbo.freq = 2;
            player1.aTurbo.count = 1;
            player1.bTurbo.freq = 2;
            player1.bTurbo.count = 1;
            player2.aTurbo.freq = 2;
            player2.aTurbo.count = 1;
            player2.bTurbo.freq = 2;
            player2.bTurbo.count = 1;
            state = SystemState.Empty;
            surfaceControl.Visible = false;
            if (this.romPath != "")
                this.OpenFile(romPath);
        }
        private void LoadSaveStateFiles()
        {
            saveSlots = new SaveState[10];
            for (int i = 0; i < 10; i++)
            {
                if (File.Exists(Path.Combine(config["savestateDir"], cpu.fileName + ".s" + i.ToString("D2"))))
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = new FileStream(Path.Combine(config["savestateDir"], cpu.fileName + ".s" + i.ToString("D2")), FileMode.Open, FileAccess.Read, FileShare.Read);
                    saveSlots[i]= (SaveState)formatter.Deserialize(stream);
                    stream.Close();
                }

            }
        }
        private void CreateEmphasisTables()
        {
            byte finalRed;
            byte finalGreen;
            byte finalBlue;
            double emphasis = 0;
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
                //red = (red > 1) ? 1 : red;
                //green = (green > 1) ? 1 : green;
                //blue = (blue > 1) ? 1 : blue;
                red = (red < 0) ? 0 : red;
                green = (green < 0) ? 0 : green;
                blue = (blue < 0) ? 0 : blue;
                for (int j = 0; j < 0x100; j++)
                {
                    finalRed = (((byte)(this.colorChart[0][j].R * red)) > 255) ? (byte)255 : ((byte)(this.colorChart[0][j].R * red));
                    finalGreen = (((byte)(this.colorChart[0][j].G * green)) > 255) ? (byte)255 : ((byte)(this.colorChart[0][j].G * green));
                    finalBlue = (((byte)(this.colorChart[0][j].B * blue)) > 255) ? (byte)255 : ((byte)(this.colorChart[0][j].B * blue));
                    this.colorChart[i][j] = Color.FromArgb(finalRed, finalGreen, finalBlue);
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
        private Controller HandleGamepad(Controller input)
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
                player1.aTurbo.on = keyState.IsPressed(keyBindings.Player1TurboA);
                if (!player1.aTurbo.on)
                    player1.aTurbo.count = 1;
                player1.bTurbo.on = keyState.IsPressed(keyBindings.Player1TurboB);
                if (!player1.bTurbo.on)
                    player1.bTurbo.count = 1;
                player2.up = keyState.IsPressed(keyBindings.Player2Up);
                player2.down = keyState.IsPressed(keyBindings.Player2Down);
                player2.left = keyState.IsPressed(keyBindings.Player2Left);
                player2.right = keyState.IsPressed(keyBindings.Player2Right);
                player2.start = keyState.IsPressed(keyBindings.Player2Start);
                player2.select = keyState.IsPressed(keyBindings.Player2Select);
                player2.a = keyState.IsPressed(keyBindings.Player2A);
                player2.b = keyState.IsPressed(keyBindings.Player2B);
                player2.aTurbo.on = keyState.IsPressed(keyBindings.Player2TurboA);
                if (!player2.aTurbo.on)
                    player2.aTurbo.count = 1;
                player2.bTurbo.on = keyState.IsPressed(keyBindings.Player2TurboB);
                if (!player2.bTurbo.on)
                    player2.bTurbo.count = 1;
                rewinding = keyState.IsPressed(keyBindings.Rewind);
                if (keyState.IsPressed(keyBindings.FastForward))
                    frameSkipper = maxFrameSkip;
                else
                    frameSkipper = 1;
            }
            catch
            {
                dKeyboard.Acquire();
            }
        }
        private unsafe void RunCPU()
        {
            bool zapStatLight = false;
            bool zapStatTrig = false;
            if (this.storeState)
            {
                this.storeState = false;
                saveSlots[quickSaveSlot] = cpu.getState();
                IFormatter formatter = new BinaryFormatter();
                Directory.CreateDirectory(config["savestateDir"]);
                Stream stream = new FileStream( Path.Combine(config["savestateDir"],cpu.fileName + ".s" + quickSaveSlot.ToString("D2")), FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, saveSlots[quickSaveSlot]);
                stream.Close();
                this.message = "State " + quickSaveSlot.ToString() + " Saved";
                this.messageDuration = 90;
            }
            if (this.loadState)
            {
                this.loadState = false;
                if (saveSlots[quickSaveSlot].isStored)
                {
                    cpu.loadState(saveSlots[quickSaveSlot]);
                    this.message = "State " + quickSaveSlot.ToString() + " Loaded";
                }
                else
                {
                    message = "Empty Save Slot";
                }
                messageDuration = 90;
            }
            HandleKeyboard();
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
            if (player1.aTurbo.on)
                player1.a = player1.aTurbo.count++ % player1.aTurbo.freq == 0;
            if (player1.bTurbo.on)
                player1.b = player1.bTurbo.count++ % player1.bTurbo.freq == 0;
            if (player2.aTurbo.on)
                player2.a = player2.aTurbo.count++ % player2.aTurbo.freq == 0;
            if (player2.bTurbo.on)
                player2.b = player2.bTurbo.count++ % player2.bTurbo.freq == 0;
            if (this.playMovie)
                this.Fm2Reader();
            if (this.generatePatternTables && this.frame % this.patternTableUpdate == 0)
            {
                cpu.generatePatternLine = this.generatePatternLine;
                cpu.generatePatternTables = true;
            }
            if (this.generateNameTables && this.frame % this.nameTableUpdate == 0)
            {
                cpu.generateLine = this.generateLine;
                cpu.generateNameTables = true;
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
                        cpu.loadState(saveBuffer[saveBufferCounter]);
                }
                else
                {
                    saveSafeRewind = false;
                    if (frame % saveBufferFreq == 0)
                    {
                        saveBuffer[saveBufferCounter] = cpu.getState();
                        saveBufferCounter++;
                        if (saveBufferCounter >= ((60 / saveBufferFreq) * saveBufferSeconds))
                            saveBufferCounter = 0;
                        if (saveBufferAvaliable != ((60 / saveBufferFreq) * saveBufferSeconds))
                            saveBufferAvaliable++;
                    }

                }
            }


            Point curPoint = LocateMouse();

            /*if (player2Zap.triggerPulled)
            {

                frameBuffer.SetPixel(screenPoint.X, screenPoint.Y, Color.Magenta);
                frameBuffer.Save(frame.ToString() + ".png", ImageFormat.Png);
            }*/
            player2Zap.triggerPulled = dMouse.GetCurrentState().IsPressed(0) && (curPoint.X != 0 || curPoint.Y != 0);
            player2Zap.lightDetected = colorChart[0][cpu.scanlines[curPoint.Y][curPoint.X]].GetBrightness() >= 0.95;
            zapStatLight = player2Zap.lightDetected;
            zapStatTrig = player2Zap.triggerPulled;
            cpu.Start(player1, player2, player1Zap, player2Zap, (this.frame % this.frameSkipper != 0));
            if (!rewinding && frameSkipper == 1)
            {
                if (wavRecord)
                {
                    wavFile.Write(cpu.APU.outBytes, 0, cpu.APU.outputPtr * 4);
                    wavSamples += cpu.APU.outputPtr;
                }
                while (sVoice.State.BuffersQueued > 2)
                {
                    Thread.Sleep(1);
                }
                audioBuffer.AudioData.SetLength(0);
                audioBuffer.AudioData.Write(cpu.APU.outBytes, 0, cpu.APU.outputPtr * 4);
                audioBuffer.AudioData.Position = 0;
                audioBuffer.AudioBytes = cpu.APU.outputPtr * 4;
                audioBuffer.Flags = BufferFlags.None;
                sVoice.SubmitSourceBuffer(audioBuffer);
            }
            cpu.APU.ResetBuffer();
            if (this.generatePatternTables && this.frame % this.patternTableUpdate == 0)
            {
                this.patternTablePreview.UpdatePatternTables(cpu.patternTables, cpu.patternTablesPalette);
                this.generatePatternLine = this.patternTablePreview.generateLine;
            }
            if (this.generateNameTables && this.frame % this.nameTableUpdate == 0)
            {
                this.generateLine = this.nameTablePreview.UpdateNameTables(cpu.nameTables);
            }
            if (this.frame % this.frameSkipper == 0)
            {
                BitmapData frameBMD = frameBuffer.LockBits(new Rectangle(0, 0, frameBuffer.Width, frameBuffer.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte* framePixels = (byte*)frameBMD.Scan0;
                byte[][] scanlines = cpu.scanlines;
                for (int y = 0; y < 240; y++)
                {
                    int emphasisTable = 0;
                    if (this.cpu.blueEmph[y])
                        emphasisTable |= 4;
                    if (this.cpu.greenEmph[y])
                        emphasisTable |= 2;
                    if (this.cpu.redEmph[y])
                        emphasisTable |= 1;
                    for (int x = 0; x < 256; x++)
                    {
                        framePixels[(((y * 4) * 256) + (x * 4))] = this.colorChart[emphasisTable][scanlines[y][x]].B;
                        framePixels[(((y * 4) * 256) + (x * 4)) + 1] = this.colorChart[emphasisTable][scanlines[y][x]].G;
                        framePixels[(((y * 4) * 256) + (x * 4)) + 2] = this.colorChart[emphasisTable][scanlines[y][x]].R;
                        framePixels[(((y * 4) * 256) + (x * 4)) + 3] = 255;
                    }
                }
                frameBuffer.UnlockBits(frameBMD);
                //frameBuffer.SetPixel(curPoint.X, curPoint.Y, Color.Magenta);
                /*if (zapStatLight)
                    frameBuffer.SetPixel(2, 2, Color.Orange);
                if (zapStatTrig)
                    frameBuffer.SetPixel(4, 4, Color.Teal);
                frameBuffer.Save(frame.ToString() + ".png", ImageFormat.Png);*/
            }
        }
        private Point LocateMouse()
        {
            Point curPoint = Cursor.Position;
            if (fullScreen)
            {
                curPoint.Offset(-this.Location.X, -this.Location.Y);
                curPoint.Offset(-(this.surfaceControl.Location.X), -(this.surfaceControl.Location.Y));
            }
            else
            {
                curPoint.Offset(-this.Location.X, -this.Location.Y);
                curPoint.Offset(-(this.surfaceControl.Location.X + SystemInformation.FrameBorderSize.Width), -(this.surfaceControl.Location.Y + SystemInformation.CaptionHeight + SystemInformation.FrameBorderSize.Width));
            }

            Point screenPoint = new Point((int)((curPoint.X * 1.0) / ((this.surfaceControl.Width * 1.0) / (this.frameBuffer.Width * 1.0))), (int)((curPoint.Y * 1.0) / ((this.surfaceControl.Height * 1.0) / (this.frameBuffer.Height * 1.0))));
            if (screenPoint.X < 0 || screenPoint.X >= this.frameBuffer.Width)
            {
                screenPoint.X = 0;
                screenPoint.Y = 0;
            }
            if (screenPoint.Y < 0 || screenPoint.Y >= this.frameBuffer.Height)
            {
                screenPoint.X = 0;
                screenPoint.Y = 0;
            }
            return screenPoint;
        }
        Dictionary<char, int> charSheetSprites = new Dictionary<char, int>();
        Texture charSheet;
        int charSize;
        private void LoadCharSheet()
        {
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
        }
        private void DrawString(string str, int x, int y)
        {
            str = str.ToLower();
            messageSprite.Begin(SpriteFlags.AlphaBlend);
            for (int i = 0; i < str.Length; i++)
            {
                int charNum = charSheetSprites[str[i]];
                int charX = (charNum % charSize) * charSize;
                int charY = (charNum / charSize) * charSize;
                Rectangle rect = new Rectangle(charX, charY, charSize, charSize);
                messageSprite.Transform = Matrix.Translation(x + (i * charSize), y, 0);
                messageSprite.Draw(charSheet, rect, new SlimDX.Color4(Color.White));
            }
            messageSprite.End();
        }
        private unsafe void Render()
        {
            if (device == null) // If the device is empty don't bother rendering
            {
                return;
            }
            try
            {
                if (state == SystemState.Playing)
                {
                    scaledFrameBuffer = imageScaler.PerformScale(frameBuffer);
                    DataRectangle drt = texture.LockRectangle(0, LockFlags.None);
                    BitmapData frameBMD = scaledFrameBuffer.LockBits(new Rectangle(0, 0, scaledFrameBuffer.Width, scaledFrameBuffer.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    int* framePixels = (int*)frameBMD.Scan0;
                    int* textPixels = (int*)drt.Data.DataPointer;
                    int size = this.imageScaler.xSize * this.imageScaler.ySize;
                    for (int i = 0; i < size; i++)
                        textPixels[i] = framePixels[i];
                    texture.UnlockRectangle(0);
                    scaledFrameBuffer.UnlockBits(frameBMD);
                }
                device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
                device.BeginScene();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
                if (this.messageDuration > 0)
                    DrawString(this.message, 4, 4);
                if (this.showFPS)
                    DrawString(lastFrameRate.ToString(), this.surfaceControl.Width - ((charSize*lastFrameRate.ToString().Length)+4), 4);
                if(this.showInput)
                {
                    string inputString = "";
                    if(this.player1.up)
                        inputString += "^";
                    else
                        inputString += " ";
                    if(this.player1.down)
                        inputString += "_";
                    else
                        inputString += " ";
                    if(this.player1.left)
                        inputString += "<";
                    else
                        inputString += " ";
                    if(this.player1.right)
                        inputString += ">";
                    else
                        inputString += " ";
                    if(this.player1.start)
                        inputString += "*&";
                    else
                        inputString += "  ";
                    if(this.player1.select)
                        inputString += "*$";
                    else
                        inputString += "  ";
                    if(this.player1.a)
                        inputString += "A";
                    else
                        inputString += " ";
                    if(this.player1.b)
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
                device.EndScene();
                device.Present();
            }
            catch (Direct3D9Exception e)
            {
                if (!this.closed)
                {
                    if (e.ResultCode == SlimDX.Direct3D9.ResultCode.DeviceLost)
                    {
                        reinitializeD3D = true;
                    }
#if DEBUG
                    else
                        MessageBox.Show(e.Message,"D3D ON CLOSING");
#endif
                }
#if DEBUG
                else
                    MessageBox.Show(e.Message,"D3D");
#endif
            }
            catch (Exception e)
            {
#if DEBUG
                MessageBox.Show(e.Message,"OTHER EXCEP");
#endif

            }
        }
        VertexBuffer vertexBuffer;
        private void CreateScreenBuffer()
        {
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
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 0)
                Application.Run(new Program());
            else
                Application.Run(new Program(args[0]));
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SystemState old = state;
            state = SystemState.SystemPause;
            if (this.openFile.ShowDialog() == DialogResult.OK)
            {
                state = old;
                this.OpenFile(this.openFile.FileName);
                this.openFile.InitialDirectory = Path.GetDirectoryName(this.openFile.FileName);
            }
            else
                state = old;
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
                this.cpu.restart();
                this.LoadGame();
                this.message = "Reset";
                this.messageDuration = 90;
            }
            else if (e.KeyCode == keyBindings.Power)
            {
                this.SaveGame();
                this.StartEmu();
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
        private bool fullScreen = false;
        private Point smallLocation;
        private Size smallSize;
        private void ToggleFullScreen()
        {
            if (this.imageScaler.resizeable)
            {
                if (fullScreen)
                {
                    this.menuStrip.Show();
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.Size = this.smallSize;
                    this.Location = this.smallLocation;
                    this.fullScreen = false;
                }
                else
                {
                    this.smallLocation = this.Location;
                    this.smallSize = this.Size;
                    this.menuStrip.Hide();
                    this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    this.Location = new Point(0, 0);
                    this.Size = SystemInformation.PrimaryMonitorSize;
                    if (this.imageScaler.maintainAspectRatio)
                    {
                        int height = this.insideSize.Height;
                        int width = this.insideSize.Width;
                        if (height / 15.0 > width / 16.0)
                        {
                            this.surfaceControl.Width = width;
                            this.surfaceControl.Height = (int)(width * (15.0 / 16.0));
                            this.surfaceControl.Location = new Point(0, (int)(((height - (width * (15.0 / 16.0))) / 2.0)));
                        }
                        else
                        {
                            this.surfaceControl.Height = height;
                            this.surfaceControl.Width = (int)(height * (16.0 / 15.0));
                            this.surfaceControl.Location = new Point((int)((width - (height * (16.0 / 15.0))) / 2.0), 0);
                        }
                    }
                    else
                    {
                        this.surfaceControl.Width = this.insideSize.Width;
                        this.surfaceControl.Height = this.insideSize.Height;
                        this.surfaceControl.Location = new Point(0, 0);
                    }
                    this.fullScreen = true;
                }
                pps.BackBufferHeight = this.surfaceControl.Height;
                pps.BackBufferWidth = this.surfaceControl.Width;
                ResetDevice();
            }
        }
        private void ResetDevice()
        {
            try
            {
                if (device != null)
                {
                    SystemState old = state;
                    state = SystemState.SystemPause;
                    Thread.Sleep(100);
                    texture.Dispose();
                    messageSprite.Dispose();
                    device.Reset(pps);
                    texture = new Texture(device, this.imageScaler.xSize, this.imageScaler.ySize, 0, Usage.Dynamic, Format.A8R8G8B8, Pool.Default);
                    LoadCharSheet();
                    CreateScreenBuffer();
                    state = old;
                }
            }
            catch(Exception e)
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
        private void Fm2Reader()
        {
            String line = " ";
            while (line[0] != '|')
                line = fm2File.ReadLine();
            if (line[3] != '.')
                this.player1.right = true;
            else
                this.player1.right = false;
            if (line[4] != '.')
                this.player1.left = true;
            else
                this.player1.left = false;
            if (line[5] != '.')
                this.player1.down = true;
            else
                this.player1.down = false;
            if (line[6] != '.')
                this.player1.up = true;
            else
                this.player1.up = false;
            if (line[7] != '.')
                this.player1.start = true;
            else
                this.player1.start = false;
            if (line[8] != '.')
                this.player1.select = true;
            else
                this.player1.select = false;
            if (line[9] != '.')
                this.player1.b = true;
            else
                this.player1.b = false;
            if (line[10] != '.')
                this.player1.a = true;
            else
                this.player1.a = false;
            if (line[12] != '.')
                this.player2.right = true;
            else
                this.player2.right = false;
            if (line[13] != '.')
                this.player2.left = true;
            else
                this.player2.left = false;
            if (line[14] != '.')
                this.player2.down = true;
            else
                this.player2.down = false;
            if (line[15] != '.')
                this.player2.up = true;
            else
                this.player2.up = false;
            if (line[16] != '.')
                this.player2.start = true;
            else
                this.player2.start = false;
            if (line[17] != '.')
                this.player2.select = true;
            else
                this.player2.select = false;
            if (line[18] != '.')
                this.player2.b = true;
            else
                this.player2.b = false;
            if (line[19] != '.')
                this.player2.a = true;
            else
                this.player2.a = false;

        }

        private void openMovieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.openMovieDialog.ShowDialog() == DialogResult.OK)
            {
                this.fm2File = File.OpenText(this.openMovieDialog.FileName);
                this.playMovieToolStripMenuItem.Enabled = true;

            }
        }

        private void playMovieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.playMovie)
            {
                this.playMovie = false;
                this.playMovieToolStripMenuItem.Text = "Play Movie";
            }
            else
            {
                this.playMovie = true;
                this.playMovieToolStripMenuItem.Text = "Stop Movie";
            }
        }
        private void ShowLog()
        {
            File.WriteAllText("log.txt", this.cpu.logBuilder.ToString());
            Process log = new Process();
            log.StartInfo.FileName = this.config["logReader"];
            log.StartInfo.Arguments = "log.txt";
            log.Start();
        }

        private void loadPaletteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.openPaletteDialog.ShowDialog() == DialogResult.OK)
            {
                FileStream palFile = File.OpenRead(this.openPaletteDialog.FileName);
                this.config["palette"] = this.openPaletteDialog.FileName;
                int i = 0;
                while (palFile.Position < palFile.Length)
                {
                    this.colorChart[0][i] = Color.FromArgb(palFile.ReadByte(), palFile.ReadByte(), palFile.ReadByte());
                    i++;
                }
                palFile.Close();
                CreateEmphasisTables();
            }
        }

        private void enableLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.cpu.logging = !this.cpu.logging;
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
        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Program));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.recentFileMenu1 = new System.Windows.Forms.ToolStripMenuItem();
            this.recentFileMenu2 = new System.Windows.Forms.ToolStripMenuItem();
            this.recentFileMenu3 = new System.Windows.Forms.ToolStripMenuItem();
            this.recentFileMenu4 = new System.Windows.Forms.ToolStripMenuItem();
            this.recentFileMenu5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.recordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recordWAVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopWAVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openWithFXCEUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadPaletteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.videoModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sizeableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fillToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.xToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.scale2xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.scale3xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.displayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showFPSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showInputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.spriteLimitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.spritesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.backgroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.keyBindingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gameGenieCodesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nameTablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.patternTablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.moiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMovieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playMovieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableLoggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.romInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutEmuoTronToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFile = new System.Windows.Forms.OpenFileDialog();
            this.surfaceControl = new System.Windows.Forms.Panel();
            this.insideSize = new System.Windows.Forms.Panel();
            this.openPaletteDialog = new System.Windows.Forms.OpenFileDialog();
            this.openMovieDialog = new System.Windows.Forms.OpenFileDialog();
            this.recordDialog = new System.Windows.Forms.SaveFileDialog();
            this.enableSoundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip.SuspendLayout();
            this.insideSize.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.moiveToolStripMenuItem,
            this.logToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(512, 24);
            this.menuStrip.TabIndex = 2;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.toolStripSeparator1,
            this.recentFileMenu1,
            this.recentFileMenu2,
            this.recentFileMenu3,
            this.recentFileMenu4,
            this.recentFileMenu5,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem,
            this.toolStripSeparator3,
            this.recordToolStripMenuItem,
            this.openWithFXCEUToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadToolStripMenuItem
            // 
            this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            this.loadToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.loadToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.loadToolStripMenuItem.Text = "Open...";
            this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // recentFileMenu1
            // 
            this.recentFileMenu1.Enabled = false;
            this.recentFileMenu1.Name = "recentFileMenu1";
            this.recentFileMenu1.Size = new System.Drawing.Size(180, 22);
            this.recentFileMenu1.Text = "Recent Files";
            this.recentFileMenu1.Click += new System.EventHandler(this.recentFileMenu1_Click);
            // 
            // recentFileMenu2
            // 
            this.recentFileMenu2.Name = "recentFileMenu2";
            this.recentFileMenu2.Size = new System.Drawing.Size(180, 22);
            this.recentFileMenu2.Text = "toolStripMenuItem3";
            this.recentFileMenu2.Visible = false;
            this.recentFileMenu2.Click += new System.EventHandler(this.recentFileMenu2_Click);
            // 
            // recentFileMenu3
            // 
            this.recentFileMenu3.Name = "recentFileMenu3";
            this.recentFileMenu3.Size = new System.Drawing.Size(180, 22);
            this.recentFileMenu3.Text = "toolStripMenuItem4";
            this.recentFileMenu3.Visible = false;
            this.recentFileMenu3.Click += new System.EventHandler(this.recentFileMenu3_Click);
            // 
            // recentFileMenu4
            // 
            this.recentFileMenu4.Name = "recentFileMenu4";
            this.recentFileMenu4.Size = new System.Drawing.Size(180, 22);
            this.recentFileMenu4.Text = "toolStripMenuItem5";
            this.recentFileMenu4.Visible = false;
            this.recentFileMenu4.Click += new System.EventHandler(this.recentFileMenu4_Click);
            // 
            // recentFileMenu5
            // 
            this.recentFileMenu5.Name = "recentFileMenu5";
            this.recentFileMenu5.Size = new System.Drawing.Size(180, 22);
            this.recentFileMenu5.Text = "toolStripMenuItem1";
            this.recentFileMenu5.Visible = false;
            this.recentFileMenu5.Click += new System.EventHandler(this.recentFileMenu5_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(177, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.X)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(177, 6);
            // 
            // recordToolStripMenuItem
            // 
            this.recordToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.recordWAVToolStripMenuItem,
            this.stopWAVToolStripMenuItem});
            this.recordToolStripMenuItem.Name = "recordToolStripMenuItem";
            this.recordToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.recordToolStripMenuItem.Text = "Record";
            // 
            // recordWAVToolStripMenuItem
            // 
            this.recordWAVToolStripMenuItem.Name = "recordWAVToolStripMenuItem";
            this.recordWAVToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.recordWAVToolStripMenuItem.Text = "Record WAV";
            this.recordWAVToolStripMenuItem.Click += new System.EventHandler(this.recordWAVToolStripMenuItem_Click);
            // 
            // stopWAVToolStripMenuItem
            // 
            this.stopWAVToolStripMenuItem.Enabled = false;
            this.stopWAVToolStripMenuItem.Name = "stopWAVToolStripMenuItem";
            this.stopWAVToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.stopWAVToolStripMenuItem.Text = "Stop WAV";
            this.stopWAVToolStripMenuItem.Click += new System.EventHandler(this.stopWAVToolStripMenuItem_Click);
            // 
            // openWithFXCEUToolStripMenuItem
            // 
            this.openWithFXCEUToolStripMenuItem.Name = "openWithFXCEUToolStripMenuItem";
            this.openWithFXCEUToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openWithFXCEUToolStripMenuItem.Text = "Open with FCEUX";
            this.openWithFXCEUToolStripMenuItem.Click += new System.EventHandler(this.openWithFCEUXToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableSoundToolStripMenuItem,
            this.loadPaletteToolStripMenuItem,
            this.videoModeToolStripMenuItem,
            this.displayToolStripMenuItem,
            this.keyBindingsToolStripMenuItem,
            this.gameGenieCodesToolStripMenuItem,
            this.nameTablesToolStripMenuItem,
            this.patternTablesToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // loadPaletteToolStripMenuItem
            // 
            this.loadPaletteToolStripMenuItem.Name = "loadPaletteToolStripMenuItem";
            this.loadPaletteToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.loadPaletteToolStripMenuItem.Text = "Load Palette...";
            this.loadPaletteToolStripMenuItem.Click += new System.EventHandler(this.loadPaletteToolStripMenuItem_Click);
            // 
            // videoModeToolStripMenuItem
            // 
            this.videoModeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sizeableToolStripMenuItem,
            this.fillToolStripMenuItem,
            this.xToolStripMenuItem,
            this.xToolStripMenuItem1,
            this.scale2xToolStripMenuItem,
            this.scale3xToolStripMenuItem});
            this.videoModeToolStripMenuItem.Name = "videoModeToolStripMenuItem";
            this.videoModeToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.videoModeToolStripMenuItem.Text = "Video Mode";
            // 
            // sizeableToolStripMenuItem
            // 
            this.sizeableToolStripMenuItem.Name = "sizeableToolStripMenuItem";
            this.sizeableToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.sizeableToolStripMenuItem.Text = "Resizable";
            this.sizeableToolStripMenuItem.Click += new System.EventHandler(this.sizeableToolStripMenuItem_Click);
            // 
            // fillToolStripMenuItem
            // 
            this.fillToolStripMenuItem.Name = "fillToolStripMenuItem";
            this.fillToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.fillToolStripMenuItem.Text = "Fill";
            this.fillToolStripMenuItem.Click += new System.EventHandler(this.fillToolStripMenuItem_Click);
            // 
            // xToolStripMenuItem
            // 
            this.xToolStripMenuItem.Name = "xToolStripMenuItem";
            this.xToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.xToolStripMenuItem.Text = "1x";
            this.xToolStripMenuItem.Click += new System.EventHandler(this.xToolStripMenuItem_Click);
            // 
            // xToolStripMenuItem1
            // 
            this.xToolStripMenuItem1.Name = "xToolStripMenuItem1";
            this.xToolStripMenuItem1.Size = new System.Drawing.Size(122, 22);
            this.xToolStripMenuItem1.Text = "2x";
            this.xToolStripMenuItem1.Click += new System.EventHandler(this.xToolStripMenuItem1_Click);
            // 
            // scale2xToolStripMenuItem
            // 
            this.scale2xToolStripMenuItem.Name = "scale2xToolStripMenuItem";
            this.scale2xToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.scale2xToolStripMenuItem.Text = "Scale2x";
            this.scale2xToolStripMenuItem.Click += new System.EventHandler(this.scale2xToolStripMenuItem_Click);
            // 
            // scale3xToolStripMenuItem
            // 
            this.scale3xToolStripMenuItem.Name = "scale3xToolStripMenuItem";
            this.scale3xToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.scale3xToolStripMenuItem.Text = "Scale3x";
            this.scale3xToolStripMenuItem.Click += new System.EventHandler(this.scale3xToolStripMenuItem_Click);
            // 
            // displayToolStripMenuItem
            // 
            this.displayToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showFPSToolStripMenuItem,
            this.showInputToolStripMenuItem,
            this.toolStripSeparator5,
            this.spriteLimitToolStripMenuItem,
            this.spritesToolStripMenuItem,
            this.backgroundToolStripMenuItem});
            this.displayToolStripMenuItem.Name = "displayToolStripMenuItem";
            this.displayToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.displayToolStripMenuItem.Text = "Display";
            // 
            // showFPSToolStripMenuItem
            // 
            this.showFPSToolStripMenuItem.CheckOnClick = true;
            this.showFPSToolStripMenuItem.Name = "showFPSToolStripMenuItem";
            this.showFPSToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.showFPSToolStripMenuItem.Text = "Show FPS";
            this.showFPSToolStripMenuItem.CheckedChanged += new System.EventHandler(this.showFPSToolStripMenuItem_CheckedChanged);
            // 
            // showInputToolStripMenuItem
            // 
            this.showInputToolStripMenuItem.Name = "showInputToolStripMenuItem";
            this.showInputToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.showInputToolStripMenuItem.Text = "Show Input";
            this.showInputToolStripMenuItem.Click += new System.EventHandler(this.showInputToolStripMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(172, 6);
            // 
            // spriteLimitToolStripMenuItem
            // 
            this.spriteLimitToolStripMenuItem.Checked = true;
            this.spriteLimitToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.spriteLimitToolStripMenuItem.Name = "spriteLimitToolStripMenuItem";
            this.spriteLimitToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.spriteLimitToolStripMenuItem.Text = "Disable Sprite Limit";
            this.spriteLimitToolStripMenuItem.Click += new System.EventHandler(this.spriteLimitToolStripMenuItem_Click);
            // 
            // spritesToolStripMenuItem
            // 
            this.spritesToolStripMenuItem.Checked = true;
            this.spritesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.spritesToolStripMenuItem.Name = "spritesToolStripMenuItem";
            this.spritesToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.spritesToolStripMenuItem.Text = "Sprites";
            this.spritesToolStripMenuItem.Click += new System.EventHandler(this.spritesToolStripMenuItem_Click);
            // 
            // backgroundToolStripMenuItem
            // 
            this.backgroundToolStripMenuItem.Checked = true;
            this.backgroundToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.backgroundToolStripMenuItem.Name = "backgroundToolStripMenuItem";
            this.backgroundToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.backgroundToolStripMenuItem.Text = "Background";
            this.backgroundToolStripMenuItem.Click += new System.EventHandler(this.backgroundToolStripMenuItem_Click);
            // 
            // keyBindingsToolStripMenuItem
            // 
            this.keyBindingsToolStripMenuItem.Name = "keyBindingsToolStripMenuItem";
            this.keyBindingsToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.keyBindingsToolStripMenuItem.Text = "Key Bindings...";
            this.keyBindingsToolStripMenuItem.Click += new System.EventHandler(this.keyBindingsToolStripMenuItem_Click);
            // 
            // gameGenieCodesToolStripMenuItem
            // 
            this.gameGenieCodesToolStripMenuItem.Name = "gameGenieCodesToolStripMenuItem";
            this.gameGenieCodesToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.gameGenieCodesToolStripMenuItem.Text = "Game Genie Codes...";
            this.gameGenieCodesToolStripMenuItem.Click += new System.EventHandler(this.gameGenieCodesToolStripMenuItem_Click);
            // 
            // nameTablesToolStripMenuItem
            // 
            this.nameTablesToolStripMenuItem.Name = "nameTablesToolStripMenuItem";
            this.nameTablesToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.nameTablesToolStripMenuItem.Text = "Name Tables...";
            this.nameTablesToolStripMenuItem.Click += new System.EventHandler(this.nameTablesToolStripMenuItem_Click);
            // 
            // patternTablesToolStripMenuItem
            // 
            this.patternTablesToolStripMenuItem.Name = "patternTablesToolStripMenuItem";
            this.patternTablesToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.patternTablesToolStripMenuItem.Text = "Pattern Tables...";
            this.patternTablesToolStripMenuItem.Click += new System.EventHandler(this.patternTablesToolStripMenuItem_Click);
            // 
            // moiveToolStripMenuItem
            // 
            this.moiveToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openMovieToolStripMenuItem,
            this.playMovieToolStripMenuItem});
            this.moiveToolStripMenuItem.Name = "moiveToolStripMenuItem";
            this.moiveToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.moiveToolStripMenuItem.Text = "Moive";
            // 
            // openMovieToolStripMenuItem
            // 
            this.openMovieToolStripMenuItem.Name = "openMovieToolStripMenuItem";
            this.openMovieToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.openMovieToolStripMenuItem.Text = "Open Movie...";
            this.openMovieToolStripMenuItem.Click += new System.EventHandler(this.openMovieToolStripMenuItem_Click);
            // 
            // playMovieToolStripMenuItem
            // 
            this.playMovieToolStripMenuItem.Enabled = false;
            this.playMovieToolStripMenuItem.Name = "playMovieToolStripMenuItem";
            this.playMovieToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.playMovieToolStripMenuItem.Text = "Play Movie";
            this.playMovieToolStripMenuItem.Click += new System.EventHandler(this.playMovieToolStripMenuItem_Click);
            // 
            // logToolStripMenuItem
            // 
            this.logToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableLoggingToolStripMenuItem,
            this.openLogToolStripMenuItem});
            this.logToolStripMenuItem.Name = "logToolStripMenuItem";
            this.logToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.logToolStripMenuItem.Text = "Log";
            // 
            // enableLoggingToolStripMenuItem
            // 
            this.enableLoggingToolStripMenuItem.Name = "enableLoggingToolStripMenuItem";
            this.enableLoggingToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.enableLoggingToolStripMenuItem.Text = "Enable Logging";
            this.enableLoggingToolStripMenuItem.Click += new System.EventHandler(this.enableLoggingToolStripMenuItem_Click);
            // 
            // openLogToolStripMenuItem
            // 
            this.openLogToolStripMenuItem.Name = "openLogToolStripMenuItem";
            this.openLogToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.openLogToolStripMenuItem.Text = "Open Log...";
            this.openLogToolStripMenuItem.Click += new System.EventHandler(this.openLogToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpToolStripMenuItem1,
            this.romInfoToolStripMenuItem,
            this.toolStripSeparator4,
            this.aboutEmuoTronToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // helpToolStripMenuItem1
            // 
            this.helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
            this.helpToolStripMenuItem1.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.helpToolStripMenuItem1.Size = new System.Drawing.Size(176, 22);
            this.helpToolStripMenuItem1.Text = "Help...";
            this.helpToolStripMenuItem1.Click += new System.EventHandler(this.helpToolStripMenuItem1_Click);
            // 
            // romInfoToolStripMenuItem
            // 
            this.romInfoToolStripMenuItem.Name = "romInfoToolStripMenuItem";
            this.romInfoToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.romInfoToolStripMenuItem.Text = "Rom Info...";
            this.romInfoToolStripMenuItem.Click += new System.EventHandler(this.romInfoToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(173, 6);
            // 
            // aboutEmuoTronToolStripMenuItem
            // 
            this.aboutEmuoTronToolStripMenuItem.Name = "aboutEmuoTronToolStripMenuItem";
            this.aboutEmuoTronToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.aboutEmuoTronToolStripMenuItem.Text = "About Emu-o-Tron";
            this.aboutEmuoTronToolStripMenuItem.Click += new System.EventHandler(this.aboutEmuoTronToolStripMenuItem_Click);
            // 
            // openFile
            // 
            this.openFile.DefaultExt = "nes";
            this.openFile.Filter = "Supported File Types|*.nes;*.rar;*.zip;*.7z;*.ips;*.ups|NES Roms|*.nes|Archives|*" +
                ".rar;*.zip;*.7z|Patches|*.ips;*.ups|All Files|*.*";
            this.openFile.Title = "Load Rom";
            // 
            // surfaceControl
            // 
            this.surfaceControl.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.surfaceControl.BackColor = System.Drawing.Color.Black;
            this.surfaceControl.Location = new System.Drawing.Point(0, 24);
            this.surfaceControl.Name = "surfaceControl";
            this.surfaceControl.Size = new System.Drawing.Size(512, 480);
            this.surfaceControl.TabIndex = 3;
            // 
            // insideSize
            // 
            this.insideSize.BackColor = System.Drawing.Color.Black;
            this.insideSize.Controls.Add(this.surfaceControl);
            this.insideSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.insideSize.Location = new System.Drawing.Point(0, 0);
            this.insideSize.Name = "insideSize";
            this.insideSize.Size = new System.Drawing.Size(512, 504);
            this.insideSize.TabIndex = 0;
            // 
            // openPaletteDialog
            // 
            this.openPaletteDialog.DefaultExt = "pal";
            this.openPaletteDialog.Filter = "Palette Files (*.pal)|*.pal|All Files (*.*)|*.*";
            this.openPaletteDialog.Title = "Load Palette";
            // 
            // openMovieDialog
            // 
            this.openMovieDialog.DefaultExt = "fm2";
            this.openMovieDialog.Filter = "FM2 Files (*.fm2)|*.fm2|All Files (*.*)|*.*";
            this.openMovieDialog.Title = "Open Movie";
            // 
            // recordDialog
            // 
            this.recordDialog.DefaultExt = "wav";
            this.recordDialog.Filter = "Wav files (*.wav)|*.wav";
            // 
            // enableSoundToolStripMenuItem
            // 
            this.enableSoundToolStripMenuItem.Name = "enableSoundToolStripMenuItem";
            this.enableSoundToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.enableSoundToolStripMenuItem.Text = "Enable Sound";
            this.enableSoundToolStripMenuItem.Click += new System.EventHandler(this.enableSoundToolStripMenuItem_Click);
            // 
            // Program
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(512, 504);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.insideSize);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(272, 302);
            this.Name = "Program";
            this.Text = "Emu-o-Tron";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Program_FormClosing);
            this.ResizeEnd += new System.EventHandler(this.Program_Resize);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Program_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Program_DragEnter);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EmuWindow_KeyUp);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.insideSize.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFile;
        private System.Windows.Forms.Panel surfaceControl;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem openWithFXCEUToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem moiveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openMovieToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem playMovieToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem logToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadPaletteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableLoggingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gameGenieCodesToolStripMenuItem;
        private Panel insideSize;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutEmuoTronToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem1;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem showFPSToolStripMenuItem;
        private ToolStripMenuItem recentFileMenu1;
        private ToolStripMenuItem recentFileMenu2;
        private ToolStripMenuItem recentFileMenu3;
        private ToolStripMenuItem recentFileMenu4;
        private ToolStripMenuItem recentFileMenu5;
        private ToolStripMenuItem nameTablesToolStripMenuItem;
        private ToolStripMenuItem patternTablesToolStripMenuItem;

        private void Program_Resize(object sender, EventArgs e)
        {
            if (this.imageScaler.maintainAspectRatio)
            {
                int height = this.insideSize.Height - this.menuStrip.Height;
                int width = this.insideSize.Width;
                if (height / 15.0 > width / 16.0)
                {
                    this.surfaceControl.Width = width;
                    this.surfaceControl.Height = (int)(width * (15.0 / 16.0));
                    this.surfaceControl.Location = new Point(0, (int)(((height - (width * (15.0 / 16.0))) / 2.0)) + this.menuStrip.Height);
                }
                else
                {
                    this.surfaceControl.Height = height;
                    this.surfaceControl.Width = (int)(height * (16.0 / 15.0));
                    this.surfaceControl.Location = new Point((int)((width - (height * (16.0 / 15.0))) / 2.0), this.menuStrip.Height);
                }
            }
            else
            {
                this.surfaceControl.Width = this.insideSize.Width;
                this.surfaceControl.Height = this.insideSize.Height - this.menuStrip.Height;
                this.surfaceControl.Location = new Point(0, this.menuStrip.Height);
            }
            this.config["width"] = this.Width.ToString();
            this.config["height"] = this.Height.ToString();
            pps.BackBufferHeight = this.surfaceControl.Height;
            pps.BackBufferWidth = this.surfaceControl.Width;
            ResetDevice();
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
                SevenZipFormat Format = new SevenZipFormat(this.config["7z"]);
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
                logState = this.cpu.logging;
            this.cpu = new NESCore(this.romPath, this.appPath);
            audioFormat.SamplesPerSecond = this.cpu.APU.CPUClock / this.cpu.APU.divider;
            sVoice = new SourceVoice(dAudio, audioFormat);/*
            FilterParameters filter = new FilterParameters();
            filter.Frequency = 0.5f;
            filter.Type = FilterType.LowPassFilter;
            filter.OneOverQ = 1.5f;
            sVoice.FilterParameters = filter;*/
            sVoice.Start();
            this.cpu.logging = logState;
            this.cpu.displayBG = (config["displayBG"] == "1");
            this.cpu.displaySprites = (config["displaySprites"] == "1");
            this.cpu.displaySpriteLimit = !(config["disableSpriteLimit"] == "1");
            this.cpu.APU.mute = !(config["sound"] == "1");
            this.LoadGame();
            this.LoadSaveStateFiles();
            this.cpu.gameGenieCodeNum = this.gameGenieCodeCount;
            this.cpu.gameGenieCodes = this.gameGenieCodes;
            this.Text = this.cpu.fileName + " - Emu-o-Tron";
            this.state = SystemState.Playing;
            this.surfaceControl.Visible = true;

        }
        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this.ActiveControl, this.config["helpFile"]);
        }

        private void nameTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!this.generateNameTables)
            {
                this.nameTablePreview = new NameTablePreview(this.colorChart[0], this.generateLine);
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
                this.patternTablePreview = new PatternTablePreview(this.colorChart[0], this.generatePatternLine);
                this.patternTablePreview.FormClosed += new FormClosedEventHandler(this.patternTablePreviewForm_Closed);
                this.generatePatternTables = true;
                patternTablePreview.Show();
            }
            else
                this.patternTablePreview.Activate();
        }

        private void showFPSToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            this.showFPS = !this.showFPS;
            if (this.showFPS)
                this.config["showFPS"] = "1";
            else
                this.config["showFPS"] = "0";
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

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
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
                if (this.cpu.sramPresent)
                    File.WriteAllBytes(Path.Combine(this.config["sramDir"], this.cpu.fileName + ".sav"), this.cpu.GetSRAM());
        }
        private void LoadGame()
        {
            Directory.CreateDirectory(this.config["sramDir"]);
            if (this.cpu != null)
            {
                if (this.cpu.sramPresent)
                {
                    if (File.Exists(Path.Combine(this.config["sramDir"], this.cpu.fileName + ".sav")))
                        this.cpu.SetSRAM(File.ReadAllBytes(Path.Combine(this.config["sramDir"], this.cpu.fileName + ".sav")));
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
            this.closed = true;
            Thread.Sleep(32);
        }

        private void romInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.cpu != null)
            {
                RomInfoBox romInfoBox = new RomInfoBox(cpu.romInfo.ToString());
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
            Keybind keyBindWindow = new Keybind(keyBindings);
            if (keyBindWindow.ShowDialog() == DialogResult.OK)
                keyBindings = keyBindWindow.keys;
            state = old;
        }

        private void spritesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cpu != null)
                cpu.displaySprites = !cpu.displaySprites;
            this.spritesToolStripMenuItem.Checked = !this.spritesToolStripMenuItem.Checked;
            config["displaySprites"] = this.spritesToolStripMenuItem.Checked ? "1" : "0";
        }

        private void backgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cpu != null)
                cpu.displayBG = !cpu.displayBG;
            this.backgroundToolStripMenuItem.Checked = !this.backgroundToolStripMenuItem.Checked;
            config["displayBG"] = this.backgroundToolStripMenuItem.Checked ? "1" : "0";
        }

        private void spriteLimitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cpu != null)
                cpu.displaySpriteLimit = !cpu.displaySpriteLimit;
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
            SystemState old = state;
            state = SystemState.SystemPause;
            Thread.Sleep(100);
            imageScaler = new Sizeable();
            state = old;
            PrepareScaler();
            config["scaler"] = "sizeable";
        }

        private void fillToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            fillToolStripMenuItem.Checked = true;
            SystemState old = state;
            state = SystemState.SystemPause;
            Thread.Sleep(100);
            imageScaler = new Fill();
            state = old;
            PrepareScaler();
            config["scaler"] = "fill";
        }

        private void xToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            xToolStripMenuItem.Checked = true;
            SystemState old = state;
            state = SystemState.SystemPause;
            Thread.Sleep(100);
            imageScaler = new NearestNeighbor1x();
            state = old;
            PrepareScaler();
            config["scaler"] = "1x";
        }

        private void xToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            xToolStripMenuItem1.Checked = true;
            SystemState old = state;
            state = SystemState.SystemPause;
            Thread.Sleep(100);
            imageScaler = new NearestNeighbor2x();
            state = old;
            PrepareScaler();
            config["scaler"] = "2x";
        }

        private void scale2xToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            scale2xToolStripMenuItem.Checked = true;
            SystemState old = state;
            state = SystemState.SystemPause;
            Thread.Sleep(100);
            imageScaler = new Scale2x();
            state = old;
            PrepareScaler();
            config["scaler"] = "scale2x";
        }

        private void scale3xToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in videoModeToolStripMenuItem.DropDownItems)
                item.Checked = false;
            scale3xToolStripMenuItem.Checked = true;
            SystemState old = state;
            state = SystemState.SystemPause;
            Thread.Sleep(100);
            imageScaler = new Scale3x();
            state = old;
            PrepareScaler();
            config["scaler"] = "scale3x";
        }
        private void PrepareScaler()
        {

            frameBuffer = new Bitmap(256, 240);
            scaledFrameBuffer = new Bitmap(this.imageScaler.xSize, this.imageScaler.ySize);
            if (imageScaler.maintainAspectRatio)
            {
                if (imageScaler.resizeable)
                {
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.MaximizeBox = true;
                    this.Width = Convert.ToInt32(config["width"]);
                    this.Height = Convert.ToInt32(config["height"]);
                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.Fixed3D;
                    this.MaximizeBox = false;
                    this.Width = (this.Width - this.insideSize.Width) + this.imageScaler.xSize;
                    this.Height = (this.Height - (this.insideSize.Height - this.menuStrip.Height)) + this.imageScaler.ySize;
                }
            }
            else
            {
                this.surfaceControl.Width = this.insideSize.Width;
                this.surfaceControl.Height = this.insideSize.Height - this.menuStrip.Height;
                this.surfaceControl.Location = new Point(0, this.menuStrip.Height);
            }
            pps.BackBufferHeight = this.surfaceControl.Height;
            pps.BackBufferWidth = this.surfaceControl.Width;
            ResetDevice();
            Program_Resize(this, new EventArgs());
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
        FileStream wavFile;
        bool wavRecord;
        int wavSamples;
        private void recordWAVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (state != SystemState.Empty)
            {
                recordDialog.FileName = cpu.fileName;
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
        }
    }
    class ArchiveCallback : IArchiveExtractCallback
    {
        private uint FileNumber;
        private string FileName;
        private OutStreamWrapper FileStream;
        private int index = 0;

        public ArchiveCallback(uint fileNumber, string fileName)
        {
            this.FileNumber = fileNumber;
            this.FileName = fileName;
        }

        #region IArchiveExtractCallback Members

        public void SetTotal(ulong total)
        {
        }

        public void SetCompleted(ref ulong completeValue)
        {
        }

        public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            if ((index == FileNumber) && (askExtractMode == AskMode.kExtract))
            {
                string FileDir = Path.GetDirectoryName(FileName);
                if (!string.IsNullOrEmpty(FileDir))
                    Directory.CreateDirectory(FileDir);
                FileStream = new OutStreamWrapper(File.Create(FileName));

                outStream = FileStream;
            }
            else
                outStream = null;
            return 0;
        }

        public void PrepareOperation(AskMode askExtractMode)
        {
        }

        public void SetOperationResult(OperationResult resultEOperationResult)
        {
            try
            {
                if(index == FileNumber)
                    FileStream.Dispose();
                index++;
                if (index == 1)
                    FileStream.Dispose(); //Stupid stupid hack to make both 7z and zip dispose the stream
            }
            catch (Exception e) //7zip exectues this function once for each file until the one requested, zip will only execute the one time for needed file. Possibly use a counter until it == index?
            {
                //MessageBox.Show(e.Message);
            }
        }
        #endregion
        
    }
    struct VertexPositionRhwTexture
    {
        public Vector4 PositionRhw;
        public Vector2 Texture1;
    }
}
