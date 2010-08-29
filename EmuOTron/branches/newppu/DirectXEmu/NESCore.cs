
//#define nestest

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
        private int FlagDecimal = 0;
        private int FlagBreak = 1;
        private int FlagNotUsed= 1;
        private int FlagOverflow = 0;
        private int FlagSign = 0; //Bit 7 of P
        private int counter = 0;

        private int invalidCount = 0;

        public mappers.Mapper romMapper;

        private bool interruptReset = false;
        private bool interruptBRK = false;

        public StringBuilder logBuilder = new StringBuilder();
        public bool logging = false;

        public StringBuilder romInfo = new StringBuilder();

        private Controller player1;
        private Controller player2;
        private Controller player3;
        private Controller player4;
        private int controlReg1;
        private int controlReg2;
        private bool controlReady;
        public bool fourScore = true;

        public GameGenie[] gameGenieCodes = new GameGenie[0xFF];
        public int gameGenieCodeNum = 0;

        public bool sramPresent = false;
        public string romHash;
        public UInt32 CRC = 0xffffffff;
        public string filePath;
        public string fileName;
        public bool VS;
        public bool creditService;
        public bool dip1;
        public bool dip2;
        public bool dip3;
        public bool dip4;
        public bool dip5;
        public bool dip6;
        public bool dip7;
        public bool dip8;

        public string cartDBLocation = "";

        private bool turbo = false;
        private OpInfo OpCodes = new OpInfo();
        public APU APU;
        public PPU PPU;

        public void Start(Controller player1, Controller player2, bool turbo)
        {
            this.player1 = player1;
            this.player2 = player2;
            player3 = player1;
            player4 = player1;
            PPU.turbo = turbo; //not functioning, maybe never functioning
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
                PPU.AddCycles(opCycles);
                if (romMapper.mapper == 69)
                    romMapper.MapperIRQ(opCycles, 0);
#if !nestest
                if (interruptBRK)
                {
                    PushWordStack((RegPC + 1) & 0xFFFF);
                    PushByteStack(PToByte() | 0x30);
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFE);
                    interruptBRK = false;
                }
                if (interruptReset)
                {
                    PushWordStack(RegPC);
                    PushByteStack(PToByte());
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFC);
                    interruptReset = false;
                }
                else if (PPU.interruptNMI)
                {
                    PushWordStack(RegPC);
                    FlagBreak = 0;
                    PushByteStack(PToByte());
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFA);
                    PPU.interruptNMI = false;
                }
                else if ((romMapper.interruptMapper || APU.frameIRQ || APU.dmcInterrupt) && FlagIRQ == 0)
                {
                    PushWordStack(RegPC);
                    FlagBreak = 0;
                    PushByteStack(PToByte());
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFE);
                }
#endif
                if (PPU.frameComplete)
                {
                    emulationRunning = false;
                    PPU.frameComplete = false;
                    PPU.generateNameTables = false;
                    PPU.generatePatternTables = false;
                }
            }
            APU.Update();
        }
        public NESCore(String input, String cartDBLocation)
        {
            this.cartDBLocation = cartDBLocation;
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
            VS = ((highMapper & 0x01) != 0);
            int mapper = (lowMapper >> 4) + (highMapper & 0xF0);
            Memory = new MemoryStore(0x20 + (numprgrom * 0x10), false);
            Memory.swapOffset = 0x20;
            APU = new APU(Memory);
            PPU = new PPU(this, numvrom);
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
                PPU.PPUMemory.banks[(i / 0x400) + PPU.PPUMemory.swapOffset][i % 0x400] = nextByte;
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
                PPU.PPUMemory.FourScreenMirroring();
                PPU.PPUMemory.hardwired = true;
            }
            else if (vertMirroring)
            {
                PPU.PPUMemory.VerticalMirroring();
            }
            else
            {
                PPU.PPUMemory.HorizontalMirroring();
            }
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
                    romMapper = new mappers.m000(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 1: //MMC1
                    romMapper = new mappers.m001(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 2: //UNROM
                    romMapper = new mappers.m002(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 3://CNROM
                    romMapper = new mappers.m003(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 4: //MMC3
                    romMapper = new mappers.m004(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 7: //AOROM
                    romMapper = new mappers.m007(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 9: //MMC2
                    romMapper = new mappers.m009(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 11: //Color Dreams
                    romMapper = new mappers.m011(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 34: //BNROM and NINA-001
                    romMapper = new mappers.m034(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 69: //Sunsoft5
                    romMapper = new mappers.m069(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 70: //Bandai
                    romMapper = new mappers.m070(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 71: //Camerica
                    romMapper = new mappers.m071(Memory, PPU.PPUMemory, numprgrom, numvrom);
                    break;
                case 99: //VS Unisystem
                    romMapper = new mappers.m099(Memory, PPU.PPUMemory, numprgrom, numvrom);
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
            }
            this.CPUMirror(0x0000, 0x0800, 0x0800, 3);
            this.CPUMirror(0x2000, 0x2008, 0x08, 0x3FF);
            RegPC = PeekWord(0xFFFC);//entry point
#if nestest
            RegPC = 0xC000;
#endif
        }
        private byte Read(int address)
        {
            address = this.MirrorMap[address & 0xFFFF];
            byte nextByte = this.Memory[this.MirrorMap[address]];
            if (address == 0x4016) //Player1 Controller
            {
                nextByte = 0;
                if (player1.zapper.connected)
                {
                    if (player1.zapper.triggerPulled)
                        nextByte |= 0x10;
                    if (!player1.zapper.lightDetected)
                        nextByte |= 0x08;
                }
                if (controlReady)
                {
                    nextByte |= (byte)(controlReg1 & 1);
                    controlReg1 >>= 1;
                }
            }
            else if (address == 0x4017) //Player2 Controller
            {
                nextByte = 0;
                if (player2.zapper.connected)
                {
                    if (player2.zapper.triggerPulled)
                        nextByte |= 0x10;
                    if (!player2.zapper.lightDetected)
                        nextByte |= 0x08;
                }
                if (controlReady)
                {
                    nextByte |= (byte)(controlReg2 & 1);
                    controlReg2 >>= 1;
                }
            }
            if (VS)
            {
                if (address == 0x4016)
                {
                    //nextbyte should be coming from controller reg with data in bit 1
                    /*
                     * Port 4016h/Read:
                        Bit2    Credit Service Button       (0=Released, 1=Service Credit)
                        Bit3-4  DIP Switch 1-2              (0=Off, 1=On)
                        Bit5-6  Credit Left/Right Coin Slot (0=None, 1=Coin) (Acknowledge via 4020h)
                     */
                    if (creditService)
                        nextByte |= 0x04;
                    else
                        nextByte &= 0xFB;
                    if (dip1)
                        nextByte |= 0x08;
                    else
                        nextByte &= 0xF7;
                    if (dip2)
                        nextByte |= 0x10;
                    else
                        nextByte &= 0xEF;
                    if (player1.coin)
                        nextByte |= 0x20;
                    else
                        nextByte &= 0xDF;
                    if (player2.coin)
                        nextByte |= 0x40;
                    else
                        nextByte &= 0xBF;
                }
                else if (address == 0x4017)
                {
                    if (dip3)
                        nextByte |= 0x04;
                    else
                        nextByte &= 0xFB;
                    if (dip4)
                        nextByte |= 0x08;
                    else
                        nextByte &= 0xF7;
                    if (dip5)
                        nextByte |= 0x10;
                    else
                        nextByte &= 0xEF;
                    if (dip6)
                        nextByte |= 0x20;
                    else
                        nextByte &= 0xDF;
                    if (dip7)
                        nextByte |= 0x40;
                    else
                        nextByte &= 0xBF;
                    if (dip8)
                        nextByte |= 0x80;
                    else
                        nextByte &= 0x7F;
                }
            }
            nextByte = APU.Read(nextByte, (ushort)address);
            nextByte = PPU.Read(nextByte, (ushort)address);
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
        private void Write(int address, int value)
        {
            address = MirrorMap[address & 0xFFFF];
            romMapper.MapperWrite((ushort)address, (byte)value);
            if (address == 0x4016)
            {
                if ((value & 0x01) == 1)
                {
                    controlReg1 = 0;
                    if (fourScore)
                    {
                        controlReg1 |= 1;
                        controlReg1 <<= 1;
                        controlReg1 |= 0;
                        controlReg1 <<= 1;
                        controlReg1 |= 0;
                        controlReg1 <<= 1;
                        controlReg1 |= 0;
                        controlReg1 <<= 1;
                        controlReg1 |= player3.right ? 1 : 0;
                        controlReg1 <<= 1;
                        controlReg1 |= player3.left ? 1 : 0;
                        controlReg1 <<= 1;
                        controlReg1 |= player3.down ? 1 : 0;
                        controlReg1 <<= 1;
                        controlReg1 |= player3.up ? 1 : 0;
                        controlReg1 <<= 1;
                        controlReg1 |= player3.start ? 1 : 0;
                        controlReg1 <<= 1;
                        controlReg1 |= player3.select ? 1 : 0;
                        controlReg1 <<= 1;
                        controlReg1 |= player3.b ? 1 : 0;
                        controlReg1 <<= 1;
                        controlReg1 |= player3.a ? 1 : 0;
                        controlReg1 <<= 1;
                    }
                    controlReg1 |= player1.right ? 1 : 0;
                    controlReg1 <<= 1;
                    controlReg1 |= player1.left ? 1 : 0;
                    controlReg1 <<= 1;
                    controlReg1 |= player1.down ? 1 : 0;
                    controlReg1 <<= 1;
                    controlReg1 |= player1.up ? 1 : 0;
                    controlReg1 <<= 1;
                    controlReg1 |= player1.start ? 1 : 0;
                    controlReg1 <<= 1;
                    controlReg1 |= player1.select ? 1 : 0;
                    controlReg1 <<= 1;
                    controlReg1 |= player1.b ? 1 : 0;
                    controlReg1 <<= 1;
                    controlReg1 |= player1.a ? 1 : 0;

                    controlReg2 = 0;
                    if(fourScore)
                    {
                        controlReg2 |= 1;
                        controlReg2 <<= 1;
                        controlReg2 |= 0;
                        controlReg2 <<= 1;
                        controlReg2 |= 0;
                        controlReg2 <<= 1;
                        controlReg2 |= player4.right ? 1 : 0;
                        controlReg2 <<= 1;
                        controlReg2 |= player4.left ? 1 : 0;
                        controlReg2 <<= 1;
                        controlReg2 |= player4.down ? 1 : 0;
                        controlReg2 <<= 1;
                        controlReg2 |= player4.up ? 1 : 0;
                        controlReg2 <<= 1;
                        controlReg2 |= player4.start ? 1 : 0;
                        controlReg2 <<= 1;
                        controlReg2 |= player4.select ? 1 : 0;
                        controlReg2 <<= 1;
                        controlReg2 |= player4.b ? 1 : 0;
                        controlReg2 <<= 1;
                        controlReg2 |= player4.a ? 1 : 0;
                        controlReg2 <<= 1;
                    }
                    controlReg2 |= player2.right ? 1 : 0;
                    controlReg2 <<= 1;
                    controlReg2 |= player2.left ? 1 : 0;
                    controlReg2 <<= 1;
                    controlReg2 |= player2.down ? 1 : 0;
                    controlReg2 <<= 1;
                    controlReg2 |= player2.up ? 1 : 0;
                    controlReg2 <<= 1;
                    controlReg2 |= player2.start ? 1 : 0;
                    controlReg2 <<= 1;
                    controlReg2 |= player2.select ? 1 : 0;
                    controlReg2 <<= 1;
                    controlReg2 |= player2.b ? 1 : 0;
                    controlReg2 <<= 1;
                    controlReg2 |= player2.a ? 1 : 0;

                    controlReady = false;
                }
                else
                {
                    controlReady = true;
                }
            }
            if (VS)
            {
                if (address == 0x4020)
                {
                    if ((value & 1) != 0)
                    {
                        player1.coin = false;
                        player2.coin = false;
                    }
                }
            }
            APU.Write((byte)value, (ushort)address);
            PPU.Write((byte)value, (ushort)address);
            Memory[address] = (byte)value;
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
                    if (op == 0x4C || op == 0x20)
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
            line.AppendFormat(" S:{0} CYC:{1} SL:{2}", RegS.ToString("X2"), (PPU.scanlineCycle * 3).ToString().PadLeft(3), PPU.scanline.ToString().PadLeft(3));
            return line.ToString();
        }
        public void AddCycles(int value)
        {
            counter += value;
        }
        private void CPUMirror(ushort address, ushort mirrorAddress, ushort length, int repeat)
        {
            for (int j = 0; j < repeat; j++)
                for (int i = 0; i < length; i++)
                    this.MirrorMap[mirrorAddress + i + (j * length)] = (ushort)(this.MirrorMap[address + i]);
        }
        public SaveState getState()
        {
            SaveState newState = new SaveState();
            newState.stateProgramCounter = RegPC;
            newState.stateMemory = (byte[][])Memory.StoreBanks().Clone();
            newState.stateMemBanks = (bool[])Memory.saveBanks.Clone();
            newState.stateMemMap = (int[])Memory.memMap.Clone();
            newState.statePPUMemory = (byte[][])PPU.PPUMemory.StoreBanks().Clone();
            newState.statePPUBanks = (bool[])PPU.PPUMemory.saveBanks.Clone();
            newState.statePPUMap = (int[])PPU.PPUMemory.memMap.Clone();
            newState.statePalMemory = (byte[])PPU.PalMemory.Clone();
            newState.stateA = RegA;
            newState.stateX = RegX;
            newState.stateY = RegY;
            newState.stateS = RegS;
            newState.stateP = PToByte();
            newState.stateCounter = this.counter;
            //newState.stateSlCounter = this.slCounter;
            //newState.stateScanline = this.scanline;
            //newState.stateVblank = this.vblank;
            //newState.stateSPRMemory = (byte[])this.SPRMemory.Clone();
            newState.stateInterruptReset = this.interruptReset;
            //newState.stateInterruptNMI = this.interruptNMI;
            newState.stateInterruptMapper = romMapper.interruptMapper;
            //newState.vertOffset = this.vertOffset;
            //newState.horzOffset = this.horzOffset;
            //newState.nameTableOffset = this.nameTableOffset;
            //newState.loopyT = this.loopyT;
            //newState.loopyV = this.loopyV;
            //newState.loopyX = this.loopyX;
            newState.controlReg1 = this.controlReg1;
            newState.controlReg2 = this.controlReg2;
            newState.controlReady = this.controlReady;
            //newState.PPUAddrFlip = this.PPUAddrFlip;
            //newState.readBuffer = this.readBuffer;
            //newState.spriteZeroHit = this.spriteZeroHit;
            //newState.spriteOverflow = this.spriteOverflow;
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
            PPU.PPUMemory.LoadBanks((bool[])oldState.statePPUBanks.Clone(), (byte[][])oldState.statePPUMemory.Clone());
            PPU.PPUMemory.memMap = (int[])oldState.statePPUMap.Clone();
            PPU.PalMemory = (byte[])oldState.statePalMemory.Clone();
            RegA = oldState.stateA;
            RegX = oldState.stateX;
            RegY = oldState.stateY;
            RegS = oldState.stateS;
            PFromByte(oldState.stateP);
            this.counter = oldState.stateCounter;
            //this.slCounter = oldState.stateSlCounter;
            //this.scanline = oldState.stateScanline;
            //this.vblank = oldState.stateVblank;
            //this.SPRMemory = (byte[])oldState.stateSPRMemory.Clone();
            //this.interruptReset = oldState.stateInterruptReset;
            //this.interruptNMI = oldState.stateInterruptNMI;
            romMapper.interruptMapper = oldState.stateInterruptMapper;
            //this.vertOffset = oldState.vertOffset;
            //this.horzOffset = oldState.horzOffset;
            //this.nameTableOffset = oldState.nameTableOffset;
            //this.loopyT = oldState.loopyT;
            //this.loopyV = oldState.loopyV;
            //this.loopyX = oldState.loopyX;
            this.controlReg1 = oldState.controlReg1;
            this.controlReg2 = oldState.controlReg2;
            this.controlReady = oldState.controlReady;
            //this.PPUAddrFlip = oldState.PPUAddrFlip;
            //this.readBuffer = oldState.readBuffer;
            //this.spriteZeroHit = oldState.spriteZeroHit;
            //this.spriteOverflow = oldState.spriteOverflow;
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
        public int controlReg1;
        public int controlReg2;
        public bool controlReady;
        public bool PPUAddrFlip;
        public byte readBuffer;
        public bool spriteZeroHit;
        public bool spriteOverflow;
        public bool isStored;
        public MemoryStream mapperState;
    }
}
