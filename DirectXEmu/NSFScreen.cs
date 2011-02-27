using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using EmuoTron;
using System.IO;

namespace DirectXEmu
{
    class NSFScreen
    {
        NESCore nes;
        private uint[,] charSheet;
        private Dictionary<char, int> charSheetSprites = new Dictionary<char, int>();
        private int charSize;
        public NSFScreen(NESCore nes)
        {
            this.nes = nes;
            LoadCharSheet();
        }
        public void ReDraw()
        {
            for (int x = 0; x < 256; x++)
                for (int y = 0; y < 240; y++)
                    nes.PPU.screen[y, x] = 0xFF000000;
            DrawMessage("Title: " + ((EmuoTron.Mappers.mNSF)(nes.mapper)).songName, 1, 3);
            DrawMessage("Artist: " + ((EmuoTron.Mappers.mNSF)(nes.mapper)).artist, 1, 5);
            DrawMessage("Copyright: " + ((EmuoTron.Mappers.mNSF)(nes.mapper)).copyright, 1, 7);

            DrawMessage("Track " + ((EmuoTron.Mappers.mNSF)(nes.mapper)).currentSong + " of " + ((EmuoTron.Mappers.mNSF)(nes.mapper)).totalSongs, 1, 10);

            if (((EmuoTron.Mappers.mNSF)(nes.mapper)).totalSongs != 1)
            {
                DrawMessage("      Cycle tracks with", 1, 24);
                DrawMessage("        left or right.", 1, 25);
            }

        }
        private void DrawMessage(string message, int xOffset, int yOffset)
        {
            message = message.ToLower();
            int realXOffset = xOffset * charSize;
            int realYOffset = yOffset * charSize;
            for (int i = 0; i < message.Length; i++)
            {
                if (charSheetSprites.ContainsKey(message[i]))
                {
                    DrawCharacter(message[i], realXOffset + (i * charSize), realYOffset);
                }
            }
        }
        private void DrawCharacter(char character, int xLoc, int yLoc)
        {
            if (xLoc > 256 - 16 || yLoc > 240 - 8)
                return;
            int charNum = charSheetSprites[character];
            int charX = (charNum % 16) * charSize;
            int charY = (charNum / 16) * charSize;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    nes.PPU.screen[yLoc + y, xLoc + x] = charSheet[y + charY, x + charX];
                }
            }
        }
        private void LoadCharSheet()
        {
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            Stream file = thisExe.GetManifestResourceStream("DirectXEmu.images.charSheet.png");
            Bitmap charBitmap = (Bitmap)Bitmap.FromStream(file);
            file.Close();
            charSheet = new uint[charBitmap.Height / 2, charBitmap.Width / 2];
            unsafe
            {
                BitmapData bmd = charBitmap.LockBits(new Rectangle(0, 0, charBitmap.Width, charBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                uint* pixels = (uint*)(bmd.Scan0);
                for (int x = 0; x < (charBitmap.Width >> 1); x++)
                {
                    for (int y = 0; y < (charBitmap.Height >> 1); y++)
                    {
                        uint color = pixels[((y << 1) * charBitmap.Width) + (x << 1)];
                        if((color & 0xFF000000) != 0xFF000000)
                            color = 0xFF000000;
                        charSheet[y, x] = color;
                    }
                }
                charBitmap.UnlockBits(bmd);
            }
            charSize = 8;
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
            charSheetSprites['.'] = 45;
            charSheetSprites[':'] = 46;
        }
    }
}
