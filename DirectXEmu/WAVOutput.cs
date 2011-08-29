using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DirectXEmu
{
    class WAVOutput : IDisposable
    {
        private string outputPath;
        private int samples;
        private int sampleRate;
        private FileStream outputFile;
        private BinaryWriter outputWriter;
        private bool recording;

        public WAVOutput(string outputPath, int sampleRate)
        {
            this.outputPath = outputPath;
            this.sampleRate = sampleRate;
            samples = 0;
            outputFile = File.Create(outputPath);
            outputWriter = new BinaryWriter(outputFile);
            for (int i = 0; i < 44; i++)
                outputWriter.Write((byte)0);
            recording = true;
        }
        public void AddSamples(short[] buffer, int samples)
        {
            if (recording)
            {
                this.samples += samples;
                for (int i = 0; i < samples; i++)
                    outputWriter.Write(buffer[i]);
            }
        }
        public void CompleteRecording()
        {
            if (recording)
            {
                outputWriter.Seek(0, SeekOrigin.Begin);
                int subchunk2Size = samples * 1 * (16 / 8);
                outputWriter.Write((byte)'R');
                outputWriter.Write((byte)'I');
                outputWriter.Write((byte)'F');
                outputWriter.Write((byte)'F');
                outputWriter.Write((int)(subchunk2Size + 36));
                outputWriter.Write((byte)'W');
                outputWriter.Write((byte)'A');
                outputWriter.Write((byte)'V');
                outputWriter.Write((byte)'E');
                outputWriter.Write((byte)'f');
                outputWriter.Write((byte)'m');
                outputWriter.Write((byte)'t');
                outputWriter.Write((byte)' ');
                outputWriter.Write((int)16);
                outputWriter.Write((short)1); //Format, PCM is apparently 1
                outputWriter.Write((short)1); //Channels
                outputWriter.Write(sampleRate); //Samples per Second
                outputWriter.Write(sampleRate * 2); //Bytes per Second
                outputWriter.Write((short)2); //Block Alignment, bytes per sample * channels
                outputWriter.Write((short)16); //Bits per Sample
                outputWriter.Write((byte)'d');
                outputWriter.Write((byte)'a');
                outputWriter.Write((byte)'t');
                outputWriter.Write((byte)'a');
                outputWriter.Write(subchunk2Size);
                outputWriter.Close();
                outputWriter = null;
                recording = false;
            }
        }
        ~WAVOutput()
        {
            Dispose();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (outputWriter != null)
            {
                outputWriter.Close();
                outputWriter = null;
                recording = false;
            }
        }

        #endregion
    }
}
