
//#define nestest

using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace EmuoTron
{
    public class NESCore
    {
        public SystemType nesRegion;

        public Rom rom;
        public Mappers.Mapper mapper;
        public APU APU;
        public PPU PPU;
        public Debug debug;

        public MemoryStore Memory;
        public ushort[] MirrorMap = new ushort[0x10000];
        public int RegA = 0;
        public int RegX = 0;
        public int RegY = 0;
        public int RegS = 0xFD;
        public int RegPC = 0;
        public int FlagCarry = 0; //Bit 0 of P
        public int FlagZero = 1; //backwards
        public int FlagIRQ = 1;
        public int FlagDecimal = 0;
        public int FlagBreak = 1;
        public int FlagNotUsed = 1;
        public int FlagOverflow = 0;
        public int FlagSign = 0; //Bit 7 of P
        private int counter = 0;
        private bool interruptReset = false;
        private bool interruptBRK = false;

        private OpInfo OpCodes = new OpInfo();
        private int[] opList;

        public Controller[] players = new Controller[4];
        private Inputs.Input PortOne;
        private Inputs.Input PortTwo;
        public bool fourScore;
        public bool creditService;
        public bool dip1;
        public bool dip2;
        public bool dip3;
        public bool dip4;
        public bool dip5;
        public bool dip6;
        public bool dip7;
        public bool dip8;

        public GameGenie[] gameGenieCodes = new GameGenie[0xFF];
        public int gameGenieCodeNum = 0;

        public void Start(Controller player1, Controller player2, Controller player3, Controller player4, bool turbo)
        {
            players[0] = player1;
            players[1] = player2;
            players[2] = player3;
            players[3] = player4;
            fourScore = true;
            PPU.turbo = turbo;
            APU.turbo = turbo;
            this.Start();
        }

        public void Start(Controller player1, Controller player2, bool turbo)
        {
            players[0] = player1;
            players[1] = player2;
            fourScore = false;
            PPU.turbo = turbo;
            APU.turbo = turbo;
            this.Start();
        }
        public void Start(Controller player1, bool turbo)
        {
            players[0] = player1;
            fourScore = false;
            PPU.turbo = turbo;
            APU.turbo = turbo;
            this.Start();
        }
        public void Start()
        {
            bool emulationRunning = true;
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
            while (!PPU.frameComplete && !debug.debugInterrupt && emulationRunning)
            {
                debug.Execute((ushort)RegPC);
                op = Read(RegPC);
                opInfo = opList[op];
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
                        {
                            if ((RegPC & 0xFF00) != ((addr + RegPC) & 0xFF00))
                                opCycleAdd++;
                            addr += RegPC;
                        }
                        else
                        {
                            if ((RegPC & 0xFF00) != ((addr + RegPC - 256) & 0xFF00))
                               opCycleAdd++;
                            addr += RegPC - 256;
                        }

                        break;
                    case OpInfo.AddrIndirectX:
                        addr = Read(opAddr + 1);
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
                        RegA = FlagSign = FlagZero = temp & 0xFF;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrAND:
                        RegA &= Read(addr);
                        FlagSign = FlagZero = RegA;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrASL:
                        if (addressing == OpInfo.AddrAccumulator)
                        {
                            FlagCarry = (RegA >> 7) & 1;
                            RegA = (RegA << 1) & 0xFF;
                            FlagSign = FlagZero = RegA;
                        }
                        else
                        {
                            value = Read(addr);
                            FlagCarry = (value >> 7) & 1;
                            value = (value << 1) & 0xFF;
                            FlagSign = FlagZero = value;
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
                        FlagSign = (value & 0x80);
                        FlagOverflow = (value & 0x40) >> 6;
                        FlagZero = value & RegA;
                        break;
                    case OpInfo.InstrBMI:
                        if ((FlagSign >> 7) != 0)
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
                        if ((FlagSign >> 7) == 0)
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
                        FlagSign = (RegA - value) & 0xFF;
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
                        FlagSign = (RegX - value) & 0xFF;
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
                        FlagSign = (RegY - value) & 0xFF;
                        break;
                    case OpInfo.InstrDEC:
                        value = Read(addr);
                        value = FlagSign = FlagZero = (value - 1) & 0xFF;
                        Write(addr, value);
                        break;
                    case OpInfo.InstrDEX:
                        RegX = FlagSign = FlagZero = (RegX - 1) & 0xFF;
                        break;
                    case OpInfo.InstrDEY:
                        RegY = FlagSign = FlagZero = (RegY - 1) & 0xFF;
                        break;
                    case OpInfo.InstrEOR:
                        RegA ^= Read(addr);
                        RegA = FlagSign = FlagZero = RegA & 0xFF;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrINC:
                        value = Read(addr);
                        value = FlagSign = FlagZero = (value + 1) & 0xFF;
                        Write(addr, value);
                        break;
                    case OpInfo.InstrINX:
                        RegX = FlagSign = FlagZero = (RegX + 1) & 0xFF;
                        break;
                    case OpInfo.InstrINY:
                        RegY = FlagSign = FlagZero = (RegY + 1) & 0xFF;
                        break;
                    case OpInfo.InstrJMP:
                        RegPC = addr;
                        break;
                    case OpInfo.InstrJSR:
                        PushWordStack(RegPC - 1);
                        RegPC = addr;
                        break;
                    case OpInfo.InstrLDA:
                        RegA = FlagSign = FlagZero = Read(addr);
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrLDX:
                        RegX = FlagSign = FlagZero = Read(addr);
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrLDY:
                        RegY = FlagSign = FlagZero = Read(addr);
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrLSR:
                        if (addressing == OpInfo.AddrAccumulator)
                        {
                            FlagCarry = RegA & 1;
                            RegA = FlagSign = FlagZero = RegA >> 1;
                        }
                        else
                        {
                            value = Read(addr);
                            FlagCarry = value & 1;
                            value = FlagSign = FlagZero = value >> 1;
                            Write(addr, value);
                        }
                        break;
                    case OpInfo.InstrNOP:
                        break;
                    case OpInfo.InstrORA:
                        RegA = FlagSign = FlagZero = (RegA | Read(addr)) & 0xFF;
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
                        RegA = FlagSign = FlagZero = PopByteStack();
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
                                RegA = FlagSign = FlagZero = ((RegA << 1) + 1) & 0xFF;
                            }
                            else
                            {
                                FlagCarry = RegA >> 7;
                                RegA = FlagSign = FlagZero = (RegA << 1) & 0xFF;
                            }
                        }
                        else
                        {
                            value = Read(addr);
                            if (FlagCarry != 0)
                            {
                                FlagCarry = value >> 7;
                                value = FlagSign = FlagZero = ((value << 1) + 1) & 0xFF;
                            }
                            else
                            {
                                FlagCarry = value >> 7;
                                value = FlagSign = FlagZero = (value << 1) & 0xFF;
                            }
                            Write(addr, value);
                        }
                        break;
                    case OpInfo.InstrROR:
                        if (addressing == OpInfo.AddrAccumulator)
                        {
                            if (FlagCarry != 0)
                            {
                                FlagCarry = RegA & 1;
                                RegA = FlagSign = FlagZero = ((RegA >> 1) + 0x80) & 0xFF;
                            }
                            else
                            {
                                FlagCarry = RegA & 1;
                                RegA = FlagSign = FlagZero = (RegA >> 1) & 0xFF;
                                FlagSign = 0;
                            }
                        }
                        else
                        {
                            value = Read(addr);
                            if (FlagCarry != 0)
                            {
                                FlagCarry = value & 1;
                                value = FlagSign = FlagZero = ((value >> 1) + 0x80) & 0xFF;
                            }
                            else
                            {
                                FlagCarry = value & 1;
                                value = FlagSign = FlagZero = (value >> 1) & 0xFF;
                            }
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
                        RegA = FlagSign = FlagZero = (temp & 0xFF);
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
                        RegX = FlagSign = FlagZero = RegA;
                        break;
                    case OpInfo.InstrTAY:
                        RegY = FlagSign = FlagZero = RegA;
                        break;
                    case OpInfo.InstrTSX:
                        RegX = FlagSign = FlagZero = RegS;
                        break;
                    case OpInfo.InstrTXA:
                        RegA = FlagSign = FlagZero = RegX;
                        break;
                    case OpInfo.InstrTXS:
                        RegS = RegX;
                        break;
                    case OpInfo.InstrTYA:
                        RegA = FlagSign = FlagZero = RegY;
                        break;
                    default:
                        debug.SetError("Illegal OP");
                        switch (instruction) //Illegal Ops
                        {
                            case OpInfo.IllInstrALR:
                                RegA &= Read(addr);
                                FlagCarry = RegA & 1;
                                RegA >>= 1;
                                FlagSign = FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrANC:
                                RegA &= Read(addr);
                                FlagCarry = RegA >> 7;
                                FlagSign = FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrARR:
                                RegA &= Read(addr);
                                if (FlagCarry != 0)
                                {
                                    FlagCarry = RegA & 1;
                                    RegA = FlagSign = FlagZero = ((RegA >> 1) + 0x80) & 0xFF;
                                }
                                else
                                {
                                    FlagCarry = RegA & 1;
                                    RegA = FlagSign = FlagZero = (RegA >> 1) & 0xFF;
                                }
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
                                RegX = FlagSign = FlagZero = (temp & 0xFF);
                                break;
                            case OpInfo.IllInstrDCP:
                                value = Read(addr);
                                value = FlagSign = FlagZero = (value - 1) & 0xFF;
                                Write(addr, value);
                                if (RegA >= value)
                                    FlagCarry = 1;
                                else
                                    FlagCarry = 0;
                                if (RegA == value)
                                    FlagZero = 0;
                                else
                                    FlagZero = 1;
                                FlagSign = (RegA - value) & 0xFF;
                                break;
                            case OpInfo.IllInstrISC:
                                value = Read(addr);
                                value = (value + 1) & 0xFF;
                                Write(addr, value);
                                temp = RegA - value - (1 - FlagCarry);
                                FlagCarry = (temp < 0 ? 0 : 1);
                                FlagOverflow = ((((RegA ^ temp) & 0x80) != 0 && ((RegA ^ Read(addr)) & 0x80) != 0) ? 1 : 0);
                                RegA = FlagSign = FlagZero = (temp & 0xFF);
                                break;
                            case OpInfo.IllInstrKIL:
                                debug.SetError("KIL encountered");
                                //SHOULD crash CPU, but Im going to treat it as a NOP.
                                break;
                            case OpInfo.IllInstrLAS:
                                opCycles += opCycleAdd;
                                break;
                            case OpInfo.IllInstrLAX:
                                RegA = RegX = FlagSign = FlagZero = Read(addr);
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
                                Write(addr, value);
                                RegA &= value;
                                FlagSign = FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrRRA:
                                value = Read(addr);
                                if (FlagCarry != 0)
                                {
                                    FlagCarry = value & 1;
                                    value = ((value >> 1) + 0x80) & 0xFF;
                                }
                                else
                                {
                                    FlagCarry = value & 1;
                                    value = (value >> 1) & 0xFF;
                                }
                                Write(addr, value);
                                temp = RegA + value + FlagCarry;
                                FlagOverflow = ((!(((RegA ^ value) & 0x80) != 0) && (((RegA ^ temp) & 0x80)) != 0) ? 1 : 0);
                                FlagCarry = temp > 0xFF ? 1 : 0;
                                RegA = FlagSign = FlagZero = temp & 0xFF;
                                break;
                            case OpInfo.IllInstrSAX:
                                Write(addr, (RegA & RegX) & 0xFF);
                                break;
                            case OpInfo.IllInstrSBC:
                                value = Read(addr);
                                temp = RegA - value - (1 - FlagCarry);
                                FlagCarry = (temp < 0 ? 0 : 1);
                                FlagOverflow = ((((RegA ^ temp) & 0x80) != 0 && ((RegA ^ Read(addr)) & 0x80) != 0) ? 1 : 0);
                                RegA = FlagSign = FlagZero = (temp & 0xFF);
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
                                Write(addr, value);
                                RegA |= value;
                                FlagSign = FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrSRE:
                                value = Read(addr);
                                FlagCarry = value & 1;
                                value = value >> 1;
                                Write(addr, value);
                                RegA ^= value;
                                FlagSign = FlagZero = RegA;
                                break;
                            case OpInfo.InstrDummy:
                                    debug.SetError("Missing OP");
                                    debug.LogInfo("Missing OP: " + OpInfo.GetOpNames()[OpInfo.GetOps()[op] & 0xFF] + " " + op.ToString("X2") + " Program Counter: " + RegPC.ToString("X4"));
                                break;
                        }
                        break;
                }
                #endregion
                counter += opCycles;
                APU.AddCycles(opCycles);
                PPU.AddCycles(opCycles);
                debug.AddCycles(opCycles);
                if (mapper.cycleIRQ)
                    mapper.IRQ(opCycles);
#if !nestest
                if (interruptBRK)
                {
                    PushWordStack((RegPC + 1) & 0xFFFF);
                    PushByteStack(PToByte() | 0x30);
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFE);
                    interruptBRK = false;
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
                else if ((mapper.interruptMapper || APU.frameIRQ || APU.dmcInterrupt) && FlagIRQ == 0)
                {
                    PushWordStack(RegPC);
                    FlagBreak = 0;
                    PushByteStack(PToByte());
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFE);
                }
                else if (interruptReset)
                {
                    PushWordStack(RegPC);
                    PushByteStack(PToByte());
                    FlagIRQ = 1;
                    RegPC = PeekWord(0xFFFC);
                    interruptReset = false;
                    APU.Reset();
                    PPU.Reset();
                }
#endif
            }
            if (debug.debugInterrupt && (PPU.scanlineCycle < 256 && PPU.scanline > -1 && PPU.scanline < 240))
                PPU.screen[PPU.scanlineCycle, PPU.scanline] |= 0x1C0;
            emulationRunning = false;
            PPU.frameComplete = false;
            PPU.generateNameTables = false;
            PPU.generatePatternTables = false;
            APU.Update();
        }
        public NESCore(SystemType region, String input, String fdsImage, String cartDBLocation, int sampleRate, int frameBuffer, bool ignoreFileCheck = false) //FDS Load
        {
            this.nesRegion = region;
            opList = OpInfo.GetOps();
            if (!File.Exists(input))
            {
                throw new Exception("FDS BIOS not found.");
            }
            Stream biosStream = File.OpenRead(input);
            Stream diskStream = File.OpenRead(fdsImage);
            rom.filePath = fdsImage;
            rom.fileName = Path.GetFileNameWithoutExtension(rom.filePath);
            rom.mapper = 20;
            Memory = new MemoryStore(0x40, true);
            Memory.swapOffset = 0x20;
            Memory.SetReadOnly(0, 2, false);
            APU = new APU(this, sampleRate, frameBuffer);
            PPU = new PPU(this);
            debug = new Debug(this);
            debug.LogInfo(rom.filePath);
            debug.LogInfo("Mapper: " + rom.mapper);
            if (biosStream.ReadByte() == 'N' && biosStream.ReadByte() == 'E' && biosStream.ReadByte() == 'S' && biosStream.ReadByte() == 0x1A)
                biosStream.Position = 0x6010;
            else
                biosStream.Position = 0;
            for (int i = 0x00; i < 8 * 0x400; i++)
            {
                Memory.banks[(i / 0x400) + Memory.swapOffset][i % 0x400] = (byte)biosStream.ReadByte();
            }
            mapper = new Mappers.m020(this, diskStream, ignoreFileCheck);
            rom.crc = ((Mappers.m020)mapper).crc; //I don't like this.
            debug.LogInfo("ROM CRC32: " + rom.crc.ToString("X8"));
            diskStream.Close();
            biosStream.Close();
            for (int i = 0; i < 0x10000; i++)
            {
                this.MirrorMap[i] = (ushort)i;
            }
            this.CPUMirror(0x0000, 0x0800, 0x0800, 3);
            this.CPUMirror(0x2000, 0x2008, 0x08, 0x3FF);
            Power();

        }
        public NESCore(SystemType region, String input, String cartDBLocation, int sampleRate, int frameBuffer, bool ignoreFileCheck = false) : 
            this(region, File.OpenRead(input), cartDBLocation, sampleRate, frameBuffer, ignoreFileCheck)
        {
            rom.filePath = input;
            rom.fileName = Path.GetFileNameWithoutExtension(rom.filePath);
            debug.LogInfo(rom.filePath);
        }
        public NESCore(SystemType region, Stream inputStream, String cartDBLocation, int sampleRate, int frameBuffer, bool ignoreFileCheck = false)
        {
            this.nesRegion = region;
            opList = OpInfo.GetOps();
            inputStream.Position = 0;
            if (!ignoreFileCheck)
            {
                if (inputStream.ReadByte() != 'N' || inputStream.ReadByte() != 'E' || inputStream.ReadByte() != 'S' || inputStream.ReadByte() != 0x1A)
                {
                    inputStream.Close();
                    throw (new Exception("Invalid File"));
                }
            }
            inputStream.Position = 0x4;
            rom.prgROM = inputStream.ReadByte() * 16;
            rom.vROM = inputStream.ReadByte() * 8;
            int lowMapper = inputStream.ReadByte();
            int highMapper = inputStream.ReadByte();
            inputStream.Position = 0x0F;
            if (inputStream.ReadByte() != 0)
                highMapper = 0;
            rom.trainer = ((lowMapper & 0x04) != 0);
            if (((lowMapper & 0x08) != 0))
                rom.mirroring = Mirroring.fourScreen;
            else if (((lowMapper & 0x01) != 0))
                rom.mirroring = Mirroring.vertical;
            else
                rom.mirroring = Mirroring.horizontal;
            rom.sRAM = ((lowMapper & 0x02) != 0);
            rom.PC10 = ((highMapper & 0x02) != 0);
            rom.vsUnisystem = ((highMapper & 0x01) != 0);
            rom.mapper = (lowMapper >> 4) + (highMapper & 0xF0);
            if (rom.mapper == 5)
                Memory = new MemoryStore(0x20 + rom.prgROM + 64, true); //give mmc5 64kb prgram to simplify things
            else
                Memory = new MemoryStore(0x20 + rom.prgROM, true);
            Memory.swapOffset = 0x20;
            Memory.SetReadOnly(0, 2, false);
            APU = new APU(this, sampleRate, frameBuffer);
            PPU = new PPU(this);
            debug = new Debug(this);
            debug.LogInfo("Mapper: " + rom.mapper);
            debug.LogInfo("PRG-ROM: " + rom.prgROM.ToString() + "KB");
            debug.LogInfo("CHR-ROM: " + rom.vROM.ToString() + "KB");
            debug.LogInfo("Mirroring: " + (rom.mirroring == Mirroring.fourScreen ? "Four-screen" : (rom.mirroring == Mirroring.vertical ? "Vertical" : "Horizontal")));
            if(rom.vsUnisystem)
                debug.LogInfo("VS Unisystem Game");
            if (rom.sRAM)
            {
                debug.LogInfo("SRAM Present");
                Memory.SetReadOnly(0x6000, 8, false);
            }
            inputStream.Position = 0x10;
            if (rom.trainer)
            {
                debug.LogInfo("Trainer Present");
                for (int i = 0x00; i < 0x200; i++)
                {
                    this.Memory[i + 0x7000] = (byte)inputStream.ReadByte();
                }
            }
            rom.crc = 0xFFFFFFFF;
            for (int i = 0x00; i < rom.prgROM * 0x400; i++)
            {
                byte nextByte = (byte)inputStream.ReadByte();
                Memory.banks[(i / 0x400) + Memory.swapOffset][i % 0x400] = nextByte;
                rom.crc = CRC32.crc32_adjust(rom.crc, nextByte);
            }
            for (int i = 0x00; i < rom.vROM * 0x400; i++)
            {
                byte nextByte = (byte)inputStream.ReadByte();
                PPU.PPUMemory.banks[(i / 0x400) + PPU.PPUMemory.swapOffset][i % 0x400] = nextByte;
                rom.crc = CRC32.crc32_adjust(rom.crc, nextByte);
            }
            rom.crc = rom.crc ^ 0xFFFFFFFF;
            debug.LogInfo("ROM CRC32: " + rom.crc.ToString("X8"));
            if (rom.PC10)
            {
                debug.LogInfo("PC10 Game");
                for (int i = 0x00; i < 0x2000; i++)
                {
                    inputStream.ReadByte();
                }
            }
            for (int i = 0x00; i < 0x80 && inputStream.Position < inputStream.Length; i++)
            {
                byte titleChar = (byte)inputStream.ReadByte();
                if (titleChar != 255)
                    rom.title += (char)titleChar;
            }
            if (rom.title != null)
                debug.LogInfo("Name: " + rom.title);
            inputStream.Close();

            if (rom.mirroring == Mirroring.fourScreen)
            {
                PPU.PPUMemory.FourScreenMirroring();
                PPU.PPUMemory.hardwired = true;
            }
            else if (rom.mirroring == Mirroring.vertical)
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
                string system = "";
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
                                        if (xmlReader.Name == "system")
                                            system = xmlReader.Value;
                                        if (xmlReader.Name == "crc")
                                            if (xmlReader.Value == rom.crc.ToString("X8"))
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
                    rom.title = gameName;
                    rom.mapper = Convert.ToInt32(dbMapper);
                    rom.board = board;
                    debug.LogInfo("Found in database");
                    debug.LogInfo("Name: " + rom.title);
                    debug.LogInfo("Board: " + board);
                    debug.LogInfo("Mapper: " + rom.mapper);
                    switch (system)
                    {
                        case "Famicom":
                        case "NES-NTSC":
                        default:
                            debug.LogInfo("System: " + system);
                            break;
                        case "NES-PAL-A":
                        case "NES-PAL-B":
                            debug.LogInfo("System: " + system);
                            break;
                    }
                }
                else
                {
                    debug.LogInfo("No database entry");
                }
            }
            else
            {
                debug.LogInfo("NesCarts.xml not found");
            }
            #region mappers
            switch (rom.mapper)
            {
                case 0://NROM
                    mapper = new Mappers.m000(this);
                    break;
                case 1: //MMC1
                    mapper = new Mappers.m001(this);
                    break;
                case 2: //UNROM
                    mapper = new Mappers.m002(this);
                    break;
                case 3://CNROM
                    mapper = new Mappers.m003(this);
                    break;
                case 206:
                case 37:
                case 4: //MMC3
                    mapper = new Mappers.m004(this);
                    break;
                case 5: //MMC5
                    mapper = new Mappers.m005(this);
                    break;
                case 7: //AOROM
                    mapper = new Mappers.m007(this);
                    break;
                case 9: //MMC2
                    mapper = new Mappers.m009(this);
                    break;
                case 10: //MMC4
                    mapper = new Mappers.m010(this);
                    break;
                case 11: //Color Dreams
                    mapper = new Mappers.m011(this);
                    break;
                case 16: //Bandai - EEPROM
                case 159:
                case 153: //I dont think 153 belongs here but bootgod's xml reports Dragon Ball as 153 and I know that's 16
                    mapper = new Mappers.m016(this);
                    break;
                case 19:
                case 210:
                    mapper = new Mappers.m019(this);
                    break;
                case 21: //VRC4a, VRC4c
                    mapper = new Mappers.mVRC4(this, 0x00, 0x02, 0x04, 0x06, 0x00, 0x40, 0x80, 0xC0);
                    break;
                case 22: //VRC2a
                    mapper = new Mappers.m022(this);
                    break;
                case 23: //VRC4e, VRC4f
                    mapper = new Mappers.mVRC4(this, 0x00, 0x04, 0x08, 0x0C, 0x00, 0x01, 0x02, 0x03);
                    break;
                case 24: //VRC6a
                    mapper = new Mappers.mVRC6(this, 0x00, 0x01, 0x02, 0x03);
                    break;
                case 25: //VRC4b, VRC4d
                    mapper = new Mappers.mVRC4(this, 0x00, 0x02, 0x01, 0x03, 0x00, 0x08, 0x04, 0x0C);
                    break;
                case 26: //VRC6b
                    mapper = new Mappers.mVRC6(this, 0x00, 0x03, 0x02, 0x01);
                    break;
                case 33:
                    mapper = new Mappers.m033(this);
                    break;
                case 34: //BNROM and NINA-001
                    mapper = new Mappers.m034(this);
                    break;
                case 48:
                    mapper = new Mappers.m048(this);
                    break;
                case 66: //GxROM
                    mapper = new Mappers.m066(this);
                    break;
                case 67: //Sunsoft3
                    mapper = new Mappers.m067(this);
                    break;
                case 68: //Sunsoft4
                    mapper = new Mappers.m068(this);
                    break;
                case 69: //Sunsoft5
                    mapper = new Mappers.m069(this);
                    break;
                case 70: //Bandai
                    mapper = new Mappers.m070(this);
                    break;
                case 71: //Camerica
                    mapper = new Mappers.m071(this);
                    break;
                case 73: //VRC3
                    mapper = new Mappers.m073(this);
                    break;
                case 75: //VRC1
                    mapper = new Mappers.m075(this);
                    break;
                case 79:
                    mapper = new Mappers.m079(this);
                    break;
                case 80:
                    mapper = new Mappers.m080(this);
                    break;
                case 85: //VRC7a, VRC7b
                    mapper = new Mappers.m085(this, 0x10, 0x08);
                    break;
                case 86: //Jaleco
                    mapper = new Mappers.m086(this);
                    break;
                case 87: //Jaleco/Konami
                    mapper = new Mappers.m087(this);
                    break;
                case 99: //VS Unisystem
                    mapper = new Mappers.m099(this);
                    break;
                case 113:
                    mapper = new Mappers.m113(this);
                    break;
                case 151: //VS Unisystem
                    mapper = new Mappers.m151(this);
                    break;
                case 152:
                    mapper = new Mappers.m152(this);
                    break;
                case 184://Sunsoft
                    mapper = new Mappers.m184(this);
                    break;
                case 207:
                    mapper = new Mappers.m207(this);
                    break;
                case 226: //76 in 1
                    mapper = new Mappers.m226(this);
                    break;
                case 228: //Action 52, Cheetah Men II
                    mapper = new Mappers.m228(this);
                    break;
                default:
                    debug.SetError("Mapper Unsupported");
                    debug.LogInfo("This game will probably not load, mapper unsupported.\r\nMapper:" + rom.mapper.ToString() + " PRG-ROM:" + rom.prgROM.ToString() + "KB CHR-ROM:" + rom.vROM.ToString() + "KB");
                    goto case 0;

            }
            #endregion
            for (int i = 0; i < 0x10000; i++)
            {
                this.MirrorMap[i] = (ushort)i;
            }
            this.CPUMirror(0x0000, 0x0800, 0x0800, 3);
            this.CPUMirror(0x2000, 0x2008, 0x08, 0x3FF);
            Power();
#if nestest
            RegPC = 0xC000;
#endif
        }
        public byte Read(int address)
        {
            address = this.MirrorMap[address & 0xFFFF];
            byte nextByte = this.Memory[address];

            nextByte = PortOne.Read(nextByte, (ushort)address);
            nextByte = PortTwo.Read(nextByte, (ushort)address);
            if (rom.vsUnisystem)
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
                    if (players[0].coin)
                        nextByte |= 0x20;
                    else
                        nextByte &= 0xDF;
                    if (players[1].coin)
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
            nextByte = mapper.Read(nextByte, (ushort)address);
            nextByte = APU.Read(nextByte, (ushort)address);
            nextByte = PPU.Read(nextByte, (ushort)address);
            nextByte = debug.Read(nextByte, (ushort)address);
            nextByte = GameGenie(nextByte, (ushort)address);
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

            PortOne.Write((byte)value, (ushort)address);
            PortTwo.Write((byte)value, (ushort)address);
            if (rom.vsUnisystem)
            {
                if (address == 0x4020)
                {
                    if ((value & 1) != 0)
                    {
                        players[0].coin = false;
                        players[1].coin = false;
                    }
                }
            }
            mapper.Write((byte)value, (ushort)address);
            APU.Write((byte)value, (ushort)address);
            PPU.Write((byte)value, (ushort)address);
            debug.Write((byte)value, (ushort)address);
            Memory[address] = (byte)value;
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
            if ((FlagSign >> 7) != 0) value |= 0x80;
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
            FlagSign =      ((p >> 7) & 1) != 0 ? 0x80 : 0;
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
            Write((ushort)(RegS + 0x0100), value);
            RegS--;
            RegS &= 0xFF;
        }
        private byte PopByteStack()
        {
            RegS++;
            RegS &= 0xFF;
            return Read((ushort)(RegS + 0x0100));
        }
        public void AddCycles(int value)
        {
            counter += value;
            APU.AddCycles(value);
            PPU.AddCycles(value);
            debug.AddCycles(value);
            if (mapper.cycleIRQ)
                mapper.IRQ(value);
        }
        private void CPUMirror(ushort address, ushort mirrorAddress, ushort length, int repeat)
        {
            for (int j = 0; j < repeat; j++)
                for (int i = 0; i < length; i++)
                    this.MirrorMap[mirrorAddress + i + (j * length)] = (ushort)(this.MirrorMap[address + i]);
        }
        public void EjectDisk(bool diskInserted)
        {
            if (rom.mapper == 20)
            {
                ((Mappers.m020)mapper).EjectDisk(diskInserted);
            }
        }
        public void SetDiskSide(int diskSide)
        {
            if (rom.mapper == 20)
            {
                ((Mappers.m020)mapper).SetDiskSide(diskSide);
            }
        }
        public bool GetEjectDisk()
        {
            if (rom.mapper == 20)
            {
                return ((Mappers.m020)mapper).diskInserted;
            }
            return false;
        }
        public int GetDiskSide()
        {
            if (rom.mapper == 20)
            {
                return ((Mappers.m020)mapper).currentSide;
            }
            return 0;
        }
        public int GetSideCount()
        {
            if (rom.mapper == 20)
            {
                return ((Mappers.m020)mapper).sideCount;
            }
            return 0;
        }
        public void SetControllers(ControllerType portOne, ControllerType portTwo, bool fourScore)
        {
            this.fourScore = fourScore;
            switch (portOne)
            {
                case ControllerType.Controller:
                    PortOne = new Inputs.Controller(this, Inputs.Port.PortOne);
                    break;
                case ControllerType.Zapper:
                    PortOne = new Inputs.Zapper(this, Inputs.Port.PortOne);
                    break;
                case ControllerType.Paddle:
                    PortOne = new Inputs.Paddle(this, Inputs.Port.PortOne);
                    break;
                case ControllerType.FamiPaddle:
                    PortOne = new Inputs.FamiPaddle(this, Inputs.Port.PortOne);
                    break;
                default:
                case ControllerType.Empty:
                    PortOne = new Inputs.Empty();
                    break;
            }
            switch (portTwo)
            {
                case ControllerType.Controller:
                    PortTwo = new Inputs.Controller(this, Inputs.Port.PortTwo);
                    break;
                case ControllerType.Zapper:
                    PortTwo = new Inputs.Zapper(this, Inputs.Port.PortTwo);
                    break;
                case ControllerType.Paddle:
                    PortTwo = new Inputs.Paddle(this, Inputs.Port.PortTwo);
                    break;
                case ControllerType.FamiPaddle:
                    PortTwo = new Inputs.FamiPaddle(this, Inputs.Port.PortTwo);
                    break;
                default:
                case ControllerType.Empty:
                    PortTwo = new Inputs.Empty();
                    break;
            }
        }
        public SaveState StateSave()
        {
            SaveState newState = new SaveState();
            newState.stateStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(newState.stateStream);
            writer.Seek(0, SeekOrigin.Begin);
            writer.Write(RegPC);
            writer.Write(RegA);
            writer.Write(RegX);
            writer.Write(RegY);
            writer.Write(RegS);
            writer.Write(PToByte());
            writer.Write(counter);
            writer.Write(interruptReset);
            writer.Write(mapper.interruptMapper);
            PortOne.StateSave(writer);
            PortTwo.StateSave(writer);
            Memory.StateSave(writer);
            mapper.StateSave(writer);
            PPU.StateSave(writer);
            APU.StateSave(writer);
            newState.isStored = true;
            return newState;
        }
        public void StateLoad(SaveState oldState)
        {
            BinaryReader reader = new BinaryReader(oldState.stateStream);
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            RegPC = reader.ReadInt32();
            RegA = reader.ReadInt32();
            RegX = reader.ReadInt32();
            RegY = reader.ReadInt32();
            RegS = reader.ReadInt32();
            PFromByte(reader.ReadByte());
            counter = reader.ReadInt32();
            interruptReset = reader.ReadBoolean();
            mapper.interruptMapper = reader.ReadBoolean();
            PortOne.StateLoad(reader);
            PortTwo.StateLoad(reader);
            Memory.StateLoad(reader);
            mapper.StateLoad(reader);
            PPU.StateLoad(reader);
            APU.StateLoad(reader);
        }
        public void Power()
        {
            RegA = 0;
            RegX = 0;
            RegY = 0;
            RegS = 0xFD;
            RegPC = 0;
            FlagCarry = 0; //Bit 0 of P
            FlagZero = 1; //backwards
            FlagIRQ = 1;
            FlagDecimal = 0;
            FlagBreak = 1;
            FlagNotUsed = 1;
            FlagOverflow = 0;
            FlagSign = 0; //Bit 7 of P
            counter = 0;
            for (int i = 0; i < 0x800; i++)
            {
                Memory[i] = 0;
            }
            interruptReset = false;
            interruptBRK = false;
            mapper.Power();
            PPU.Power();
            APU.Power();
            RegPC = PeekWord(0xFFFC);//entry point
        }
        public void Reset()
        {
            this.interruptReset = true;
        }
        private byte GameGenie(byte value, ushort address)
        {
            for (int i = 0; i < this.gameGenieCodeNum; i++)
            {
                if (gameGenieCodes[i].address == address)
                {
                    if (this.gameGenieCodes[i].code == "DUMMY")
                        return this.gameGenieCodes[i].value;
                    else if (this.gameGenieCodes[i].code.Length == 6)
                        return this.gameGenieCodes[i].value;
                    else if (this.gameGenieCodes[i].code.Length == 8 && value == this.gameGenieCodes[i].check)
                        return this.gameGenieCodes[i].value;
                }
            }
            return value;
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
    public enum SystemType
    {
        NTSC,
        PAL,
        Dendy
    }
    public struct Controller
    {
        public bool up;
        public bool down;
        public bool start;
        public bool select;
        public bool left;
        public bool right;
        public bool a;
        public bool b;
        public bool coin;
        public bool triggerPulled;
        public byte x;
        public byte y;
    }
    public enum ControllerType
    {
        Controller,
        Zapper,
        Paddle,
        FamiPaddle,
        Empty
    }
    public struct SoundVolume
    {
        public float master;
        public float pulse1;
        public float pulse2;
        public float triangle;
        public float noise;
        public float dmc;
    }
    public struct Rom
    {
        public int mapper;
        public string board;
        public string fileName;
        public string filePath;
        public string title;
        public int prgROM;
        public int vROM;
        public bool sRAM;
        public bool vsUnisystem;
        public bool PC10;
        public bool trainer;
        public UInt32 crc;
        public Mirroring mirroring;

    }
    public enum Mirroring
    {
        horizontal,
        vertical,
        fourScreen,
        singleScreen
    }

    public struct SaveState
    {
        public MemoryStream stateStream;
        public bool isStored;
        public int frame;
    }
}
