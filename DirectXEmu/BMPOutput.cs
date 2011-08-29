using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace DirectXEmu
{
    class BMPOutput : IDisposable
    {
        private string outputPath;
        private int width;
        private int height;
        private FileStream outputBlobFile;
        private BinaryWriter outputBlobWriter;
        private int frame;
        private bool recording;

        public BMPOutput(string outputPath, int width, int height)
        {
            this.outputPath = outputPath;
            this.width = width;
            this.height = height;
            frame = 0;
            outputBlobFile = File.Create(outputPath);
            outputBlobWriter = new BinaryWriter(outputBlobFile);
            recording = true;
        }

        public void AddFrame(uint[,] screen)
        {
            if (recording)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        outputBlobWriter.Write((byte)(screen[y, x] & 0x000000FF));
                        outputBlobWriter.Write((byte)((screen[y, x] & 0x0000FF00) >> 8));
                        outputBlobWriter.Write((byte)((screen[y, x] & 0x00FF0000) >> 16));
                    }
                }
                frame++;
            }
        }

        public void CompleteRecording()
        {
            if (recording)
            {
                WaitingForm wait = new WaitingForm(); //This should really be in its own thread to avoid hanging the gui.
                wait.Show();
                wait.Refresh();
                frame--;
                outputBlobWriter.Close();
                outputBlobWriter = null;
                BinaryReader outputBlobReader = new BinaryReader(File.OpenRead(outputPath));
                byte[] header = new byte[54];
                BinaryWriter headerBuilder = new BinaryWriter(new MemoryStream(header));
                int bmpSize = width * height * 3;
                headerBuilder.Write('B');//BMP Header
                headerBuilder.Write('M');
                headerBuilder.Write((uint)(54 + bmpSize));
                headerBuilder.Write((ushort)(0));
                headerBuilder.Write((ushort)(0));
                headerBuilder.Write((uint)(54));
                headerBuilder.Write((uint)(40));//DIB Header
                headerBuilder.Write((int)(width));
                headerBuilder.Write((int)(height));
                headerBuilder.Write((ushort)(1));
                headerBuilder.Write((ushort)(24));
                headerBuilder.Write((int)(0));
                headerBuilder.Write((uint)(bmpSize));
                headerBuilder.Write((int)(2835));//Random values for pixels/meter that I stole from a sample, hopefully they are reasonable.
                headerBuilder.Write((int)(2835));
                headerBuilder.Write((int)(0));
                headerBuilder.Write((int)(0));
                headerBuilder.Close();
                string frameStringLength = frame.ToString().Length.ToString();
                string framePath = Path.ChangeExtension(outputPath, null);
                for (int i = 0; i <= frame; i++)
                {
                    FileStream frameFile = File.Create(framePath + "-" + i.ToString("D" + frameStringLength) + ".bmp");
                    BinaryWriter frameWriter = new BinaryWriter(frameFile);
                    frameWriter.Write(header);
                    frameWriter.Write(outputBlobReader.ReadBytes(bmpSize));
                    frameWriter.Close();
                    wait.ReportProgress((int)((i / (frame * 1.0)) * 100));
                }
                outputBlobReader.Close();
                File.Delete(outputPath);
                recording = false;
                wait.Close();
            }
        }
        ~BMPOutput()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (outputBlobWriter != null)
            {
                outputBlobWriter.Close();
                outputBlobWriter = null;
                recording = false;
            }
        }

    }
    class WaitingForm : Form
    {
        public WaitingForm()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 25);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(206, 23);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.Maximum = 100;
            this.progressBar1.MarqueeAnimationSpeed = 100;
            this.progressBar1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(209, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Dumping frame data, this may take a while.";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(230, 59);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBar1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "Form1";
            this.Text = "Saving Data";
            this.TopMost = true;
            this.MaximizeBox = false;
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        public void ReportProgress(int value)
        {
            this.progressBar1.Value = value;
            Application.DoEvents();
        }

        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label1;
    }
}
