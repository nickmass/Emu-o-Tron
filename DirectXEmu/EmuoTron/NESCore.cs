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

        public bool nsfPlayer;

        public Rom rom;
        public Mappers.Mapper mapper;
        public APU APU;
#if SCANLINE_PPU
        public SPPU PPU;
#else
        public PPU PPU;
#endif
        public Debug debug;

        public MemoryStore Memory;
        public int RegA = 0;
        public int RegX = 0;
        public int RegY = 0;
        public int RegS = 0xFD;
        public int RegPC = 0;
        public int RegP //Don't bother with bits 4 and 5 they dont really exist and just make things complicated.
        {
            get
            {
                byte value = 0;
                if (FlagCarry != 0) value |= 0x01;
                if (FlagZero == 0) value |= 0x02;
                if (FlagIRQ != 0) value |= 0x04;
                if (FlagDecimal != 0) value |= 0x08;
                if (FlagOverflow != 0) value |= 0x40;
                if ((FlagSign >> 7) != 0) value |= 0x80;
                return value;
            }
            set
            {
                FlagCarry = value & 1;
                FlagZero = ((value >> 1) & 1) == 0 ? 1 : 0;
                FlagIRQ = ((value >> 2) & 1);
                FlagDecimal = ((value >> 3) & 1);
                FlagOverflow = ((value >> 6) & 1);
                FlagSign = ((value >> 7) & 1) != 0 ? 0x80 : 0;
            }
        }
        public int FlagCarry = 0; //Bit 0 of P
        public int FlagZero = 1; //backwards
        public int FlagIRQ = 1;
        public int FlagDecimal = 0;
        public int FlagOverflow = 0;
        public int FlagSign = 0; //Bit 7 of P
        public int counter = 0;
        private bool interruptReset = false;
        private int pendingFlagIRQ1 = 0;
        private int pendingFlagIRQ2 = 0;
        private int pendingFlagIRQValue1 = 0;
        private int pendingFlagIRQValue2 = 0;
        private int delayedFlagIRQ = 0;
        private byte lastRead = 0;

        private OpInfo OpCodes = new OpInfo();
        private int[] opList;

        public Controller[] players = new Controller[4];
        private Inputs.Input PortOne = new Inputs.Empty();
        private Inputs.Input PortTwo = new Inputs.Empty();
        private Inputs.Input Expansion = new Inputs.Empty();
        public bool fourScore;
        public bool filterIllegalInput;
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
            PPU.turbo = turbo;
            APU.turbo = turbo;
            Start();
        }

        public void Start(Controller player1, Controller player2, bool turbo)
        {
            players[0] = player1;
            players[1] = player2;
            PPU.turbo = turbo;
            APU.turbo = turbo;
            Start();
        }
        public void Start(Controller player1, bool turbo)
        {
            players[0] = player1;
            PPU.turbo = turbo;
            APU.turbo = turbo;
            Start();
        }
        public void Start()
        {
            bool emulationRunning = true;
            int op;
            int opInfo;
            int addressing;
            int instruction;
            int opAddr;
            int addr = 0;
            int temp;
            int value;
            int dummy;
            APU.ResetBuffer();
            while (!PPU.frameComplete && !debug.debugInterrupt && emulationRunning)
            {
#if DEBUGGER
                debug.Execute((ushort)RegPC);
#endif
                op = Read(RegPC);
                opInfo = opList[op];
                //opCycles = (opInfo >> 20) & 0xF;
                addressing = (opInfo >> 8) & 0xFF;
                instruction = opInfo & 0xFF;
                dummy = (opInfo >> 24) & 0xFF;
                opAddr = RegPC;
                RegPC += (opInfo >> 16) & 0xF;
                RegPC &= 0xFFFF;

                #region CPU
                switch (addressing)
                {
                    case OpInfo.AddrNone:
                        Read(opAddr + 1);
                        break;
                    case OpInfo.AddrAccumulator:
                        Read(opAddr + 1);
                        addr = RegA;
                        break;
                    case OpInfo.AddrImmediate:
                        addr = opAddr + 1;
                        break;
                    case OpInfo.AddrZeroPage:
                        addr = Read(opAddr + 1);
                        break;
                    case OpInfo.AddrZeroPageX:
                        addr = Read(opAddr + 1);
                        Read(addr);
                        addr = (addr + RegX) & 0xFF;
                        break;
                    case OpInfo.AddrZeroPageY:
                        addr = Read(opAddr + 1);
                        Read(addr);
                        addr = (addr + RegY) & 0xFF;
                        break;
                    case OpInfo.AddrAbsolute:
                        addr = ReadWord(opAddr + 1);
                        break;
                    case OpInfo.AddrAbsoluteX:
                        addr = ReadWord(opAddr + 1);
                        if ((addr & 0xFF00) != ((addr + RegX) & 0xFF00))
                        {
                            if (dummy == OpInfo.DummyOnCarry)
                                Read((addr & 0xFF00) | ((addr + RegX) & 0xFF));

                        }
                        if (dummy == OpInfo.DummyAlways)
                            Read((addr & 0xFF00) | ((addr + RegX) & 0xFF));

                        addr += RegX;
                        break;
                    case OpInfo.AddrAbsoluteY:
                        addr = ReadWord(opAddr + 1);
                        if ((addr & 0xFF00) != ((addr + RegY) & 0xFF00))
                        {
                            if (dummy == OpInfo.DummyOnCarry)
                                Read((addr & 0xFF00) | ((addr + RegY) & 0xFF));

                        }
                        if (dummy == OpInfo.DummyAlways)
                            Read((addr & 0xFF00) | ((addr + RegY) & 0xFF));
                        addr += RegY;
                        break;
                    case OpInfo.AddrIndirectAbs:
                        addr = ReadWordWrap(ReadWord(opAddr + 1));
                        break;
                    case OpInfo.AddrRelative:
                        addr = Read(opAddr + 1);
                        break;
                    case OpInfo.AddrIndirectX:
                        addr = Read(opAddr + 1);
                        Read(addr);
                        addr += RegX;
                        addr &= 0xFF;
                        addr = ReadWordWrap(addr);
                        break;
                    case OpInfo.AddrIndirectY:
                        addr = ReadWordWrap(Read(opAddr + 1));
                        if ((addr & 0xFF00) != ((addr + RegY) & 0xFF00))
                        {
                            if (dummy == OpInfo.DummyOnCarry)
                                Read((addr & 0xFF00) | ((addr + RegY) & 0xFF));

                        }
                        if (dummy == OpInfo.DummyAlways)
                            Read((addr & 0xFF00) | ((addr + RegY) & 0xFF));
                        addr += RegY;
                        break;
                }
                addr &= 0xFFFF;
                switch (instruction)
                {
                    case OpInfo.InstrADC:
                        value = Read(addr);
                        temp = RegA + value + FlagCarry;
                        FlagOverflow = ((~(RegA ^ value) & (RegA ^ temp)) >> 7) & 1;
                        FlagCarry = temp > 0xFF ? 1 : 0;
                        RegA = FlagSign = FlagZero = temp & 0xFF;
                        break;
                    case OpInfo.InstrAND:
                        RegA &= Read(addr);
                        FlagSign = FlagZero = RegA;
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
                            Write(addr, value);
                            FlagCarry = (value >> 7) & 1;
                            value = (value << 1) & 0xFF;
                            FlagSign = FlagZero = value;
                            Write(addr, value);
                        }
                        break;
                    case OpInfo.InstrBCC:
                        if (FlagCarry == 0)
                        {
                            Read(RegPC);
                            if (addr < 0x80)
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr) & 0x00FF));
                                addr += RegPC;
                            }
                            else
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC - 256) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr - 256) & 0x00FF));
                                addr += RegPC - 256;
                            }
                            RegPC = addr;
                        }
                        break;
                    case OpInfo.InstrBCS:
                        if (FlagCarry != 0)
                        {
                            Read(RegPC);
                            if (addr < 0x80)
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr) & 0x00FF));
                                addr += RegPC;
                            }
                            else
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC - 256) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr - 256) & 0x00FF));
                                addr += RegPC - 256;
                            }
                            RegPC = addr;
                        }
                        break;
                    case OpInfo.InstrBEQ:
                        if (FlagZero == 0)
                        {
                            Read(RegPC);
                            if (addr < 0x80)
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr) & 0x00FF));
                                addr += RegPC;
                            }
                            else
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC - 256) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr - 256) & 0x00FF));
                                addr += RegPC - 256;
                            }
                            RegPC = addr;
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
                            Read(RegPC);
                            if (addr < 0x80)
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr) & 0x00FF));
                                addr += RegPC;
                            }
                            else
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC - 256) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr - 256) & 0x00FF));
                                addr += RegPC - 256;
                            }
                            RegPC = addr;
                        }
                        break;
                    case OpInfo.InstrBNE:
                        if (FlagZero != 0)
                        {
                            Read(RegPC);
                            if (addr < 0x80)
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr) & 0x00FF));
                                addr += RegPC;
                            }
                            else
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC - 256) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr - 256) & 0x00FF));
                                addr += RegPC - 256;
                            }
                            RegPC = addr;
                        }
                        break;
                    case OpInfo.InstrBPL:
                        if ((FlagSign >> 7) == 0)
                        {
                            Read(RegPC);
                            if (addr < 0x80)
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr) & 0x00FF));
                                addr += RegPC;
                            }
                            else
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC - 256) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr - 256) & 0x00FF));
                                addr += RegPC - 256;
                            }
                            RegPC = addr;
                        }
                        break;
                    case OpInfo.InstrBRK:
                        Read(addr);
                        PushWordStack((RegPC) & 0xFFFF);
                        PushByteStack(RegP | 0x30);
                        FlagIRQ = 1;
                        delayedFlagIRQ = 1;
                        pendingFlagIRQ1 = 0;
                        pendingFlagIRQ2 = 0;
                        RegPC = ReadWord(0xFFFE);

                        break;
                    case OpInfo.InstrBVC:
                        if (FlagOverflow == 0)
                        {
                            Read(RegPC);
                            if (addr < 0x80)
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr) & 0x00FF));
                                addr += RegPC;
                            }
                            else
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC - 256) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr - 256) & 0x00FF));
                                addr += RegPC - 256;
                            }
                            RegPC = addr;
                        }
                        break;
                    case OpInfo.InstrBVS:
                        if (FlagOverflow != 0)
                        {
                            Read(RegPC);
                            if (addr < 0x80)
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr) & 0x00FF));
                                addr += RegPC;
                            }
                            else
                            {
                                if ((RegPC & 0xFF00) != ((addr + RegPC - 256) & 0xFF00))
                                    Read((RegPC & 0xFF00) | ((RegPC + addr - 256) & 0x00FF));
                                addr += RegPC - 256;
                            }
                            RegPC = addr;
                        }
                        break;
                    case OpInfo.InstrCLC:
                        FlagCarry = 0;
                        break;
                    case OpInfo.InstrCLD:
                        FlagDecimal = 0;
                        break;
                    case OpInfo.InstrCLI:
                        if (pendingFlagIRQ1 != 0)
                        {
                            pendingFlagIRQ2 = 2;
                            pendingFlagIRQValue2 = 0;
                        }
                        else
                        {
                            pendingFlagIRQ1 = 2;
                            pendingFlagIRQValue1 = 0;
                        }
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
                        Write(addr, value);
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
                        break;
                    case OpInfo.InstrINC:
                        value = Read(addr);
                        Write(addr, value);
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
                        Read(RegS | 0x0100);
                        PushWordStack(RegPC - 1);
                        RegPC = addr;
                        break;
                    case OpInfo.InstrLDA:
                        RegA = FlagSign = FlagZero = Read(addr);
                        break;
                    case OpInfo.InstrLDX:
                        RegX = FlagSign = FlagZero = Read(addr);
                        break;
                    case OpInfo.InstrLDY:
                        RegY = FlagSign = FlagZero = Read(addr);
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
                            Write(addr, value);
                            FlagCarry = value & 1;
                            value = FlagSign = FlagZero = value >> 1;
                            Write(addr, value);
                        }
                        break;
                    case OpInfo.InstrNOP:
                        break;
                    case OpInfo.InstrORA:
                        RegA = FlagSign = FlagZero = (RegA | Read(addr)) & 0xFF;
                        break;
                    case OpInfo.InstrPHA:
                        PushByteStack(RegA);
                        break;
                    case OpInfo.InstrPHP:
                        PushByteStack(RegP | 0x30);
                        break;
                    case OpInfo.InstrPLA:
                        Read(RegS | 0x0100);
                        RegA = FlagSign = FlagZero = PopByteStack();
                        break;
                    case OpInfo.InstrPLP:
                        Read(RegS | 0x0100);
                        RegP = PopByteStack();
                        if (pendingFlagIRQ1 != 0)
                        {
                            pendingFlagIRQ2 = 2;
                            pendingFlagIRQValue2 = FlagIRQ;
                        }
                        else
                        {
                            pendingFlagIRQ1 = 2;
                            pendingFlagIRQValue1 = FlagIRQ;
                        }
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
                            Write(addr, value);
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
                            Write(addr, value);
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
                        Read(RegS | 0x100);
                        RegP = PopByteStack();
                        delayedFlagIRQ = FlagIRQ;
                        RegPC = PopWordStack();
                        pendingFlagIRQ1 = 0;
                        pendingFlagIRQ2 = 0;
                        break;
                    case OpInfo.InstrRTS:
                        Read(RegS | 0x0100);
                        RegPC = (PopWordStack() + 1) & 0xFFFF;
                        Read(RegPC);
                        if(nsfPlayer && RegS == 0xFF)
                        {
                            PushWordStack(RegPC - 1);
                            ((Mappers.mNSF) mapper).readOut = true;
                            int i = 0;
                            while ((int)((Mappers.mNSF)mapper).counter > 0)
                            {
                                if(i % 3 == 0)
                                    Write(0xFFFF,0xFF);
                                else
                                    Read(0x0000);
                                i++;
                            }
                            ((Mappers.mNSF)mapper).readOut = false;
                            PPU.frameComplete = true;
                            ((Mappers.mNSF)mapper).counter = ((Mappers.mNSF)mapper).speed;
                        }
                        break;
                    case OpInfo.InstrSBC:
                        value = Read(addr);
                        temp = RegA - value - (1 - FlagCarry);
                        FlagCarry = (temp < 0 ? 0 : 1);
                        FlagOverflow = (((RegA ^ value) & (RegA ^ temp)) >> 7) & 1;
                        RegA = FlagSign = FlagZero = (temp & 0xFF);
                        break;
                    case OpInfo.InstrSEC:
                        FlagCarry = 1;
                        break;
                    case OpInfo.InstrSED:
                        FlagDecimal = 1;
                        break;
                    case OpInfo.InstrSEI:
                        if (pendingFlagIRQ1 != 0)
                        {
                            pendingFlagIRQ2 = 2;
                            pendingFlagIRQValue2 = 1;
                        }
                        else
                        {
                            pendingFlagIRQ1 = 2;
                            pendingFlagIRQValue1 = 1;
                        }
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
                            case OpInfo.IllInstrAHX:
                                Read(addr); //Timing filler
                                break;
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
                                Write(addr, value);
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
                                Write(addr, value);
                                value = (value + 1) & 0xFF;
                                Write(addr, value);
                                temp = RegA - value - (1 - FlagCarry);
                                FlagCarry = (temp < 0 ? 0 : 1);
                                FlagOverflow = (((RegA ^ value) & (RegA ^ temp)) >> 7) & 1;
                                RegA = FlagSign = FlagZero = (temp & 0xFF);
                                break;
                            case OpInfo.IllInstrKIL:
                                debug.SetError("KIL encountered");
                                //SHOULD crash CPU, but Im going to treat it as a NOP.
                                break;
                            case OpInfo.IllInstrLAS: //Filler
                                Read(addr);
                                break;
                            case OpInfo.IllInstrLAX:
                                RegA = RegX = FlagSign = FlagZero = Read(addr);
                                break;
                            case OpInfo.IllInstrNOP:
                                if (addressing == OpInfo.AddrImmediate || addressing == OpInfo.AddrZeroPage || addressing == OpInfo.AddrAbsolute || addressing == OpInfo.AddrZeroPageX || addressing == OpInfo.AddrAbsoluteX)
                                    Read(addr);
                                break;
                            case OpInfo.IllInstrRLA:
                                value = Read(addr);
                                Write(addr, value);
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
                                Write(addr, value);
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
                                FlagOverflow = ((~(RegA ^ value) & (RegA ^ temp)) >> 7) & 1;
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
                                FlagOverflow = (((RegA ^ value) & (RegA ^ temp)) >> 7) & 1;
                                RegA = FlagSign = FlagZero = (temp & 0xFF);
                                break;
                            case OpInfo.IllInstrSHX: //Passes Tests but may be wrong in some minute detail
                                value = (RegX & ((addr >> 8) + 1)) & 0xFF;
                                temp = (addr - RegY) & 0xFF;
                                if((RegY + temp) <= 0xFF)
                                    Write(addr, value);
                                else
                                    Write(addr, Memory[addr]); //Not sure what to do for this cycle :(
                                break;
                            case OpInfo.IllInstrSHY: //Passes Tests but may be wrong in some minute detail
                                value = (RegY & ((addr >> 8) + 1)) & 0xFF;
                                temp = (addr - RegX) & 0xFF;
                                if ((RegX + temp) <= 0xFF)
                                    Write(addr, value);
                                else
                                    Write(addr, Memory[addr]);
                                break;
                            case OpInfo.IllInstrSLO:
                                value = Read(addr);
                                Write(addr, value);
                                FlagCarry = value >> 7;
                                value = (value << 1) & 0xFF;
                                Write(addr, value);
                                RegA |= value;
                                FlagSign = FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrSRE:
                                value = Read(addr);
                                Write(addr, value);
                                FlagCarry = value & 1;
                                value = value >> 1;
                                Write(addr, value);
                                RegA ^= value;
                                FlagSign = FlagZero = RegA;
                                break;
                            case OpInfo.IllInstrTAS://Mostly filler never tested.
                                RegS = RegX & RegA;
                                Write(addr, RegS & (addr >> 8));
                                break;
                            case OpInfo.IllInstrXAA:
                                RegA = RegX & Read(addr);
                                FlagSign = FlagZero = RegA;
                                break;
                            case OpInfo.InstrDummy:
                            default:
                                    debug.SetError("Missing OP");
                                    debug.LogInfo("Missing OP: " + OpInfo.GetOpNames()[OpInfo.GetOps()[op] & 0xFF] + " " + op.ToString("X2") + " Program Counter: " + RegPC.ToString("X4"));
                                break;
                        }
                        break;
                }
                #endregion
#if SCANLINE_PPU
                if (!nsfPlayer)
                {
                    if (PPU.pendingNMI != 0)
                    {
                        PPU.pendingNMI--;
                        if (PPU.pendingNMI == 0)
                            PPU.interruptNMI = true;
                    }
                }
#endif
                if (pendingFlagIRQ1 != 0)
                {
                    pendingFlagIRQ1--;
                    if (pendingFlagIRQ1 == 0)
                        delayedFlagIRQ = pendingFlagIRQValue1;
                }
                if (pendingFlagIRQ2 != 0)
                {
                    pendingFlagIRQ2--;
                    if (pendingFlagIRQ2 == 0)
                        delayedFlagIRQ = pendingFlagIRQValue2;
                }
                if (PPU.interruptNMI)
                {
                    ReadWord(RegPC);//Supposedly takes 7 cycles and is the same pattern as BRK
                    PushWordStack(RegPC);
                    PushByteStack(RegP | 0x20);
                    FlagIRQ = 1;
                    delayedFlagIRQ = 1;
                    pendingFlagIRQ1 = 0;
                    pendingFlagIRQ2 = 0;
                    RegPC = ReadWord(0xFFFA);
                    PPU.interruptNMI = false;
                }
                else if ((mapper.interruptMapper || APU.interruptAPU ) && (delayedFlagIRQ == 0))
                {
                    ReadWord(RegPC);//Supposedly takes 7 cycles and is the same pattern as BRK
                    PushWordStack(RegPC);
                    PushByteStack(RegP | 0x20);
                    FlagIRQ = 1;
                    delayedFlagIRQ = 1;
                    pendingFlagIRQ1 = 0;
                    pendingFlagIRQ2 = 0;
                    RegPC = ReadWord(0xFFFE);
                }
                else if (interruptReset)
                {
                    FlagIRQ = 1;
                    delayedFlagIRQ = 1;
                    pendingFlagIRQ1 = 0;
                    pendingFlagIRQ2 = 0;
                    RegPC = ReadWord(0xFFFC);
                    RegS = (RegS - 3) & 0xFF;
                    interruptReset = false;
                    APU.Reset();
                    PPU.Reset();
                }
            }
            if (debug.debugInterrupt && (PPU.scanlineCycle < 256 && PPU.scanline > -1 && PPU.scanline < 240))
                PPU.screen[PPU.scanline, PPU.scanlineCycle] ^= 0x00FFFFFF;
            emulationRunning = false;
            PPU.frameComplete = false;
            PPU.generateNameTables = false;
            PPU.generatePatternTables = false;
            APU.Update();
        }
        public NESCore(string input, int sampleRate, bool ignoreFileCheck = false) //NSF Load
        {
            nsfPlayer = true;
            opList = OpInfo.GetOps();
            rom.fileName = Path.GetFileNameWithoutExtension(input);
            Stream inputStream = File.OpenRead(input);
            inputStream.Position = 0;
            if (!ignoreFileCheck)
            {
                if (inputStream.ReadByte() != 'N' || inputStream.ReadByte() != 'E' || inputStream.ReadByte() != 'S' || inputStream.ReadByte() != 'M' || inputStream.ReadByte() != 0x1A)
                {
                    inputStream.Close();
                    throw (new BadHeaderException("Invalid File"));
                }
            }
            inputStream.Position = 0x5;
            int version = inputStream.ReadByte();
            int totalSongs = inputStream.ReadByte();
            int startingSong = inputStream.ReadByte();
            int loadAddress = inputStream.ReadByte() | (inputStream.ReadByte() << 8);
            int initAddress = inputStream.ReadByte() | (inputStream.ReadByte() << 8);
            int playAddress = inputStream.ReadByte() | (inputStream.ReadByte() << 8);
            string songName = "";
            for (int i = 0; i < 32; i++)
            {
                byte nextByte = (byte)inputStream.ReadByte();
                if (nextByte != 0)
                    songName += (char)nextByte;
                else
                    break;
            }
            inputStream.Position = 0x2E;
            string artist = "";
            for (int i = 0; i < 32; i++)
            {
                byte nextByte = (byte)inputStream.ReadByte();
                if (nextByte != 0)
                    artist += (char)nextByte;
                else
                    break;
            }
            inputStream.Position = 0x4E;
            string copyright = "";
            for (int i = 0; i < 32; i++)
            {
                byte nextByte = (byte)inputStream.ReadByte();
                if (nextByte != 0)
                    copyright += (char)nextByte;
                else
                    break;
            }
            inputStream.Position = 0x6E;
            int ntscPBRATE = inputStream.ReadByte() | (inputStream.ReadByte() << 8);
            byte[] banks = new byte[8];
            bool bankSwitching = false;
            for (int i = 0; i < 8; i++)
            {
                banks[i] = (byte)inputStream.ReadByte();
                if (banks[i] != 0)
                    bankSwitching = true;
            }
            int palPBRATE = inputStream.ReadByte() | (inputStream.ReadByte() << 8);
            int region = inputStream.ReadByte();
            int PBRATE;
            int specialChip = inputStream.ReadByte();
            Memory = new MemoryStore(2, 32, 0, true);
            APU = new APU(this, sampleRate);
#if SCANLINE_PPU
            PPU = new SPPU(this);
#else
            PPU = new PPU(this);
#endif
            debug = new Debug(this);
            debug.LogInfo("Mapper: NSF");
            switch (region & 3)
            {
                case 3://dual
                case 2://dual
                    debug.LogInfo("Region: Dual");
                    nesRegion = SystemType.NTSC;
                    PBRATE = ntscPBRATE;
                    break;
                case 0://ntsc
                default:
                    debug.LogInfo("Region: NTSC");
                    nesRegion = SystemType.NTSC;
                    PBRATE = ntscPBRATE;
                    break;
                case 1://pal
                    debug.LogInfo("Region: PAL");
                    nesRegion = SystemType.PAL;
                    PBRATE = palPBRATE;
                    break;
            }
            debug.LogInfo("Name: " + songName);
            debug.LogInfo("Artist: " + artist);
            debug.LogInfo("Copyright: " + copyright);
            debug.LogInfo("Total Songs: " + totalSongs.ToString());
            debug.LogInfo("Starting Song: " + startingSong.ToString());
            debug.LogInfo("Playing Speed: " + Math.Round((1000000.0 / PBRATE), 3).ToString() + "hz");
            if ((specialChip & 1) != 0)
                debug.LogInfo("VRC6");
            if ((specialChip & 2) != 0)
                debug.LogInfo("VRC7");
            if ((specialChip & 4) != 0)
                debug.LogInfo("FDS");
            if ((specialChip & 8) != 0)
                debug.LogInfo("MMC5");
            if ((specialChip & 0x10) != 0)
                debug.LogInfo("Namco 163");
            if ((specialChip & 0x20) != 0)
                debug.LogInfo("Sunsoft FME-07");
            debug.LogInfo("Load Address: 0x" + loadAddress.ToString("X4"));
            debug.LogInfo("Init Address: 0x" + initAddress.ToString("X4"));
            debug.LogInfo("Play Address: 0x" + playAddress.ToString("X4"));
            debug.LogInfo("Bankswitching: " + (bankSwitching ? "Yes" : "No"));
            if(bankSwitching)
                debug.LogInfo("Initial Banks: " + banks[0].ToString("X2") + " " + banks[1].ToString("X2") + " " 
                                                + banks[2].ToString("X2") + " " + banks[3].ToString("X2") + " " 
                                                + banks[4].ToString("X2") + " " + banks[5].ToString("X2") + " " 
                                                + banks[6].ToString("X2") + " " + banks[7].ToString("X2") + " ");
            debug.LogInfo("Data Length: " + (inputStream.Length - 0x80) + "bytes");
            inputStream.Position = 0x80;
            int startOffset = bankSwitching ? (loadAddress & 0xFFF) : (loadAddress - 0x8000);
            for (int i = startOffset; i < 32 * 0x400 && inputStream.Position < inputStream.Length; i++)
            {
                byte nextByte = (byte)inputStream.ReadByte();
                Memory.banks[(i / 0x400) + Memory.swapOffset][i % 0x400] = nextByte;
            }
            mapper = new Mappers.mNSF(this, banks, PBRATE, specialChip);
            ((Mappers.mNSF)mapper).totalSongs = totalSongs;
            ((Mappers.mNSF)mapper).startSong = startingSong;
            ((Mappers.mNSF)mapper).currentSong = startingSong;
            ((Mappers.mNSF)mapper).initAddress = initAddress;
            ((Mappers.mNSF)mapper).loadAddress = loadAddress;
            ((Mappers.mNSF)mapper).playAddress = playAddress;
            ((Mappers.mNSF)mapper).songName = songName;
            ((Mappers.mNSF)mapper).artist = artist;
            ((Mappers.mNSF)mapper).copyright = copyright;
            Power();
        }
        public NESCore(SystemType region, String input, String fdsImage, String cartDBLocation, int sampleRate, bool ignoreFileCheck = false) //FDS Load
        {
            nesRegion = region;
            opList = OpInfo.GetOps();
            if (!File.Exists(input))
            {
                throw new FDSBiosException("FDS BIOS not found.");
            }
            Stream biosStream = File.OpenRead(input);
            rom.filePath = fdsImage;
            rom.fileName = Path.GetFileNameWithoutExtension(rom.filePath);
            rom.mapper = 20;
            Memory = new MemoryStore(2, 8, 32, true);
            APU = new APU(this, sampleRate);
#if SCANLINE_PPU
            PPU = new SPPU(this);
#else
            PPU = new PPU(this);
#endif
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
            biosStream.Close();
            Stream diskStream = File.OpenRead(fdsImage);
            mapper = new Mappers.m020(this, diskStream, ignoreFileCheck);
            rom.crc = ((Mappers.m020)mapper).crc; //I don't like this.
            debug.LogInfo("ROM CRC32: " + rom.crc.ToString("X8"));
            diskStream.Close();
            Power();

        }
        public NESCore(SystemType region, String input, String cartDBLocation, int sampleRate, bool ignoreFileCheck = false) : 
            this(region, File.OpenRead(input), cartDBLocation, sampleRate, ignoreFileCheck)
        {
            rom.filePath = input;
            rom.fileName = Path.GetFileNameWithoutExtension(rom.filePath);
            debug.LogInfo(rom.filePath);
        }
        public NESCore(SystemType region, Stream inputStream, String cartDBLocation, int sampleRate, bool ignoreFileCheck = false)
        {
            nesRegion = region;
            opList = OpInfo.GetOps();
            inputStream.Position = 0;
            if (!ignoreFileCheck)
            {
                if (inputStream.ReadByte() != 'N' || inputStream.ReadByte() != 'E' || inputStream.ReadByte() != 'S' || inputStream.ReadByte() != 0x1A)
                {
                    inputStream.Close();
                    throw (new BadHeaderException("Invalid File"));
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
            rom.mapper = (lowMapper >> 4) | (highMapper & 0xF0);
            bool iNes2 = ((highMapper >> 2) & 3) == 2;
            if (iNes2)
            {
                int higherMapper = inputStream.ReadByte();
                int highPrgChrRom = inputStream.ReadByte();
                int prgRam = inputStream.ReadByte();
                int batteryBackedPrgRam = prgRam & 0xF;
                if (batteryBackedPrgRam != 0)
                {
                    batteryBackedPrgRam = (int)(Math.Pow(2, batteryBackedPrgRam + 6)) / 1024;
                    if (batteryBackedPrgRam == 0)
                        batteryBackedPrgRam = 1;
                }
                int unbackedPrgRam = (prgRam >> 4) & 0xF;
                if (unbackedPrgRam != 0)
                {
                    unbackedPrgRam = (int)(Math.Pow(2, unbackedPrgRam + 6)) / 1024;
                    if (unbackedPrgRam == 0)
                        unbackedPrgRam = 1;
                }
                int chrRam = inputStream.ReadByte();
                int tvSystem = inputStream.ReadByte();
                int vsSystem = inputStream.ReadByte();
                rom.prgRAM = (rom.mapper == 5 ? 64 : unbackedPrgRam + batteryBackedPrgRam);
            }
            else
            {
                rom.prgRAM = (rom.mapper == 5 ? 64 : 8);//Screw it everything gets atleast 8kb ram
            }
            Memory = new MemoryStore(2, rom.prgROM, rom.prgRAM, true);
            APU = new APU(this, sampleRate);
#if SCANLINE_PPU
            PPU = new SPPU(this);
#else
            PPU = new PPU(this);
#endif
            debug = new Debug(this);
            if (rom.vsUnisystem)
                Expansion = new Inputs.Unisystem(this);
            debug.LogInfo("Mapper: " + rom.mapper);
            debug.LogInfo("PRG-ROM: " + rom.prgROM.ToString() + "KB");
            debug.LogInfo("CHR-ROM: " + rom.vROM.ToString() + "KB");
            debug.LogInfo("Mirroring: " + (rom.mirroring == Mirroring.fourScreen ? "Four-screen" : (rom.mirroring == Mirroring.vertical ? "Vertical" : "Horizontal")));
            if(rom.vsUnisystem)
                debug.LogInfo("VS Unisystem Game");
            if(iNes2)
                debug.LogInfo("NES 2.0 marker found in header.");
            inputStream.Position = 0x10;
            if (rom.trainer)
            {
                debug.LogInfo("Trainer Present");
                for (int i = 0x00; i < 0x200; i++)
                {
                    Memory.ForceValue(0x7000 + i, (byte)inputStream.ReadByte());
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

            if (File.Exists(Path.Combine(cartDBLocation, "NesCarts.xml")))
            {
                string gameName = "";
                string board = "";
                string dbMapper = "";
                string system = "";
                string horz = "", vert = "";
                bool done = false;
                XmlReader xmlReader = XmlReader.Create(File.OpenRead(Path.Combine(cartDBLocation, "NesCarts.xml")));
                while (!done && xmlReader.ReadToFollowing("game"))
                {
                    gameName = xmlReader.GetAttribute("name");
                    xmlReader.ReadToDescendant("cartridge");
                    system = xmlReader.GetAttribute("system");
                    if (xmlReader.GetAttribute("crc") == rom.crc.ToString("X8"))
                    {
                        xmlReader.ReadToDescendant("board");
                        board = xmlReader.GetAttribute("type");
                        dbMapper = xmlReader.GetAttribute("mapper");
                        xmlReader.ReadToDescendant("pad");
                        horz = xmlReader.GetAttribute("h");
                        vert = xmlReader.GetAttribute("v");
                        done = true;
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
                    if (horz == "1" && vert == "0")
                    {
                        PPU.PPUMemory.VerticalMirroring();
                        rom.mirroring = Mirroring.vertical;debug.LogInfo("Mirroring: Vertical");
                    }
                    if (horz == "0" && vert == "1")
                    {
                        PPU.PPUMemory.HorizontalMirroring();
                        rom.mirroring = Mirroring.horizontal; debug.LogInfo("Mirroring: Horizontal");
                    }
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
                    rom.mapper = 4;
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
                case 13:
                    mapper = new Mappers.m013(this);
                    break;
                case 15:
                    mapper = new Mappers.m015(this);
                    break;
                case 16: //Bandai - EEPROM
                case 159:
                case 153: //I dont think 153 belongs here but bootgod's xml reports Dragon Ball as 153 and I know that it's 16
                    mapper = new Mappers.m016(this);
                    break;
                case 18:
                    mapper = new Mappers.m018(this);
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
                    mapper = new Mappers.mVRC6(this, 0x00, 0x02, 0x01, 0x03);
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
                case 105://NES-EVENT, Nintendo World Championships 1990
                    mapper = new Mappers.m105(this);
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
                case 185:
                    mapper = new Mappers.m185(this);
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
            Power();
        }
        public byte Read(int addr)
        {
            AddCycles(1);
            if (APU.dmc.fetching && !APU.dmc.reading)
            {
                while (APU.dmc.idleCycles > 0)
                {
                    APU.dmc.Fetch(false, -1);
                    AddCycles(1);
                }
                APU.dmc.reading = true;
                APU.dmc.Fetch(false, Read(APU.dmc.sampleCurrentAddress));
                APU.dmc.reading = false;
            }
            ushort address = (ushort)addr;
            byte nextByte = Memory[address];
            if ((address & 0xF000) == 0x4000 && (address < 0x4018))
            {
                switch (address)
                {
                    case 0x4015:
                        nextByte = APU.Read();
                        break;
                    case 0x4016:
                        if(!nsfPlayer)
                            nextByte = (byte)(PortOne.Read(address) | (lastRead & 0xC0) | Expansion.Read(address));
                        else
                            nextByte = lastRead;
                        break;
                    case 0x4017:
                        if(!nsfPlayer)
                            nextByte = (byte)(PortTwo.Read(address) | (lastRead & 0xC0) | Expansion.Read(address));
                        else
                            nextByte = lastRead;
                        break;
                    default:
                        nextByte = lastRead;
                        break;
                }
            }
            else if ((address & 0xE000) == 0x2000 && !nsfPlayer)
            {
                nextByte = PPU.Read((ushort)(address & 0x2007));
            }
            nextByte = mapper.Read(nextByte, address);
#if DEBUGGER
            nextByte = debug.Read(nextByte, address);
#endif
            nextByte = GameGenie(nextByte, address);
            lastRead = nextByte;
            return nextByte;
        }
        private int ReadWord(int address)
        {
            int highAddress = (address + 1) & 0xFFFF;
            return Read(address) | (Read(highAddress) << 8);
        }
        private int ReadWordWrap(int address)
        {
            int highAddress = (address & 0xFF00) | ((address + 1) & 0xFF);
            return Read(address) | (Read(highAddress) << 8);
        }
        private void Write(int addr, int val)
        {
            AddCycles(1);
            if (APU.dmc.fetching)
                APU.dmc.Fetch(true, -1);
            ushort address = (ushort)addr;
            byte value = (byte)val;

            if ((address & 0xF000) == 0x4000)
            {
                APU.Write(value, address);
                if (!nsfPlayer)
                {
                    PortOne.Write(value, address);
                    PortTwo.Write(value, address);
                    Expansion.Write(value, address);
                    if(address == 0x4014)
                        PPU.Write(value, address);//Need this in here for sprite DMA
                }
            }
            else if ((address & 0xE000) == 0x2000 && !nsfPlayer)
            {
                PPU.Write(value, (ushort)(address & 0x2007));
            }

            mapper.Write(value, address);
#if DEBUGGER
            debug.Write(value, address);
#endif
            Memory[address] = value;
        }
        public void PushWordStack(int value)
        {
            Write(RegS | 0x0100, value >> 8);
            RegS--;
            RegS &= 0xFF;
            Write(RegS | 0x0100, value);
            RegS--;
            RegS &= 0xFF;
        }
        public int PopWordStack()
        {
            RegS += 2;
            RegS &= 0xFF;
            return ReadWordWrap(((RegS - 1) & 0xFF) | 0x0100);
        }
        private void PushByteStack(int value)
        {
            Write(RegS | 0x0100, value);
            RegS--;
            RegS &= 0xFF;
        }
        public byte PopByteStack()
        {
            RegS++;
            RegS &= 0xFF;
            return Read(RegS | 0x0100);
        }
        public void AddCycles(int value)
        {
            if (!nsfPlayer)
            {
                counter += value;
                APU.AddCycles(value);
                PPU.AddCycles(value);
                if (mapper.cycleIRQ)
                    mapper.IRQ(value);
            }
            else
            {
                value = ((Mappers.mNSF)mapper).IRQ(value);
                counter += value;
                APU.AddCycles(value);
            }
#if DEBUGGER
            debug.AddCycles(value);
#endif
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
        public void NSFPlay(int song)
        {
            if (nsfPlayer)
            {
                if (song > ((Mappers.mNSF)mapper).totalSongs)
                    song = 1;
                else if (song < 1)
                    song = ((Mappers.mNSF)mapper).totalSongs;
                ((Mappers.mNSF)mapper).currentSong = song;
                RegPC = ((Mappers.mNSF)mapper).initAddress;
                RegS = 0xFF;
                PushWordStack(((Mappers.mNSF)mapper).playAddress - 1);
                RegA = (song - 1) & 0xFF;
                if (RegA == 0xFF)
                    RegA = 0;
                if (nesRegion == SystemType.NTSC)
                    RegX = 0;
                else
                    RegX = 1;
                Start();
            }

        }
        public void NSFNextSong()
        {
            if (nsfPlayer)
            {
                ((Mappers.mNSF)mapper).currentSong++;
                Power();
            }
        }
        public void NSFPreviousSong()
        {
            if (nsfPlayer)
            {
                ((Mappers.mNSF)mapper).currentSong--;
                Power();
            }
        }
        public void SetControllers(ControllerType portOne, ControllerType portTwo, ControllerType expansion, bool fourScore, bool filterIllegalInput)
        {
            this.fourScore = fourScore;
            this.filterIllegalInput = filterIllegalInput;
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
                default:
                case ControllerType.Empty:
                    PortTwo = new Inputs.Empty();
                    break;
            }
            switch (expansion)
            {
                case ControllerType.FamiPaddle:
                    Expansion = new Inputs.FamiPaddle(this, Inputs.Port.PortOne);
                    break;
                default:
                case ControllerType.Empty:
                    Expansion = new Inputs.Empty();
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
            writer.Write(RegP);
            writer.Write(counter);
            writer.Write(pendingFlagIRQ1);
            writer.Write(pendingFlagIRQ2);
            writer.Write(pendingFlagIRQValue1);
            writer.Write(pendingFlagIRQValue2);
            writer.Write(delayedFlagIRQ);
            writer.Write(lastRead);
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
            RegP = reader.ReadInt32();
            counter = reader.ReadInt32();
            pendingFlagIRQ1 =reader.ReadInt32();
            pendingFlagIRQ2 = reader.ReadInt32();
            pendingFlagIRQValue1 = reader.ReadInt32();
            pendingFlagIRQValue2 = reader.ReadInt32();
            delayedFlagIRQ =reader.ReadInt32();
            lastRead = reader.ReadByte();
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
            Memory.memMap[0x0000 >> 0xA] = 0;
            Memory.memMap[0x0400 >> 0xA] = 1;
            Memory.memMap[0x0800 >> 0xA] = Memory.memMap[0x0000 >> 0xA];
            Memory.memMap[0x0C00 >> 0xA] = Memory.memMap[0x0400 >> 0xA];
            Memory.memMap[0x1000 >> 0xA] = Memory.memMap[0x0000 >> 0xA];
            Memory.memMap[0x1400 >> 0xA] = Memory.memMap[0x0400 >> 0xA];
            Memory.memMap[0x1800 >> 0xA] = Memory.memMap[0x0000 >> 0xA];
            Memory.memMap[0x1C00 >> 0xA] = Memory.memMap[0x0400 >> 0xA];
            Memory.SetReadOnly(0, 8, false);
            if (rom.sRAM)
            {
                debug.LogInfo("SRAM Present");
                Memory.Swap8kRAM(0x6000, 0, false);
            }
            RegA = 0;
            RegX = 0;
            RegY = 0;
            RegS = 0xFD;
            RegPC = 0;
            FlagCarry = 0; //Bit 0 of P
            FlagZero = 1; //backwards
            FlagIRQ = 1;
            FlagDecimal = 0;
            FlagOverflow = 0;
            FlagSign = 0; //Bit 7 of P
            counter = 0;
            pendingFlagIRQ1 = 0;
            pendingFlagIRQ2 = 0;
            pendingFlagIRQValue1 = 0;
            pendingFlagIRQValue2 = 0;
            delayedFlagIRQ = 1;
            lastRead = 0;
            for (int i = 0; i < 0x800; i++)
            {
                Memory[i] = (byte)((i & 0x04) == 0 ? 0x00 : 0xFF);
            }
            interruptReset = false;
            PPU.Power();
            APU.Power();
            mapper.Power();
            RegPC = ReadWord(0xFFFC);//entry point
            if (nsfPlayer)
            {
                NSFPlay(((Mappers.mNSF)mapper).currentSong);
            }
        }
        public void Reset()
        {
            interruptReset = true;
        }
        private byte GameGenie(byte value, ushort address)
        {
            for (int i = 0; i < gameGenieCodeNum; i++)
            {
                if (gameGenieCodes[i].address == address)
                {
                    if (gameGenieCodes[i].code == "DUMMY")
                        return gameGenieCodes[i].value;
                    else if (gameGenieCodes[i].code.Length == 6)
                        return gameGenieCodes[i].value;
                    else if (gameGenieCodes[i].code.Length == 8 && value == gameGenieCodes[i].check)
                        return gameGenieCodes[i].value;
                }
            }
            return value;
        }
        public byte[] GetSRAM()
        {
            byte[] sram = new byte[0x2000];
            for (int i = 0x0; i < 0x2000; i++)
                sram[i] = Memory[i + 0x6000];
            return sram;
        }
        public void SetSRAM(byte[] sram)
        {
            for (int i = 0x0; i < 0x2000; i++)
                Memory[i + 0x6000] = sram[i];
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
        public float square1;
        public float square2;
        public float triangle;
        public float noise;
        public float dmc;
        public float external;
    }
    public struct Rom
    {
        public int mapper;
        public string board;
        public string fileName;
        public string filePath;
        public string title;
        public int prgRAM;
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

    public class BadHeaderException : Exception
    {
        public BadHeaderException() { }
        public BadHeaderException(string message) : base(message) { }
        public BadHeaderException(string message, Exception inner) : base(message, inner) { }
    }
    public class FDSBiosException : Exception
    {
        public FDSBiosException() { }
        public FDSBiosException(string message) : base(message) { }
        public FDSBiosException(string message, Exception inner) : base(message, inner) { }
    }
}
