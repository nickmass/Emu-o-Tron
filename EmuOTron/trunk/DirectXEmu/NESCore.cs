//#define LogOpcodeStats

using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Xml;

namespace DirectXEmu
{
    class NESCore
    {

        private ushort programCounter = 0x0;
        private MemoryStore Memory;
        private ushort[] MirrorMap = new ushort[0x10000];
        private bool[] readOnly = new bool[0x10000];
        private bool emulationRunning = false;
        private byte A = 0;
        private byte X = 0;
        private byte Y = 0;
        private byte S = 0xFD;
        private byte P = 0x34;
        private int counter = 0;

        private mappers.Mapper romMapper;

        private bool interruptReset = false;
        private bool interruptIRQ = false;
        private bool interruptNMI = false;
        private bool interruptBRK = false;

        private int[] cycles = new int[0x100];
        private string[] opcodes = new string[0x100];
        private int[] addressingTypes = new int[0x100];

        private int[] scanlineLengths = { 113, 113, 114 };
        private byte slCounter = 0;
        private int scanline = 241;
        private int vblank = 1;

        private bool PPUAddrFlip = false;

        private MemoryStore PPUMemory;
        private ushort[] PPUMirrorMap = new ushort[0x8000];
        private bool[] PPUReadOnly = new bool[0x8000];
        private byte[] SPRMemory = new byte[0x100];
        private byte[] PalMemory = new byte[0x20];

        private int horzOffset = 0;
        private int vertOffset = 0;
        private int nameTableOffset = 0;

        public StringBuilder logBuilder = new StringBuilder();
        public bool logging = false;

        public StringBuilder romInfo = new StringBuilder();

        private Controller player1;
        private int player1Read = 0;
        private Controller player2;
        private int player2Read = 0;
        private Zapper player1Zap;
        private Zapper player2Zap;

        public GameGenie[] gameGenieCodes = new GameGenie[0xFF];
        public int gameGenieCodeNum = 0;

        public byte[][] scanlines = new byte[240][];
        public bool[] blueEmph = new bool[240];
        public bool[] greenEmph = new bool[240];
        public bool[] redEmph = new bool[240];

        public bool generateNameTables = false;
        public int generateLine = 0;
        public byte[][,] nameTables;

        public bool generatePatternTables = false;
        public int generatePatternLine = 0;
        public byte[][] patternTablesPalette;
        public byte[][,] patternTables;

        private bool spriteZeroHit = false;
        private bool spriteOverflow = false;

        private int[] flip = new int[8];

        public bool sramPresent = false;
        public string romHash;
        public UInt32 CRC = 0xffffffff;
        public string filePath;
        public string fileName;

        private ushort loopyT;
        private ushort loopyX;
        private ushort loopyV;
        private byte readBuffer;

        public bool displaySprites = true;
        public bool displayBG = true;
        public bool displaySpriteLimit = false;

        public string cartDBLocation = "";

        private bool turbo = false;

        public APU APU;
#if LogOpcodeStats
        private byte[] opcodeUsage = new byte[50000000];
        private int op = 0;
#endif
        public void Start(Controller player1, Controller player2, Zapper player1Zap, Zapper player2Zap, bool turbo)
        {
            this.player1 = player1;
            this.player2 = player2;
            this.player1Zap = player1Zap;
            this.player2Zap = player2Zap;
            this.turbo = turbo;
            this.Start();
        }
        public void Start(Controller player1)
        {
            this.player1 = player1;
            this.Start();
        }
        public void Start()
        {
            this.emulationRunning = true;
            byte value;
            ushort sum;
            byte opCode;
            while (this.emulationRunning)
            {
                opCode = this.readByte();
#if LogOpcodeStats
                if (op < opcodeUsage.Length)
                {
                    opcodeUsage[op] = opCode;
                    op++;
                }
                if (op == opcodeUsage.Length)
                {
                    op++;
                    int[] opCounter = new int[0x100];
                    for (int i = 0; i < opcodeUsage.Length; i++)
                    {
                        opCounter[opcodeUsage[i]]++;
                    }
                    string[] opSorted = new string[0x100];
                    int place = 0;
                    bool sorted = false;
                    while (!sorted)
                    {
                        int topDog = 0;
                        int topDogVal = -1;
                        for (int i = 0; i < 0x100; i++)
                        {
                            if (opCounter[i] > topDogVal)
                            {
                                topDog = i;
                                topDogVal = opCounter[i];
                            }
                        }
                        if (topDogVal != -1)
                        {
                            opSorted[place] = topDog.ToString() + " " + this.opcodes[topDog] + " " + topDogVal.ToString() + " " + (topDogVal*1.0) / opcodeUsage.Length * 100 + "%";
                            opCounter[topDog] = -1;
                            place++;
                        }
                        else
                            sorted = true;
                    }
                    File.WriteAllLines("opcoderesults.txt", opSorted);
                }
#endif
                #region logging
                if (this.logging)
                {
                    string data = "";
                    string addresses = "";
                    switch (addressingTypes[opCode])
                    {
                        case 0://null
                            data = "        ";
                            addresses = "                             ";
                            break;
                        case 1://#aa
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + "     ";
                            addresses = " #$" + this.readLogByte(this.programCounter).ToString("X2") + "                        ";
                            break;
                        case 2://A
                            data = "        ";
                            addresses = " A                           ";
                            break;
                        case 3://aa
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + "     ";
                            addresses = " $" + this.readLogByte(this.programCounter).ToString("X2") + " = " + this.readLogByte(this.readLogByte(this.programCounter)).ToString("X2") + "                    ";
                            break;
                        case 4://aa,X
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + "     ";
                            addresses = " $" + this.readLogByte(this.programCounter).ToString("X2") + ",X @ " + ((byte)(this.readLogByte(this.programCounter) + this.X)).ToString("X2") + " = " + this.readLogByte((byte)(this.readLogByte(this.programCounter) + this.X)).ToString("X2") + "             ";
                            break;
                        case 5://aa,Y
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + "     ";
                            addresses = " $" + this.readLogByte(this.programCounter).ToString("X2") + ",Y @ " + ((byte)(this.readLogByte(this.programCounter) + this.Y)).ToString("X2") + " = " + this.readLogByte((byte)(this.readLogByte(this.programCounter) + this.Y)).ToString("X2") + "             ";
                            break;
                        case 6://aaaa
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + " " + this.readLogByte((ushort)(this.programCounter + 1)).ToString("X2") + "  ";
                            addresses = " $" + this.readLogWord(this.programCounter).ToString("X4") + " = " + this.readLogByte(this.readLogWord(this.programCounter)).ToString("X2") + "                  ";
                            break;
                        case 7://aaaa,X
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + " " + this.readLogByte((ushort)(this.programCounter + 1)).ToString("X2") + "  ";
                            addresses = " $" + this.readLogWord(this.programCounter).ToString("X4") + ",X @ " + (this.readLogWord(this.programCounter) + this.X).ToString("X4") + " = " + this.readLogByte((ushort)(this.readLogWord(this.programCounter) + this.X)).ToString("X2") + "         ";
                            break;
                        case 8://aaaa,Y
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + " " + this.readLogByte((ushort)(this.programCounter + 1)).ToString("X2") + "  ";
                            addresses = " $" + this.readLogWord(this.programCounter).ToString("X4") + ",Y @ " + (this.readLogWord(this.programCounter) + this.Y).ToString("X4") + " = " + this.readLogByte((ushort)(this.readLogWord(this.programCounter) + this.Y)).ToString("X2") + "         ";
                            break;
                        case 9://(aa,X)
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + "     ";
                            addresses = " ($" + this.readLogByte(this.programCounter).ToString("X2") + ",X) @ " + ((byte)(this.readLogByte(this.programCounter) + (sbyte)this.X)).ToString("X2") + " = " + this.readLogWord(((byte)(this.readLogByte(this.programCounter) + (sbyte)this.X))).ToString("X4") + " = " + this.readLogByte(this.readLogWord(((byte)(this.readLogByte(this.programCounter) + (sbyte)this.X)))).ToString("X2") + "    ";
                            break;
                        case 10://(aa),Y
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + "     ";
                            addresses = " ($" + this.readLogByte(this.programCounter).ToString("X2") + "),Y = " + this.readLogWord(this.readLogByte(this.programCounter)).ToString("X4") + " @ " + (this.readLogWord(this.readLogByte(this.programCounter)) + this.Y).ToString("X4") + " = " + this.readLogByte((ushort)(this.readLogWord(this.readLogByte(this.programCounter)) + this.Y)).ToString("X2") + "  ";
                            break;
                        case 11://(aaaa) JMP
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + " " + this.readLogByte((ushort)(this.programCounter + 1)).ToString("X2") + "  ";
                            addresses = " ($" + this.readLogWord(this.programCounter).ToString("X4") + ") = " + this.readLogWord(this.readLogWord(this.programCounter)).ToString("X4") + "             ";
                            break;
                        case 12://Branch
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + "     ";
                            ushort tmp = (ushort)(this.programCounter + (sbyte)this.readLogByte(this.programCounter) + 1);
                            addresses = " $" + tmp.ToString("X4") + "                       ";
                            break;
                        case 13://aaaa JMP
                            data = " " + this.readLogByte(this.programCounter).ToString("X2") + " " + this.readLogByte((ushort)(this.programCounter + 1)).ToString("X2") + "  ";
                            addresses = " $" + this.readLogWord(this.programCounter).ToString("X4") + "                       ";
                            break;

                    }
                    if (this.logBuilder.Length > 1024 * 1024 * 100)
                        this.logBuilder.Remove(0, 1024 * 1024 * 50);
                    this.logBuilder.Append((this.programCounter - 1).ToString("X4") + "  " +
                        opCode.ToString("X2") + data + this.opcodes[opCode] + addresses + "A:" +
                        this.A.ToString("X2") + " X:" + this.X.ToString("X2") + " Y:" +
                        this.Y.ToString("X2") + " P:" + this.P.ToString("X2") + " SP:" +
                        this.S.ToString("X2") + " CYC:" + (this.counter * 3).ToString().PadLeft(3, ' ') +
                        " SL:" + (this.scanline).ToString().PadLeft(3, ' ') + "\r\n");
                }
                #endregion
                this.counter += this.cycles[opCode];
                #region opcodes
                switch (opCode)
                {
                    case 0x69: //ADC #aa
                        value = this.readByte();
                        goto ADC;
                    case 0x65: //ADC aa
                        value = this.readByte(this.readByte());
                        goto ADC;
                    case 0x75: //ADC aa,X
                        value = this.readByte(this.zpOffset(this.X));
                        goto ADC;
                    case 0x6D: //ADC aaaa
                        value = this.readByte(this.readWord());
                        goto ADC;
                    case 0x7D: //ADC aaaa,X
                        value = this.readByte(this.absOffset(this.X));
                        goto ADC;
                    case 0x79: //ADC aaaa,Y
                        value = this.readByte(this.absOffset(this.Y));
                        goto ADC;
                    case 0x61: //ADC (aa,X)
                        value = this.readByte(this.indexedIndirect(this.X));
                        goto ADC;
                    case 0x71: //ADC (aa),Y
                        value = this.readByte(this.indirectIndexed(this.Y));
                    ADC:/*
                        if (((this.A ^ value) & 0x80) != 0)
                            this.P &= 0xBF;
                        else
                            this.P |= 0x40;
                    sum = (ushort)(this.A + (this.P & 0x01) + value);
                    if (sum > 0xFF)
                    {
                        this.P |= 0x01;
                        if (((this.P & 0xBF) != 0) && (sum >= 0x180))
                            this.P &= 0xBF;
                    }
                    else
                    {
                        this.P &= 0xFE;
                        if (((this.P & 0xBF) != 0) && (sum < 0x80))
                            this.P &= 0xBF;
                    }
                    this.A = (byte)sum;*/
                    byte carry = (byte)(this.P & 0x01);
                    if (adcCarry[value, this.A, carry])
                        this.P |= 0x01;
                    else
                        this.P &= 0xFE;
                    if (adcOverflow[value, this.A, carry])
                        this.P |= 0x40;
                    else
                        this.P &= 0xBF;
                    this.A = adcTable[value, this.A, carry];
                    if ((this.A & 0x80) != 0)
                        this.P |= 0x80;
                    else
                        this.P &= 0x7F;
                    if (this.A == 0)
                        this.P |= 0x02;
                    else
                        this.P &= 0xFD;
                    break;
                    case 0x29: //AND #aa
                    this.A &= this.readByte();
                    goto AND;
                    case 0x25: //AND aa
                    this.A &= this.readByte(this.readByte());
                    goto AND;
                    case 0x35: //AND aa,X
                    this.A &= this.readByte(this.zpOffset(this.X));
                    goto AND;
                    case 0x2D: //AND aaaa
                    this.A &= this.readByte(this.readWord());
                    goto AND;
                    case 0x3D: //AND aaaa,X
                    this.A &= this.readByte(this.absOffset(this.X));
                    goto AND;
                    case 0x39: //AND aaaa,Y
                    this.A &= this.readByte(this.absOffset(this.Y));
                    goto AND;
                    case 0x21: //AND (aa,X)
                    this.A &= this.readByte(this.indexedIndirect(this.X));
                    goto AND;
                    case 0x31: //AND (aa),Y
                    this.A &= this.readByte(this.indirectIndexed(this.Y));
                AND:
                    if ((this.A & 0x80) != 0)
                        this.P |= 0x80;
                    else
                        this.P &= 0x7F;
                if (this.A == 0)
                    this.P |= 0x02;
                else
                    this.P &= 0xFD;
                break;
                    case 0x0A: //ASL A
                if ((this.A & 0x80) != 0)
                    this.P |= 0x01;
                else
                    this.P &= 0xFE;
                this.A <<= 1;
                if ((this.A & 0x80) != 0)
                    this.P |= 0x80;
                else
                    this.P &= 0x7F;
                if (this.A == 0)
                    this.P |= 0x02;
                else
                    this.P &= 0xFD;
                break;
                    case 0x06: //ASL aa
                sum = this.readByte();
                goto ASL;
                    case 0x16: //ASL aa,X
                sum = this.zpOffset(this.X);
                goto ASL;
                    case 0x0E: //ASL aaaa
                sum = this.readWord();
                goto ASL;
                    case 0x1E: //ASL aaaa,X
                sum = this.absOffset(this.X);
            ASL:
                value = this.readByte(sum);
            if ((value & 0x80) != 0)
                this.P |= 0x01;
            else
                this.P &= 0xFE;
            value <<= 1;
            if ((value & 0x80) != 0)
                this.P |= 0x80;
            else
                this.P &= 0x7F;
            if (value == 0)
                this.P |= 0x02;
            else
                this.P &= 0xFD;
            this.writeByte(sum, value);
            break;
                    case 0x90: //BCC
            value = this.readByte();
            if ((this.P & 0x01) == 0)
                this.programCounter = (ushort)(this.programCounter + (sbyte)value);
            break;
                    case 0xB0: //BCS
            value = this.readByte();
            if ((this.P & 0x01) != 0)
                this.programCounter = (ushort)(this.programCounter + (sbyte)value);
            /*{
                if (value < 0x80)
                    this.programCounter += value;
                else
                    this.programCounter -= (ushort)(((byte)(~value)) + 1);
            }*/
            break;
                    case 0xF0: //BEQ
            value = this.readByte();
            if ((this.P & 0x02) != 0)
                this.programCounter = (ushort)(this.programCounter + (sbyte)value);
            break;
                    case 0x24: //BIT aa
            value = this.readByte(this.readByte());
            goto BIT;
                    case 0x2C: //BIT aaaa
            value = this.readByte(this.readWord());
        BIT:
            if ((value & 0x80) != 0)
                this.P |= 0x80;
            else
                this.P &= 0x7F;
        if ((value & 0x40) != 0)
            this.P |= 0x40;
        else
            this.P &= 0xBF;
        if ((value & this.A) == 0)
            this.P |= 0x02;
        else
            this.P &= 0xFD;
        break;
                    case 0x30: //BMI
        value = this.readByte();
        if ((this.P & 0x80) != 0)
            this.programCounter = (ushort)(this.programCounter + (sbyte)value);
        break;
                    case 0xD0: //BNE
        value = this.readByte();
        if ((this.P & 0x02) == 0)
            this.programCounter = (ushort)(this.programCounter + (sbyte)value);
        break;
                    case 0x10: //BPL
        value = this.readByte();
        if ((this.P & 0x80) == 0)
            this.programCounter = (ushort)(this.programCounter + (sbyte)value);
        break;
                    case 0x00: //BRK
        this.interruptBRK = true;
        //this.emulationRunning = false;
        break;
                    case 0x50: //BVC
        value = this.readByte();
        if ((this.P & 0x40) == 0)
            this.programCounter = (ushort)(this.programCounter + (sbyte)value);
        break;
                    case 0x70: //BVS
        value = this.readByte();
        if ((this.P & 0x40) != 0)
            this.programCounter = (ushort)(this.programCounter + (sbyte)value);
        break;
                    case 0x18: //CLC
        this.P &= 0xFE;
        break;
                    case 0xD8: //CLD
        this.P &= 0xF7;
        break;
                    case 0x58: //CLI
        this.P &= 0xFB;
        break;
                    case 0xB8: //CLV
        this.P &= 0xBF;
        break;
                    case 0xC9: //CMP #aa
        value = this.readByte();
        goto CMP;
                    case 0xC5: //CMP aa
        value = this.readByte(this.readByte());
        goto CMP;
                    case 0xD5: //CMP aa,X
        value = this.readByte(this.zpOffset(this.X));
        goto CMP;
                    case 0xCD: //CMP aaaa
        value = this.readByte(this.readWord());
        goto CMP;
                    case 0xDD: //CMP aaaa,X
        value = this.readByte(this.absOffset(this.X));
        goto CMP;
                    case 0xD9: //CMP aaaa,Y
        value = this.readByte(this.absOffset(this.Y));
        goto CMP;
                    case 0xC1: //CMP (aa,X)
        value = this.readByte(this.indexedIndirect(this.X));
        goto CMP;
                    case 0xD1: //CMP (aa),Y
        value = this.readByte(this.indirectIndexed(this.Y));
    CMP:
        if (this.A >= value)
            this.P |= 0x01;
        else
            this.P &= 0xFE;
    sum = (ushort)(this.A - value);
    if (sum == 0)
        this.P |= 0x02;
    else
        this.P &= 0xFD;
    if ((sum & 0x80) != 0)
        this.P |= 0x80;
    else
        this.P &= 0x7F;
    break;
                    case 0xE0: //CPX #aa
    value = this.readByte();
    goto CPX;
                    case 0xE4: //CPX aa
    value = this.readByte(this.readByte());
    goto CPX;
                    case 0xEC: //CPX aaaa
    value = this.readByte(this.readWord());
CPX:
    if (this.X >= value)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
sum = (ushort)(this.X - value);
if (sum == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
if ((sum & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
break;
                    case 0xC0: //CPY #aa
value = this.readByte();
goto CPY;
                    case 0xC4: //CPY aa
value = this.readByte(this.readByte());
goto CPY;
                    case 0xCC: //CPY aaaa
value = this.readByte(this.readWord());
CPY:
if (this.Y >= value)
    this.P |= 0x01;
else
    this.P &= 0xFE;
sum = (ushort)(this.Y - value);
if (sum == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
if ((sum & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
break;
                    case 0xC7: //DCP aa
sum = this.readByte();
goto DCP;
                    case 0xD7: //DCP aa,X
sum = zpOffset(this.X);
goto DCP;
                    case 0xCF: //DCP aaaa
sum = this.readWord();
goto DCP;
                    case 0xDF: //DCP aaaa,X
sum = this.absOffset(this.X);
goto DCP;
                    case 0xDB: //DCP aaaa,Y
sum = this.absOffset(this.Y);
goto DCP;
                    case 0xC3: //DCP (aa,X)
sum = this.indexedIndirect(this.X);
goto DCP;
                    case 0xD3: //DCP (aa),Y
sum = this.indirectIndexed(this.Y);
DCP:
value = this.readByte(sum);
value--;
this.writeByte(sum, value);
goto CMP;
                    case 0xC6: //DEC aa
sum = this.readByte();
goto DEC;
                    case 0xD6: //DEC aa,X
sum = this.zpOffset(this.X);
goto DEC;
                    case 0xCE: //DEC aaaa
sum = this.readWord();
goto DEC;
                    case 0xDE: //DEC aaaa,X
sum = this.absOffset(this.X);
DEC:
value = this.readByte(sum);
value--;
if ((value & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (value == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
this.writeByte(sum, value);
break;
                    case 0xCA: //DEX
this.X--;
if ((this.X & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.X == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x88: //DEY
this.Y--;
if ((this.Y & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.Y == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x49: //EOR #aa
this.A ^= this.readByte();
goto EOR;
                    case 0x45: //EOR aa
this.A ^= this.readByte(this.readByte());
goto EOR;
                    case 0x55: //EOR aa,X
this.A ^= this.readByte(this.zpOffset(this.X));
goto EOR;
                    case 0x4D: //EOR aaaa
this.A ^= this.readByte(this.readWord());
goto EOR;
                    case 0x5D: //EOR aaaa,X
this.A ^= this.readByte(this.absOffset(this.X));
goto EOR;
                    case 0x59: //EOR aaaa,Y
this.A ^= this.readByte(this.absOffset(this.Y));
goto EOR;
                    case 0x41: //EOR (aa,X)
this.A ^= this.readByte(this.indexedIndirect(this.X));
goto EOR;
                    case 0x51: //EOR (aa),Y
this.A ^= this.readByte(this.indirectIndexed(this.Y));
EOR:
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0xE6: //INC aa
sum = this.readByte();
goto INC;
                    case 0xF6: //INC aa,X
sum = this.zpOffset(this.X);
goto INC;
                    case 0xEE: //INC aaaa
sum = this.readWord();
goto INC;
                    case 0xFE: //INC aaaa,X
sum = this.absOffset(this.X);
INC:
value = this.readByte(sum);
value++;
if ((value & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (value == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
this.writeByte(sum, value);
break;
                    case 0xE8: //INX
this.X++;
if ((this.X & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.X == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0xC8: //INY
this.Y++;
if ((this.Y & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.Y == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0xE7: //ISC aa
sum = this.readByte();
goto ISC;
                    case 0xF7: //ISC aa,X
sum = this.zpOffset(this.X);
goto ISC;
                    case 0xEF: //ISC aaaa
sum = this.readWord();
goto ISC;
                    case 0xFF: //ISC aaaa,X
sum = this.absOffset(this.X);
goto ISC;
                    case 0xFB: //ISC aaaa,Y
sum = this.absOffset(this.Y);
goto ISC;
                    case 0xE3: //ISC (aa,X)
sum = this.indexedIndirect(this.X);
goto ISC;
                    case 0xF3: //ISC (aa),Y
sum = this.indirectIndexed(this.Y);
ISC:
value = this.readByte(sum);
value++;
this.writeByte(sum, value);
goto SBC;
                    case 0x4C: //JMP aaaa
this.programCounter = this.readWord();
break;
                    case 0x6C: //JMP (aaaa)
this.programCounter = this.readWord(this.readWord());
break;
                    case 0x20: //JSR aaaa
sum = this.readWord();
this.pushWordStack((ushort)(this.programCounter - 1));
this.programCounter = sum;
break;
                    case 0xA7: //LAX aa
value = this.readByte(this.readByte());
goto LAX;
                    case 0xB7: //LAX aa,Y
value = this.readByte(this.zpOffset(this.Y));
goto LAX;
                    case 0xAF: //LAX aaaa
value = this.readByte(this.readWord());
goto LAX;
                    case 0xBF: //LAX aaaa,Y
value = this.readByte(this.absOffset(this.Y));
goto LAX;
                    case 0xA3: //LAX (aa,X)
value = this.readByte(this.indexedIndirect(this.X));
goto LAX;
                    case 0xB3: //LAX (aa),Y
value = this.readByte(this.indirectIndexed(this.Y));
LAX:
this.A = value;
this.X = value;
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0xA9: //LDA #aa
this.A = this.readByte();
goto LDA;
                    case 0xA5: //LDA aa
this.A = this.readByte(this.readByte());
goto LDA;
                    case 0xB5: //LDA aa,X
this.A = this.readByte(this.zpOffset(this.X));
goto LDA;
                    case 0xAD: //LDA aaaa
this.A = this.readByte(this.readWord());
goto LDA;
                    case 0xBD: //LDA aaaa,X
this.A = this.readByte(this.absOffset(this.X));
goto LDA;
                    case 0xB9: //LDA aaaa,Y
this.A = this.readByte(this.absOffset(this.Y));
goto LDA;
                    case 0xA1: //LDA (aa,X)
this.A = this.readByte(this.indexedIndirect(this.X));
goto LDA;
                    case 0xB1: //LDA (aa),Y
this.A = this.readByte(this.indirectIndexed(this.Y));
LDA:
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0xA2: //LDX #aa
this.X = this.readByte();
goto LDX;
                    case 0xA6: //LDX aa
this.X = this.readByte(this.readByte());
goto LDX;
                    case 0xB6: //LDX aa,Y
this.X = this.readByte(this.zpOffset(this.Y));
goto LDX;
                    case 0xAE: //LDX aaaa
this.X = this.readByte(this.readWord());
goto LDX;
                    case 0xBE: //LDX aaaa,Y
this.X = this.readByte(this.absOffset(this.Y));
LDX:
if ((this.X & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.X == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0xA0: //LDY #aa
this.Y = this.readByte();
goto LDY;
                    case 0xA4: //LDY aa
this.Y = this.readByte(this.readByte());
goto LDY;
                    case 0xB4: //LDY aa,X
this.Y = this.readByte(this.zpOffset(this.X));
goto LDY;
                    case 0xAC: //LDY aaaa
this.Y = this.readByte(this.readWord());
goto LDY;
                    case 0xBC: //LDY aaaa,X
this.Y = this.readByte(this.absOffset(this.X));
LDY:
if ((this.Y & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.Y == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x4A: //LSR A
if ((this.A & 0x01) != 0)
    this.P |= 0x01;
else
    this.P &= 0xFE;
this.A >>= 1;
this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x46: //LSR aa
sum = this.readByte();
goto LSR;
                    case 0x56: //LSR aa,X
sum = this.zpOffset(this.X);
goto LSR;
                    case 0x4E: //LSR aaaa
sum = this.readWord();
goto LSR;
                    case 0x5E: //LSR aaaa,X
sum = this.absOffset(this.X);
LSR:
value = this.readByte(sum);
if ((value & 0x01) != 0)
    this.P |= 0x01;
else
    this.P &= 0xFE;
value >>= 1;
this.P &= 0x7F;
if (value == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
this.writeByte(sum, value);
break;
                    case 0x04: //*NOP
                    case 0x44:
                    case 0x64:
                    case 0x14:
                    case 0x34:
                    case 0x54:
                    case 0x74:
                    case 0xD4:
                    case 0xF4:
                    case 0x80:
                    case 0x82:
                    case 0x89:
                    case 0xC2:
                    case 0xE2:
this.readByte();
break;
                    case 0x0C: //*NOP
                    case 0x1C:
                    case 0x3C:
                    case 0x5C:
                    case 0x7C:
                    case 0xDC:
                    case 0xFC:
this.readWord();
break;
                    case 0xEA: //NOP
                    case 0x1A:
                    case 0x3A:
                    case 0x5A:
                    case 0x7A:
                    case 0xDA:
                    case 0xFA:
break;
                    case 0x09: //ORA #aa
this.A |= this.readByte();
goto ORA;
                    case 0x05: //ORA aa
this.A |= this.readByte(this.readByte());
goto ORA;
                    case 0x15: //ORA aa,X
this.A |= this.readByte(this.zpOffset(this.X));
goto ORA;
                    case 0x0D: //ORA aaaa
this.A |= this.readByte(this.readWord());
goto ORA;
                    case 0x1D: //ORA aaaa,X
this.A |= this.readByte(this.absOffset(this.X));
goto ORA;
                    case 0x19: //ORA aaaa,Y
this.A |= this.readByte(this.absOffset(this.Y));
goto ORA;
                    case 0x01: //ORA (aa,X)
this.A |= this.readByte(this.indexedIndirect(this.X));
goto ORA;
                    case 0x11: //ORA (aa),Y
this.A |= this.readByte(this.indirectIndexed(this.Y));
ORA:
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x48: //PHA
this.pushByteStack(this.A);
break;
                    case 0x08: //PHP
value = this.P;
value |= 0x20;
value |= 0x10;
this.pushByteStack(value);
break;
                    case 0x68: //PLA
this.A = this.popByteStack();
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x28: //PLP
this.P = this.popByteStack();
this.P &= 0xEF;
break;
                    case 0x27: //RLA aa
sum = this.readByte();
goto RLA;
                    case 0x37: //RLA aa,X
sum = this.zpOffset(this.X);
goto RLA;
                    case 0x2F: //RLA aaaa
sum = this.readWord();
goto RLA;
                    case 0x3F: //RLA aaaa,X
sum = this.absOffset(this.X);
goto RLA;
                    case 0x3B: //RLA aaaa,Y
sum = this.absOffset(this.Y);
goto RLA;
                    case 0x23: //RLA (aa,X)
sum = this.indexedIndirect(this.X);
goto RLA;
                    case 0x33: //RLA (aa),Y
sum = this.indirectIndexed(this.Y);
RLA:
value = this.readByte(sum);
if ((this.P & 0x01) != 0)
{
    if ((value & 0x80) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    value <<= 1;
    value |= 0x01;
}
else
{
    if ((value & 0x80) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    value <<= 1;
}
this.writeByte(sum, value);
this.A &= value;
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x2A: //ROL A
if ((this.P & 0x01) != 0)
{
    if ((this.A & 0x80) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    this.A <<= 1;
    this.A |= 0x01;
}
else
{
    if ((this.A & 0x80) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    this.A <<= 1;
}
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x26: //ROL aa
sum = this.readByte();
goto ROL;
                    case 0x36: //ROL aa,X
sum = this.zpOffset(this.X);
goto ROL;
                    case 0x2E: //ROL aaaa
sum = this.readWord();
goto ROL;
                    case 0x3E: //ROL aaaa,X
sum = this.absOffset(this.X);
ROL:
value = this.readByte(sum);
if ((this.P & 0x01) != 0)
{
    if ((value & 0x80) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    value <<= 1;
    value |= 0x01;
}
else
{
    if ((value & 0x80) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    value <<= 1;
}
this.writeByte(sum, value);
if ((value & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (value == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x6A: //ROR A
if ((this.P & 0x01) != 0)
{
    if ((this.A & 0x01) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    this.A >>= 1;
    this.A |= 0x80;
}
else
{
    if ((this.A & 0x01) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    this.A >>= 1;
}
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x66: //ROR aa
sum = this.readByte();
goto ROR;
                    case 0x76: //ROR aa,X
sum = this.zpOffset(this.X);
goto ROR;
                    case 0x6E: //ROR aaaa
sum = this.readWord();
goto ROR;
                    case 0x7E: //ROR aaaa,X
sum = this.absOffset(this.X);
ROR:
value = this.readByte(sum);
if ((this.P & 0x01) != 0)
{
    if ((value & 0x01) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    value >>= 1;
    value |= 0x80;
}
else
{
    if ((value & 0x01) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    value >>= 1;
}
this.writeByte(sum, value);
if ((value & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (value == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x67: //RRA aa
sum = this.readByte();
goto RRA;
                    case 0x77: //RRA aa,X
sum = this.zpOffset(this.X);
goto RRA;
                    case 0x6F: //RRA aaaa
sum = this.readWord();
goto RRA;
                    case 0x7F: //RRA aaaa,X
sum = this.absOffset(this.X);
goto RRA;
                    case 0x7B: //RRA aaaa,Y
sum = this.absOffset(this.Y);
goto RRA;
                    case 0x63: //RRA (aa,X)
sum = this.indexedIndirect(this.X);
goto RRA;
                    case 0x73: //RRA (aa),Y
sum = this.indirectIndexed(this.Y);
RRA:
value = this.readByte(sum);
if ((this.P & 0x01) != 0)
{
    if ((value & 0x01) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    value >>= 1;
    value |= 0x80;
}
else
{
    if ((value & 0x01) != 0)
        this.P |= 0x01;
    else
        this.P &= 0xFE;
    value >>= 1;
}
this.writeByte(sum, value);
goto ADC;
                    case 0x40: //RTI
this.P = this.popByteStack();
this.programCounter = this.popWordStack();
break;
                    case 0x60: //RTS
this.programCounter = (ushort)(this.popWordStack() + 1);
break;
                    case 0x87: //SAX aa
value = (byte)(this.A & this.X);
this.writeByte(this.readByte(), value);
break;
                    case 0x97: //SAX aa,Y
value = (byte)(this.A & this.X);
this.writeByte(this.zpOffset(this.Y), value);
break;
                    case 0x8F: //SAX aaaa
value = (byte)(this.A & this.X);
this.writeByte(this.readWord(), value);
break;
                    case 0x83: //SAX (aa,X)
value = (byte)(this.A & this.X);
this.writeByte(this.indexedIndirect(this.X), value);
break;
                    case 0xEB: //SBC #aa
                    case 0xE9: //SBC #aa
value = this.readByte();
goto SBC;
                    case 0xE5: //SBC aa
value = this.readByte(this.readByte());
goto SBC;
                    case 0xF5: //SBC aa,X
value = this.readByte(this.zpOffset(this.X));
goto SBC;
                    case 0xED: //SBC aaaa
value = this.readByte(this.readWord());
goto SBC;
                    case 0xFD: //SBC aaaa,X
value = this.readByte(this.absOffset(this.X));
goto SBC;
                    case 0xF9: //SBC aaaa,Y
value = this.readByte(this.absOffset(this.Y));
goto SBC;
                    case 0xE1: //SBC (aa,X)
value = this.readByte(this.indexedIndirect(this.X));
goto SBC;
                    case 0xF1: //SBC (aa),Y
value = this.readByte(this.indirectIndexed(this.Y));
SBC:
value ^= 0xFF;
goto ADC;
       /*                
if (((this.A ^ value) & 0x80) == 0)
    this.P &= 0xBF;
else
    this.P |= 0x40;
sum = (ushort)(0xFF + this.A + (this.P & 0x01) - value);
if (sum <= 0xFF)
{
    this.P &= 0xFE;
    if (((this.P & 0xBF) != 0) && (sum < 0x80))
        this.P &= 0xBF;
}
else
{
    this.P |= 0x01;
    if (((this.P & 0xBF) != 0) && (sum >= 0x180))
        this.P &= 0xBF;
}
this.A = (byte)sum;
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;*/
                    case 0x38: // SEC
this.P |= 0x01;
break;
                    case 0xF8: // SED
this.P |= 0x08;
break;
                    case 0x78: //SEI
this.P |= 0x04;
break;
                    case 0x07: //SLO aa
sum = this.readByte();
goto SLO;
                    case 0x17: //SLO aa,X
sum = this.zpOffset(this.X);
goto SLO;
                    case 0x0F: //SLO aaaa
sum = this.readWord();
goto SLO;
                    case 0x1F: //SLO aaaa,X
sum = this.absOffset(this.X);
goto SLO;
                    case 0x1B: //SLO aaaa,Y
sum = this.absOffset(this.Y);
goto SLO;
                    case 0x03: //SLO (aa,X)
sum = this.indexedIndirect(this.X);
goto SLO;
                    case 0x13: //SLO (aa),Y
sum = this.indirectIndexed(this.Y);
SLO:
value = this.readByte(sum);
if ((value & 0x80) != 0)
    this.P |= 0x01;
else
    this.P &= 0xFE;
value <<= 1;
this.writeByte(sum, value);
this.A |= value;
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x47: //SRE aa
sum = this.readByte();
goto SRE;
                    case 0x57: //SRE aa,X
sum = this.zpOffset(this.X);
goto SRE;
                    case 0x4F: //SRE aaaa
sum = this.readWord();
goto SRE;
                    case 0x5F: //SRE aaaa,X
sum = this.absOffset(this.X);
goto SRE;
                    case 0x5B: //SRE aaaa,Y
sum = this.absOffset(this.Y);
goto SRE;
                    case 0x43: //SRE (aa,X)
sum = this.indexedIndirect(this.X);
goto SRE;
                    case 0x53: //SRE (aa),Y
sum = this.indirectIndexed(this.Y);
SRE:
value = this.readByte(sum);
if ((value & 0x01) != 0)
    this.P |= 0x01;
else
    this.P &= 0xFE;
value >>= 1;
this.writeByte(sum, value);
this.A ^= value;
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x85: //STA aa
this.writeByte(this.readByte(), this.A);
break;
                    case 0x95: //STA aa,X
this.writeByte(this.zpOffset(this.X), this.A);
break;
                    case 0x8D: //STA aaaa
this.writeByte(this.readWord(), this.A);
break;
                    case 0x9D: //STA aaaa,X
this.writeByte(this.absOffset(this.X), this.A);
break;
                    case 0x99: //STA aaaa,Y
this.writeByte(this.absOffset(this.Y), this.A);
break;
                    case 0x81: //STA (aa,X)
this.writeByte(this.indexedIndirect(this.X), this.A);
break;
                    case 0x91: //STA (aa),Y
this.writeByte(this.indirectIndexed(this.Y), this.A);
break;
                    case 0x86: //STX aa
this.writeByte(this.readByte(), this.X);
break;
                    case 0x96: //STX aa,Y
this.writeByte(this.zpOffset(this.Y), this.X);
break;
                    case 0x8E: //STX aaaa
this.writeByte(this.readWord(), this.X);
break;
                    case 0x84: //STY aa
this.writeByte(this.readByte(), this.Y);
break;
                    case 0x94: //STY aa,X
this.writeByte(this.zpOffset(this.X), this.Y);
break;
                    case 0x8C: //STY aaaa
this.writeByte(this.readWord(), this.Y);
break;
                    case 0xAA: //TAX
this.X = this.A;
if ((this.X & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.X == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0xA8: //TAY
this.Y = this.A;
if ((this.Y & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.Y == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0xBA: //TSX
this.X = this.S;
if ((this.X & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.X == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x8A: //TXA
this.A = this.X;
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    case 0x9A: //TXS
this.S = this.X;
break;
                    case 0x98: //TYA
this.A = this.Y;
if ((this.A & 0x80) != 0)
    this.P |= 0x80;
else
    this.P &= 0x7F;
if (this.A == 0)
    this.P |= 0x02;
else
    this.P &= 0xFD;
break;
                    default:
//throw new Exception("Unkown opcode: " + opCode.ToString("X2") + " " + this.opcodes[opCode]);
break;
                }
                #endregion
                APU.AddCycles(this.cycles[opCode]);
                if (this.interruptBRK)
                {
                    this.pushWordStack((ushort)(this.programCounter+1));
                    this.pushByteStack((byte)(this.P | 0x30));
                    this.P |= 0x04;//Was 0x14 CHANGED
                    this.programCounter = this.readLogWord(0xFFFE);
                    this.interruptBRK = false;
                }
                if (this.interruptReset)
                {
                    this.pushWordStack(this.programCounter);
                    this.pushByteStack(this.P);
                    this.P |= 0x04;
                    this.programCounter = this.readLogWord(0xFFFC);
                    this.interruptReset = false;
                }
                else if (this.interruptNMI)
                {
                    this.pushWordStack(this.programCounter);
                    this.pushByteStack(this.P);
                    this.P |= 0x04;
                    this.programCounter = this.readLogWord(0xFFFA);
                    this.interruptNMI = false;
                }
                else if ((this.interruptIRQ || romMapper.interruptMapper || APU.frameIRQ || APU.dmcInterrupt) && (this.P & 0x04) == 0)
                {
                    this.pushWordStack(this.programCounter);
                    this.pushByteStack(this.P);
                    this.P |= 0x04;
                    this.programCounter = this.readLogWord(0xFFFE);
                    this.interruptIRQ = false;
                }
                if (this.counter >= this.scanlineLengths[this.slCounter % 3])
                {
                    this.counter -= this.scanlineLengths[this.slCounter % 3];
                    this.slCounter++;

                    if (this.scanline == -1)
                    {
                        HorizontalReset();
                        VerticalReset();
                        HorizontalIncrement();
                        HorizontalIncrement();
                    }
                    if (this.scanline >= 0 && this.scanline < 240)
                    {
                        if (!turbo)
                            this.scanlines[this.scanline] = this.ProcessScanline(this.scanline);
                        else
                            SpriteZeroHit(this.scanline);
                    }
                    if (this.generateNameTables && this.scanline == this.generateLine)
                        this.nameTables = this.GenerateNameTables();
                    if (this.generatePatternTables && this.scanline == this.generatePatternLine)
                    {
                        this.patternTablesPalette = this.GeneratePatternTablePalette();
                        this.patternTables = this.GeneratePatternTables();
                    }

                    romMapper.MapperScanline(scanline, vblank);

                    this.scanline++;
                    if (this.scanline >= 241)
                    {
                        if (this.scanline == 241 && this.vblank == 0)
                        {
                            //this.PPUAddrFlip = false;
                            this.Memory[0x2002] |= 0x80;
                            if ((this.Memory[0x2000] & 0x80) != 0)
                                this.interruptNMI = true;
                        }
                        this.vblank++;
                        if (this.vblank >= 20)
                        {
                            this.spriteZeroHit = false;
                            this.spriteOverflow = false;
                            this.Memory[0x2002] &= 0x7F;
                            this.vblank = 0;
                            this.scanline = -1;
                            this.emulationRunning = false;
                        }
                    }
                }
            }
            APU.Update();
            this.generateNameTables = false;
            this.generatePatternTables = false;
        }/*
        private void HorizontalIncrement()
        {
            loopyV = (ushort)((loopyV & 0x7FE0) | ((loopyV + 0x01) & 0x1F));
            if ((loopyV & 0x1F) == 0)
                loopyV ^= 0x0400;
        }
        private void VerticalIncrement()
        {
            loopyV = (ushort)((loopyV + 0x1000) & 0x7FFF);//Vert Increment
            if ((loopyV & 0x7000) == 0)
            {
                loopyV = (ushort)((loopyV & 0x7C1F) | ((loopyV + 0x20) & 0x03E0));
                if ((loopyV & 0x03E0) == 0x03C0)
                    loopyV = (ushort)((loopyV & 0x7C1F) ^ 0x0800);
            }
        }
        private void VerticalReset()
        {
            if((this.Memory[0x2001] & 0x18) != 0)//dummy line
                loopyV = loopyT; //Vert reset
        }
        private void HorizontalReset()
        {
            if ((this.Memory[0x2001] & 0x18) != 0)
                loopyV = (ushort)((loopyV & 0x7BE0) | (loopyT & 0x041F)); //Horz reset
        }*/
        private void HorizontalIncrement() { }
        private void VerticalIncrement() { }
        private void VerticalReset() { }
        private void HorizontalReset() { }
        public NESCore(String input, String cartDBLocation)
        {
            this.cartDBLocation = cartDBLocation;
            for (int i = 7, j = 0; i >= -7; i -= 2, j++)
                this.flip[j] = i;
            for (int i = 0; i < 240; i++)
                this.scanlines[i] = new byte[256];
            this.loadCycles();
            this.loadOpcodes();
            this.loadAddressingTypes();
            this.TableADC();
            this.filePath = input;
            this.fileName = Path.GetFileNameWithoutExtension(filePath);
            FileStream inputStream = File.OpenRead(input);
            inputStream.Position = 0x10;
            HashAlgorithm romHash = HashAlgorithm.Create("MD5");
            byte[] romHashArray = romHash.ComputeHash(inputStream);
            for (int i = 0; i < 16; i++)
                this.romHash += romHashArray[i].ToString("X2");
            inputStream.Position = 0;
            if(inputStream.ReadByte() != 'N' || inputStream.ReadByte() != 'E' || inputStream.ReadByte() != 'S' || inputStream.ReadByte() != 0x1A)
                if (MessageBox.Show("File appears to be invalid. Attempt load anyway?", "Error", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    inputStream.Close();
                    throw (new Exception("Invalid File"));
                }
            inputStream.Position = 0x4;
            int numprgrom = inputStream.ReadByte();
            int numvrom = inputStream.ReadByte();
            int lowMapper = inputStream.ReadByte();
            bool trainer = ((lowMapper & 0x04) != 0);
            bool vertMirroring = ((lowMapper & 0x01) != 0);
            bool fourScreenMirroring = ((lowMapper & 0x08) != 0);
            this.sramPresent = ((lowMapper & 0x02) != 0);
            int highMapper = inputStream.ReadByte();
            inputStream.Position = 0x0F;
            if (inputStream.ReadByte() != 0)
                highMapper = 0;
            bool PC10 = ((highMapper & 0x02) != 0);
            bool VS = ((highMapper & 0x01) != 0);
            int mapper = (lowMapper >> 4) + (highMapper & 0xF0);
            Memory = new MemoryStore(0x20 + (numprgrom * 0x10), false);
            Memory.swapOffset = 0x20;
            if(numvrom > 0)
                PPUMemory = new MemoryStore(0x20 + (numvrom * 0x08), false);
            else
                PPUMemory = new MemoryStore(0x20 + (4 * 0x08), false);
            PPUMemory.swapOffset = 0x20;
            APU = new APU(Memory);
            romInfo.AppendLine(input);
            romInfo.AppendLine();
            romInfo.AppendLine("Mapper: " + mapper);
            romInfo.AppendLine("PRG-ROM: " + numprgrom.ToString() + " * 16KB");
            romInfo.AppendLine("CHR-ROM: " + numvrom.ToString() + " * 8KB");
            romInfo.AppendLine("Mirroring: " + (fourScreenMirroring ? "Four-screen" : (vertMirroring ? "Vertical" : "Horizontal")));
            if(VS)
                romInfo.AppendLine("VS Unisystem Game");
            if (this.sramPresent)
                romInfo.AppendLine("SRAM Present");
            inputStream.Position = 0x10;
            if (trainer)
            {
                romInfo.AppendLine("Trainer Present");
                for (int i = 0x00; i < 0x200; i++)
                {
                    this.Memory[i + 0x7000] = (byte)inputStream.ReadByte();
                    this.readOnly[i + 0x7000] = true;
                }
            }
            for (int i = 0x00; i < numprgrom * 0x4000; i++)
            {
                byte nextByte = (byte)inputStream.ReadByte();
                Memory.banks[(i / 0x400) + Memory.swapOffset][i % 0x400] = nextByte;
                CRC = CRC32.crc32_adjust(CRC, nextByte);
            }
            for (int i = 0x00; i < numvrom * 0x2000; i++)
            {
                byte nextByte = (byte)inputStream.ReadByte();
                PPUMemory.banks[(i / 0x400) + PPUMemory.swapOffset][i % 0x400] = nextByte;
                CRC = CRC32.crc32_adjust(CRC, nextByte);
            }
            CRC = CRC ^ 0xFFFFFFFF;
            romInfo.AppendLine("ROM MD5: " + this.romHash);
            romInfo.AppendLine("ROM CRC32: " + CRC.ToString("X8"));
            if (PC10)
            {
                romInfo.AppendLine("PC10 Game");
                for (int i = 0x00; i < 0x2000; i++)
                {
                    inputStream.ReadByte();
                }
            }
            string title = "";
            for (int i = 0x00; i < 0x80; i++)
            {
                byte titleChar = (byte)inputStream.ReadByte();
                if (titleChar != 255)
                    title += (char)titleChar;
            }
            if (title != "")
                romInfo.AppendLine("Title: " + title);
            inputStream.Close();

            this.readOnly[0x2002] = true;
            if (fourScreenMirroring)
            {
                PPUMemory.FourScreenMirroring();
                PPUMemory.hardwired = true;
            }
            else if (vertMirroring)
                PPUMemory.VerticalMirroring();
            else
                PPUMemory.HorizontalMirroring();
            if (File.Exists(Path.Combine(cartDBLocation, "NesCarts.xml")))
            {
                string gameName = "";
                string board = "";
                string dbMapper = "";
                bool done = false;
                bool match = false;
                XmlTextReader xmlReader = new XmlTextReader(Path.Combine(cartDBLocation, "NesCarts.xml"));
                while (xmlReader.Read() && !done)
                {
                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        if (xmlReader.Name == "game")
                        {
                            while (xmlReader.MoveToNextAttribute())
                            {
                                if (xmlReader.Name == "name")
                                    gameName = xmlReader.Value;
                            }
                            while ((!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "game")) && !done)
                            {
                                xmlReader.Read();
                                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "cartridge")
                                {
                                    while (xmlReader.MoveToNextAttribute())
                                    {
                                        if (xmlReader.Name == "crc")
                                            if (xmlReader.Value == CRC.ToString("X8"))
                                                match = true;
                                    }
                                    while ((!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "cartridge")) && !done)
                                    {
                                        xmlReader.Read();
                                        if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "board" && match)
                                        {
                                            while (xmlReader.MoveToNextAttribute())
                                            {
                                                if (xmlReader.Name == "type")
                                                    board = xmlReader.Value;
                                                if (xmlReader.Name == "mapper")
                                                    dbMapper = xmlReader.Value;

                                            }
                                            done = true;
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
                if (done)
                {
                    romInfo.AppendLine("Found in database");
                    romInfo.AppendLine("Name: " + gameName);
                    romInfo.AppendLine("Board: " + board);
                    romInfo.AppendLine("Mapper: " + dbMapper);
                }
                else
                {
                    romInfo.AppendLine("No database entry");
                }
            }
            else
            {
                romInfo.AppendLine("NesCarts.xml not found");
            }
            #region mappers
            switch (mapper)
            {
                case 0://NROM
                    romMapper = new mappers.m000(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                case 1: //MMC1
                    romMapper = new mappers.m001(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                case 2: //UNROM
                    romMapper = new mappers.m002(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                case 3://CNROM
                    romMapper = new mappers.m003(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                case 4: //MMC3
                    romMapper = new mappers.m004(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                case 7: //AOROM
                    romMapper = new mappers.m007(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                case 34: //BNROM and NINA-001
                    romMapper = new mappers.m034(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                case 70: //Bandai
                    romMapper = new mappers.m070(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                case 71: //Camerica
                    romMapper = new mappers.m071(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                default:
                    MessageBox.Show("This game will probably not load, mapper unsupported.\r\nMapper:" + mapper.ToString() + " PRG-ROM:" + numprgrom.ToString() + " CHR-ROM:" + numvrom.ToString());
                    goto case 0;

            }
            romMapper.MapperInit();
            #endregion
            for (int i = 0; i < 0x10000; i++)
            {
                this.MirrorMap[i] = (ushort)i;
                this.PPUMirrorMap[(i & 0x7FFF)] = (ushort)(i & 0x7FFF);
            }
            this.PPUMirror(0x3F00, 0x3F10, 1, 1);
            this.PPUMirror(0x3F04, 0x3F14, 1, 1);
            this.PPUMirror(0x3F08, 0x3F18, 1, 1);
            this.PPUMirror(0x3F0C, 0x3F1C, 1, 1);
            this.PPUMirror(0x2000, 0x3000, 0x0F00, 1);
            this.PPUMirror(0x3F00, 0x3F20, 0x20, 7);
            this.PPUMirror(0x0000, 0x4000, 0x4000, 1);
            this.CPUMirror(0x0000, 0x0800, 0x0800, 3);
            this.CPUMirror(0x2000, 0x2008, 0x08, 0x3FF);
            this.programCounter = this.readLogWord(0xFFFC);//entry point
            for(int i = 0; i < 0x20; i++)
                this.PalMemory[i] = 0x0F; //Sets the background to black on startup to prevent grey flashes, not exactly accurate but it looks nicer
        }
        private byte readByte()
        {
            byte nextByte = this.readByte(this.programCounter);
            this.programCounter++;
            return nextByte;
        }
        private byte readByte(ushort address)
        {
            byte nextByte = this.Memory[this.MirrorMap[address]];
            if (this.MirrorMap[address] == 0x2002) //PPU Status register
            {
                this.Memory[0x2002] = (byte)(nextByte & 0x7F);
                if (this.spriteOverflow)
                {
                    this.Memory[0x2002] |= 0x20;
                    nextByte |= 0x20;
                }
                else
                {
                    this.Memory[0x2002] &= 0xDF; 
                    nextByte &= 0xDF;
                }
                if (this.spriteZeroHit)
                {
                    this.Memory[0x2002] |= 0x40;
                    nextByte |= 0x40;
                }
                else
                {
                    this.Memory[0x2002] &= 0xBF;
                    nextByte &= 0xBF;
                }/*
                else
                {
                    if (this.SpriteZeroHit(this.scanline, this.counter))
                    {
                        this.spriteZeroHit = true;
                        this.Memory[0x2002] |= 0x40;
                        nextByte |= 0x40;
                    }
                    else
                    {
                        this.Memory[0x2002] &= 0xBF;
                        nextByte &= 0xBF;
                    }
                }*/
                this.interruptNMI = false;
                this.PPUAddrFlip = false;
            }
            else if (this.MirrorMap[address] == 0x2004)
            {
                nextByte = this.SPRMemory[this.Memory[this.MirrorMap[0x2003]]];
            }
            else if (this.MirrorMap[address] == 0x2007) //Read write PPU Data
            {/*
                if (this.PPUMirrorMap[this.PPUAddr] >= 0x3F00)
                {
                    nextByte = this.PPUMemory[this.PPUMirrorMap[this.PPUAddr]];
                    this.PPUAddrDelay = this.PPUMemory[this.PPUMirrorMap[this.PPUAddr]];
                }
                else
                {
                    nextByte = this.PPUAddrDelay;
                    this.PPUAddrDelay = this.PPUMemory[this.PPUMirrorMap[this.PPUAddr]];
                }
                if ((this.PPUMemory[0x2000] & 0x04) == 0)
                {
                    this.PPUAddr += 1;
                }
                else
                    this.PPUAddr += 32;*/
                if ((loopyV & 0x3F00) == 0x3F00)
                {
                    nextByte = PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F];
                    readBuffer = this.PPUMemory[this.PPUMirrorMap[(loopyV & 0x2FFF)]];
                }
                else
                {
                    nextByte = readBuffer;
                    readBuffer = this.PPUMemory[this.PPUMirrorMap[(loopyV & 0x3FFF)]];
                }
                loopyV = (ushort)((loopyV + ((this.Memory[0x2000] & 0x04) != 0 ? 0x20 : 0x01)) & 0x7FFF);

            }
            else if (this.MirrorMap[address] == 0x4016) //Player1 Controller
            {
                nextByte = 0;
                if (player1Zap.connected)
                {
                    if (player1Zap.triggerPulled)
                        nextByte |= 0x10;
                    if (!player1Zap.lightDetected)
                        nextByte |= 0x08;
                }
                switch (this.player1Read)
                {
                    case 2:
                        if (this.player1.a)
                            nextByte |= 0x01;
                        this.player1Read++;
                        break;
                    case 3:
                        if (this.player1.b)
                            nextByte |= 0x01;
                        this.player1Read++;
                        break;
                    case 4:
                        if (this.player1.select)
                            nextByte |= 0x01;
                        this.player1Read++;
                        break;
                    case 5:
                        if (this.player1.start)
                            nextByte |= 0x01;
                        this.player1Read++;
                        break;
                    case 6:
                        if (this.player1.up)
                            nextByte |= 0x01;
                        this.player1Read++;
                        break;
                    case 7:
                        if (this.player1.down)
                            nextByte |= 0x01;
                        this.player1Read++;
                        break;
                    case 8:
                        if (this.player1.left)
                            nextByte |= 0x01;
                        this.player1Read++;
                        break;
                    case 9:
                        if (this.player1.right)
                            nextByte |= 0x01;
                        this.player1Read++;
                        break;
                    default:
                        nextByte |= 0x01;
                        break;


                }
            }
            else if (this.MirrorMap[address] == 0x4017) //Player2 Controller
            {
                nextByte = 0;
                if (player2Zap.connected)
                {
                    if (player2Zap.triggerPulled)
                        nextByte |= 0x10;
                    if (!player2Zap.lightDetected)
                        nextByte |= 0x08;
                }
                switch (this.player2Read)
                {
                    case 2:
                        if (this.player2.a)
                            nextByte |= 0x01;
                        this.player2Read++;
                        break;
                    case 3:
                        if (this.player2.b)
                            nextByte |= 0x01;
                        this.player2Read++;
                        break;
                    case 4:
                        if (this.player2.select)
                            nextByte |= 0x01;
                        this.player2Read++;
                        break;
                    case 5:
                        if (this.player2.start)
                            nextByte |= 0x01;
                        this.player2Read++;
                        break;
                    case 6:
                        if (this.player2.up)
                            nextByte |= 0x01;
                        this.player2Read++;
                        break;
                    case 7:
                        if (this.player2.down)
                            nextByte |= 0x01;
                        this.player2Read++;
                        break;
                    case 8:
                        if (this.player2.left)
                            nextByte |= 0x01;
                        this.player2Read++;
                        break;
                    case 9:
                        if (this.player2.right)
                            nextByte |= 0x01;
                        this.player2Read++;
                        break;
                    default:
                        nextByte |= 0x01;
                        break;
                }
            }
            nextByte = APU.Read(nextByte, this.MirrorMap[address]);
            return nextByte;
        }
        private ushort readWord()
        {
            return (ushort)(this.readByte() + (this.readByte() << 8));
        }
        private ushort readWord(ushort address)
        {
            byte highByte = (byte)(address >> 8);
            byte lowByte = (byte)(address + 1);
            ushort highAddress = (ushort)(lowByte + (highByte << 8));
            return (ushort)(this.readByte(address) + (this.readByte(highAddress) << 8));
        }
        private byte readLogByte(ushort address)
        {
            byte nextByte = this.Memory[address];
            return nextByte;
        }
        private ushort readLogWord(ushort address)
        {
            byte highByte = (byte)(address >> 8);
            byte lowByte = (byte)(address + 1);
            ushort highAddress = (ushort)(lowByte + (highByte << 8));
            return (ushort)(this.readLogByte(address) + (this.readLogByte(highAddress) << 8));
        }
        private void writeByte(ushort address, byte value)
        {
            romMapper.MapperWrite(MirrorMap[address], value);
            /*if (this.MirrorMap[address] == 0x2001) //Everynes doc says sprite memory is destroyed when rendering is disabled, didn't appear to increase accuracy, but only caused lad amount of blinking.
            {
                if((value & 0x18) == 0)
                    this.SPRMemory = new byte[0x100];
            }*/
            if (this.MirrorMap[address] == 0x4014) //Sprite DMA
            {
                ushort startAddress = (ushort)(value << 8);
                byte sprAddress = this.Memory[this.MirrorMap[0x2003]];
                for (int i = 0; i < 0x100; i++)
                {
                    this.SPRMemory[(byte)(sprAddress + i)] = this.Memory[this.MirrorMap[(ushort)(startAddress + i)]];
                }
                this.counter += 512;
            }
            else if (this.MirrorMap[address] == 0x2004) //Sprite Write
            {
                byte sprAddress = this.Memory[this.MirrorMap[0x2003]];
                this.SPRMemory[sprAddress] = value;
                sprAddress++;
                this.Memory[0x2003] = sprAddress;
            }
            else if (this.MirrorMap[address] == 0x2000)
            {
                loopyT = (ushort)((loopyT & 0x0C00) | (value << 10));
                this.nameTableOffset = value & 0x03;
            }
            else if (this.MirrorMap[address] == 0x2005) //PPUScroll
            {
                if (this.PPUAddrFlip) //2nd Write
                {
                    /*if (value >= 240)                     //Some documents claim that vert offset doesnt change untill vblank but I have my doubts, or my implimentation is poor. See Top Gun (E)[!].nes plane cockpit during flight.
                        this.nextVertOffset = 240 - value;
                    else
                        this.nextVertOffset = value;*/
                    if (value >= 240)
                        this.vertOffset = 240 - value;
                    else
                        this.vertOffset = value;

                    loopyT = (ushort)((loopyT & 0x0C1F) | ((value & 0x07) << 12) | ((value & 0xF8) << 2));
                }
                else //1st Write
                {
                    this.horzOffset = value;

                    loopyT = (ushort)((loopyT & 0x7FE0) | (value >> 3));
                    loopyX = (ushort)(value & 0x07);
                }
                this.PPUAddrFlip = !this.PPUAddrFlip;
            }
            else if (this.MirrorMap[address] == 0x2006) //PPUAddr
            {
                if (this.PPUAddrFlip)//2nd Write
                {
                    this.vertOffset = (this.vertOffset & 0xC7) | ((value & 0xE0) >> 2);
                    this.horzOffset = (this.horzOffset & 0x07) | ((value & 0x1F) << 3);
                    if (vertOffset > 240)
                        vertOffset -= 240;
                    loopyT = (ushort)((loopyT & 0xFF00) | value);
                    loopyV = loopyT;
                }
                else//1st Write
                {
                    this.nameTableOffset = (byte)((value & 0xC) >> 2); //TO-DO: this is wrong but I don't think it will effect many games other then fixing mario
                    this.vertOffset = (this.vertOffset & 0xF8) | ((value & 0x30) >> 4);
                    this.vertOffset = (this.vertOffset & 0x3F) | ((value & 3) << 6);
                    if (vertOffset > 240)
                        vertOffset -= 240;
                    loopyT = (ushort)((loopyT & 0x00FF) | ((value & 0x3F) << 8));
                }
                this.PPUAddrFlip = !this.PPUAddrFlip;
            }
            else if (this.MirrorMap[address] == 0x2007) //PPU Write
            {
                /*
                if (!this.PPUReadOnly[this.PPUMirrorMap[this.PPUAddr]])
                    this.PPUMemory[this.PPUMirrorMap[this.PPUAddr]] = value;

                if ((this.readByte(0x2000) & 0x04) == 0)
                {
                    this.PPUAddr += 1;
                }
                else
                    this.PPUAddr += 32;*/
                if ((loopyV & 0x3F00) == 0x3F00)
                    PalMemory[(loopyV & 0x3) != 0 ? loopyV & 0x1F : loopyV & 0x0F] = (byte)(value & 0x3F);
                else if (!this.PPUReadOnly[this.PPUMirrorMap[loopyV & 0x3FFF]])
                    this.PPUMemory[this.PPUMirrorMap[loopyV & 0x3FFF]] = value;
                loopyV = (ushort)((loopyV + ((this.Memory[0x2000] & 0x04) != 0 ? 0x20 : 0x01)) & 0x7FFF);
            }
            else if (this.MirrorMap[address] == 0x4016)
            {
                if ((value & 0x01) != 0)
                {
                    this.player1Read = 1;
                    this.player2Read = 1;
                }
                if (this.player1Read == 1)
                {
                    if ((value & 0x01) == 0)
                        this.player1Read = 2;
                }
                if (this.player2Read == 1)
                {
                    if ((value & 0x01) == 0)
                        this.player2Read = 2;
                }
            }

            APU.Write(value, this.MirrorMap[address]);

            if (this.readOnly[this.MirrorMap[address]] == false)
                this.Memory[this.MirrorMap[address]] = value;
            ApplyGameGenie();
        }
        private ushort absOffset(ushort address, byte offset)
        {
            return (ushort)(address + offset);
        }
        private ushort absOffset(byte offset)
        {
            return (ushort)(this.readWord() + offset);
        }
        private ushort zpOffset(byte address, byte offset)
        {
            return (byte)(address + (sbyte)offset);
        }
        private ushort zpOffset(byte offset)
        {
            return (byte)(this.readByte() + (sbyte)offset);
        }
        private ushort indexedIndirect(byte address, byte offset)
        {
            return this.readWord((byte)this.zpOffset(address, offset));
        }
        private ushort indexedIndirect(byte offset)
        {
            return this.readWord((byte)this.zpOffset(offset));
        }
        private ushort indirectIndexed(byte address, byte offset)
        {
            return (ushort)(this.readWord(address) + offset);
        }
        private ushort indirectIndexed(byte offset)
        {
            return (ushort)(this.readWord(this.readByte()) + offset);
        }
        private void pushWordStack(ushort address)
        {
            this.writeByte((ushort)(this.S + 0x0100), (byte)(address >> 8));
            this.S--;
            this.writeByte((ushort)(this.S + 0x0100), (byte)address);
            this.S--;
        }
        private ushort popWordStack()
        {
            this.S += 2;
            return this.readWord((ushort)((this.S - 1) + 0x0100));
        }
        private void pushByteStack(byte value)
        {
            this.writeByte((ushort)(this.S + 0x0100), value);
            this.S--;
        }
        private byte popByteStack()
        {
            this.S++;
            return this.readByte((ushort)(this.S + 0x0100));
        }
        private void SpriteZeroHit(int scanline)
        {
            if (spriteZeroHit != true)
            {
                byte PPUCTRL = this.Memory[0x2000];
                bool squareSprites = false;
                if ((PPUCTRL & 0x20) == 0)
                    squareSprites = true;
                byte yPos = (byte)(this.SPRMemory[0] + 1);
                if((squareSprites && (yPos <= scanline && yPos + 8 > scanline)) || (!squareSprites &&  (yPos <= scanline && yPos + 16 > scanline)))
                    this.scanlines[scanline] = ProcessScanline(scanline);
            }
        }
        private bool SpriteZeroHit(int scanline, int counter)
        {
            if (spriteZeroHit)
                return true;
            byte PPUCTRL = this.Memory[0x2000];
            byte PPUMASK = this.Memory[0x2001];
            if ((PPUMASK & 0x08) != 0 && (PPUMASK & 0x10) != 0) //If rendering enabled
            {
                bool squareSprites = false;
                ushort spriteTable = 0;
                if ((PPUCTRL & 0x20) == 0)
                {
                    squareSprites = true;
                    if ((PPUCTRL & 0x08) != 0)
                        spriteTable = 0x1000;
                }
                byte yPos = (byte)(this.SPRMemory[0] + 1);
                byte tileNum = this.SPRMemory[1];
                byte attr = this.SPRMemory[2];
                byte xPos = this.SPRMemory[3];
                byte palette = (byte)(attr & 0x03);


                ushort backgroundTable = 0;
                if ((PPUCTRL & 0x10) != 0)
                    backgroundTable = 0x1000;
                ushort nameTableOrigin = (ushort)(((this.nameTableOffset) * 0x400) + 0x2000);

                if (squareSprites)//8x8 Sprites
                {
                    if((yPos <= scanline && yPos + 8 > scanline) && (xPos <= counter*3))//If sprite is on this scan line and rendered by now
                    {
                        for (byte spritePixel = 0; spritePixel < 8 && (xPos + spritePixel <= counter * 3) && (xPos + spritePixel < 256); spritePixel++)
                        {
                            ushort nameTableOffset = nameTableOrigin;
                            int pointY = yPos + scanline + vertOffset;
                            int pointX = spritePixel + xPos + horzOffset;
                            if (pointY >= 240) //If pixel is off the edge of origin nametable, move to next table
                            {
                                if (nameTableOrigin == 0x2000 || nameTableOrigin == 0x2400)
                                    nameTableOffset += 0x800;
                                else
                                    nameTableOffset -= 0x800;
                                pointY -= 240;
                            }
                            if (pointX >= 256)
                            {
                                if (nameTableOrigin == 0x2000 || nameTableOrigin == 0x2800)
                                    nameTableOffset += 0x400;
                                else
                                    nameTableOffset -= 0x400;
                                pointX -= 256;
                            }
                            byte bgTileNumber = this.PPUMemory[this.PPUMirrorMap[nameTableOffset + ((pointY / 8) * 32) + (pointX / 8)]];
                            byte backColor = GetTilePixel(backgroundTable, bgTileNumber, (byte)(pointX % 8), (byte)(pointY % 8), false);
                            if (spritePixel + xPos < 255 && xPos > 0 && scanline + yPos < 240 && backColor != 0)
                            {
                                int vertFlip = 0;
                                if ((attr & 0x80) != 0)
                                    vertFlip = this.flip[(scanline - yPos) % 8];
                                byte color = GetTilePixel(spriteTable, tileNum, spritePixel, (byte)(scanline - yPos + vertFlip), (attr & 0x40) != 0);
                                if (color != 0)
                                    return true;
                            }
                        }
                    }
                }
                else //8x16 Limit
                {
                    if ((yPos <= scanline && yPos + 16 > scanline) && (xPos <= counter * 3))//If sprite is on this scan line and rendered by now
                    {
                        if ((tileNum & 0x01) != 0)
                            spriteTable = 0x1000;
                        else
                            spriteTable = 0x0;
                        tileNum &= 0xFE;
                        for (byte spritePixel = 0; spritePixel < 8 && xPos + spritePixel <= counter * 3 && (xPos + spritePixel < 256); spritePixel++)
                        {
                            ushort nameTableOffset = nameTableOrigin;
                            int pointY = yPos + scanline + vertOffset;
                            int pointX = spritePixel + xPos + horzOffset;
                            if (pointY >= 240) //If pixel is off the edge of origin nametable, move to next table
                            {
                                if (nameTableOrigin == 0x2000 || nameTableOrigin == 0x2400)
                                    nameTableOffset += 0x800;
                                else
                                    nameTableOffset -= 0x800;
                                pointY -= 240;
                            }
                            if (pointX >= 256)
                            {
                                if (nameTableOrigin == 0x2000 || nameTableOrigin == 0x2800)
                                    nameTableOffset += 0x400;
                                else
                                    nameTableOffset -= 0x400;
                                pointX -= 256;
                            }
                            byte bgTileNumber = this.PPUMemory[this.PPUMirrorMap[nameTableOffset + ((pointY / 8) * 32) + (pointX / 8)]];
                            byte backColor = GetTilePixel(backgroundTable, bgTileNumber, (byte)(pointX % 8), (byte)(pointY % 8), false);
                            if (spritePixel + xPos < 255 && xPos > 0 && scanline + yPos < 240 && backColor != 0)
                            {
                                int vertFlip = 0;
                                if ((attr & 0x80) != 0)
                                    vertFlip = this.flip[(scanline - yPos) % 8];
                                byte color;
                                if ((attr & 0x80) != 0) //If vertical flipped
                                    if (scanline - yPos < 8)
                                        color = GetTilePixel(spriteTable, (byte)(tileNum + 1), spritePixel, (byte)(((scanline - yPos) % 8) + vertFlip), (attr & 0x40) != 0); //Need mod 8 here because tile can be 16 px tall
                                    else
                                        color = GetTilePixel(spriteTable, tileNum, spritePixel, (byte)(((scanline - yPos) % 8) + vertFlip), (attr & 0x40) != 0);
                                else
                                    if (scanline - yPos < 8)
                                        color = GetTilePixel(spriteTable, tileNum, spritePixel, (byte)(((scanline - yPos) % 8)), (attr & 0x40) != 0);
                                    else
                                        color = GetTilePixel(spriteTable, (byte)(tileNum + 1), spritePixel, (byte)(((scanline - yPos) % 8)), (attr & 0x40) != 0);
                                if (color != 0)
                                    return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        private void CPUMirror(ushort address, ushort mirrorAddress, ushort length, int repeat)
        {
            for (int j = 0; j < repeat; j++)
                for (int i = 0; i < length; i++)
                    this.MirrorMap[mirrorAddress + i + (j * length)] = (ushort)(this.MirrorMap[address + i]);
        }
        private void PPUMirror(ushort address, ushort mirrorAddress, ushort length, int repeat)
        {
            for (int j = 0; j < repeat; j++)
                for (int i = 0; i < length; i++)
                    this.PPUMirrorMap[mirrorAddress + i + (j * length)] = (ushort)(this.PPUMirrorMap[address + i]);
        }
        private byte[] ProcessScanline(int line)//256 wide, 240 tall, 32x30
        {
            byte PPUCTRL = this.Memory[0x2000];
            byte PPUMASK = this.Memory[0x2001];
            if((PPUMASK & 0x80)!= 0)
                this.blueEmph[line] = true;
            else
                this.blueEmph[line] = false;
            if ((PPUMASK & 0x40) != 0)
                this.greenEmph[line] = true;
            else
                this.greenEmph[line] = false;
            if ((PPUMASK & 0x20) != 0)
                this.redEmph[line] = true;
            else
                this.redEmph[line] = false;
            
            byte[] scanline = new byte[256];
            bool[] zeroBackground = new bool[256];
            byte[] spriteLine = new byte[256];
            bool[] spriteAboveLine = new bool[256];
            bool[] spriteBelowLine = new bool[256];

            bool monochrome = (PPUMASK & 0x01) != 0;
            if ((PPUMASK & 0x08) != 0) //If background rendering is enabled
            {
                bool backgroundClipping = (PPUMASK & 0x2) == 0;
                ushort backgroundTable = 0;
                if ((PPUCTRL & 0x10) != 0)
                    backgroundTable = 0x1000;
                ushort nameTableOrigin = (ushort)(((this.nameTableOffset) * 0x400) + 0x2000);
                //ushort nameTableOrigin = (ushort)((((byte)(PPUCTRL & 0x03)) * 0x400) + 0x2000);
                //ushort nameTableOrigin = (ushort)((((loopyV >> 0xA) & 0x03) * 0x400) + 0x2000);
                for (int column = 0; column < 256; column++)//For each pixel in scanline
                {
                    ushort nameTableOffset = nameTableOrigin;
                    int pointY = line + vertOffset;
                    int pointX = column + horzOffset;
                    if (pointY >= 240) //If pixel is off the edge of origin nametable, move to next table
                    {
                        if (nameTableOrigin == 0x2000 || nameTableOrigin == 0x2400)
                            nameTableOffset += 0x800;
                        else
                            nameTableOffset -= 0x800;
                        pointY -= 240;
                    }
                    if (pointX >= 256)
                    {
                        if (nameTableOrigin == 0x2000 || nameTableOrigin == 0x2800)
                            nameTableOffset += 0x400;
                        else
                            nameTableOffset -= 0x400;
                        pointX -= 256;
                    }
                    byte tileNumber = this.PPUMemory[this.PPUMirrorMap[nameTableOffset + ((pointY / 8) * 32) + (pointX / 8)]];
                    byte color = GetTilePixel(backgroundTable, tileNumber, (byte)(pointX % 8), (byte)(pointY % 8), false);
                    if (color == 0 || (backgroundClipping && column < 8))
                    {
                        byte palColor = this.PalMemory[0x00];
                        if (monochrome) //If monochrome bit set, have NOT testing in game.
                            palColor &= 0xF0;
                        scanline[column] = palColor;
                        zeroBackground[column] = true;
                    }
                    else
                    {
                        byte attribute = this.PPUMemory[this.PPUMirrorMap[(nameTableOffset + 0x3C0) + ((pointY / 32) * 8) + (pointX / 32)]];
                        if (pointY % 32 < 16 && pointX % 32 < 16)
                            attribute &= 0x03;
                        else if (pointY % 32 < 16 && pointX % 32 >= 16)
                            attribute = (byte)((attribute >> 2) & 0x03);
                        else if (pointY % 32 >= 16 && pointX % 32 < 16)
                            attribute = (byte)((attribute >> 4) & 0x03);
                        else if (pointY % 32 >= 16 && pointX % 32 >= 16)
                            attribute = (byte)((attribute >> 6) & 0x03);

                        byte palColor = this.PalMemory[(attribute * 4) + color];
                        if (monochrome) //If monochrome bit set, have NOT testing in game.
                            palColor &= 0xF0;
                        if(this.displayBG)
                            scanline[column] = palColor;
                        else
                            scanline[column] = this.PalMemory[0x00];

                    }

                    if (column % 8 == 0)
                        HorizontalIncrement();
                }
                VerticalIncrement();
                HorizontalReset();
                HorizontalIncrement();
                HorizontalIncrement();
            }
            else //If background rendering disabled still show background color
            {
                byte palColor = this.PalMemory[0x00];
                if (monochrome)
                    palColor &= 0xF0;
                for (int column = 0; column < 256; column++)
                {
                    scanline[column] = palColor;
                    zeroBackground[column] = true;
                }
            }
            if ((PPUMASK & 0x10) != 0) //If sprite rendering is enabled
            {
                byte spritesOnLine = 0;
                bool squareSprites = false;
                ushort spriteTable = 0;
                bool spriteClipping = (PPUMASK & 0x4) == 0;
                if ((PPUCTRL & 0x20) == 0)
                {
                    squareSprites = true;
                    if ((PPUCTRL & 0x08) != 0)
                        spriteTable = 0x1000;
                }
                for (int sprite = 63; sprite >= 0; sprite--)
                {
                    byte yPos = (byte)(this.SPRMemory[sprite * 4] + 1);
                    byte tileNum = this.SPRMemory[(sprite * 4) + 1];
                    byte attr = this.SPRMemory[(sprite * 4) + 2];
                    byte xPos = this.SPRMemory[(sprite * 4) + 3];
                    byte palette = (byte)(attr & 0x03);
                    if (squareSprites)//8x8 Sprites
                    {
                        if (yPos <= line && yPos + 8 > line) //If sprite is on this line
                        {
                            spritesOnLine++;
                            if ((spritesOnLine < 9) || !displaySpriteLimit)
                            {
                                for (byte spritePixel = 0; spritePixel < 8; spritePixel++)
                                {
                                    if (spritePixel + xPos < 256)
                                    {
                                        int vertFlip = 0;
                                        if ((attr & 0x80) != 0)
                                            vertFlip = this.flip[(line - yPos) % 8];
                                        byte color = GetTilePixel(spriteTable, tileNum, spritePixel, (byte)(line - yPos + vertFlip), (attr & 0x40) != 0);
                                        if (color != 0 && !(spriteClipping && spritePixel + xPos < 8))
                                        {
                                            byte palColor = this.PalMemory[0x10 + (palette * 4) + color];
                                            if (monochrome) //If monochrome bit set, have NOT testing in game.
                                                palColor &= 0xF0;
                                            spriteLine[spritePixel + xPos] = palColor;
                                            if ((attr & 0x20) == 0) //If priority is above background
                                            {
                                                spriteAboveLine[spritePixel + xPos] = true;
                                                spriteBelowLine[spritePixel + xPos] = false;
                                            }
                                            else
                                            {
                                                spriteBelowLine[spritePixel + xPos] = true;
                                                spriteAboveLine[spritePixel + xPos] = false;
                                            }
                                            if (sprite == 0 && ((PPUMASK & 0x08) != 0) && !zeroBackground[spritePixel + xPos])
                                                this.spriteZeroHit = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else//8x16 Sprites
                    {
                        if (yPos <= line && yPos + 16 > line) //If sprite is on this line
                        {
                            spritesOnLine++;
                            if ((tileNum & 0x01) != 0)
                                spriteTable = 0x1000;
                            else
                                spriteTable = 0x0;
                            tileNum &= 0xFE;
                            if ((spritesOnLine < 9) || !displaySpriteLimit)
                            {
                                for (byte spritePixel = 0; spritePixel < 8; spritePixel++)
                                {
                                    if (spritePixel + xPos < 256)
                                    {
                                        int vertFlip = 0;
                                        if ((attr & 0x80) != 0)
                                            vertFlip = this.flip[(line - yPos) % 8];
                                        byte color;
                                        if ((attr & 0x80) != 0) //If vertical flipped
                                            if (line - yPos < 8)
                                                color = GetTilePixel(spriteTable, (byte)(tileNum + 1), spritePixel, (byte)(((line - yPos) % 8) + vertFlip), (attr & 0x40) != 0); //Need mod 8 here because tile can be 16 px tall
                                            else
                                                color = GetTilePixel(spriteTable, tileNum, spritePixel, (byte)(((line - yPos) % 8) + vertFlip), (attr & 0x40) != 0);
                                        else
                                            if (line - yPos < 8)
                                                color = GetTilePixel(spriteTable, tileNum, spritePixel, (byte)(((line - yPos) % 8)), (attr & 0x40) != 0);
                                            else
                                                color = GetTilePixel(spriteTable, (byte)(tileNum + 1), spritePixel, (byte)(((line - yPos) % 8)), (attr & 0x40) != 0);
                                        if (color != 0 && !(spriteClipping && spritePixel + xPos < 8))
                                        {
                                            byte palColor = this.PalMemory[0x10 + (palette * 4) + color];
                                            if (monochrome) //If monochrome bit set, have NOT testing in game.
                                                palColor &= 0xF0;
                                            spriteLine[spritePixel + xPos] = palColor;
                                            if ((attr & 0x20) == 0) //If priority is above background
                                            {
                                                spriteAboveLine[spritePixel + xPos] = true;
                                                spriteBelowLine[spritePixel + xPos] = false;
                                            }
                                            else
                                            {
                                                spriteBelowLine[spritePixel + xPos] = true;
                                                spriteAboveLine[spritePixel + xPos] = false;
                                            }
                                            if (sprite == 0 && ((PPUMASK & 0x08) != 0) && !zeroBackground[spritePixel + xPos])
                                                this.spriteZeroHit = true;
                                        }
                                    }
                                }
                            }
                        }
                        if (spritesOnLine >= 9)
                        {
                            this.spriteOverflow = true;
                        }
                    }
                }
                if (this.displaySprites)
                {
                    if(this.displayBG)
                    {
                        for (int column = 0; column < 256; column++)
                        {
                            if (spriteAboveLine[column])
                                scanline[column] = spriteLine[column];
                            else if (zeroBackground[column] && spriteBelowLine[column])
                                scanline[column] = spriteLine[column];
                        }
                    }
                    else
                    {
                        for (int column = 0; column < 256; column++)
                            if (spriteAboveLine[column] || spriteBelowLine[column])
                                scanline[column] = spriteLine[column];
                    }
                }
            }
            return scanline;
        }
        private byte[][,] GenerateNameTables()
        {
            byte PPUCTRL = this.Memory[0x2000];
            byte PPUMASK = this.Memory[0x2001];
            byte[][,] nameTables = new byte[4][,];
            for(int nameTable = 0; nameTable < 4; nameTable++)
            {
                nameTables[nameTable] = new byte[256, 240];
                for(int line = 0; line < 240; line++)
                {
                    ushort backgroundTable = 0;
                    if ((PPUCTRL & 0x10) != 0)
                        backgroundTable = 0x1000;
                    for (int column = 0; column < 256; column++)//For each pixel in scanline
                    {
                        ushort nameTableOffset = (ushort)((nameTable*0x400) + 0x2000);
                        byte tileNumber = this.PPUMemory[this.PPUMirrorMap[nameTableOffset + ((line / 8) * 32) + (column / 8)]]; //These 3 lines are BONKERS and I highly doubt they will work
                        byte color = GetTilePixel(backgroundTable, tileNumber, (byte)(column % 8), (byte)(line % 8), false);
                        if (color == 0)
                        {
                            byte palColor = this.PalMemory[0x00];
                            if ((PPUMASK & 0x01) != 0) //If monochrome bit set, have NOT testing in game.
                                palColor &= 0xF0;
                            nameTables[nameTable][column, line] = palColor;
                        }
                        else
                        {
                            byte attribute = this.PPUMemory[this.PPUMirrorMap[(nameTableOffset + 0x3C0) + ((line / 32) * 8) + (column / 32)]];
                            if (line % 32 < 16 && column % 32 < 16)
                                attribute &= 0x03;
                            else if (line % 32 < 16 && column % 32 >= 16)
                                attribute = (byte)((attribute >> 2) & 0x03);
                            else if (line % 32 >= 16 && column % 32 < 16)
                                attribute = (byte)((attribute >> 4) & 0x03);
                            else if (line % 32 >= 16 && column % 32 >= 16)
                                attribute = (byte)((attribute >> 6) & 0x03);

                            byte palColor = this.PalMemory[(attribute * 4) + color];
                            if ((PPUMASK & 0x01) != 0) //If monochrome bit set, have NOT testing in game.
                                palColor &= 0xF0;
                            nameTables[nameTable][column, line] = palColor;
                        }
                    }
                }
            }
            return nameTables;
        }
        private byte[][] GeneratePatternTablePalette()
        {
            byte[][] pal = new byte[8][];
            for (int palette = 0; palette < 8; palette++)
            {
                pal[palette] = new byte[4];
                pal[palette][0] = this.PalMemory[0x00];
                pal[palette][1] = this.PalMemory[(palette * 4) + 1];
                pal[palette][2] = this.PalMemory[(palette * 4) + 2];
                pal[palette][3] = this.PalMemory[(palette * 4) + 3];
            }
            return pal;
        }
        private byte[][,] GeneratePatternTables()
        {
            byte PPUCTRL = this.Memory[0x2000];
            byte PPUMASK = this.Memory[0x2001];
            byte[][,] patternTables = new byte[2][,];
            for (int patternTable = 0; patternTable < 2; patternTable++)
            {
                patternTables[patternTable] = new byte[128, 128];
                for (int line = 0; line < 128; line++)
                {
                    ushort backgroundTable = (ushort)(patternTable * 0x1000);
                    for (int column = 0; column < 128; column++)//For each pixel in scanline
                    {
                        byte tileNumber = (byte)(((line/8) * 16) + (column/8));
                        patternTables[patternTable][column, line] = GetTilePixel(backgroundTable, tileNumber, (byte)(column % 8), (byte)(line % 8), false);
                    }
                }
            }
            return patternTables;
        }
        private byte GetTilePixel(ushort table, byte tile, byte pixelX, byte pixelY, bool horzFlip)
        {
            byte tileColor1;
            byte tileColor2;
            if (horzFlip)
            {
                tileColor1 = (byte)((this.PPUMemory[this.PPUMirrorMap[(table + (tile * 16)) + pixelY]] >> pixelX) & 0x01);
                tileColor2 = (byte)((this.PPUMemory[this.PPUMirrorMap[(table + (tile * 16) + 8) + pixelY]] >> pixelX) & 0x01);
            }
            else
            {
                tileColor1 = (byte)((this.PPUMemory[this.PPUMirrorMap[(table + (tile * 16)) + pixelY]] << pixelX) & 0x80);
                tileColor2 = (byte)((this.PPUMemory[this.PPUMirrorMap[(table + (tile * 16) + 8) + pixelY]] << pixelX) & 0x80);
            }
            if (tileColor1 != 0)
                tileColor1 = 1;
            if (tileColor2 != 0)
                tileColor2 = 1;
            return (byte)(tileColor1 + (tileColor2 * 2));

        }
        public SaveState getState()
        {
            SaveState newState = new SaveState();
            newState.stateProgramCounter = this.programCounter;
            newState.stateMemory = (byte[][])Memory.StoreBanks().Clone();
            newState.stateMemBanks = (bool[])Memory.saveBanks.Clone();
            newState.stateMemMap = (int[])Memory.memMap.Clone();
            newState.statePPUMemory = (byte[][])PPUMemory.StoreBanks().Clone();
            newState.statePPUBanks = (bool[])PPUMemory.saveBanks.Clone();
            newState.statePPUMap = (int[])PPUMemory.memMap.Clone();
            newState.statePalMemory = (byte[])this.PalMemory.Clone();
            newState.stateA = this.A;
            newState.stateX = this.X;
            newState.stateY = this.Y;
            newState.stateS = this.S;
            newState.stateP = this.P;
            newState.stateCounter = this.counter;
            newState.stateSlCounter = this.slCounter;
            newState.stateScanline = this.scanline;
            newState.stateVblank = this.vblank;
            newState.stateSPRMemory = (byte[])this.SPRMemory.Clone();
            newState.stateInterruptReset = this.interruptReset;
            newState.stateInterruptIRQ = this.interruptIRQ;
            newState.stateInterruptNMI = this.interruptNMI;
            newState.stateInterruptMapper = romMapper.interruptMapper;
            newState.vertOffset = this.vertOffset;
            newState.horzOffset = this.horzOffset;
            newState.nameTableOffset = this.nameTableOffset;
            newState.loopyT = this.loopyT;
            newState.loopyV = this.loopyV;
            newState.loopyX = this.loopyX;
            newState.player1Read = this.player1Read;
            newState.player2Read = this.player2Read;
            newState.PPUAddrFlip = this.PPUAddrFlip;
            newState.readBuffer = this.readBuffer;
            newState.spriteZeroHit = this.spriteZeroHit;
            newState.spriteOverflow = this.spriteOverflow;
            newState.isStored = true;
            return newState;
        }
        public void loadState(SaveState oldState)
        {
            this.programCounter = oldState.stateProgramCounter;
            this.Memory.LoadBanks((bool[])oldState.stateMemBanks.Clone(), (byte[][])oldState.stateMemory.Clone());
            this.Memory.memMap = (int[])oldState.stateMemMap.Clone();
            this.PPUMemory.LoadBanks((bool[])oldState.statePPUBanks.Clone(), (byte[][])oldState.statePPUMemory.Clone());
            this.PPUMemory.memMap = (int[])oldState.statePPUMap.Clone();
            this.PalMemory = (byte[])oldState.statePalMemory.Clone();
            this.A = oldState.stateA;
            this.X = oldState.stateX;
            this.Y = oldState.stateY;
            this.S = oldState.stateS;
            this.P = oldState.stateP;
            this.counter = oldState.stateCounter;
            this.slCounter = oldState.stateSlCounter;
            this.scanline = oldState.stateScanline;
            this.vblank = oldState.stateVblank;
            this.SPRMemory = (byte[])oldState.stateSPRMemory.Clone();
            this.interruptReset = oldState.stateInterruptReset;
            this.interruptIRQ = oldState.stateInterruptIRQ;
            this.interruptNMI = oldState.stateInterruptNMI;
            romMapper.interruptMapper = oldState.stateInterruptMapper;
            this.vertOffset = oldState.vertOffset;
            this.horzOffset = oldState.horzOffset;
            this.nameTableOffset = oldState.nameTableOffset;
            this.loopyT = oldState.loopyT;
            this.loopyV = oldState.loopyV;
            this.loopyX = oldState.loopyX;
            this.player1Read = oldState.player1Read;
            this.player2Read = oldState.player2Read;
            this.PPUAddrFlip = oldState.PPUAddrFlip;
            this.readBuffer = oldState.readBuffer;
            this.spriteZeroHit = oldState.spriteZeroHit;
            this.spriteOverflow = oldState.spriteOverflow;
        }
        public void restart()
        {
            this.interruptReset = true;
        }
        private void ApplyGameGenie()
        {
            for (int i = 0; i < this.gameGenieCodeNum; i++)
            {
                if (this.gameGenieCodes[i].code.Length == 6)
                    this.Memory.ForceValue(this.MirrorMap[this.gameGenieCodes[i].address + 0x8000],this.gameGenieCodes[i].value);
                else if (this.gameGenieCodes[i].code.Length == 8 && this.Memory[this.MirrorMap[this.gameGenieCodes[i].address] + 0x8000] == this.gameGenieCodes[i].check)
                    this.Memory.ForceValue(this.MirrorMap[this.gameGenieCodes[i].address + 0x8000], this.gameGenieCodes[i].value);
            }
        }
        public byte[] GetSRAM()
        {
            byte[] sram = new byte[0x2000];
            for (int i = 0x0; i < 0x2000; i++)
                sram[i] = this.Memory[i + 0x6000];
            return sram;
        }
        public void SetSRAM(byte[] sram)
        {
            for (int i = 0x0; i < 0x2000; i++)
                this.Memory[i + 0x6000] = sram[i];
        }
        private byte[, ,] adcTable = new byte[0x100, 0x100, 0x2]; //Memory, A, Carry 0 - 1
        private bool[, ,] adcCarry = new bool[0x100, 0x100, 0x2];
        private bool[, ,] adcOverflow = new bool[0x100, 0x100, 0x2];
        public void TableADC()
        {
            for (int a = 0; a < 0x100; a++)
            {
                for (int m = 0; m < 0x100; m++)
                {
                    for (int c = 0; c < 0x2; c++)
                    {
                        int adc = a + m + c;
                        adcTable[a, m, c] = (byte)(adc & 0xFF);
                        adcCarry[a, m, c] = (adc > 0xFF);
                        adcOverflow[a, m, c] = (((a ^ adc) & (m ^ adc) & 0x80) != 0);
                    }
                }
            }
        }
        #region Arrays
        private void loadCycles()
        {
            this.cycles[0xA8] = 2;
            this.cycles[0xAA] = 2;
            this.cycles[0xBA] = 2;
            this.cycles[0x98] = 2;
            this.cycles[0x8A] = 2;
            this.cycles[0x9A] = 2;
            this.cycles[0xA9] = 2;
            this.cycles[0xA5] = 3;
            this.cycles[0xB5] = 4;
            this.cycles[0xAD] = 4;
            this.cycles[0xBD] = 4;//*
            this.cycles[0xB9] = 2;//*
            this.cycles[0xA1] = 6;
            this.cycles[0xB1] = 5;//*
            this.cycles[0xA2] = 2;
            this.cycles[0xA6] = 3;
            this.cycles[0xB6] = 4;
            this.cycles[0xAE] = 4;
            this.cycles[0xBE] = 4;//*
            this.cycles[0xA0] = 2;
            this.cycles[0xA4] = 3;
            this.cycles[0xB4] = 4;
            this.cycles[0xAC] = 4;
            this.cycles[0xBC] = 4;//*
            this.cycles[0x85] = 3;
            this.cycles[0x95] = 4;
            this.cycles[0x8D] = 4;
            this.cycles[0x9D] = 5;
            this.cycles[0x99] = 5;
            this.cycles[0x81] = 6;
            this.cycles[0x91] = 6;
            this.cycles[0x86] = 3;
            this.cycles[0x96] = 4;
            this.cycles[0x8E] = 4;
            this.cycles[0x84] = 3;
            this.cycles[0x94] = 4;
            this.cycles[0x8C] = 4;
            this.cycles[0x48] = 3;
            this.cycles[0x08] = 3;
            this.cycles[0x68] = 4;
            this.cycles[0x28] = 4;
            this.cycles[0x69] = 2;
            this.cycles[0x65] = 3;
            this.cycles[0x75] = 4;
            this.cycles[0x6D] = 4;
            this.cycles[0x7D] = 4;//*
            this.cycles[0x79] = 4;//*
            this.cycles[0x61] = 6;
            this.cycles[0x71] = 5;//*
            this.cycles[0xE9] = 2;
            this.cycles[0xE5] = 3;
            this.cycles[0xF5] = 4;
            this.cycles[0xED] = 4;
            this.cycles[0xFD] = 4;//*
            this.cycles[0xF9] = 4;//*
            this.cycles[0xE1] = 6;
            this.cycles[0xF1] = 5;//*
            this.cycles[0x29] = 2;
            this.cycles[0x25] = 3;
            this.cycles[0x35] = 4;
            this.cycles[0x2D] = 4;
            this.cycles[0x3D] = 4;//*
            this.cycles[0x39] = 4;//*
            this.cycles[0x21] = 6;
            this.cycles[0x31] = 5;//*
            this.cycles[0x49] = 2;
            this.cycles[0x45] = 3;
            this.cycles[0x55] = 4;
            this.cycles[0x4D] = 4;
            this.cycles[0x5D] = 4;//*
            this.cycles[0x59] = 4;//*
            this.cycles[0x41] = 6;
            this.cycles[0x51] = 5;//*
            this.cycles[0x09] = 2;
            this.cycles[0x05] = 3;
            this.cycles[0x15] = 4;
            this.cycles[0x0D] = 4;
            this.cycles[0x1D] = 4;//*
            this.cycles[0x19] = 4;//*
            this.cycles[0x01] = 6;
            this.cycles[0x11] = 5;//*
            this.cycles[0xC9] = 2;
            this.cycles[0xC5] = 3;
            this.cycles[0xD5] = 4;
            this.cycles[0xCD] = 4;
            this.cycles[0xDD] = 4;//*
            this.cycles[0xD9] = 4;//*
            this.cycles[0xC1] = 6;
            this.cycles[0xD1] = 5;//*
            this.cycles[0xE0] = 2;
            this.cycles[0xE4] = 3;
            this.cycles[0xEC] = 4;
            this.cycles[0xC0] = 2;
            this.cycles[0xC4] = 3;
            this.cycles[0xCC] = 4;
            this.cycles[0x24] = 3;
            this.cycles[0x2C] = 4;
            this.cycles[0xE6] = 5;
            this.cycles[0xF6] = 6;
            this.cycles[0xEE] = 6;
            this.cycles[0xFE] = 7;
            this.cycles[0xE8] = 2;
            this.cycles[0xC8] = 2;
            this.cycles[0xC6] = 5;
            this.cycles[0xD6] = 6;
            this.cycles[0xCE] = 6;
            this.cycles[0xDE] = 7;
            this.cycles[0xCA] = 2;
            this.cycles[0x88] = 2;
            this.cycles[0x0A] = 2;
            this.cycles[0x06] = 5;
            this.cycles[0x16] = 6;
            this.cycles[0x0E] = 6;
            this.cycles[0x1E] = 7;
            this.cycles[0x4A] = 2;
            this.cycles[0x46] = 5;
            this.cycles[0x56] = 6;
            this.cycles[0x4E] = 6;
            this.cycles[0x5E] = 7;
            this.cycles[0x2A] = 2;
            this.cycles[0x26] = 5;
            this.cycles[0x36] = 6;
            this.cycles[0x2E] = 6;
            this.cycles[0x3E] = 7;
            this.cycles[0x6A] = 2;
            this.cycles[0x66] = 5;
            this.cycles[0x76] = 6;
            this.cycles[0x6E] = 6;
            this.cycles[0x7E] = 7;
            this.cycles[0x4C] = 3;
            this.cycles[0x6C] = 5;
            this.cycles[0x20] = 6;
            this.cycles[0x40] = 6;
            this.cycles[0x60] = 6;
            this.cycles[0x10] = 2;//**
            this.cycles[0x30] = 2;//**
            this.cycles[0x50] = 2;//**
            this.cycles[0x70] = 2;//**
            this.cycles[0x90] = 2;//**
            this.cycles[0xB0] = 2;//**
            this.cycles[0xD0] = 2;//**
            this.cycles[0xF0] = 2;//**
            this.cycles[0x00] = 7;
            this.cycles[0x18] = 2;
            this.cycles[0x58] = 2;
            this.cycles[0xD8] = 2;
            this.cycles[0xB8] = 2;
            this.cycles[0x38] = 2;
            this.cycles[0x78] = 2;
            this.cycles[0xF8] = 2;
            this.cycles[0xEA] = 2;
            this.cycles[0x87] = 3;
            this.cycles[0x97] = 4;
            this.cycles[0x8F] = 4;
            this.cycles[0x83] = 6;
            this.cycles[0xA7] = 3;
            this.cycles[0xB7] = 4;
            this.cycles[0xAF] = 4;
            this.cycles[0xBF] = 4;//*
            this.cycles[0xA3] = 6;
            this.cycles[0xB3] = 5;//*
            this.cycles[0x07] = 5;
            this.cycles[0x17] = 6;
            this.cycles[0x03] = 8;
            this.cycles[0x13] = 8;
            this.cycles[0x0F] = 6;
            this.cycles[0x1F] = 7;
            this.cycles[0x1B] = 7;
            this.cycles[0x27] = 5;
            this.cycles[0x37] = 6;
            this.cycles[0x23] = 8;
            this.cycles[0x33] = 8;
            this.cycles[0x2F] = 6;
            this.cycles[0x3F] = 7;
            this.cycles[0x3B] = 7;
            this.cycles[0x47] = 5;
            this.cycles[0x57] = 6;
            this.cycles[0x43] = 8;
            this.cycles[0x53] = 8;
            this.cycles[0x4F] = 6;
            this.cycles[0x5F] = 7;
            this.cycles[0x5B] = 7;
            this.cycles[0x67] = 5;
            this.cycles[0x77] = 6;
            this.cycles[0x63] = 8;
            this.cycles[0x73] = 8;
            this.cycles[0x6F] = 6;
            this.cycles[0x7F] = 7;
            this.cycles[0x7B] = 7;
            this.cycles[0xC7] = 5;
            this.cycles[0xD7] = 6;
            this.cycles[0xC3] = 8;
            this.cycles[0xD3] = 8;
            this.cycles[0xCF] = 6;
            this.cycles[0xDF] = 7;
            this.cycles[0xDB] = 7;
            this.cycles[0xE7] = 5;
            this.cycles[0xF7] = 6;
            this.cycles[0xE3] = 8;
            this.cycles[0xF3] = 8;
            this.cycles[0xEF] = 6;
            this.cycles[0xFF] = 7;
            this.cycles[0xFB] = 7;
            this.cycles[0x0B] = 2;
            this.cycles[0x2B] = 2;
            this.cycles[0x4B] = 2;
            this.cycles[0x6B] = 2;
            this.cycles[0x8B] = 2;
            this.cycles[0xAB] = 2;
            this.cycles[0xCB] = 2;
            this.cycles[0xEB] = 2;
            this.cycles[0x93] = 6;
            this.cycles[0x9F] = 5;
            this.cycles[0x9C] = 5;
            this.cycles[0x9E] = 5;
            this.cycles[0x9B] = 5;
            this.cycles[0xBB] = 4;//*
            this.cycles[0x1A] = 2;
            this.cycles[0x3A] = 2;
            this.cycles[0x5A] = 2;
            this.cycles[0x7A] = 2;
            this.cycles[0xDA] = 2;
            this.cycles[0xFA] = 2;
            this.cycles[0x80] = 2;
            this.cycles[0x82] = 2;
            this.cycles[0x89] = 2;
            this.cycles[0xC2] = 2;
            this.cycles[0xE2] = 2;
            this.cycles[0x04] = 3;
            this.cycles[0x44] = 3;
            this.cycles[0x64] = 3;
            this.cycles[0x14] = 4;
            this.cycles[0x34] = 4;
            this.cycles[0x54] = 4;
            this.cycles[0x74] = 4;
            this.cycles[0xD4] = 4;
            this.cycles[0xF4] = 4;
            this.cycles[0x0C] = 4;
            this.cycles[0x1C] = 4;//*
            this.cycles[0x3C] = 4;//*
            this.cycles[0x5C] = 4;//*
            this.cycles[0x7C] = 4;//*
            this.cycles[0xDC] = 4;//*
            this.cycles[0xFC] = 4;//*
            this.cycles[0x02] = 0;
            this.cycles[0x12] = 0;
            this.cycles[0x22] = 0;
            this.cycles[0x32] = 0;
            this.cycles[0x42] = 0;
            this.cycles[0x52] = 0;
            this.cycles[0x62] = 0;
            this.cycles[0x72] = 0;
            this.cycles[0x92] = 0;
            this.cycles[0xB2] = 0;
            this.cycles[0xD2] = 0;
            this.cycles[0xF2] = 0;
        }
        private void loadOpcodes()
        {
            this.opcodes[0xA8] = "TAY";
            this.opcodes[0xAA] = "TAX";
            this.opcodes[0xBA] = "TSX";
            this.opcodes[0x98] = "TYA";
            this.opcodes[0x8A] = "TXA";
            this.opcodes[0x9A] = "TXS";
            this.opcodes[0xA9] = "LDA";
            this.opcodes[0xA5] = "LDA";
            this.opcodes[0xB5] = "LDA";
            this.opcodes[0xAD] = "LDA";
            this.opcodes[0xBD] = "LDA";
            this.opcodes[0xB9] = "LDA";
            this.opcodes[0xA1] = "LDA";
            this.opcodes[0xB1] = "LDA";
            this.opcodes[0xA2] = "LDX";
            this.opcodes[0xA6] = "LDX";
            this.opcodes[0xB6] = "LDX";
            this.opcodes[0xAE] = "LDX";
            this.opcodes[0xBE] = "LDX";
            this.opcodes[0xA0] = "LDY";
            this.opcodes[0xA4] = "LDY";
            this.opcodes[0xB4] = "LDY";
            this.opcodes[0xAC] = "LDY";
            this.opcodes[0xBC] = "LDY";
            this.opcodes[0x85] = "STA";
            this.opcodes[0x95] = "STA";
            this.opcodes[0x8D] = "STA";
            this.opcodes[0x9D] = "STA";
            this.opcodes[0x99] = "STA";
            this.opcodes[0x81] = "STA";
            this.opcodes[0x91] = "STA";
            this.opcodes[0x86] = "STX";
            this.opcodes[0x96] = "STX";
            this.opcodes[0x8E] = "STX";
            this.opcodes[0x84] = "STY";
            this.opcodes[0x94] = "STY";
            this.opcodes[0x8C] = "STY";
            this.opcodes[0x48] = "PHA";
            this.opcodes[0x08] = "PHP";
            this.opcodes[0x68] = "PLA";
            this.opcodes[0x28] = "PLP";
            this.opcodes[0x69] = "ADC";
            this.opcodes[0x65] = "ADC";
            this.opcodes[0x75] = "ADC";
            this.opcodes[0x6D] = "ADC";
            this.opcodes[0x7D] = "ADC";
            this.opcodes[0x79] = "ADC";
            this.opcodes[0x61] = "ADC";
            this.opcodes[0x71] = "ADC";
            this.opcodes[0xE9] = "SBC";
            this.opcodes[0xE5] = "SBC";
            this.opcodes[0xF5] = "SBC";
            this.opcodes[0xED] = "SBC";
            this.opcodes[0xFD] = "SBC";
            this.opcodes[0xF9] = "SBC";
            this.opcodes[0xE1] = "SBC";
            this.opcodes[0xF1] = "SBC";
            this.opcodes[0x29] = "AND";
            this.opcodes[0x25] = "AND";
            this.opcodes[0x35] = "AND";
            this.opcodes[0x2D] = "AND";
            this.opcodes[0x3D] = "AND";
            this.opcodes[0x39] = "AND";
            this.opcodes[0x21] = "AND";
            this.opcodes[0x31] = "AND";
            this.opcodes[0x49] = "EOR";
            this.opcodes[0x45] = "EOR";
            this.opcodes[0x55] = "EOR";
            this.opcodes[0x4D] = "EOR";
            this.opcodes[0x5D] = "EOR";
            this.opcodes[0x59] = "EOR";
            this.opcodes[0x41] = "EOR";
            this.opcodes[0x51] = "EOR";
            this.opcodes[0x09] = "ORA";
            this.opcodes[0x05] = "ORA";
            this.opcodes[0x15] = "ORA";
            this.opcodes[0x0D] = "ORA";
            this.opcodes[0x1D] = "ORA";
            this.opcodes[0x19] = "ORA";
            this.opcodes[0x01] = "ORA";
            this.opcodes[0x11] = "ORA";
            this.opcodes[0xC9] = "CMP";
            this.opcodes[0xC5] = "CMP";
            this.opcodes[0xD5] = "CMP";
            this.opcodes[0xCD] = "CMP";
            this.opcodes[0xDD] = "CMP";
            this.opcodes[0xD9] = "CMP";
            this.opcodes[0xC1] = "CMP";
            this.opcodes[0xD1] = "CMP";
            this.opcodes[0xE0] = "CPX";
            this.opcodes[0xE4] = "CPX";
            this.opcodes[0xEC] = "CPX";
            this.opcodes[0xC0] = "CPY";
            this.opcodes[0xC4] = "CPY";
            this.opcodes[0xCC] = "CPY";
            this.opcodes[0x24] = "BIT";
            this.opcodes[0x2C] = "BIT";
            this.opcodes[0xE6] = "INC";
            this.opcodes[0xF6] = "INC";
            this.opcodes[0xEE] = "INC";
            this.opcodes[0xFE] = "INC";
            this.opcodes[0xE8] = "INX";
            this.opcodes[0xC8] = "INY";
            this.opcodes[0xC6] = "DEC";
            this.opcodes[0xD6] = "DEC";
            this.opcodes[0xCE] = "DEC";
            this.opcodes[0xDE] = "DEC";
            this.opcodes[0xCA] = "DEX";
            this.opcodes[0x88] = "DEY";
            this.opcodes[0x0A] = "ASL";
            this.opcodes[0x06] = "ASL";
            this.opcodes[0x16] = "ASL";
            this.opcodes[0x0E] = "ASL";
            this.opcodes[0x1E] = "ASL";
            this.opcodes[0x4A] = "LSR";
            this.opcodes[0x46] = "LSR";
            this.opcodes[0x56] = "LSR";
            this.opcodes[0x4E] = "LSR";
            this.opcodes[0x5E] = "LSR";
            this.opcodes[0x2A] = "ROL";
            this.opcodes[0x26] = "ROL";
            this.opcodes[0x36] = "ROL";
            this.opcodes[0x2E] = "ROL";
            this.opcodes[0x3E] = "ROL";
            this.opcodes[0x6A] = "ROR";
            this.opcodes[0x66] = "ROR";
            this.opcodes[0x76] = "ROR";
            this.opcodes[0x6E] = "ROR";
            this.opcodes[0x7E] = "ROR";
            this.opcodes[0x4C] = "JMP";
            this.opcodes[0x6C] = "JMP";
            this.opcodes[0x20] = "JSR";
            this.opcodes[0x40] = "RTI";
            this.opcodes[0x60] = "RTS";
            this.opcodes[0x10] = "BPL";
            this.opcodes[0x30] = "BMI";
            this.opcodes[0x50] = "BVC";
            this.opcodes[0x70] = "BVS";
            this.opcodes[0x90] = "BCC";
            this.opcodes[0xB0] = "BCS";
            this.opcodes[0xD0] = "BNE";
            this.opcodes[0xF0] = "BEQ";
            this.opcodes[0x00] = "BRK";
            this.opcodes[0x18] = "CLC";
            this.opcodes[0x58] = "CLI";
            this.opcodes[0xD8] = "CLD";
            this.opcodes[0xB8] = "CLV";
            this.opcodes[0x38] = "SEC";
            this.opcodes[0x78] = "SEI";
            this.opcodes[0xF8] = "SED";
            this.opcodes[0xEA] = "NOP";
            this.opcodes[0x87] = "SAX";
            this.opcodes[0x97] = "SAX";
            this.opcodes[0x8F] = "SAX";
            this.opcodes[0x83] = "SAX";
            this.opcodes[0xA7] = "LAX";
            this.opcodes[0xB7] = "LAX";
            this.opcodes[0xAF] = "LAX";
            this.opcodes[0xBF] = "LAX";
            this.opcodes[0xA3] = "LAX";
            this.opcodes[0xB3] = "LAX";
            this.opcodes[0x07] = "SLO";
            this.opcodes[0x17] = "SLO";
            this.opcodes[0x03] = "SLO";
            this.opcodes[0x13] = "SLO";
            this.opcodes[0x0F] = "SLO";
            this.opcodes[0x1F] = "SLO";
            this.opcodes[0x1B] = "SLO";
            this.opcodes[0x27] = "RLA";
            this.opcodes[0x37] = "RLA";
            this.opcodes[0x23] = "RLA";
            this.opcodes[0x33] = "RLA";
            this.opcodes[0x2F] = "RLA";
            this.opcodes[0x3F] = "RLA";
            this.opcodes[0x3B] = "RLA";
            this.opcodes[0x47] = "SRE";
            this.opcodes[0x57] = "SRE";
            this.opcodes[0x43] = "SRE";
            this.opcodes[0x53] = "SRE";
            this.opcodes[0x4F] = "SRE";
            this.opcodes[0x5F] = "SRE";
            this.opcodes[0x5B] = "SRE";
            this.opcodes[0x67] = "RRA";
            this.opcodes[0x77] = "RRA";
            this.opcodes[0x63] = "RRA";
            this.opcodes[0x73] = "RRA";
            this.opcodes[0x6F] = "RRA";
            this.opcodes[0x7F] = "RRA";
            this.opcodes[0x7B] = "RRA";
            this.opcodes[0xC7] = "DCP";
            this.opcodes[0xD7] = "DCP";
            this.opcodes[0xC3] = "DCP";
            this.opcodes[0xD3] = "DCP";
            this.opcodes[0xCF] = "DCP";
            this.opcodes[0xDF] = "DCP";
            this.opcodes[0xDB] = "DCP";
            this.opcodes[0xE7] = "ISC";
            this.opcodes[0xF7] = "ISC";
            this.opcodes[0xE3] = "ISC";
            this.opcodes[0xF3] = "ISC";
            this.opcodes[0xEF] = "ISC";
            this.opcodes[0xFF] = "ISC";
            this.opcodes[0xFB] = "ISC";
            this.opcodes[0x0B] = "ANC";
            this.opcodes[0x2B] = "ANC";
            this.opcodes[0x4B] = "ALR";
            this.opcodes[0x6B] = "ARR";
            this.opcodes[0x8B] = "XAA";
            this.opcodes[0xAB] = "LAX";
            this.opcodes[0xCB] = "AXS";
            this.opcodes[0xEB] = "SBC";
            this.opcodes[0x93] = "AHX";
            this.opcodes[0x9F] = "AHX";
            this.opcodes[0x9C] = "SHY";
            this.opcodes[0x9E] = "SHX";
            this.opcodes[0x9B] = "TAS";
            this.opcodes[0xBB] = "LAS";
            this.opcodes[0x1A] = "NOP";
            this.opcodes[0x3A] = "NOP";
            this.opcodes[0x5A] = "NOP";
            this.opcodes[0x7A] = "NOP";
            this.opcodes[0xDA] = "NOP";
            this.opcodes[0xFA] = "NOP";
            this.opcodes[0x80] = "NOP";
            this.opcodes[0x82] = "NOP";
            this.opcodes[0x89] = "NOP";
            this.opcodes[0xC2] = "NOP";
            this.opcodes[0xE2] = "NOP";
            this.opcodes[0x04] = "NOP";
            this.opcodes[0x44] = "NOP";
            this.opcodes[0x64] = "NOP";
            this.opcodes[0x14] = "NOP";
            this.opcodes[0x34] = "NOP";
            this.opcodes[0x54] = "NOP";
            this.opcodes[0x74] = "NOP";
            this.opcodes[0xD4] = "NOP";
            this.opcodes[0xF4] = "NOP";
            this.opcodes[0x0C] = "NOP";
            this.opcodes[0x1C] = "NOP";
            this.opcodes[0x3C] = "NOP";
            this.opcodes[0x5C] = "NOP";
            this.opcodes[0x7C] = "NOP";
            this.opcodes[0xDC] = "NOP";
            this.opcodes[0xFC] = "NOP";
            this.opcodes[0x02] = "KIL";
            this.opcodes[0x12] = "KIL";
            this.opcodes[0x22] = "KIL";
            this.opcodes[0x32] = "KIL";
            this.opcodes[0x42] = "KIL";
            this.opcodes[0x52] = "KIL";
            this.opcodes[0x62] = "KIL";
            this.opcodes[0x72] = "KIL";
            this.opcodes[0x92] = "KIL";
            this.opcodes[0xB2] = "KIL";
            this.opcodes[0xD2] = "KIL";
            this.opcodes[0xF2] = "KIL";
        }
        private void loadAddressingTypes()
        {
            this.addressingTypes[0xA8] = 0;
            this.addressingTypes[0xAA] = 0;
            this.addressingTypes[0xBA] = 0;
            this.addressingTypes[0x98] = 0;
            this.addressingTypes[0x8A] = 0;
            this.addressingTypes[0x9A] = 0;
            this.addressingTypes[0xA9] = 1;
            this.addressingTypes[0xA5] = 3;
            this.addressingTypes[0xB5] = 4;
            this.addressingTypes[0xAD] = 6;
            this.addressingTypes[0xBD] = 7;
            this.addressingTypes[0xB9] = 8;
            this.addressingTypes[0xA1] = 9;
            this.addressingTypes[0xB1] = 10;
            this.addressingTypes[0xA2] = 1;
            this.addressingTypes[0xA6] = 3;
            this.addressingTypes[0xB6] = 5;
            this.addressingTypes[0xAE] = 6;
            this.addressingTypes[0xBE] = 8;
            this.addressingTypes[0xA0] = 1;
            this.addressingTypes[0xA4] = 3;
            this.addressingTypes[0xB4] = 4;
            this.addressingTypes[0xAC] = 6;
            this.addressingTypes[0xBC] = 7;
            this.addressingTypes[0x85] = 3;
            this.addressingTypes[0x95] = 4;
            this.addressingTypes[0x8D] = 6;
            this.addressingTypes[0x9D] = 7;
            this.addressingTypes[0x99] = 8;
            this.addressingTypes[0x81] = 9;
            this.addressingTypes[0x91] = 10;
            this.addressingTypes[0x86] = 3;
            this.addressingTypes[0x96] = 5;
            this.addressingTypes[0x8E] = 6;
            this.addressingTypes[0x84] = 3;
            this.addressingTypes[0x94] = 4;
            this.addressingTypes[0x8C] = 6;
            this.addressingTypes[0x48] = 0;
            this.addressingTypes[0x08] = 0;
            this.addressingTypes[0x68] = 0;
            this.addressingTypes[0x28] = 0;
            this.addressingTypes[0x69] = 1;
            this.addressingTypes[0x65] = 3;
            this.addressingTypes[0x75] = 4;
            this.addressingTypes[0x6D] = 6;
            this.addressingTypes[0x7D] = 7;
            this.addressingTypes[0x79] = 8;
            this.addressingTypes[0x61] = 9;
            this.addressingTypes[0x71] = 10;
            this.addressingTypes[0xE9] = 1;
            this.addressingTypes[0xE5] = 3;
            this.addressingTypes[0xF5] = 4;
            this.addressingTypes[0xED] = 6;
            this.addressingTypes[0xFD] = 7;
            this.addressingTypes[0xF9] = 8;
            this.addressingTypes[0xE1] = 9;
            this.addressingTypes[0xF1] = 10;
            this.addressingTypes[0x29] = 1;
            this.addressingTypes[0x25] = 3;
            this.addressingTypes[0x35] = 4;
            this.addressingTypes[0x2D] = 6;
            this.addressingTypes[0x3D] = 7;
            this.addressingTypes[0x39] = 8;
            this.addressingTypes[0x21] = 9;
            this.addressingTypes[0x31] = 10;
            this.addressingTypes[0x49] = 1;
            this.addressingTypes[0x45] = 3;
            this.addressingTypes[0x55] = 4;
            this.addressingTypes[0x4D] = 6;
            this.addressingTypes[0x5D] = 7;
            this.addressingTypes[0x59] = 8;
            this.addressingTypes[0x41] = 9;
            this.addressingTypes[0x51] = 10;
            this.addressingTypes[0x09] = 1;
            this.addressingTypes[0x05] = 3;
            this.addressingTypes[0x15] = 4;
            this.addressingTypes[0x0D] = 6;
            this.addressingTypes[0x1D] = 7;
            this.addressingTypes[0x19] = 8;
            this.addressingTypes[0x01] = 9;
            this.addressingTypes[0x11] = 10;
            this.addressingTypes[0xC9] = 1;
            this.addressingTypes[0xC5] = 3;
            this.addressingTypes[0xD5] = 4;
            this.addressingTypes[0xCD] = 6;
            this.addressingTypes[0xDD] = 7;
            this.addressingTypes[0xD9] = 8;
            this.addressingTypes[0xC1] = 9;
            this.addressingTypes[0xD1] = 10;
            this.addressingTypes[0xE0] = 1;
            this.addressingTypes[0xE4] = 3;
            this.addressingTypes[0xEC] = 6;
            this.addressingTypes[0xC0] = 1;
            this.addressingTypes[0xC4] = 3;
            this.addressingTypes[0xCC] = 6;
            this.addressingTypes[0x24] = 3;
            this.addressingTypes[0x2C] = 6;
            this.addressingTypes[0xE6] = 3;
            this.addressingTypes[0xF6] = 4;
            this.addressingTypes[0xEE] = 6;
            this.addressingTypes[0xFE] = 7;
            this.addressingTypes[0xE8] = 0;
            this.addressingTypes[0xC8] = 0;
            this.addressingTypes[0xC6] = 3;
            this.addressingTypes[0xD6] = 4;
            this.addressingTypes[0xCE] = 6;
            this.addressingTypes[0xDE] = 7;
            this.addressingTypes[0xCA] = 0;
            this.addressingTypes[0x88] = 0;
            this.addressingTypes[0x0A] = 2;
            this.addressingTypes[0x06] = 3;
            this.addressingTypes[0x16] = 4;
            this.addressingTypes[0x0E] = 6;
            this.addressingTypes[0x1E] = 7;
            this.addressingTypes[0x4A] = 2;
            this.addressingTypes[0x46] = 3;
            this.addressingTypes[0x56] = 4;
            this.addressingTypes[0x4E] = 6;
            this.addressingTypes[0x5E] = 7;
            this.addressingTypes[0x2A] = 2;
            this.addressingTypes[0x26] = 3;
            this.addressingTypes[0x36] = 4;
            this.addressingTypes[0x2E] = 6;
            this.addressingTypes[0x3E] = 7;
            this.addressingTypes[0x6A] = 2;
            this.addressingTypes[0x66] = 3;
            this.addressingTypes[0x76] = 4;
            this.addressingTypes[0x6E] = 6;
            this.addressingTypes[0x7E] = 7;
            this.addressingTypes[0x4C] = 13;
            this.addressingTypes[0x6C] = 11;
            this.addressingTypes[0x20] = 13;
            this.addressingTypes[0x40] = 0;
            this.addressingTypes[0x60] = 0;
            this.addressingTypes[0x10] = 12;
            this.addressingTypes[0x30] = 12;
            this.addressingTypes[0x50] = 12;
            this.addressingTypes[0x70] = 12;
            this.addressingTypes[0x90] = 12;
            this.addressingTypes[0xB0] = 12;
            this.addressingTypes[0xD0] = 12;
            this.addressingTypes[0xF0] = 12;
            this.addressingTypes[0x00] = 0;
            this.addressingTypes[0x18] = 0;
            this.addressingTypes[0x58] = 0;
            this.addressingTypes[0xD8] = 0;
            this.addressingTypes[0xB8] = 0;
            this.addressingTypes[0x38] = 0;
            this.addressingTypes[0x78] = 0;
            this.addressingTypes[0xF8] = 0;
            this.addressingTypes[0xEA] = 0;
            this.addressingTypes[0x87] = 3;
            this.addressingTypes[0x97] = 5;
            this.addressingTypes[0x8F] = 6;
            this.addressingTypes[0x83] = 9;
            this.addressingTypes[0xA7] = 3;
            this.addressingTypes[0xB7] = 5;
            this.addressingTypes[0xAF] = 6;
            this.addressingTypes[0xBF] = 7;
            this.addressingTypes[0xA3] = 9;
            this.addressingTypes[0xB3] = 10;
            this.addressingTypes[0x07] = 3;
            this.addressingTypes[0x17] = 4;
            this.addressingTypes[0x03] = 9;
            this.addressingTypes[0x13] = 10;
            this.addressingTypes[0x0F] = 6;
            this.addressingTypes[0x1F] = 7;
            this.addressingTypes[0x1B] = 8;
            this.addressingTypes[0x27] = 3;
            this.addressingTypes[0x37] = 4;
            this.addressingTypes[0x23] = 9;
            this.addressingTypes[0x33] = 10;
            this.addressingTypes[0x2F] = 6;
            this.addressingTypes[0x3F] = 7;
            this.addressingTypes[0x3B] = 8;
            this.addressingTypes[0x47] = 3;
            this.addressingTypes[0x57] = 4;
            this.addressingTypes[0x43] = 9;
            this.addressingTypes[0x53] = 10;
            this.addressingTypes[0x4F] = 6;
            this.addressingTypes[0x5F] = 7;
            this.addressingTypes[0x5B] = 8;
            this.addressingTypes[0x67] = 3;
            this.addressingTypes[0x77] = 4;
            this.addressingTypes[0x63] = 9;
            this.addressingTypes[0x73] = 10;
            this.addressingTypes[0x6F] = 6;
            this.addressingTypes[0x7F] = 7;
            this.addressingTypes[0x7B] = 8;
            this.addressingTypes[0xC7] = 3;
            this.addressingTypes[0xD7] = 4;
            this.addressingTypes[0xC3] = 9;
            this.addressingTypes[0xD3] = 10;
            this.addressingTypes[0xCF] = 6;
            this.addressingTypes[0xDF] = 7;
            this.addressingTypes[0xDB] = 8;
            this.addressingTypes[0xE7] = 3;
            this.addressingTypes[0xF7] = 4;
            this.addressingTypes[0xE3] = 9;
            this.addressingTypes[0xF3] = 10;
            this.addressingTypes[0xEF] = 6;
            this.addressingTypes[0xFF] = 7;
            this.addressingTypes[0xFB] = 8;
            this.addressingTypes[0x0B] = 1;
            this.addressingTypes[0x2B] = 1;
            this.addressingTypes[0x4B] = 1;
            this.addressingTypes[0x6B] = 1;
            this.addressingTypes[0x8B] = 1;
            this.addressingTypes[0xAB] = 1;
            this.addressingTypes[0xCB] = 1;
            this.addressingTypes[0xEB] = 1;
            this.addressingTypes[0x93] = 10;
            this.addressingTypes[0x9F] = 8;
            this.addressingTypes[0x9C] = 7;
            this.addressingTypes[0x9E] = 8;
            this.addressingTypes[0x9B] = 8;
            this.addressingTypes[0xBB] = 8;
            this.addressingTypes[0x1A] = 0;
            this.addressingTypes[0x3A] = 0;
            this.addressingTypes[0x5A] = 0;
            this.addressingTypes[0x7A] = 0;
            this.addressingTypes[0xDA] = 0;
            this.addressingTypes[0xFA] = 0;
            this.addressingTypes[0x80] = 1;
            this.addressingTypes[0x82] = 1;
            this.addressingTypes[0x89] = 1;
            this.addressingTypes[0xC2] = 1;
            this.addressingTypes[0xE2] = 1;
            this.addressingTypes[0x04] = 3;
            this.addressingTypes[0x44] = 3;
            this.addressingTypes[0x64] = 3;
            this.addressingTypes[0x14] = 4;
            this.addressingTypes[0x34] = 4;
            this.addressingTypes[0x54] = 4;
            this.addressingTypes[0x74] = 4;
            this.addressingTypes[0xD4] = 4;
            this.addressingTypes[0xF4] = 4;
            this.addressingTypes[0x0C] = 6;
            this.addressingTypes[0x1C] = 7;
            this.addressingTypes[0x3C] = 7;
            this.addressingTypes[0x5C] = 7;
            this.addressingTypes[0x7C] = 7;
            this.addressingTypes[0xDC] = 7;
            this.addressingTypes[0xFC] = 7;
            this.addressingTypes[0x02] = 0;
            this.addressingTypes[0x12] = 0;
            this.addressingTypes[0x22] = 0;
            this.addressingTypes[0x32] = 0;
            this.addressingTypes[0x42] = 0;
            this.addressingTypes[0x52] = 0;
            this.addressingTypes[0x62] = 0;
            this.addressingTypes[0x72] = 0;
            this.addressingTypes[0x92] = 0;
            this.addressingTypes[0xB2] = 0;
            this.addressingTypes[0xD2] = 0;
            this.addressingTypes[0xF2] = 0;
        }
        #endregion
    }
    [Serializable]
    struct SaveState
    {
        public ushort stateProgramCounter;
        public byte[][] stateMemory;
        public bool[] stateMemBanks;
        public int[] stateMemMap;
        public byte[][] statePPUMemory;
        public bool[] statePPUBanks;
        public int[] statePPUMap;
        public byte[] stateSPRMemory;
        public byte[] statePalMemory;
        public byte stateA;
        public byte stateX;
        public byte stateY;
        public byte stateS;
        public byte stateP;
        public int stateCounter;
        public byte stateSlCounter;
        public int stateScanline;
        public int stateVblank;
        public bool stateInterruptReset;
        public bool stateInterruptIRQ;
        public bool stateInterruptNMI;
        public bool stateInterruptMapper;
        public int vertOffset;
        public int horzOffset;
        public int nameTableOffset;
        public ushort loopyV;
        public ushort loopyT;
        public ushort loopyX;
        public int player1Read;
        public int player2Read;
        public bool PPUAddrFlip;
        public byte readBuffer;
        public bool spriteZeroHit;
        public bool spriteOverflow;
        public bool isStored;
    }
}
