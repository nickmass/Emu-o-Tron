using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DirectXEmu
{
    class RomPatching
    {
        public static string IPSPatch(string rom, string patch, string outDir)
        {
            string fileName = Path.Combine(outDir, Path.GetFileNameWithoutExtension(rom) + ".ips.tmp");
            FileStream inStream = File.OpenRead(rom);
            byte[] file = new byte[0x100FFFF];
            FileStream ipsStream = File.OpenRead(patch);
            FileStream outStream = File.Create(fileName);
            int i = 0;
            while (inStream.Position < inStream.Length)
            {
                file[i] = (byte)inStream.ReadByte();
                i++;
            }
            if (ipsStream.ReadByte() != 'P' ||
                ipsStream.ReadByte() != 'A' ||
                ipsStream.ReadByte() != 'T' ||
                ipsStream.ReadByte() != 'C' ||
                ipsStream.ReadByte() != 'H')
                throw new Exception("Invlaid Patch File");
            int maxOffset = 0;
            bool reading = true;
            while (reading)
            {
                int offset = (ipsStream.ReadByte() << 16) | (ipsStream.ReadByte() << 8) | ipsStream.ReadByte();
                if (offset == (('E' << 16) | ('O' << 8) | 'F'))
                    reading = false;
                else
                {
                    int size = (ipsStream.ReadByte() << 8) | ipsStream.ReadByte();
                    if (size == 0)
                    {
                        int RLEsize = (ipsStream.ReadByte() << 8) | ipsStream.ReadByte();
                        byte value = (byte)ipsStream.ReadByte();
                        for (int j = 0; j < RLEsize; j++)
                            file[offset + j] = value;
                        if (offset + RLEsize > maxOffset)
                            maxOffset = offset + RLEsize;

                    }
                    else
                    {
                        for (int j = 0; j < size; j++)
                            file[offset + j] = (byte)ipsStream.ReadByte();
                        if (offset + size > maxOffset)
                            maxOffset = offset + size;
                    }
                }
            }
            if (inStream.Position < inStream.Length)
            {
                maxOffset = (ipsStream.ReadByte() << 16) | (ipsStream.ReadByte() << 8) | ipsStream.ReadByte();
            }
            outStream.Write(file, 0, maxOffset);
            inStream.Close();
            ipsStream.Close();
            outStream.Close();
            return fileName;
        }

        public static string UPSPatch(string rom, string patch, string outDir)
        {
            string fileName = Path.Combine(outDir, Path.GetFileNameWithoutExtension(rom) + ".ups.tmp");
            FileStream inStream = File.OpenRead(rom);
            FileStream patchStream = File.OpenRead(patch);
            FileStream outStream = File.Create(fileName);
            if (patchStream.ReadByte() != 'U' ||
                patchStream.ReadByte() != 'P' ||
                patchStream.ReadByte() != 'S' ||
                patchStream.ReadByte() != '1')
                throw new Exception("Invlaid Patch File");
            long patchXSize = UPSDecode(patchStream);
            long patchYSize = UPSDecode(patchStream);
            long inputSize = inStream.Length;
            long outputSize = 0;
            if (inputSize == patchXSize)
                outputSize = patchYSize;
            if (inputSize == patchYSize)
                outputSize = patchXSize;
            long relative = 0;
            while (patchStream.Position < patchStream.Length - 12)
            {
                long offset = relative;
                relative += UPSDecode(patchStream);
                while (offset < relative)
                {
                    byte x;
                    if (inStream.Position < inputSize)
                        x = (byte)inStream.ReadByte();
                    else
                        x = 0;
                    if (outStream.Position < outputSize)
                        outStream.WriteByte(x);
                    offset++;
                }
                while (true)
                {
                    byte x;
                    byte y;
                    if (inStream.Position < inputSize)
                        x = (byte)inStream.ReadByte();
                    else
                        x = 0;
                    y = (byte)patchStream.ReadByte();
                    if (outStream.Position < outputSize)
                        outStream.WriteByte((byte)(x ^ y));
                    if (y == 0)
                        break;

                }
            }
            inStream.Close();
            patchStream.Close();
            outStream.Close();
            return fileName;
        }
        private static long UPSDecode(FileStream patchStream)
        {
            long offset = 0;
            long shift = 1;
            while (true)
            {
                byte x = (byte)patchStream.ReadByte();
                offset += (x & 0x7f) * shift;
                if (x >= 0x80)
                    break;
                shift <<= 7;
                offset += shift;
            }
            return offset;
        }
    }
}
