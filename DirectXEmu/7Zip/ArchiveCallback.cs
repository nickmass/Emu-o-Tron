using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace DirectXEmu
{
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
                if (index == FileNumber)
                    FileStream.Dispose();
                index++;
                if (index == 1)
                    FileStream.Dispose(); //Stupid stupid hack to make both 7z and zip dispose the stream
            }
            catch  //7zip exectues this function once for each file until the one requested, zip will only execute the one time for needed file. Possibly use a counter until it == index?
            {
                //MessageBox.Show(e.Message);
            }
        }
        #endregion

    }
}
