namespace DirectXEmu
{
    partial class Program
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
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
            this.enableSoundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadPaletteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.videoModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sizeableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fillToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.xToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.scale2xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.scale3xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tVAspectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.regionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nTSCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pALToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.displayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showFPSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showInputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.spriteLimitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.spritesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.backgroundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.keyBindingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gameGenieCodesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.soundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.moiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMovieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playMovieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableLoggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nameTablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.patternTablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.memoryViewerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pPUMemoryViewerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.testConsoleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.romInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutEmuoTronToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.netPlayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.joinGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFile = new System.Windows.Forms.OpenFileDialog();
            this.openPaletteDialog = new System.Windows.Forms.OpenFileDialog();
            this.openMovieDialog = new System.Windows.Forms.OpenFileDialog();
            this.recordDialog = new System.Windows.Forms.SaveFileDialog();
            this.surfaceControl = new System.Windows.Forms.Panel();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.moiveToolStripMenuItem,
            this.logToolStripMenuItem,
            this.netPlayToolStripMenuItem,
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
            this.regionToolStripMenuItem,
            this.displayToolStripMenuItem,
            this.keyBindingsToolStripMenuItem,
            this.gameGenieCodesToolStripMenuItem,
            this.soundToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // enableSoundToolStripMenuItem
            // 
            this.enableSoundToolStripMenuItem.Name = "enableSoundToolStripMenuItem";
            this.enableSoundToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.enableSoundToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.enableSoundToolStripMenuItem.Text = "Enable Sound";
            this.enableSoundToolStripMenuItem.Click += new System.EventHandler(this.enableSoundToolStripMenuItem_Click);
            // 
            // loadPaletteToolStripMenuItem
            // 
            this.loadPaletteToolStripMenuItem.Name = "loadPaletteToolStripMenuItem";
            this.loadPaletteToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
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
            this.scale3xToolStripMenuItem,
            this.tVAspectToolStripMenuItem});
            this.videoModeToolStripMenuItem.Name = "videoModeToolStripMenuItem";
            this.videoModeToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.videoModeToolStripMenuItem.Text = "Video Mode";
            // 
            // sizeableToolStripMenuItem
            // 
            this.sizeableToolStripMenuItem.Name = "sizeableToolStripMenuItem";
            this.sizeableToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.sizeableToolStripMenuItem.Text = "Resizable";
            this.sizeableToolStripMenuItem.Click += new System.EventHandler(this.sizeableToolStripMenuItem_Click);
            // 
            // fillToolStripMenuItem
            // 
            this.fillToolStripMenuItem.Name = "fillToolStripMenuItem";
            this.fillToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.fillToolStripMenuItem.Text = "Fill";
            this.fillToolStripMenuItem.Click += new System.EventHandler(this.fillToolStripMenuItem_Click);
            // 
            // xToolStripMenuItem
            // 
            this.xToolStripMenuItem.Name = "xToolStripMenuItem";
            this.xToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.xToolStripMenuItem.Text = "1x";
            this.xToolStripMenuItem.Click += new System.EventHandler(this.xToolStripMenuItem_Click);
            // 
            // xToolStripMenuItem1
            // 
            this.xToolStripMenuItem1.Name = "xToolStripMenuItem1";
            this.xToolStripMenuItem1.Size = new System.Drawing.Size(127, 22);
            this.xToolStripMenuItem1.Text = "2x";
            this.xToolStripMenuItem1.Click += new System.EventHandler(this.xToolStripMenuItem1_Click);
            // 
            // scale2xToolStripMenuItem
            // 
            this.scale2xToolStripMenuItem.Name = "scale2xToolStripMenuItem";
            this.scale2xToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.scale2xToolStripMenuItem.Text = "Scale2x";
            this.scale2xToolStripMenuItem.Click += new System.EventHandler(this.scale2xToolStripMenuItem_Click);
            // 
            // scale3xToolStripMenuItem
            // 
            this.scale3xToolStripMenuItem.Name = "scale3xToolStripMenuItem";
            this.scale3xToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.scale3xToolStripMenuItem.Text = "Scale3x";
            this.scale3xToolStripMenuItem.Click += new System.EventHandler(this.scale3xToolStripMenuItem_Click);
            // 
            // tVAspectToolStripMenuItem
            // 
            this.tVAspectToolStripMenuItem.Name = "tVAspectToolStripMenuItem";
            this.tVAspectToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.tVAspectToolStripMenuItem.Text = "TV Aspect";
            this.tVAspectToolStripMenuItem.Click += new System.EventHandler(this.tVAspectToolStripMenuItem_Click);
            // 
            // regionToolStripMenuItem
            // 
            this.regionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nTSCToolStripMenuItem,
            this.pALToolStripMenuItem});
            this.regionToolStripMenuItem.Name = "regionToolStripMenuItem";
            this.regionToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.regionToolStripMenuItem.Text = "Region";
            // 
            // nTSCToolStripMenuItem
            // 
            this.nTSCToolStripMenuItem.Name = "nTSCToolStripMenuItem";
            this.nTSCToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.nTSCToolStripMenuItem.Text = "NTSC";
            this.nTSCToolStripMenuItem.Click += new System.EventHandler(this.nTSCToolStripMenuItem_Click);
            // 
            // pALToolStripMenuItem
            // 
            this.pALToolStripMenuItem.Name = "pALToolStripMenuItem";
            this.pALToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.pALToolStripMenuItem.Text = "PAL";
            this.pALToolStripMenuItem.Click += new System.EventHandler(this.pALToolStripMenuItem_Click);
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
            this.displayToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
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
            this.keyBindingsToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.keyBindingsToolStripMenuItem.Text = "Key Bindings...";
            this.keyBindingsToolStripMenuItem.Click += new System.EventHandler(this.keyBindingsToolStripMenuItem_Click);
            // 
            // gameGenieCodesToolStripMenuItem
            // 
            this.gameGenieCodesToolStripMenuItem.Name = "gameGenieCodesToolStripMenuItem";
            this.gameGenieCodesToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.gameGenieCodesToolStripMenuItem.Text = "Game Genie Codes...";
            this.gameGenieCodesToolStripMenuItem.Click += new System.EventHandler(this.gameGenieCodesToolStripMenuItem_Click);
            // 
            // soundToolStripMenuItem
            // 
            this.soundToolStripMenuItem.Name = "soundToolStripMenuItem";
            this.soundToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.soundToolStripMenuItem.Text = "Sound...";
            this.soundToolStripMenuItem.Click += new System.EventHandler(this.soundToolStripMenuItem_Click);
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
            this.openLogToolStripMenuItem,
            this.nameTablesToolStripMenuItem,
            this.patternTablesToolStripMenuItem,
            this.memoryViewerToolStripMenuItem,
            this.pPUMemoryViewerToolStripMenuItem,
            this.testConsoleToolStripMenuItem});
            this.logToolStripMenuItem.Name = "logToolStripMenuItem";
            this.logToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.logToolStripMenuItem.Text = "Debug";
            // 
            // enableLoggingToolStripMenuItem
            // 
            this.enableLoggingToolStripMenuItem.Name = "enableLoggingToolStripMenuItem";
            this.enableLoggingToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.enableLoggingToolStripMenuItem.Text = "Enable Logging";
            this.enableLoggingToolStripMenuItem.Click += new System.EventHandler(this.enableLoggingToolStripMenuItem_Click);
            // 
            // openLogToolStripMenuItem
            // 
            this.openLogToolStripMenuItem.Name = "openLogToolStripMenuItem";
            this.openLogToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.openLogToolStripMenuItem.Text = "Open Log...";
            this.openLogToolStripMenuItem.Click += new System.EventHandler(this.openLogToolStripMenuItem_Click);
            // 
            // nameTablesToolStripMenuItem
            // 
            this.nameTablesToolStripMenuItem.Name = "nameTablesToolStripMenuItem";
            this.nameTablesToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.nameTablesToolStripMenuItem.Text = "Name Tables...";
            this.nameTablesToolStripMenuItem.Click += new System.EventHandler(this.nameTablesToolStripMenuItem_Click);
            // 
            // patternTablesToolStripMenuItem
            // 
            this.patternTablesToolStripMenuItem.Name = "patternTablesToolStripMenuItem";
            this.patternTablesToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.patternTablesToolStripMenuItem.Text = "Pattern Tables...";
            this.patternTablesToolStripMenuItem.Click += new System.EventHandler(this.patternTablesToolStripMenuItem_Click);
            // 
            // memoryViewerToolStripMenuItem
            // 
            this.memoryViewerToolStripMenuItem.Name = "memoryViewerToolStripMenuItem";
            this.memoryViewerToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.memoryViewerToolStripMenuItem.Text = "Memory Viewer...";
            this.memoryViewerToolStripMenuItem.Click += new System.EventHandler(this.memoryViewerToolStripMenuItem_Click);
            // 
            // pPUMemoryViewerToolStripMenuItem
            // 
            this.pPUMemoryViewerToolStripMenuItem.Name = "pPUMemoryViewerToolStripMenuItem";
            this.pPUMemoryViewerToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.pPUMemoryViewerToolStripMenuItem.Text = "PPU Memory Viewer...";
            this.pPUMemoryViewerToolStripMenuItem.Click += new System.EventHandler(this.pPUMemoryViewerToolStripMenuItem_Click);
            // 
            // testConsoleToolStripMenuItem
            // 
            this.testConsoleToolStripMenuItem.Name = "testConsoleToolStripMenuItem";
            this.testConsoleToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.testConsoleToolStripMenuItem.Text = "Test Console...";
            this.testConsoleToolStripMenuItem.Click += new System.EventHandler(this.testConsoleToolStripMenuItem_Click);
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
            // netPlayToolStripMenuItem
            // 
            this.netPlayToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startGameToolStripMenuItem,
            this.joinGameToolStripMenuItem});
            this.netPlayToolStripMenuItem.Name = "netPlayToolStripMenuItem";
            this.netPlayToolStripMenuItem.Size = new System.Drawing.Size(63, 20);
            this.netPlayToolStripMenuItem.Text = "Net Play";
            // 
            // startGameToolStripMenuItem
            // 
            this.startGameToolStripMenuItem.Name = "startGameToolStripMenuItem";
            this.startGameToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.startGameToolStripMenuItem.Text = "Start Game";
            this.startGameToolStripMenuItem.Click += new System.EventHandler(this.startGameToolStripMenuItem_Click);
            // 
            // joinGameToolStripMenuItem
            // 
            this.joinGameToolStripMenuItem.Name = "joinGameToolStripMenuItem";
            this.joinGameToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.joinGameToolStripMenuItem.Text = "Join Game";
            this.joinGameToolStripMenuItem.Click += new System.EventHandler(this.joinGameToolStripMenuItem_Click);
            // 
            // openFile
            // 
            this.openFile.DefaultExt = "nes";
            this.openFile.Filter = "Supported File Types|*.nes;*.rar;*.zip;*.7z;*.ips;*.ups|NES Roms|*.nes|Archives|*" +
                ".rar;*.zip;*.7z|Patches|*.ips;*.ups|All Files|*.*";
            this.openFile.Title = "Load Rom";
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
            // surfaceControl
            // 
            this.surfaceControl.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.surfaceControl.BackColor = System.Drawing.Color.Black;
            this.surfaceControl.Location = new System.Drawing.Point(0, 24);
            this.surfaceControl.Name = "surfaceControl";
            this.surfaceControl.Size = new System.Drawing.Size(512, 480);
            this.surfaceControl.TabIndex = 3;
            // 
            // Program
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(512, 504);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.surfaceControl);
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
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutEmuoTronToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem showFPSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recentFileMenu1;
        private System.Windows.Forms.ToolStripMenuItem recentFileMenu2;
        private System.Windows.Forms.ToolStripMenuItem recentFileMenu3;
        private System.Windows.Forms.ToolStripMenuItem recentFileMenu4;
        private System.Windows.Forms.ToolStripMenuItem recentFileMenu5;
        private System.Windows.Forms.ToolStripMenuItem nameTablesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem patternTablesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem romInfoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem keyBindingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem displayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem spritesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem backgroundToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem showInputToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem videoModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sizeableToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fillToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem xToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem xToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem scale2xToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scale3xToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem spriteLimitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recordWAVToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopWAVToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog recordDialog;
        private System.Windows.Forms.ToolStripMenuItem enableSoundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem soundToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem memoryViewerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pPUMemoryViewerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem testConsoleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem regionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nTSCToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pALToolStripMenuItem;
        private System.Windows.Forms.Panel surfaceControl;
        private System.Windows.Forms.Panel insideSize;
        private System.Windows.Forms.ToolStripMenuItem tVAspectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem netPlayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startGameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem joinGameToolStripMenuItem;
    }
}
