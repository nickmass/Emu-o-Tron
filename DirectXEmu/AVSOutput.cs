using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DirectXEmu
{
    class AVSOutput
    {
        WAVOutput wavRecorder;
        BMPOutput bmpRecorder;
        double fps;
        bool recording;
        string outputPath;
        int frames;
        public AVSOutput(string outputPath, int width, int height, double fps, int sampleRate)
        {
            this.fps = fps;
            this.outputPath = outputPath;
            wavRecorder = new WAVOutput(Path.ChangeExtension(outputPath, ".wav"), sampleRate);
            bmpRecorder = new BMPOutput(Path.ChangeExtension(outputPath, ".tmp"), width, height);
            frames = 0;
            recording = true;
        }

        public void AddFrame(uint[,] screen, short[] buffer, int samples)
        {
            if(recording)
            {
                wavRecorder.AddSamples(buffer, samples);
                bmpRecorder.AddFrame(screen);
                frames++;
            }
        }

        public void CompleteRecording()
        {
            if (recording)
            {
                frames--;
                wavRecorder.CompleteRecording();
                bmpRecorder.CompleteRecording();
                string avsData =    "video = ImageSource(\"" + Path.ChangeExtension(outputPath, null) + "-%"+ frames.ToString().Length.ToString("D2") +"d.bmp\", 0, " + frames.ToString() + ", " + fps.ToString() + ")\r\n" +
                                    "audio = WAVSource(\"" + Path.ChangeExtension(outputPath, ".wav") + "\")\r\n" +
                                    "AudioDub(video, audio)";
                File.WriteAllText(outputPath, avsData);
            }
        }
    }
}
