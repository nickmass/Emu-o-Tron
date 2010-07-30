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
    public class NESCore
    {

        public MemoryStore Memory;
        public ushort[] MirrorMap = new ushort[0x10000];
        private bool emulationRunning = false;
        private int RegA = 0;
        private int RegX = 0;
        private int RegY = 0;
        private int RegS = 0xFD;
        private int RegPC = 0;
        private int FlagCarry = 0; //Bit 0 of P
        private int FlagZero = 1; //backwards
        private int FlagIRQ = 1;
        private int FlagDecimal = 1;
        private int FlagBreak = 1;
        private int FlagNotUsed= 1;
        private int FlagOverflow = 0;
        private int FlagSign = 0; //Bit 7 of P
        private int counter = 0;

        private int invalidCount = 0;

        private mappers.Mapper romMapper;

        private bool interruptReset = false;
        private bool interruptNMI = false;
        private bool interruptBRK = false;

        private int[] scanlineLengths = { 113, 113, 114 };
        private byte slCounter = 0;
        private int scanline = 241;
        private int vblank = 1;

        private bool PPUAddrFlip = false;

        public MemoryStore PPUMemory;
        public ushort[] PPUMirrorMap = new ushort[0x8000];
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

        public bool spriteZeroHit = false;
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
        private OpInfo OpCodes = new OpInfo();
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
            int op;
            int opInfo;
            int opCycles;
            int opCycleAdd;
            int addressing;
            int instruction;
            int opAddr;
            int addr = 0;
            int temp;
            int value;
            while (this.emulationRunning)
            {
                if (logging)
                {

                    if (logBuilder.Length > 1024 * 1024 * 100)
                        logBuilder.Remove(0, 1024 * 512 * 95);
                    logBuilder.AppendLine(LogOp(RegPC));
                }
                op = Read(RegPC);
                opInfo = OpInfo.GetOps()[op];
                opCycles = (opInfo >> 24) & 0xFF;
                opCycleAdd = 0;
                addressing = (opInfo >> 8) & 0xFF;
                instruction = opInfo & 0xFF;
                opAddr = RegPC;
                RegPC += (opInfo >> 16) & 0xFF;
                RegPC &= 0xFFFF;
                #region CPU
                switch (addressing)
                {
                    case OpInfo.AddrNone:
                        break;
                    case OpInfo.AddrAccumulator:
                        addr = RegA;
                        break;
                    case OpInfo.AddrImmediate:
                        addr = opAddr + 1;
                        break;
                    case OpInfo.AddrZeroPage:
                        addr = Read(opAddr + 1);
                        break;
                    case OpInfo.AddrZeroPageX:
                        addr = (Read(opAddr + 1) + RegX) & 0xFF;
                        break;
                    case OpInfo.AddrZeroPageY:
                        addr = (Read(opAddr + 1) + RegY) & 0xFF;
                        break;
                    case OpInfo.AddrAbsolute:
                        addr = ReadWord(opAddr + 1);
                        break;
                    case OpInfo.AddrAbsoluteX:
                        addr = ReadWord(opAddr + 1);
                        if ((addr & 0xFF00) != ((addr + RegX) & 0xFF00))
                            opCycleAdd++;
                        addr += RegX;
                        break;
                    case OpInfo.AddrAbsoluteY:
                        addr = ReadWord(opAddr + 1);
                        if ((addr & 0xFF00) != ((addr + RegY) & 0xFF00))
                            opCycleAdd++;
                        addr += RegY;
                        break;
                    case OpInfo.AddrIndirectAbs:
                        addr = ReadWordWrap(ReadWord(opAddr + 1));
                        break;
                    case OpInfo.AddrRelative:
                        addr = Read(opAddr + 1);
                        if (addr < 0x80)
                            addr += RegPC;
                        else
                            addr += RegPC - 256;
                        break;
                    case OpInfo.AddrIndirectX:
                        addr = Read(opAddr + 1);
                        //if ((addr & 0xFF00) != ((addr + RegX) & 0xFF00))
                        //    opCycleAdd++;
                        addr += RegX;
                        addr &= 0xFF;
                        addr = ReadWordWrap(addr);
                        break;
                    case OpInfo.AddrIndirectY:
                        addr = ReadWordWrap(Read(opAddr + 1));
                        if ((addr & 0xFF00) != ((addr + RegY) & 0xFF00))
                            opCycleAdd++;
                        addr += RegY;
                        break;
                }
                addr &= 0xFFFF;
                switch (instruction)
                {
                    case OpInfo.InstrADC:
                        value = Read(addr);
                        temp = RegA + value + FlagCarry;
                        FlagOverflow = ((!(((RegA ^ value) & 0x80) != 0) && (((RegA ^ temp) & 0x80)) != 0) ? 1 : 0);
                        FlagCarry = temp > 0xFF ? 1 : 0;
                        RegA = FlagZero = temp & 0xFF;
                        FlagSign = RegA >> 7;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrAND:
                        RegA &= Read(addr);
                        FlagZero = RegA;
                        FlagSign = RegA >> 7;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrASL:
                        if (addressing == OpInfo.AddrAccumulator)
                        {
                            FlagCarry = (RegA >> 7) & 1;
                            RegA = (RegA << 1) & 0xFF;
                            FlagZero = RegA;
                            FlagSign = RegA >> 7;
                        }
                        else
                        {
                            value = Read(addr);
                            FlagCarry = (value >> 7) & 1;
                            value = (value << 1) & 0xFF;
                            FlagZero = value;
                            FlagSign = value >> 7;
                            Write(addr, value);
                        }
                        break;
                    case OpInfo.InstrBCC:
                        if (FlagCarry == 0)
                        {
                            RegPC = addr;
                            opCycles += opCycleAdd + 1;
                        }
                        break;
                    case OpInfo.InstrBCS:
                        if (FlagCarry != 0)
                        {
                            RegPC = addr;
                            opCycles += opCycleAdd + 1;
                        }
                        break;
                    case OpInfo.InstrBEQ:
                        if (FlagZero == 0)
                        {
                            RegPC = addr;
                            opCycles += opCycleAdd + 1;
                        }
                        break;
                    case OpInfo.InstrBIT:
                        value = Read(addr);
                        FlagSign = (value & 0x80) >> 7;
                        FlagOverflow = (value & 0x40) >> 6;
                        FlagZero = value & RegA;
                        break;
                    case OpInfo.InstrBMI:
                        if (FlagSign != 0)
                        {
                            RegPC = addr;
                            opCycles += opCycleAdd + 1;
                        }
                        break;
                    case OpInfo.InstrBNE:
                        if (FlagZero != 0)
                        {
                            RegPC = addr;
                            opCycles += opCycleAdd + 1;
                        }
                        break;
                    case OpInfo.InstrBPL:
                        if (FlagSign == 0)
                        {
                            RegPC = addr;
                            opCycles += opCycleAdd + 1;
                        }
                        break;
                    case OpInfo.InstrBRK:
                        interruptBRK = true;
                        break;
                    case OpInfo.InstrBVC:
                        if (FlagOverflow == 0)
                        {
                            RegPC = addr;
                            opCycles += opCycleAdd + 1;
                        }
                        break;
                    case OpInfo.InstrBVS:
                        if (FlagOverflow != 0)
                        {
                            RegPC = addr;
                            opCycles += opCycleAdd + 1;
                        }
                        break;
                    case OpInfo.InstrCLC:
                        FlagCarry = 0;
                        break;
                    case OpInfo.InstrCLD:
                        FlagDecimal = 0;
                        break;
                    case OpInfo.InstrCLI:
                        FlagIRQ = 0;
                        break;
                    case OpInfo.InstrCLV:
                        FlagOverflow = 0;
                        break;
                    case OpInfo.InstrCMP:
                        value = Read(addr);
                        if (RegA >= value)
                            FlagCarry = 1;
                        else
                            FlagCarry = 0;
                        if (RegA == value)
                            FlagZero = 0;
                        else
                            FlagZero = 1;
                        FlagSign = ((RegA - value) >> 7) & 1;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrCPX:
                        value = Read(addr);
                        if (RegX >= value)
                            FlagCarry = 1;
                        else
                            FlagCarry = 0;
                        if (RegX == value)
                            FlagZero = 0;
                        else
                            FlagZero = 1;
                        FlagSign = ((RegX - value) >> 7) & 1;
                        break;
                    case OpInfo.InstrCPY:
                        value = Read(addr);
                        if (RegY >= value)
                            FlagCarry = 1;
                        else
                            FlagCarry = 0;
                        if (RegY == value)
                            FlagZero = 0;
                        else
                            FlagZero = 1;
                        FlagSign = ((RegY - value) >> 7) & 1;
                        break;
                    case OpInfo.InstrDEC:
                        value = Read(addr);
                        value = FlagZero = (value - 1) & 0xFF;
                        FlagSign = value >> 7;
                        Write(addr, value);
                        break;
                    case OpInfo.InstrDEX:
                        RegX = FlagZero = (RegX - 1) & 0xFF;
                        FlagSign = RegX >> 7;
                        break;
                    case OpInfo.InstrDEY:
                        RegY = FlagZero = (RegY - 1) & 0xFF;
                        FlagSign = RegY >> 7;
                        break;
                    case OpInfo.InstrEOR:
                        RegA ^= Read(addr);
                        RegA = FlagZero = RegA & 0xFF;
                        FlagSign = RegA >> 7;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrINC:
                        value = Read(addr);
                        value = FlagZero = (value + 1) & 0xFF;
                        FlagSign = value >> 7;
                        Write(addr, value);
                        break;
                    case OpInfo.InstrINX:
                        RegX = FlagZero = (RegX + 1) & 0xFF;
                        FlagSign = RegX >> 7;
                        break;
                    case OpInfo.InstrINY:
                        RegY = FlagZero = (RegY + 1) & 0xFF;
                        FlagSign = RegY >> 7;
                        break;
                    case OpInfo.InstrJMP:
                        RegPC = addr;
                        break;
                    case OpInfo.InstrJSR:
                        PushWordStack(RegPC - 1);
                        RegPC = addr;
                        break;
                    case OpInfo.InstrLDA:
                        RegA = FlagZero = Read(addr);
                        FlagSign = RegA >> 7;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrLDX:
                        RegX = FlagZero = Read(addr);
                        FlagSign = RegX >> 7;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrLDY:
                        RegY = FlagZero = Read(addr);
                        FlagSign = RegY >> 7;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrLSR:
                        if (addressing == OpInfo.AddrAccumulator)
                        {
                            FlagCarry = RegA & 1;
                            RegA = RegA >> 1;
                            FlagSign = 0;
                            FlagZero = RegA;
                        }
                        else
                        {
                            value = Read(addr);
                            FlagCarry = value & 1;
                            value = value >> 1;
                            FlagSign = 0;
                            FlagZero = value;
                            Write(addr, value);
                        }
                        break;
                    case OpInfo.InstrNOP:
                        break;
                    case OpInfo.InstrORA:
                        RegA = FlagZero = (RegA | Read(addr)) & 0xFF;
                        FlagSign = RegA >> 7;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrPHA:
                        PushByteStack(RegA);
                        break;
                    case OpInfo.InstrPHP:
                        value = PToByte();
                        value |= 0x30;
                        PushByteStack(value);
                        break;
                    case OpInfo.InstrPLA:
                        RegA = FlagZero = PopByteStack();
                        FlagSign = RegA >> 7;
                        break;
                    case OpInfo.InstrPLP:
                        value = PopByteStack();
                        PFromByte(value);
                        FlagBreak = 1;
                        FlagNotUsed = 1;
                        break;
                    case OpInfo.InstrROL:
                        if (addressing == OpInfo.AddrAccumulator)
                        {
                            if (FlagCarry != 0)
                            {
                                FlagCarry = RegA >> 7;
                                RegA = ((RegA << 1) + 1) & 0xFF;
                            }
                            else
                            {
                                FlagCarry = RegA >> 7;
                                RegA = (RegA << 1) & 0xFF;
                            }
                            FlagSign = RegA >> 7;
                            FlagZero = RegA;
                        }
                        else
                        {
                            value = Read(addr);
                            if (FlagCarry != 0)
                            {
                                FlagCarry = value >> 7;
                                value = ((value << 1) + 1) & 0xFF;
                            }
                            else
                            {
                                FlagCarry = value >> 7;
                                value = (value << 1) & 0xFF;
                            }
                            FlagSign = value >> 7;
                            FlagZero = value;
                            Write(addr, value);
                        }
                        break;
                    case OpInfo.InstrROR:
                        if (addressing == OpInfo.AddrAccumulator)
                        {
                            if (FlagCarry != 0)
                            {
                                FlagCarry = RegA & 1;
                                RegA = ((RegA >> 1) + 0x80) & 0xFF;
                                FlagSign = 1;
                            }
                            else
                            {
                                FlagCarry = RegA & 1;
                                RegA = (RegA >> 1) & 0xFF;
                                FlagSign = 0;
                            }
                            FlagZero = RegA;
                        }
                        else
                        {
                            value = Read(addr);
                            if (FlagCarry != 0)
                            {
                                FlagCarry = value & 1;
                                value = ((value >> 1) + 0x80) & 0xFF;
                                FlagSign = 1;
                            }
                            else
                            {
                                FlagCarry = value & 1;
                                value = (value >> 1) & 0xFF;
                                FlagSign = 0;
                            }
                            FlagZero = value;
                            Write(addr, value);
                        }
                        break;
                    case OpInfo.InstrRTI:
                        value = PopByteStack();
                        PFromByte(value);
                        FlagBreak = 1;
                        FlagNotUsed = 1;
                        RegPC = PopWordStack();
                        break;
                    case OpInfo.InstrRTS:
                        RegPC = (PopWordStack() + 1) & 0xFFFF;
                        break;
                    case OpInfo.InstrSBC:
                        value = Read(addr);
                        temp = RegA - value - (1 - FlagCarry);
                        FlagCarry = (temp < 0 ? 0 : 1);
                        FlagOverflow = ((((RegA ^ temp) & 0x80) != 0 && ((RegA ^ Read(addr)) & 0x80) != 0) ? 1 : 0);
                        RegA = FlagZero = (temp & 0xFF);
                        FlagSign = RegA >> 7;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrSEC:
                        FlagCarry = 1;
                        break;
                    case OpInfo.InstrSED:
                        FlagDecimal = 1;
                        break;
                    case OpInfo.InstrSEI:
                        FlagIRQ = 1;
                        break;
                    case OpInfo.InstrSTA:
                        Write(addr, RegA);
                        break;
                    case OpInfo.InstrSTX:
                        Write(addr, RegX);
                        break;
                    case OpInfo.InstrSTY:
                        Write(addr, RegY);
                        break;
                    case OpInfo.InstrTAX:
                        RegX = FlagZero = RegA;
                        FlagSign = RegX >> 7;
                        break;
                    case OpInfo.InstrTAY:
                        RegY = FlagZero = RegA;
                        FlagSign = RegY >> 7;
                        break;
                    case OpInfo.InstrTSX:
                        RegX = FlagZero = RegS;
                        FlagSign = RegX >> 7;
                        break;
                    case OpInfo.InstrTXA:
                        RegA = FlagZero = RegX;
                        FlagSign = RegA >> 7;
                        break;
                    case OpInfo.InstrTXS:
                        RegS = RegX;
                        break;
                    case OpInfo.InstrTYA:
                        RegA = FlagZero = RegY;
                        FlagSign = RegA >> 7;
                        break;
                    default:
                        romInfo.AppendLine("Illegal OP: " + OpInfo.GetOpNames()[OpInfo.GetOps()[op] & 0xFF] + " " + op.ToString("X2") + " Program Counter: " + RegPC.ToString("X4"));
                        switch (instruction) //Illegal Ops
                        {
                            case OpInfo.IllInstrALR:
                                RegA &= Read(addr);
                                FlagCarry = RegA & 1;
                                RegA >>= 1;
                                FlagSign = RegA >> 7;
                                FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrANC:
                                RegA &= Read(addr);
                                FlagSign = FlagCarry = RegA >> 7;
                                FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrARR:
                                RegA &= Read(addr);
                                if (FlagCarry != 0)
                                {
                                    FlagCarry = RegA & 1;
                                    RegA = ((RegA >> 1) + 0x80) & 0xFF;
                                    FlagSign = 1;
                                }
                                else
                                {
                                    FlagCarry = RegA & 1;
                                    RegA = (RegA >> 1) & 0xFF;
                                    FlagSign = 0;
                                }
                                FlagZero = RegA;
                                if ((RegA & 0x40) != 0)
                                {
                                    if ((RegA & 0x20) != 0)
                                    {
                                        FlagCarry = 1;
                                        FlagOverflow = 0;
                                    }
                                    else
                                    {
                                        FlagCarry = 1;
                                        FlagOverflow = 1;
                                    }
                                }
                                else
                                {
                                    if ((RegA & 0x20) != 0)
                                    {
                                        FlagCarry = 0;
                                        FlagOverflow = 1;
                                    }
                                    else
                                    {
                                        FlagCarry = 0;
                                        FlagOverflow = 0;
                                    }
                                }
                                break;
                            case OpInfo.IllInstrAXS:
                                RegX &= RegA;
                                value = Read(addr);
                                temp = RegX - value;
                                FlagCarry = (temp < 0 ? 0 : 1);
                                RegX = FlagZero = (temp & 0xFF);
                                FlagSign = RegX >> 7;
                                break;
                            case OpInfo.IllInstrDCP:
                                value = Read(addr);
                                value = FlagZero = (value - 1) & 0xFF;
                                FlagSign = value >> 7;
                                Write(addr, value);
                                if (RegA >= value)
                                    FlagCarry = 1;
                                else
                                    FlagCarry = 0;
                                if (RegA == value)
                                    FlagZero = 0;
                                else
                                    FlagZero = 1;
                                FlagSign = ((RegA - value) >> 7) & 1;
                                break;
                            case OpInfo.IllInstrISC:
                                value = Read(addr);
                                value = FlagZero = (value + 1) & 0xFF;
                                FlagSign = value >> 7;
                                Write(addr, value);
                                temp = RegA - value - (1 - FlagCarry);
                                FlagCarry = (temp < 0 ? 0 : 1);
                                FlagOverflow = ((((RegA ^ temp) & 0x80) != 0 && ((RegA ^ Read(addr)) & 0x80) != 0) ? 1 : 0);
                                RegA = FlagZero = (temp & 0xFF);
                                FlagSign = RegA >> 7;
                                break;
                            case OpInfo.IllInstrKIL:
                                //SHOULD crash CPU, but Im going to treat it as a NOP.
                                break;
                            case OpInfo.IllInstrLAX:
                                RegA = RegX = FlagZero = Read(addr);
                                FlagSign = RegA >> 7;
                                opCycles += opCycleAdd;
                                break;
                            case OpInfo.IllInstrNOP:
                                if (addressing == OpInfo.AddrAbsoluteX)
                                    opCycles += opCycleAdd;
                                break;
                            case OpInfo.IllInstrRLA:
                                value = Read(addr);
                                if (FlagCarry != 0)
                                {
                                    FlagCarry = value >> 7;
                                    value = ((value << 1) + 1) & 0xFF;
                                }
                                else
                                {
                                    FlagCarry = value >> 7;
                                    value = (value << 1) & 0xFF;
                                }
                                FlagSign = value >> 7;
                                FlagZero = value;
                                Write(addr, value);
                                RegA &= value;
                                FlagSign = RegA >> 7;
                                FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrRRA:
                                value = Read(addr);
                                if (FlagCarry != 0)
                                {
                                    FlagCarry = value & 1;
                                    value = ((value >> 1) + 0x80) & 0xFF;
                                    FlagSign = 1;
                                }
                                else
                                {
                                    FlagCarry = value & 1;
                                    value = (value >> 1) & 0xFF;
                                    FlagSign = 0;
                                }
                                FlagZero = value;
                                Write(addr, value);
                                temp = RegA + value + FlagCarry;
                                FlagOverflow = ((!(((RegA ^ value) & 0x80) != 0) && (((RegA ^ temp) & 0x80)) != 0) ? 1 : 0);
                                FlagCarry = temp > 0xFF ? 1 : 0;
                                RegA = FlagZero = temp & 0xFF;
                                FlagSign = RegA >> 7;
                                break;
                            case OpInfo.IllInstrSAX:
                                Write(addr, (RegA & RegX) & 0xFF);
                                break;
                            case OpInfo.IllInstrSBC:
                                value = Read(addr);
                                temp = RegA - value - (1 - FlagCarry);
                                FlagCarry = (temp < 0 ? 0 : 1);
                                FlagOverflow = ((((RegA ^ temp) & 0x80) != 0 && ((RegA ^ Read(addr)) & 0x80) != 0) ? 1 : 0);
                                RegA = FlagZero = (temp & 0xFF);
                                FlagSign = RegA >> 7;
                                break;
                            case OpInfo.IllInstrSHX: //Passes Tests but may be wrong in some minute detail
                                value = (RegX & ((addr >> 8) + 1)) & 0xFF;
                                if((RegY + Read(opAddr + 1)) <= 0xFF)
                                    Write(addr, value);
                                break;
                            case OpInfo.IllInstrSHY: //Passes Tests but may be wrong in some minute detail
                                value = (RegY & ((addr >> 8) + 1)) & 0xFF;
                                if((RegX + Read(opAddr + 1)) <= 0xFF)
                                    Write(addr, value);
                                break;
                            case OpInfo.IllInstrSLO:
                                value = Read(addr);
                                FlagCarry = value >> 7;
                                value = (value << 1) & 0xFF;
                                FlagSign = value >> 7;
                                FlagZero = value;
                                Write(addr, value);
                                RegA |= value;
                                FlagSign = RegA >> 7;
                                FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrSRE:
                                value = Read(addr);
                                FlagCarry = value & 1;
                                value = value >> 1;
                                FlagSign = 0;
                                FlagZero = value;
                                Write(addr, value);
                                RegA ^= value;
                                FlagSign = RegA >> 7;
                                FlagZero = RegA;
                                break;
                            case OpInfo.InstrDummy:
                                if (invalidCount < 10)
                                {
                                    romInfo.AppendLine("Missing OP: " + OpInfo.GetOpNames()[OpInfo.GetOps()[op] & 0xFF] + " " + op.ToString("X2") + " Program Counter: " + RegPC.ToString("X4"));
                                    invalidCount++;
                                }
                                break;
                        }
                        break;
                }
                #endregion
                this.counter += opCycles;
                APU.AddCycles(opCycles);
                if (interruptBRK)
                {
                    PushWordStack((RegPC + 1) & 0xFFFF);
                    PushByteStack(PToByte() | 0x30);
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFE);
                    this.interruptBRK = false;
                }
                if (interruptReset)
                {
                    PushWordStack(RegPC);
                    PushByteStack(PToByte());
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFC);
                    this.interruptReset = false;
                }
                else if (interruptNMI)
                {
                    PushWordStack(RegPC);
                    FlagBreak = 0;
                    PushByteStack(PToByte());
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFA);
                    this.interruptNMI = false;
                }
                else if ((romMapper.interruptMapper || APU.frameIRQ || APU.dmcInterrupt) && FlagIRQ == 0)
                {
                    PushWordStack(RegPC);
                    FlagBreak = 0;
                    PushByteStack(PToByte());
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFE);
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

                    if (((Memory[0x2000] & 0x18) != 0) && vblank == 0)
                        romMapper.MapperScanline(scanline, vblank);

                    this.scanline++;
                    if (this.scanline >= 240)
                    {
                        if (this.scanline == 240 && this.vblank == 0)
                        {
                            //this.PPUAddrFlip = false;
                            this.Memory[0x2002] |= 0x80;
                            if ((this.Memory[0x2000] & 0x80) != 0)
                                this.interruptNMI = true;
                        }
                        this.vblank++;
                        if (this.vblank > 20)
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
        }

        /*
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
                case 9: //MMC2
                    romMapper = new mappers.m009(Memory, PPUMemory, numprgrom, numvrom);
                    break;
                case 11: //Color Dreams
                    romMapper = new mappers.m011(Memory, PPUMemory, numprgrom, numvrom);
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
            RegPC = PeekWord(0xFFFC);//entry point
            for(int i = 0; i < 0x20; i++)
                this.PalMemory[i] = 0x0F; //Sets the background to black on startup to prevent grey flashes, not exactly accurate but it looks nicer
        }
        private byte Read(int address)
        {
            address &= 0xFFFF;
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
        private int ReadWord(int address)
        {
            int highAddress = (address + 1) & 0xFFFF;
            return (Read(address) + (Read(highAddress) << 8)) & 0xFFFF;
        }
        private int ReadWordWrap(int address)
        {
            int highAddress = (address & 0xFF00) + ((address + 1) & 0xFF);
            return (Read(address) + (Read(highAddress) << 8)) & 0xFFFF;
        }
        private byte Peek(int address)
        {
            address = address & 0xFFFF;
            byte nextByte = this.Memory[address];
            return nextByte;
        }
        private int PeekWord(int address)
        {
            int highAddress = (address + 1) & 0xFFFF;
            return (Peek(address) + (Peek(highAddress) << 8)) & 0xFFFF;
        }
        private int PeekWordWrap(int address)
        {
            int highAddress = (address & 0xFF00) + ((address + 1) & 0xFF);
            return (Peek(address) + (Peek(highAddress) << 8)) & 0xFFFF;
        }
        private string LogOp(int address)
        {
            StringBuilder line = new StringBuilder();
            int op = Peek(address);
            int opInfo = OpInfo.GetOps()[op];
            int size = (opInfo >> 16) & 0xFF;
            int addressing = (opInfo >> 8) & 0xFF;
            line.AppendFormat("{0}  ", address.ToString("X4"));
            if (size == 0)
                line.Append("          ");
            else if (size == 1)
                line.AppendFormat("{0}       ", Peek(address).ToString("X2"));
            else if (size == 2)
                line.AppendFormat("{0} {1}    ", Peek(address).ToString("X2"), Peek(address + 1).ToString("X2"));
            else if (size == 3)
                line.AppendFormat("{0} {1} {2} ", Peek(address).ToString("X2"), Peek(address + 1).ToString("X2"), Peek(address + 2).ToString("X2"));
            line.Append(OpInfo.GetOpNames()[opInfo & 0xFF].PadLeft(4).PadRight(5));
            //Should be 20 long at this point, addressing should be 28 long

            int val1;
            int val2;
            int val3;
            int val4;
            switch (addressing)
            {
                case OpInfo.AddrNone:
                    line.Append("                            ");
                    break;
                case OpInfo.AddrAccumulator:
                    line.Append("A                           ");
                    break;
                case OpInfo.AddrImmediate:
                    line.AppendFormat("#${0}                        ", Peek(address + 1).ToString("X2"));
                    break;
                case OpInfo.AddrZeroPage:
                    line.AppendFormat("${0} = {1}                    ", Peek(address + 1).ToString("X2"), Peek(Peek(address + 1)).ToString("X2"));
                    break;
                case OpInfo.AddrZeroPageX:
                    line.AppendFormat("${0},X @ {1} = {2}             ", Peek(address + 1).ToString("X2"), ((Peek(address + 1) + RegX) & 0xFF).ToString("X2"), Peek((Peek(address + 1) + RegX) & 0xFF).ToString("X2"));
                    break;
                case OpInfo.AddrZeroPageY:
                    line.AppendFormat("${0},Y @ {1} = {2}             ", Peek(address + 1).ToString("X2"), ((Peek(address + 1) + RegY) & 0xFF).ToString("X2"), Peek((Peek(address + 1) + RegY) & 0xFF).ToString("X2"));
                    break;
                case OpInfo.AddrAbsolute:
                    if(op == 0x4C || op == 0x20)
                        line.AppendFormat("${0}                       ", PeekWord(address + 1).ToString("X4"));
                    else
                        line.AppendFormat("${0} = {1}                  ", PeekWord(address + 1).ToString("X4"), Peek(PeekWord(address + 1)).ToString("X2"));
                    break;
                case OpInfo.AddrAbsoluteX:
                    line.AppendFormat("${0},X @ {1} = {2}         ", PeekWord(address + 1).ToString("X4"), ((PeekWord(address + 1) + RegX) & 0xFFFF).ToString("X4"), Peek((PeekWord(address + 1) + RegX) & 0xFFFF).ToString("X2"));
                    break;
                case OpInfo.AddrAbsoluteY:
                    line.AppendFormat("${0},Y @ {1} = {2}         ", PeekWord(address + 1).ToString("X4"), ((PeekWord(address + 1) + RegY) & 0xFFFF).ToString("X4"), Peek((PeekWord(address + 1) + RegY) & 0xFFFF).ToString("X2"));
                    break;
                case OpInfo.AddrIndirectAbs:
                    line.AppendFormat("(${0}) = {1}              ", PeekWord(address + 1).ToString("X4"), PeekWordWrap(PeekWord(address + 1)).ToString("X4"));
                    break;
                case OpInfo.AddrRelative:
                    int addr = Peek(address + 1);
                    if (addr < 0x80)
                        addr += (address + 1);
                    else
                        addr += (address + 1) - 256;
                    line.AppendFormat("${0}                       ", addr.ToString("X4"));
                    break;
                case OpInfo.AddrIndirectX:
                    addr = val1 = Peek(address + 1);
                    addr += RegX;
                    addr &= 0xFF;
                    val2 = addr;
                    addr = val3 = Peek(addr) + (Peek((addr + 1) & 0xFF) << 8);
                    addr = val4 = Peek(addr);
                    line.AppendFormat("(${0},X) @ {1} = {2} = {3}    ", val1.ToString("X2"), val2.ToString("X2"), val3.ToString("X4"), val4.ToString("X2"));
                    break;
                case OpInfo.AddrIndirectY:
                    addr = val1 = Peek(address + 1);
                    addr = val2 = Peek(addr) + (Peek((addr + 1) & 0xFF) << 8);
                    addr += RegY;
                    addr &= 0xFFFF;
                    val3 = addr;
                    addr = val4 = Peek(addr & 0xFFFF);
                    line.AppendFormat("(${0}),Y = {1} @ {2} = {3}  ", val1.ToString("X2"), val2.ToString("X4"), val3.ToString("X4"), val4.ToString("X2"));
                    break;
            }
            line.AppendFormat("A:{0} X:{1} Y:{2} P:", RegA.ToString("X2"), RegX.ToString("X2"), RegY.ToString("X2"));
            if (FlagCarry != 0)
                line.Append("C");
            else
                line.Append("c");
            if (FlagZero == 0)
                line.Append("Z");
            else
                line.Append("z");
            if (FlagIRQ != 0)
                line.Append("I");
            else
                line.Append("i");
            if (FlagDecimal != 0)
                line.Append("D");
            else
                line.Append("d");
            if (FlagBreak != 0)
                line.Append("B");
            else
                line.Append("b");
            if (FlagNotUsed != 0)
                line.Append("-");
            else
                line.Append("_");
            if (FlagOverflow != 0)
                line.Append("V");
            else
                line.Append("v");
            if (FlagSign != 0)
                line.Append("N");
            else
                line.Append("n");
            line.AppendFormat(" S:{0} CYC:{1} SL:{2}", RegS.ToString("X2"), (counter * 3).ToString().PadLeft(3), scanline.ToString().PadLeft(3));
            return line.ToString();
        }
        public void AddCycles(int value)
        {
            counter += value;
        }
        private void Write(int address, int value)
        {
            romMapper.MapperWrite(MirrorMap[address], (byte)value);
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
                this.SPRMemory[sprAddress] = (byte)value;
                sprAddress++;
                this.Memory[0x2003] = sprAddress;
            }
            else if (this.MirrorMap[address] == 0x2000)
            {
                loopyT = (ushort)((loopyT & 0xF3FF) | ((value & 3) << 10));
                //loopyT = (ushort)((loopyT & 0x0C00) | (value << 10));
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
                    loopyT = (ushort)((loopyT & 0xFF00) | value);
                    loopyV = loopyT;
                }
                else//1st Write
                {
                    this.nameTableOffset = (byte)((value & 0xC) >> 2); //TO-DO: this is wrong but I don't think it will effect many games other then fixing mario
                    this.vertOffset = (this.vertOffset & 0xF8) | ((value & 0x30) >> 4);
                    this.vertOffset = (this.vertOffset & 0x3F) | ((value & 3) << 6);
                    int oldA12 = ((loopyT >> 12) & 1);
                    loopyT = (ushort)((loopyT & 0x00FF) | ((value & 0x3F) << 8));
                    if (oldA12 == 0 && oldA12 != ((loopyT >> 12) & 1))
                        romMapper.MapperScanline(scanline, vblank);
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
                else
                    this.PPUMemory[this.PPUMirrorMap[loopyV & 0x3FFF]] = (byte)value;
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
            
            APU.Write((byte)value, this.MirrorMap[address]);

            if (this.MirrorMap[address] != 0x2002)
                this.Memory[this.MirrorMap[address]] = (byte)value;
            ApplyGameGenie();
        }
        private byte PToByte()
        {
            byte value = 0;
            if (FlagCarry != 0) value |= 0x01;
            if (FlagZero == 0) value |= 0x02;
            if (FlagIRQ != 0) value |= 0x04;
            if (FlagDecimal != 0) value |= 0x08;
            if (FlagBreak != 0) value |= 0x10;
            if (FlagNotUsed != 0) value |= 0x10;
            if (FlagOverflow != 0) value |= 0x40;
            if (FlagSign != 0) value |= 0x80;
            return value;
        }
        private void PFromByte(int p)
        {
            p &= 0xFF;
            FlagCarry =     p & 1;
            FlagZero =      ((p >> 1) & 1) == 0 ? 1 : 0;
            FlagIRQ =       ((p >> 2) & 1);
            FlagDecimal =   ((p >> 3) & 1);
            FlagBreak =     ((p >> 4) & 1);
            FlagNotUsed =   ((p >> 5) & 1);
            FlagOverflow =  ((p >> 6) & 1);
            FlagSign =      ((p >> 7) & 1);
        }
        private void PushWordStack(int value)
        {
            Write((ushort)(RegS + 0x0100), (byte)(value >> 8));
            RegS--;
            RegS &= 0xFF;
            Write((ushort)(RegS + 0x0100), (byte)value);
            RegS--;
            RegS &= 0xFF;
        }
        private int PopWordStack()
        {
            RegS += 2;
            RegS &= 0xFF;
            return ReadWord((ushort)((RegS - 1) + 0x0100));
        }
        private void PushByteStack(int value)
        {
            this.Write((ushort)(RegS + 0x0100), value);
            RegS--;
            RegS &= 0xFF;
        }
        private byte PopByteStack()
        {
            RegS++;
            RegS &= 0xFF;
            return Read((ushort)(RegS + 0x0100));
        }
        private void SpriteZeroHit(int scanline)
        {
            if (spriteZeroHit != true)
            {
                byte PPUCTRL = this.Memory[0x2000];
                bool squareSprites = false;
                if ((PPUCTRL & 0x20) == 0)
                    squareSprites = true;
                int yPos = this.SPRMemory[0] + 1;
                if((squareSprites && (yPos <= scanline && yPos + 8 > scanline)) || (!squareSprites &&  (yPos <= scanline && yPos + 16 > scanline)))
                    this.scanlines[scanline] = ProcessScanline(scanline);
            }
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
                for (int sprite = 0; sprite < 64; sprite++)
                {
                    int yPos = this.SPRMemory[sprite * 4] + 1;
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
                                    if (spritePixel + xPos < 256 && (spriteBelowLine[spritePixel + xPos] == false && spriteAboveLine[spritePixel + xPos] == false))
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
                                    if (spritePixel + xPos < 256 && (spriteBelowLine[spritePixel + xPos] == false && spriteAboveLine[spritePixel + xPos] == false))
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
            newState.stateProgramCounter = RegPC;
            newState.stateMemory = (byte[][])Memory.StoreBanks().Clone();
            newState.stateMemBanks = (bool[])Memory.saveBanks.Clone();
            newState.stateMemMap = (int[])Memory.memMap.Clone();
            newState.statePPUMemory = (byte[][])PPUMemory.StoreBanks().Clone();
            newState.statePPUBanks = (bool[])PPUMemory.saveBanks.Clone();
            newState.statePPUMap = (int[])PPUMemory.memMap.Clone();
            newState.statePalMemory = (byte[])this.PalMemory.Clone();
            newState.stateA = RegA;
            newState.stateX = RegX;
            newState.stateY = RegY;
            newState.stateS = RegS;
            newState.stateP = PToByte();
            newState.stateCounter = this.counter;
            newState.stateSlCounter = this.slCounter;
            newState.stateScanline = this.scanline;
            newState.stateVblank = this.vblank;
            newState.stateSPRMemory = (byte[])this.SPRMemory.Clone();
            newState.stateInterruptReset = this.interruptReset;
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
            newState.mapperState = new MemoryStream();
            romMapper.MapperStateSave(ref newState.mapperState);
            newState.isStored = true;
            return newState;
        }
        public void loadState(SaveState oldState)
        {
            RegPC = oldState.stateProgramCounter;
            this.Memory.LoadBanks((bool[])oldState.stateMemBanks.Clone(), (byte[][])oldState.stateMemory.Clone());
            this.Memory.memMap = (int[])oldState.stateMemMap.Clone();
            this.PPUMemory.LoadBanks((bool[])oldState.statePPUBanks.Clone(), (byte[][])oldState.statePPUMemory.Clone());
            this.PPUMemory.memMap = (int[])oldState.statePPUMap.Clone();
            this.PalMemory = (byte[])oldState.statePalMemory.Clone();
            RegA = oldState.stateA;
            RegX = oldState.stateX;
            RegY = oldState.stateY;
            RegS = oldState.stateS;
            PFromByte(oldState.stateP);
            this.counter = oldState.stateCounter;
            this.slCounter = oldState.stateSlCounter;
            this.scanline = oldState.stateScanline;
            this.vblank = oldState.stateVblank;
            this.SPRMemory = (byte[])oldState.stateSPRMemory.Clone();
            this.interruptReset = oldState.stateInterruptReset;
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
            romMapper.MapperStateLoad(oldState.mapperState);
        }
        public void restart()
        {
            this.interruptReset = true;
        }
        private void ApplyGameGenie()
        {
            for (int i = 0; i < this.gameGenieCodeNum; i++)
            {
                if (this.gameGenieCodes[i].code == "DUMMY")
                    this.Memory.ForceValue(this.MirrorMap[this.gameGenieCodes[i].address], this.gameGenieCodes[i].value);
                else if (this.gameGenieCodes[i].code.Length == 6)
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
    }
    [Serializable]
    public struct SaveState
    {
        public int stateProgramCounter;
        public byte[][] stateMemory;
        public bool[] stateMemBanks;
        public int[] stateMemMap;
        public byte[][] statePPUMemory;
        public bool[] statePPUBanks;
        public int[] statePPUMap;
        public byte[] stateSPRMemory;
        public byte[] statePalMemory;
        public int stateA;
        public int stateX;
        public int stateY;
        public int stateS;
        public int stateP;
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
        public MemoryStream mapperState;
    }
}
