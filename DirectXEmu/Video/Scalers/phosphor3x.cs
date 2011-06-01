using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    class Phosphor3x : IScaler
    {
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
        

        static double phosphor_bleed = 0.78f;
        static double scale_add = 1.0f;
        static double scale_times = 0.8f;

        static double scanrange_low = 0.5f;
        static double scanrange_high = 0.65f;

        static double[] phosphor_bloom = new double[256];
        static double[] scan_range = new double[256];
        public Phosphor3x()
        {
            _resizedX = 960;
            _resizedY = 720;
             _ratioX = 4;
            _ratioY = 3;
            _isResizable = false;
            _maintainAspectRatio = true;
            
            for (int i = 0; i < 256; i++)
            {
                phosphor_bloom[i] = scale_times * Math.Pow(i / 255.0f, 1.0f/2.2f) + scale_add;
                scan_range[i] = scanrange_low + i * (scanrange_high - scanrange_low) / 255.0f;
            }
        }
        public unsafe void PerformScale(uint* origPixels, uint* resizePixels)
        {
            for (int y = 0; y < 240; y++)
            {
                uint* outLine = resizePixels + ((ResizedX * y) * 3);
                uint* inLine = origPixels + (256 * y);

                BlitLinearLine(outLine, inLine, ResizedX);

                BleedPhosphors(outLine, ResizedX);

                StretchScanline(outLine, outLine + ResizedX, ResizedX);
                StretchScanline(outLine + ResizedX, outLine + ResizedX + ResizedX, ResizedX);

            }
        }
        private static unsafe void BlitLinearLine(uint* outLine, uint* inLine, int width)
        {
            int j = 0;
            for (int i = 0; i < 255; i++)
            {
                outLine[j] = inLine[i];
                if ((i & 3) == 3)
                {
                    outLine[(j) + 1] = BlendPixels21(inLine[i], inLine[i + 1]);
                    outLine[(j) + 2] = BlendPixels12(inLine[i], inLine[i + 1]);
                    j += 3;
                }
                else
                {
                    outLine[(j) + 1] = BlendPixels21(inLine[i], inLine[i + 1]);
                    outLine[(j) + 2] = BlendPixels(inLine[i], inLine[i + 1]);
                    outLine[(j) + 3] = BlendPixels12(inLine[i], inLine[i + 1]);
                    j += 4;
                }
            }
            outLine[width - 3] = inLine[255];
            outLine[width - 2] = BlendPixels21(inLine[255], 0);
            outLine[width - 1] = BlendPixels12(inLine[255], 0);
        }
        private static uint BlendPixels(uint a, uint b)
        {
            return (((a >> 1) & 0x7f7f7f7f) + ((b >> 1) & 0x7f7f7f7f)) | 0xff000000;
        }
        private static uint BlendPixels21(uint a, uint b)
        {
           return (5 * ((a >> 3) & 0x1f1f1f1f) + 3 * ((b >> 3) & 0x1f1f1f1f)) | 0xff000000;
        }
        private static uint BlendPixels12(uint a, uint b)
        {
           return (3 * ((a >> 3) & 0x1f1f1f1f) + 5 * ((b >> 3) & 0x1f1f1f1f)) | 0xff000000;
        }
        private static byte Clamp(double x)
        {
            return (x > 255) ? (byte)255 : ((x < 0) ? (byte)0 : (byte)x);
        }
        private static unsafe void BleedPhosphors(uint* scanline, int width)
        {

            byte* u = (byte*)scanline;

            // Red phosphor
            for (int x = 0; x < width; x += 3)
            {
                byte r = u[((x) * 4) + 2];
                u[((x + 1) * 4) + 2] = Clamp(r * phosphor_bleed * phosphor_bloom[r]);
                u[((x + 2) * 4) + 2] = (byte)(u[((x + 1) * 4) + 2] >> 1);
            }

            // Green phosphor
            for (int x = 1; x < width - 3; x += 3)
            {
                byte g = u[((x) * 4) + 1];
                u[((x + 1) * 4) + 1] = Clamp(g * phosphor_bleed * phosphor_bloom[g]);
                u[((x + 2) * 4) + 1] = (byte)(u[((x + 1) * 4) + 1] >> 1);
            }

            // Blue phosphor
            byte bEdge = u[((2) * 4) + 0];
            u[((0) * 4) + 0] = Clamp(bEdge * phosphor_bleed * phosphor_bloom[bEdge]);
            u[((1) * 4) + 0] = u[((0) * 4) + 0];
            for (int x = 2; x < width - 3; x += 3)
            {
                byte b = u[((x) * 4) + 0];
                u[((x + 1) * 4) + 0] = Clamp(b * phosphor_bleed * phosphor_bloom[b]);
                u[((x + 2) * 4) + 0] = (byte)(u[((x + 1) * 4) + 0] >> 1);
            }
        }
        private static unsafe void StretchScanline(uint* scan_in, uint* scan_out, int width)
        {
            byte* u_in = (byte*)scan_in;
            byte* u_out = (byte*)scan_out;

            for (int x = 0; x < width; x++)
            {
                uint max = MaxComponent(scan_in[x]);
                u_out[(x * 4) + 3] = 0xff;
                u_out[(x * 4) + 1] = (byte)(scan_range[max] * u_in[(x * 4) + 1]);
                u_out[(x * 4) + 2] = (byte)(scan_range[max] * u_in[(x * 4) + 2]);
                u_out[(x * 4) + 0] = (byte)(scan_range[max] * u_in[(x * 4) + 0]);
            }
        }
        private static uint MaxComponent(uint col)
        {
            uint max = ((col >> 16) & 0xFF);
            max = (((col >> 8) & 0xFF) > max) ? ((col >> 8) & 0xFF) : max;
            max = ((col & 0xFF) > max) ? (col & 0xFF) : max;
            return max;
        }
    }
}
