using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EmuoTron;

namespace DirectXEmu
{
    public class EmuConfig
    {
        private string configFile;
        private Dictionary<string, string> settings;
        public Dictionary<string, string> defaults;
        public EmuConfig(string path)
        {
            this.configFile = path;
            if (!File.Exists(this.configFile))
            {
                FileStream tmpFile = File.Create(this.configFile);
                tmpFile.Close();
            }
            this.defaults = this.LoadDefaults();
            this.settings = this.Load(this.configFile);
        }
        private Dictionary<string, string> LoadDefaults()
        {
            Dictionary<string, string> defaults = new Dictionary<string, string>();
            defaults["palette"] = Path.Combine("palettes","Nestopia.pal");
            defaults["paletteDir"] = @"palettes";
            defaults["movieDir"] = @"movies";
            defaults["sramDir"] = @"sav";
            defaults["savestateDir"] = @"savestates";
            defaults["romPath1"] = @"roms";
            defaults["romPath2"] = "";
            defaults["romPath3"] = "";
            defaults["romPath4"] = "";
            defaults["romPath5"] = "";
            defaults["recentFile1"] = "";
            defaults["recentFile2"] = "";
            defaults["recentFile3"] = "";
            defaults["recentFile4"] = "";
            defaults["recentFile5"] = "";
            defaults["logReader"] = "";
            defaults["previewEmu"] = "";
            defaults["showFPS"] = "0";
            defaults["showInput"] = "0";
            defaults["helpFile"] = @"Emu-o-Tron.chm";
            defaults["player1Up"] = EmuKeys.UpArrow.ToString();
            defaults["player1Down"] = EmuKeys.DownArrow.ToString();
            defaults["player1Left"] = EmuKeys.LeftArrow.ToString();
            defaults["player1Right"] = EmuKeys.RightArrow.ToString();
            defaults["player1Start"] = EmuKeys.Return.ToString();
            defaults["player1Select"] = EmuKeys.Apostrophe.ToString();
            defaults["player1A"] = EmuKeys.Z.ToString();
            defaults["player1B"] = EmuKeys.X.ToString();
            defaults["player1TurboA"] = EmuKeys.A.ToString();
            defaults["player1TurboB"] = EmuKeys.S.ToString();
            defaults["player2Up"] = EmuKeys.NumberPad8.ToString();
            defaults["player2Down"] = EmuKeys.NumberPad5.ToString();
            defaults["player2Left"] = EmuKeys.NumberPad4.ToString();
            defaults["player2Right"] = EmuKeys.NumberPad6.ToString();
            defaults["player2Start"] = EmuKeys.NumberPad7.ToString();
            defaults["player2Select"] = EmuKeys.NumberPad9.ToString();
            defaults["player2A"] = EmuKeys.NumberPad1.ToString();
            defaults["player2B"] = EmuKeys.NumberPad3.ToString();
            defaults["player2TurboA"] = EmuKeys.Home.ToString();
            defaults["player2TurboB"] = EmuKeys.End.ToString();
            defaults["fastForward"] = EmuKeys.LeftShift.ToString();
            defaults["rewind"] = EmuKeys.Tab.ToString();
            defaults["saveState"] = EmuKeys.D1.ToString();
            defaults["loadState"] = EmuKeys.D2.ToString();
            defaults["pause"] = EmuKeys.Space.ToString();
            defaults["restart"] = EmuKeys.Backspace.ToString();
            defaults["power"] = EmuKeys.Delete.ToString();
            defaults["scaler"] = "sizeable";
            defaults["width"] = "528";
            defaults["height"] = "542";
            defaults["rewindEnabled"] = "1";
            defaults["rewindBufferFreq"] = "2";
            defaults["rewindBufferSeconds"] = "30";
            defaults["7z"] = @"7z.dll";
            defaults["7z64"] = @"7z64.dll";
            defaults["tmpDir"] = @"tmp";
            defaults["disableSpriteLimit"] = "1";
            defaults["displayBG"] = "1";
            defaults["displaySprites"] = "1";
            defaults["sound"] = "1";
            defaults["volume"] = "100";
            defaults["showDebug"] = "0";
            defaults["region"] = ((int)SystemType.NTSC).ToString();
            defaults["serverPort"] = "7878";
            defaults["fdsBios"] = @"disksys.rom";
            defaults["sampleRate"] = "48000";
            defaults["portOne"] = "Controller";
            defaults["portTwo"] = "Controller";
            defaults["expansion"] = "Empty";
            defaults["fourScore"] = "0";
            defaults["filterIllegalInput"] = "1";
            defaults["smoothOutput"] = "0";
            defaults["renderer"] = "DX9";
            defaults["audio"] = "XA2";
            defaults["input"] = "Win";
            defaults["simulatedPalette"] = "1";
            defaults["gamma"] = "2.0";
            defaults["brightness"] = "1.0";
            defaults["hue"] = "3.9";
            defaults["saturation"] = "1.7";
            defaults["rawNESPalette"] = "0";

#if DEBUG
            defaults["romPath1"] = @"C:\Games\Emulators\Roms\NES";
            defaults["romPath2"] = @"C:\Games\Emulators\Roms\NES";
            defaults["romPath3"] = @"C:\Games\Emulators\Roms\MapperNes";
            defaults["romPath4"] = @"C:\Games\Emulators\Roms\TestNes";
            defaults["romPath5"] = "";
            defaults["logReader"] = @"C:\Program Files\Vim\vim72\gvim.exe";
            defaults["previewEmu"] = @"C:\Games\Emulators\FCEUX-2.1.1\fceux.exe";
            defaults["showDebug"] = "1";
#endif

            return defaults;
        }
        private Dictionary<string, string> Load(string path)
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            StreamReader conf = File.OpenText(path);
            while (!conf.EndOfStream)
            {
                string line = conf.ReadLine();
                settings[line.Substring(0, line.IndexOf(' '))] =  line.Substring(line.IndexOf(' ') + 1).Trim();
            }
            conf.Close();
            return settings;
        }
        public void Save()
        {
            StringBuilder conf = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in this.settings)
            {
                conf.AppendLine(entry.Key + " " + entry.Value);
            }
            File.WriteAllText(this.configFile, conf.ToString());
        }
        public string Get(string key)
        {
            string val;
            if (this.settings.TryGetValue(key, out val))
            {
                return val;
            }
            else
            {
                if (this.defaults.TryGetValue(key, out val))
                {
                    this.settings[key] = val;
                    return val;
                }
                else
                    throw (new Exception("Invalid setting."));
            }
        }
        public void Set(string key, string val)
        {
            this.settings[key] = val;
        }
        public string this[string key]
        {
            get
            {
                return this.Get(key);
            }
            set
            {
                this.Set(key, value);
            }
        }
    }
}
