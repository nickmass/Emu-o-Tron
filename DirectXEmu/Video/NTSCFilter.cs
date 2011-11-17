using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    unsafe class NTSCFilter
    {
        bool phase;

        const double black = .518f, white = 1.962f, attenuation = .746f;
        static double[] levels = {.350, .518, .962,1.550,  // Signal low
                    1.094,1.506,1.962,1.962}; // Signal high

        double[] signalLevels = new double[256 * 8];
        uint* screen;
        uint* outScreen;
        int outWidth;
        int yScale;
        uint[] scanline = new uint[256];
        public static double gamma = 2.0;
        public static double hue = 3.9;
        public static double sat = 1.7;
        public static double brightness = 1.0;


        public NTSCFilter(int outWidth, int yScale)
        {
            this.outWidth = outWidth;
            this.yScale = 2;
        }

        public void Filter(uint* screen, uint* outScreen)
        {
            this.screen = screen;
            this.outScreen = outScreen;
            phase = !phase;
            int ppuCounter;
            if (phase)
                ppuCounter = 0;
            else
                ppuCounter = 1;
            for (int y = 0; y < 240; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    scanline[x] = screen[(y * 256) + x];
                }
                GenerateScanlineSignal(ppuCounter, scanline);
                DecodeSignal(y, ppuCounter, outWidth);
                ppuCounter += 341;
            }

        }

        private static bool InColorPhase(int color, int phase)
        {
            return (color + phase) % 12 < 6;
        }
        private static double CalculateSignal(int phase, uint pixel)
        {
            int color = (int)pixel & 0xF;
            int level = (int)(pixel >> 4) & 3;
            int emphasis = (int)(pixel >> 6) & 7;
            if (color > 13)
                level = 1;

            double low = levels[level + 0];
            double high = levels[level + 4];

            if (color == 0)
                low = high;
            if (color > 12)
                high = low;


            double signal = InColorPhase(color, phase) ? high : low;

            if (((emphasis & 1) != 0 && InColorPhase(0, phase)) ||
                ((emphasis & 2) != 0 && InColorPhase(4, phase)) ||
                ((emphasis & 4) != 0 && InColorPhase(8, phase)))
                signal = signal * attenuation;
            return (signal - black) / (white - black);
        }

        private void GenerateScanlineSignal(int ppuCounter, uint[] scanline)
        {
            int phase = (ppuCounter * 8) % 12;
            for (int x = 0; x < 256; x++)
            {
                for (int p = 0; p < 8; p++)
                {
                    signalLevels[(x * 8) + p] = CalculateSignal(phase, scanline[x]);
                    phase++;
                }
            }
        }

        public void DecodeSignal(int scanline, int ppuCounter, int width)
        {
            double phase = ((ppuCounter * 8) % 12) + hue;
            for (int x = 0; x < width; x++)
            {
                int center = (int)(x * (256 * 8) / (double)width);
                int begin = center - 6;
                if (begin < 0)
                    begin = 0;
                int end = center + 6;
                if (end > 256 * 8)
                    end = 256 * 8;
                double y = 0.0f, i = 0.0f, q = 0.0f;
                for (int p = begin; p < end; p++)
                {
                    double level = signalLevels[p] * (brightness / (double)(end - begin));
                    y += level;
                    i += level * Math.Cos((Math.PI / 6.0) * (phase + p)) * sat;
                    q += level * Math.Sin((Math.PI / 6.0) * (phase + p)) * sat;
                }
                RenderPixel(scanline, x, y, i, q);
            }
        }

        public void RenderPixel(int scanline, int x, double y, double i, double q)
        {
            uint rgb =
                    (0x10000 * Clamp((255.95 * GammaFix(y + 0.946882 * i + 0.623557 * q))) +
                     0x00100 * Clamp((255.95 * GammaFix(y + -0.274788 * i + -0.635691 * q))) +
                     0x00001 * Clamp((255.95 * GammaFix(y + -1.108545 * i + 1.709007 * q)))) | 0xFF000000;
            for (int j = 0; j < yScale; j++)
            {
                outScreen[(((scanline * yScale) + j) * outWidth) + x] = rgb;
            }
        }

        private static double GammaFix(double x)
        {
            return x <= 0 ? 0 : Math.Pow(x, 2.2 / gamma);
        }

        private static uint Clamp(double x)
        {
            return x > 255 ? 255 : (uint)x;
        }

        public static uint NESToRGB(uint color)
        {
            double[] signals = new double[12];
            for (int j = 0; j < 12; j++)
            {
                signals[j] = CalculateSignal(j, color);
            }
            double y = 0.0f, i = 0.0f, q = 0.0f;
            for (int j = 0; j < 12; j++)
            {
                double level = signals[j] * (brightness / 12.0);
                y += level;
                i += level * Math.Cos((Math.PI / 6.0) * (j + hue)) * sat;
                q += level * Math.Sin((Math.PI / 6.0) * (j + hue)) * sat;
            }
            return (0x10000 * Clamp((255.95 * GammaFix(y + 0.946882 * i + 0.623557 * q))) +
                 0x00100 * Clamp((255.95 * GammaFix(y + -0.274788 * i + -0.635691 * q))) +
                 0x00001 * Clamp((255.95 * GammaFix(y + -1.108545 * i + 1.709007 * q)))) | 0xFF000000;
        }
        public static uint NESToRGB(int color)
        {
            return NESToRGB((uint)color);
        }
        public static uint[] NesPalette
        {
            get
            {
                uint[] pal = new uint[0x200];
                for (int i = 0; i < 0x200; i++)
                {
                    pal[i] = NESToRGB(i);
                }
                return pal;
            }
        }
    }
}
