using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace DirectXEmu
{
    unsafe class HQ2x : IScaler
    {
        private static uint ALPHA = 0xFF000000;
        private static uint MASK_2 = 0x00FF00;
        private static uint MASK_13 = 0xFF00FF;
        private static uint Ymask = 0x00FF0000;
        private static uint Umask = 0x0000FF00;
        private static uint Vmask = 0x000000FF;
        private static uint trY = 0x00300000;
        private static uint trU = 0x00000700;
        private static uint trV = 0x00000006;

        private static uint[] RGBtoYUV = new uint[16777216];
        private static uint  YUV1, YUV2;

        private uint[] w = new uint[10];

        private int Xres = 256;
        private int Yres = 240;
        
        private int _resizedX;
        private int _resizedY;
        private bool _isResizable;
        private bool _maintainAspectRatio;
        private double _ratioX;
        private double _ratioY;

        public int ResizedX { get { return _resizedX; } }
        public int ResizedY { get { return _resizedY; } }
        public double RatioX { get { return _ratioX; } }
        public double RatioY { get { return _ratioY; } }
        public bool IsResizable { get { return _isResizable; } }
        public bool MaintainAspectRatio { get { return _maintainAspectRatio; } }

        public HQ2x()
        {
            _resizedX = 512;
            _resizedY = 480;
             _ratioX = 16;
            _ratioY = 15;
            _isResizable = false;
            _maintainAspectRatio = true;

            uint c, r, g, b, y, u, v;

            for (c = 0; c < 16777215; c++)
            {
                r = (c & 0xFF0000) >> 16;
                g = (c & 0x00FF00) >> 8;
                b = c & 0x0000FF;
                y = (uint)(0.299 * r + 0.587 * g + 0.114 * b);
                u = (uint)(-0.169 * r - 0.331 * g + 0.5 * b) + 128;
                v = (uint)(0.5 * r - 0.419 * g - 0.081 * b) + 128;
                RGBtoYUV[c] = (y << 16) + (u << 8) + v;
            }
        }

        private static bool Diff(uint w1, uint w2)
        {
            // Mask against RGB_MASK to discard the alpha channel
            YUV1 = RGBtoYUV[w1 & 0x00FFFFFF];
            YUV2 = RGBtoYUV[w2 & 0x00FFFFFF];
            return ((Math.Abs((YUV1 & Ymask) - (YUV2 & Ymask)) > trY) ||
                    (Math.Abs((YUV1 & Umask) - (YUV2 & Umask)) > trU) ||
                    (Math.Abs((YUV1 & Vmask) - (YUV2 & Vmask)) > trV));
        }

        #region Interp
        private static void Interp1(uint* pc, uint c1, uint c2)
        {
            //*pc = (c1*3+c2) >> 2;
            if (c1 == c2) {
                *pc = c1;
                return;
            }
            *pc = ((((c1 & MASK_2) * 3 + (c2 & MASK_2)) >> 2) & MASK_2) +
                ((((c1 & MASK_13) * 3 + (c2 & MASK_13)) >> 2) & MASK_13) | ALPHA;
        }

        private static void Interp2(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*2+c2+c3) >> 2;
            *pc = ((((c1 & MASK_2) * 2 + (c2 & MASK_2) + (c3 & MASK_2)) >> 2) & MASK_2) +
                  ((((c1 & MASK_13) * 2 + (c2 & MASK_13) + (c3 & MASK_13)) >> 2) & MASK_13) | ALPHA;
        }

        private static void Interp3(uint* pc, uint c1, uint c2)
        {
            //*pc = (c1*7+c2)/8;
            if (c1 == c2) {
                *pc = c1;
                return;
            }
            *pc = ((((c1 & MASK_2) * 7 + (c2 & MASK_2)) >> 3) & MASK_2) +
                ((((c1 & MASK_13) * 7 + (c2 & MASK_13)) >> 3) & MASK_13) | ALPHA;
        }

        private static void Interp4(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*2+(c2+c3)*7)/16;
            *pc = ((((c1 & MASK_2) * 2 + (c2 & MASK_2) * 7 + (c3 & MASK_2) * 7) >> 4) & MASK_2) +
                  ((((c1 & MASK_13) * 2 + (c2 & MASK_13) * 7 + (c3 & MASK_13) * 7) >> 4) & MASK_13) | ALPHA;
        }

        private static void Interp5(uint* pc, uint c1, uint c2)
        {
            //*pc = (c1+c2) >> 1;
            if (c1 == c2) {
                *pc = c1;
                return;
            }
            *pc = ((((c1 & MASK_2) + (c2 & MASK_2)) >> 1) & MASK_2) +
                ((((c1 & MASK_13) + (c2 & MASK_13)) >> 1) & MASK_13) | ALPHA;
        }

        private static void Interp6(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*5+c2*2+c3)/8;
            *pc = ((((c1 & MASK_2) * 5 + (c2 & MASK_2) * 2 + (c3 & MASK_2)) >> 3) & MASK_2) +
                  ((((c1 & MASK_13) * 5 + (c2 & MASK_13) * 2 + (c3 & MASK_13)) >> 3) & MASK_13) | ALPHA;
        }

        private static void Interp7(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*6+c2+c3)/8;
            *pc = ((((c1 & MASK_2) * 6 + (c2 & MASK_2) + (c3 & MASK_2)) >> 3) & MASK_2) +
                  ((((c1 & MASK_13) * 6 + (c2 & MASK_13) + (c3 & MASK_13)) >> 3) & MASK_13) | ALPHA;
        }

        private static void Interp8(uint* pc, uint c1, uint c2)
        {
            //*pc = (c1*5+c2*3)/8;
            if (c1 == c2) {
                *pc = c1;
                return;
            }
            *pc = ((((c1 & MASK_2) * 5 + (c2 & MASK_2) * 3) >> 3) & MASK_2) +
                  ((((c1 & MASK_13) * 5 + (c2 & MASK_13) * 3) >> 3) & MASK_13) | ALPHA;
        }

        private static void Interp9(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*2+(c2+c3)*3)/8;
            *pc = ((((c1 & MASK_2) * 2 + (c2 & MASK_2) * 3 + (c3 & MASK_2) * 3) >> 3) & MASK_2) +
                  ((((c1 & MASK_13) * 2 + (c2 & MASK_13) * 3 + (c3 & MASK_13) * 3) >> 3) & MASK_13) | ALPHA;
        }

        private static void Interp10(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*14+c2+c3)/16;
            *pc = ((((c1 & MASK_2) * 14 + (c2 & MASK_2) + (c3 & MASK_2)) >> 4) & MASK_2) +
                  ((((c1 & MASK_13) * 14 + (c2 & MASK_13) + (c3 & MASK_13)) >> 4) & MASK_13) | ALPHA;
        }
#endregion

        public void Switch(int pattern, int dpL, uint* dp)
        {
            switch (pattern)
            {
                case 0:
                case 1:
                case 4:
                case 32:
                case 128:
                case 5:
                case 132:
                case 160:
                case 33:
                case 129:
                case 36:
                case 133:
                case 164:
                case 161:
                case 37:
                case 165:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 2:
                case 34:
                case 130:
                case 162:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 16:
                case 17:
                case 48:
                case 49:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 64:
                case 65:
                case 68:
                case 69:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 8:
                case 12:
                case 136:
                case 140:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 3:
                case 35:
                case 131:
                case 163:
                    Interp1(dp, w[5], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 6:
                case 38:
                case 134:
                case 166:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 20:
                case 21:
                case 52:
                case 53:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 144:
                case 145:
                case 176:
                case 177:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 192:
                case 193:
                case 196:
                case 197:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 96:
                case 97:
                case 100:
                case 101:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 40:
                case 44:
                case 168:
                case 172:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 9:
                case 13:
                case 137:
                case 141:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 18:
                case 50:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 80:
                case 81:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 72:
                case 76:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 10:
                case 138:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 66:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 24:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 7:
                case 39:
                case 135:
                    Interp1(dp, w[5], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 148:
                case 149:
                case 180:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 224:
                case 228:
                case 225:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 41:
                case 169:
                case 45:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 22:
                case 54:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 208:
                case 209:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 104:
                case 108:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 11:
                case 139:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 19:
                case 51:
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp, w[5], w[4]);
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp6(dp, w[5], w[2], w[4]);
                        Interp9(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 146:
                case 178:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                        Interp1(dp + dpL + 1, w[5], w[8]);
                    }
                    else
                    {
                        Interp9(dp + 1, w[5], w[2], w[6]);
                        Interp6(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    break;
                case 84:
                case 85:
                    Interp2(dp, w[5], w[4], w[2]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + 1, w[5], w[2]);
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp6(dp + 1, w[5], w[6], w[2]);
                        Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    break;
                case 112:
                case 113:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL, w[5], w[4]);
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp6(dp + dpL, w[5], w[8], w[4]);
                        Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 200:
                case 204:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                        Interp1(dp + dpL + 1, w[5], w[6]);
                    }
                    else
                    {
                        Interp9(dp + dpL, w[5], w[8], w[4]);
                        Interp6(dp + dpL + 1, w[5], w[8], w[6]);
                    }
                    break;
                case 73:
                case 77:
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp, w[5], w[2]);
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp6(dp, w[5], w[4], w[2]);
                        Interp9(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 42:
                case 170:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                        Interp1(dp + dpL, w[5], w[8]);
                    }
                    else
                    {
                        Interp9(dp, w[5], w[4], w[2]);
                        Interp6(dp + dpL, w[5], w[4], w[8]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 14:
                case 142:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                        Interp1(dp + 1, w[5], w[6]);
                    }
                    else
                    {
                        Interp9(dp, w[5], w[4], w[2]);
                        Interp6(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 67:
                    Interp1(dp, w[5], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 70:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 28:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 152:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 194:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 98:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 56:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 25:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 26:
                case 31:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 82:
                case 214:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 88:
                case 248:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 74:
                case 107:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 27:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[3]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 86:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 216:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp1(dp + dpL, w[5], w[7]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 106:
                    Interp1(dp, w[5], w[1]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 30:
                    Interp1(dp, w[5], w[1]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 210:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp1(dp + 1, w[5], w[3]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 120:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 75:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp1(dp + dpL, w[5], w[7]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 29:
                    Interp1(dp, w[5], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 198:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 184:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 99:
                    Interp1(dp, w[5], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 57:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 71:
                    Interp1(dp, w[5], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 156:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 226:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 60:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 195:
                    Interp1(dp, w[5], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 102:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 153:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 58:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 83:
                    Interp1(dp, w[5], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 92:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 202:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 78:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 154:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 114:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 89:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 90:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 55:
                case 23:
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp, w[5], w[4]);
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp6(dp, w[5], w[2], w[4]);
                        Interp9(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 182:
                case 150:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                        Interp1(dp + dpL + 1, w[5], w[8]);
                    }
                    else
                    {
                        Interp9(dp + 1, w[5], w[2], w[6]);
                        Interp6(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    break;
                case 213:
                case 212:
                    Interp2(dp, w[5], w[4], w[2]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + 1, w[5], w[2]);
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp6(dp + 1, w[5], w[6], w[2]);
                        Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    break;
                case 241:
                case 240:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL, w[5], w[4]);
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp6(dp + dpL, w[5], w[8], w[4]);
                        Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 236:
                case 232:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                        Interp1(dp + dpL + 1, w[5], w[6]);
                    }
                    else
                    {
                        Interp9(dp + dpL, w[5], w[8], w[4]);
                        Interp6(dp + dpL + 1, w[5], w[8], w[6]);
                    }
                    break;
                case 109:
                case 105:
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp, w[5], w[2]);
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp6(dp, w[5], w[4], w[2]);
                        Interp9(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 171:
                case 43:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                        Interp1(dp + dpL, w[5], w[8]);
                    }
                    else
                    {
                        Interp9(dp, w[5], w[4], w[2]);
                        Interp6(dp + dpL, w[5], w[4], w[8]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 143:
                case 15:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                        Interp1(dp + 1, w[5], w[6]);
                    }
                    else
                    {
                        Interp9(dp, w[5], w[4], w[2]);
                        Interp6(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 124:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 203:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp1(dp + dpL, w[5], w[7]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 62:
                    Interp1(dp, w[5], w[1]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 211:
                    Interp1(dp, w[5], w[4]);
                    Interp1(dp + 1, w[5], w[3]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 118:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 217:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp1(dp + dpL, w[5], w[7]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 110:
                    Interp1(dp, w[5], w[1]);
                    Interp1(dp + 1, w[5], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 155:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[3]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 188:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 185:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 61:
                    Interp1(dp, w[5], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 157:
                    Interp1(dp, w[5], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 103:
                    Interp1(dp, w[5], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 227:
                    Interp1(dp, w[5], w[4]);
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 230:
                    Interp2(dp, w[5], w[1], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 199:
                    Interp1(dp, w[5], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 220:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 158:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 234:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 242:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 59:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 121:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 87:
                    Interp1(dp, w[5], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 79:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 122:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 94:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 218:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 91:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 229:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 167:
                    Interp1(dp, w[5], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 173:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 181:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 186:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 115:
                    Interp1(dp, w[5], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 93:
                    Interp1(dp, w[5], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 206:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 205:
                case 201:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp + dpL, w[5], w[7]);
                    }
                    else
                    {
                        Interp7(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 174:
                case 46:
                    if (Diff(w[4], w[2]))
                    {
                        Interp1(dp, w[5], w[1]);
                    }
                    else
                    {
                        Interp7(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[6]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 179:
                case 147:
                    Interp1(dp, w[5], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp + 1, w[5], w[3]);
                    }
                    else
                    {
                        Interp7(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 117:
                case 116:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp1(dp + dpL, w[5], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL + 1, w[5], w[9]);
                    }
                    else
                    {
                        Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 189:
                    Interp1(dp, w[5], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 231:
                    Interp1(dp, w[5], w[4]);
                    Interp1(dp + 1, w[5], w[6]);
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 126:
                    Interp1(dp, w[5], w[1]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 219:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[3]);
                    Interp1(dp + dpL, w[5], w[7]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 125:
                    if (Diff(w[8], w[4]))
                    {
                        Interp1(dp, w[5], w[2]);
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp6(dp, w[5], w[4], w[2]);
                        Interp9(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + 1, w[5], w[2]);
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 221:
                    Interp1(dp, w[5], w[2]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + 1, w[5], w[2]);
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp6(dp + 1, w[5], w[6], w[2]);
                        Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    Interp1(dp + dpL, w[5], w[7]);
                    break;
                case 207:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                        Interp1(dp + 1, w[5], w[6]);
                    }
                    else
                    {
                        Interp9(dp, w[5], w[4], w[2]);
                        Interp6(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[7]);
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 238:
                    Interp1(dp, w[5], w[1]);
                    Interp1(dp + 1, w[5], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                        Interp1(dp + dpL + 1, w[5], w[6]);
                    }
                    else
                    {
                        Interp9(dp + dpL, w[5], w[8], w[4]);
                        Interp6(dp + dpL + 1, w[5], w[8], w[6]);
                    }
                    break;
                case 190:
                    Interp1(dp, w[5], w[1]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                        Interp1(dp + dpL + 1, w[5], w[8]);
                    }
                    else
                    {
                        Interp9(dp + 1, w[5], w[2], w[6]);
                        Interp6(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    Interp1(dp + dpL, w[5], w[8]);
                    break;
                case 187:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                        Interp1(dp + dpL, w[5], w[8]);
                    }
                    else
                    {
                        Interp9(dp, w[5], w[4], w[2]);
                        Interp6(dp + dpL, w[5], w[4], w[8]);
                    }
                    Interp1(dp + 1, w[5], w[3]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 243:
                    Interp1(dp, w[5], w[4]);
                    Interp1(dp + 1, w[5], w[3]);
                    if (Diff(w[6], w[8]))
                    {
                        Interp1(dp + dpL, w[5], w[4]);
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp6(dp + dpL, w[5], w[8], w[4]);
                        Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 119:
                    if (Diff(w[2], w[6]))
                    {
                        Interp1(dp, w[5], w[4]);
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp6(dp, w[5], w[2], w[4]);
                        Interp9(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 237:
                case 233:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[2], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 175:
                case 47:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp10(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[6]);
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    break;
                case 183:
                case 151:
                    Interp1(dp, w[5], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[8], w[4]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 245:
                case 244:
                    Interp2(dp, w[5], w[4], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    Interp1(dp + dpL, w[5], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 250:
                    Interp1(dp, w[5], w[1]);
                    Interp1(dp + 1, w[5], w[3]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 123:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[3]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 95:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[7]);
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 222:
                    Interp1(dp, w[5], w[1]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[7]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 252:
                    Interp2(dp, w[5], w[1], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 249:
                    Interp1(dp, w[5], w[2]);
                    Interp2(dp + 1, w[5], w[3], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 235:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp2(dp + 1, w[5], w[3], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 111:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp10(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                    break;
                case 63:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp10(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                    break;
                case 159:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 215:
                    Interp1(dp, w[5], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp2(dp + dpL, w[5], w[7], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 246:
                    Interp2(dp, w[5], w[1], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 254:
                    Interp1(dp, w[5], w[1]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 253:
                    Interp1(dp, w[5], w[2]);
                    Interp1(dp + 1, w[5], w[2]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 251:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[3]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 239:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp10(dp, w[5], w[4], w[2]);
                    }
                    Interp1(dp + 1, w[5], w[6]);
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[6]);
                    break;
                case 127:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp10(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + 1, w[5], w[2], w[6]);
                    }
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL, w[5], w[8], w[4]);
                    }
                    Interp1(dp + dpL + 1, w[5], w[9]);
                    break;
                case 191:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp10(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[8]);
                    Interp1(dp + dpL + 1, w[5], w[8]);
                    break;
                case 223:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp2(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[7]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 247:
                    Interp1(dp, w[5], w[4]);
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + 1, w[5], w[2], w[6]);
                    }
                    Interp1(dp + dpL, w[5], w[4]);
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
                case 255:
                    if (Diff(w[4], w[2]))
                    {
                        *dp = w[5];
                    }
                    else
                    {
                        Interp10(dp, w[5], w[4], w[2]);
                    }
                    if (Diff(w[2], w[6]))
                    {
                        *(dp + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + 1, w[5], w[2], w[6]);
                    }
                    if (Diff(w[8], w[4]))
                    {
                        *(dp + dpL) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL, w[5], w[8], w[4]);
                    }
                    if (Diff(w[6], w[8]))
                    {
                        *(dp + dpL + 1) = w[5];
                    }
                    else
                    {
                        Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                    }
                    break;
            }
        }

        public void PerformScale(uint* sp, uint* dp)
        {
            int prevline, nextline;
            int i, j, k;
            int dpL = Xres * 2;
            
            uint YUV1, YUV2;

            //   +----+----+----+
            //   |    |    |    |
            //   | w1 | w2 | w3 |
            //   +----+----+----+
            //   |    |    |    |
            //   | w4 | w5 | w6 |
            //   +----+----+----+
            //   |    |    |    |
            //   | w7 | w8 | w9 |
            //   +----+----+----+

            for (j=0; j<Yres; j++)
            {
                if (j>0)      prevline = -Xres; else prevline = 0;
                if (j<Yres-1) nextline =  Xres; else nextline = 0;

                for (i=0; i<Xres; i++)
                {
                    w[2] = *(sp + prevline);
                    w[5] = *sp;
                    w[8] = *(sp + nextline);

                    if (i>0)
                    {
                        w[1] = *(sp + prevline - 1);
                        w[4] = *(sp - 1);
                        w[7] = *(sp + nextline - 1);
                    }
                    else
                    {
                        w[1] = w[2];
                        w[4] = w[5];
                        w[7] = w[8];
                    }

                    if (i<Xres-1)
                    {
                        w[3] = *(sp + prevline + 1);
                        w[6] = *(sp + 1);
                        w[9] = *(sp + nextline + 1);
                    }
                    else
                    {
                        w[3] = w[2];
                        w[6] = w[5];
                        w[9] = w[8];
                    }

                    int pattern = 0;
                    int flag = 1;

                    YUV1 = RGBtoYUV[w[5] & 0x00FFFFFF];

                    for (k=1; k<=9; k++)
                    {
                        if (k==5) continue;

                        if (w[k] != w[5])
                        {
                            YUV2 = RGBtoYUV[w[k] & 0x00FFFFFF];
                            if ( ( Math.Abs((YUV1 & Ymask) - (YUV2 & Ymask)) > trY ) ||
                                    ( Math.Abs((YUV1 & Umask) - (YUV2 & Umask)) > trU ) ||
                                    ( Math.Abs((YUV1 & Vmask) - (YUV2 & Vmask)) > trV ) )
                                pattern |= flag;
                        }
                        flag <<= 1;
                    }

                    Switch(pattern, dpL, dp); //Trick JIT compiler to not be gay http://connect.microsoft.com/VisualStudio/feedback/details/510290/slow-jit-with-many-ifs-in-loop-in-release-mode#details

                    dp += 2;
                    sp++;
                }
                dp += dpL;
            }
        }
    }
}
