using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    public class GameGenie
    {
        enum symbols
        {
            A = 0x00,
            P = 0x01,
            Z = 0x02,
            L = 0x03,
            G = 0x04,
            I = 0x05,
            T = 0x06,
            Y = 0x07,
            E = 0x08,
            O = 0x09,
            X = 0x0A,
            U = 0x0B,
            K = 0x0C,
            S = 0x0D,
            V = 0x0E,
            N = 0x0F
        }
        public string code;
        public ushort address;
        public byte value;
        public byte check;
        public GameGenie(string code)
        {
            code = code.ToUpper();
            this.code = code;
            this.address = 0;
            this.value = 0;
            if (code.Length == 6)
            {
                for (int i = 0; i < code.Length; i++)
                {
                    int charVal = (int)Enum.Parse(typeof(symbols),code[i].ToString());
                    switch(i)
                    {
                        case 0:
                            value |= (charVal & 0x1) != 0 ? (byte)0x01 : (byte)0x00;
                            value |= (charVal & 0x2) != 0 ? (byte)0x02 : (byte)0x00;
                            value |= (charVal & 0x4) != 0 ? (byte)0x04 : (byte)0x00;
                            value |= (charVal & 0x8) != 0 ? (byte)0x80 : (byte)0x00;
                            break;
                        case 1:
                            value |= (charVal & 0x1) != 0 ? (byte)0x10 : (byte)0x00;
                            value |= (charVal & 0x2) != 0 ? (byte)0x20 : (byte)0x00;
                            value |= (charVal & 0x4) != 0 ? (byte)0x40 : (byte)0x00;
                            address |= (charVal & 0x8) != 0 ? (ushort)0x80 : (ushort)0x00;
                            break;
                        case 2:
                            address |= (charVal & 0x1) != 0 ? (ushort)0x10 : (ushort)0x00;
                            address |= (charVal & 0x2) != 0 ? (ushort)0x20 : (ushort)0x00;
                            address |= (charVal & 0x4) != 0 ? (ushort)0x40 : (ushort)0x00;
                            //address |= (charVal & 0x8)  != 0 ? 0x80 : 0x00; DETERMINE LENGTH BIT
                            break;
                        case 3:
                            address |= (charVal & 0x1) != 0 ? (ushort)0x1000 : (ushort)0x00;
                            address |= (charVal & 0x2) != 0 ? (ushort)0x2000 : (ushort)0x00;
                            address |= (charVal & 0x4) != 0 ? (ushort)0x4000 : (ushort)0x00;
                            address |= (charVal & 0x8) != 0 ? (ushort)0x08 : (ushort)0x00;
                            break;
                        case 4:
                            address |= (charVal & 0x1) != 0 ? (ushort)0x01 : (ushort)0x00;
                            address |= (charVal & 0x2) != 0 ? (ushort)0x02 : (ushort)0x00;
                            address |= (charVal & 0x4) != 0 ? (ushort)0x04 : (ushort)0x00;
                            address |= (charVal & 0x8) != 0 ? (ushort)0x800 : (ushort)0x00;
                            break;
                        case 5:
                            address |= (charVal & 0x1) != 0 ? (ushort)0x100 : (ushort)0x00;
                            address |= (charVal & 0x2) != 0 ? (ushort)0x200 : (ushort)0x00;
                            address |= (charVal & 0x4) != 0 ? (ushort)0x400 : (ushort)0x00;
                            value |= (charVal & 0x8) != 0 ? (byte)0x08 : (byte)0x00;
                            break;
                    }
                }
            }
            else if (code.Length == 8)
            {
                for (int i = 0; i < code.Length; i++)
                {
                    int charVal = (int)Enum.Parse(typeof(symbols), code[i].ToString());
                    switch (i)
                    {
                        case 0:
                            value |= (charVal & 0x1) != 0 ? (byte)0x01 : (byte)0x00;
                            value |= (charVal & 0x2) != 0 ? (byte)0x02 : (byte)0x00;
                            value |= (charVal & 0x4) != 0 ? (byte)0x04 : (byte)0x00;
                            value |= (charVal & 0x8) != 0 ? (byte)0x80 : (byte)0x00;
                            break;
                        case 1:
                            value |= (charVal & 0x1) != 0 ? (byte)0x10 : (byte)0x00;
                            value |= (charVal & 0x2) != 0 ? (byte)0x20 : (byte)0x00;
                            value |= (charVal & 0x4) != 0 ? (byte)0x40 : (byte)0x00;
                            address |= (charVal & 0x8) != 0 ? (ushort)0x80 : (ushort)0x00;
                            break;
                        case 2:
                            address |= (charVal & 0x1) != 0 ? (ushort)0x10 : (ushort)0x00;
                            address |= (charVal & 0x2) != 0 ? (ushort)0x20 : (ushort)0x00;
                            address |= (charVal & 0x4) != 0 ? (ushort)0x40 : (ushort)0x00;
                            //address |= (charVal & 0x8)  != 0 ? 0x80 : 0x00; DETERMINE LENGTH BIT
                            break;
                        case 3:
                            address |= (charVal & 0x1) != 0 ? (ushort)0x1000 : (ushort)0x00;
                            address |= (charVal & 0x2) != 0 ? (ushort)0x2000 : (ushort)0x00;
                            address |= (charVal & 0x4) != 0 ? (ushort)0x4000 : (ushort)0x00;
                            address |= (charVal & 0x8) != 0 ? (ushort)0x08 : (ushort)0x00;
                            break;
                        case 4:
                            address |= (charVal & 0x1) != 0 ? (ushort)0x01 : (ushort)0x00;
                            address |= (charVal & 0x2) != 0 ? (ushort)0x02 : (ushort)0x00;
                            address |= (charVal & 0x4) != 0 ? (ushort)0x04 : (ushort)0x00;
                            address |= (charVal & 0x8) != 0 ? (ushort)0x800 : (ushort)0x00;
                            break;
                        case 5:
                            address |= (charVal & 0x1) != 0 ? (ushort)0x100 : (ushort)0x00;
                            address |= (charVal & 0x2) != 0 ? (ushort)0x200 : (ushort)0x00;
                            address |= (charVal & 0x4) != 0 ? (ushort)0x400 : (ushort)0x00;
                            check |= (charVal & 0x8) != 0 ? (byte)0x08 : (byte)0x00;
                            break;
                        case 6:
                            check |= (charVal & 0x1) != 0 ? (byte)0x01 : (byte)0x00;
                            check |= (charVal & 0x2) != 0 ? (byte)0x02 : (byte)0x00;
                            check |= (charVal & 0x4) != 0 ? (byte)0x04 : (byte)0x00;
                            check |= (charVal & 0x8) != 0 ? (byte)0x80 : (byte)0x00;
                            break;
                        case 7:
                            check |= (charVal & 0x1) != 0 ? (byte)0x10 : (byte)0x00;
                            check |= (charVal & 0x2) != 0 ? (byte)0x20 : (byte)0x00;
                            check |= (charVal & 0x4) != 0 ? (byte)0x40 : (byte)0x00;
                            value |= (charVal & 0x8) != 0 ? (byte)0x08 : (byte)0x00;
                            break;
                    }
                }
            }
            this.code = code;
        }
    }
}
