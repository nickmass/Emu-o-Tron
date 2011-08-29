using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron
{
    public class Debug
    {
        private NESCore nes;

        public bool irqEnable = false;
        private int irqCounter = 0;
        public bool debugInterrupt = false;

        public bool pendingError;
        public string errorMessage;
        
        public int lastExec;

        private bool runTo;
        private ushort runToAddress;

        public bool breakOnSpriteZeroHit = false;
        public bool breakOnSpriteOverflow = false;
        public byte[] breakPoints = new byte[0x10000];

        public byte BREAKPOINTREAD = 0x01;
        public byte BREAKPOINTWRITE = 0x02;
        public byte BREAKPOINTEXEC = 0x04;

        public StringBuilder romInfo = new StringBuilder();

        public StringBuilder logBuilder = new StringBuilder();
        public bool logging = false;

        public int Scanline
        {
            get
            {
                return nes.PPU.scanline;
            }
        }

        public int Cycle
        {
            get
            {
                return nes.PPU.scanlineCycle;
            }
        }

        public byte RegA
        {
            get
            {
                return (byte)(nes.RegA & 0xFF);
            }
        }
        public byte RegX
        {
            get
            {
                return (byte)(nes.RegX & 0xFF);
            }
        }
        public byte RegY
        {
            get
            {
                return (byte)(nes.RegY & 0xFF);
            }
        }
        public byte RegS
        {
            get
            {
                return (byte)(nes.RegS & 0xFF);
            }
        }
        public ushort RegPC
        {
            get
            {
                return (ushort)(nes.RegPC & 0xFFFF);
            }
        }
        public bool FlagCarry
        {
            get
            {
                return (nes.FlagCarry != 0);
            }
        }
        public bool FlagZero
        {
            get
            {
                return (nes.FlagZero == 0);
            }
        }
        public bool FlagIRQ
        {
            get
            {
                return (nes.FlagIRQ != 0);
            }
        }
        public bool FlagDecimal
        {
            get
            {
                return (nes.FlagDecimal != 0);
            }
        }
        public bool FlagBreak
        {
            get
            {
                return (nes.FlagBreak != 0);
            }
        }
        public bool FlagNotUsed
        {
            get
            {
                return (nes.FlagNotUsed != 0);
            }
        }
        public bool FlagOverflow
        {
            get
            {
                return (nes.FlagOverflow != 0);
            }
        }
        public bool FlagSign
        {
            get
            {
                return ((nes.FlagSign >> 7) != 0);
            }
        }
        public Debug(NESCore nes)
        {
            this.nes = nes;
        }

        public void Execute(ushort address)
        {
            lastExec = address;
            if (logging)
            {
                if (logBuilder.Length > 1024 * 1024 * 100)
                    logBuilder.Remove(0, 1024 * 512 * 95);
                logBuilder.AppendLine(TraceLog(nes.RegPC));
            }
            if ((breakPoints[address] & BREAKPOINTEXEC) != 0)
                debugInterrupt = true;
            if (runTo && address == runToAddress)
            {
                debugInterrupt = true;
                runTo = false;
            }
        }
        public byte Read(byte value, ushort address)
        {
            if ((breakPoints[address] & BREAKPOINTREAD) != 0)
                debugInterrupt = true;
            return value;
        }
        public void Write(byte value, ushort address)
        {
            if((breakPoints[address] & BREAKPOINTWRITE) != 0)
                debugInterrupt = true;
        }
        public void LogInfo(string line)
        {
            romInfo.AppendLine(line);
        }
        public void SpriteZeroHit()
        {
            if (breakOnSpriteZeroHit)
                debugInterrupt = true;
        }
        public void SpriteOverflow()
        {
            if (breakOnSpriteOverflow)
                debugInterrupt = true;
        }
        public void AddCycles(int cycles)
        {
            if (irqEnable)
            {
                for (int i = 0; i < cycles; i++)
                {
                    if (irqCounter == 0)
                        debugInterrupt = true;
                    else
                        irqCounter--;
                }
            }
        }
        public void AddReadBreakpoint(ushort address)
        {
            breakPoints[address] |= BREAKPOINTREAD;
        }
        public void AddWriteBreakpoint(ushort address)
        {
            breakPoints[address] |= BREAKPOINTWRITE;
        }
        public void AddExecuteBreakpoint(ushort address)
        {
            breakPoints[address] |= BREAKPOINTEXEC;
        }
        public void RunTo(ushort address)
        {
            runTo = true;
            runToAddress = address;
        }
        public void StepCycles(int cycles)
        {
            irqCounter = cycles;
            irqEnable = true;
        }
        public void SetError(string error)
        {
            errorMessage = error;
            pendingError = true;
        }
        public string LogOp(int address)
        {
            StringBuilder line = new StringBuilder();
            int op = Peek(address);
            int opInfo = OpInfo.GetOps()[op];
            int size = (opInfo >> 16) & 0xF;
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
                    line.AppendFormat("${0},X @ {1} = {2}             ", Peek(address + 1).ToString("X2"), ((Peek(address + 1) + nes.RegX) & 0xFF).ToString("X2"), Peek((Peek(address + 1) + nes.RegX) & 0xFF).ToString("X2"));
                    break;
                case OpInfo.AddrZeroPageY:
                    line.AppendFormat("${0},Y @ {1} = {2}             ", Peek(address + 1).ToString("X2"), ((Peek(address + 1) + nes.RegY) & 0xFF).ToString("X2"), Peek((Peek(address + 1) + nes.RegY) & 0xFF).ToString("X2"));
                    break;
                case OpInfo.AddrAbsolute:
                    if (op == 0x4C || op == 0x20)
                        line.AppendFormat("${0}                       ", PeekWord(address + 1).ToString("X4"));
                    else
                        line.AppendFormat("${0} = {1}                  ", PeekWord(address + 1).ToString("X4"), Peek(PeekWord(address + 1)).ToString("X2"));
                    break;
                case OpInfo.AddrAbsoluteX:
                    line.AppendFormat("${0},X @ {1} = {2}         ", PeekWord(address + 1).ToString("X4"), ((PeekWord(address + 1) + nes.RegX) & 0xFFFF).ToString("X4"), Peek((PeekWord(address + 1) + nes.RegX) & 0xFFFF).ToString("X2"));
                    break;
                case OpInfo.AddrAbsoluteY:
                    line.AppendFormat("${0},Y @ {1} = {2}         ", PeekWord(address + 1).ToString("X4"), ((PeekWord(address + 1) + nes.RegY) & 0xFFFF).ToString("X4"), Peek((PeekWord(address + 1) + nes.RegY) & 0xFFFF).ToString("X2"));
                    break;
                case OpInfo.AddrIndirectAbs:
                    line.AppendFormat("(${0}) = {1}              ", PeekWord(address + 1).ToString("X4"), PeekWordWrap(PeekWord(address + 1)).ToString("X4"));
                    break;
                case OpInfo.AddrRelative:
                    int addr = Peek(address + 1);
                    if (addr < 0x80)
                        addr += (address + size);
                    else
                        addr += (address + size) - 256;
                    line.AppendFormat("${0}                       ", addr.ToString("X4"));
                    break;
                case OpInfo.AddrIndirectX:
                    addr = val1 = Peek(address + 1);
                    addr += nes.RegX;
                    addr &= 0xFF;
                    val2 = addr;
                    addr = val3 = Peek(addr) + (Peek((addr + 1) & 0xFF) << 8);
                    addr = val4 = Peek(addr);
                    line.AppendFormat("(${0},X) @ {1} = {2} = {3}    ", val1.ToString("X2"), val2.ToString("X2"), val3.ToString("X4"), val4.ToString("X2"));
                    break;
                case OpInfo.AddrIndirectY:
                    addr = val1 = Peek(address + 1);
                    addr = val2 = Peek(addr) + (Peek((addr + 1) & 0xFF) << 8);
                    addr += nes.RegY;
                    addr &= 0xFFFF;
                    val3 = addr;
                    addr = val4 = Peek(addr & 0xFFFF);
                    line.AppendFormat("(${0}),Y = {1} @ {2} = {3}  ", val1.ToString("X2"), val2.ToString("X4"), val3.ToString("X4"), val4.ToString("X2"));
                    break;
            }
            return line.ToString().Trim();
        }
        public int OpSize(int address)
        {
            int op = Peek(address);
            int opInfo = OpInfo.GetOps()[op];
            return (opInfo >> 16) & 0xF;
        }
        private string TraceLog(int address)
        {
            StringBuilder line = new StringBuilder();
            int op = Peek(address);
            int opInfo = OpInfo.GetOps()[op];
            int size = (opInfo >> 16) & 0xF;
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
                    line.AppendFormat("${0},X @ {1} = {2}             ", Peek(address + 1).ToString("X2"), ((Peek(address + 1) + nes.RegX) & 0xFF).ToString("X2"), Peek((Peek(address + 1) + nes.RegX) & 0xFF).ToString("X2"));
                    break;
                case OpInfo.AddrZeroPageY:
                    line.AppendFormat("${0},Y @ {1} = {2}             ", Peek(address + 1).ToString("X2"), ((Peek(address + 1) + nes.RegY) & 0xFF).ToString("X2"), Peek((Peek(address + 1) + nes.RegY) & 0xFF).ToString("X2"));
                    break;
                case OpInfo.AddrAbsolute:
                    if (op == 0x4C || op == 0x20)
                        line.AppendFormat("${0}                       ", PeekWord(address + 1).ToString("X4"));
                    else
                        line.AppendFormat("${0} = {1}                  ", PeekWord(address + 1).ToString("X4"), Peek(PeekWord(address + 1)).ToString("X2"));
                    break;
                case OpInfo.AddrAbsoluteX:
                    line.AppendFormat("${0},X @ {1} = {2}         ", PeekWord(address + 1).ToString("X4"), ((PeekWord(address + 1) + nes.RegX) & 0xFFFF).ToString("X4"), Peek((PeekWord(address + 1) + nes.RegX) & 0xFFFF).ToString("X2"));
                    break;
                case OpInfo.AddrAbsoluteY:
                    line.AppendFormat("${0},Y @ {1} = {2}         ", PeekWord(address + 1).ToString("X4"), ((PeekWord(address + 1) + nes.RegY) & 0xFFFF).ToString("X4"), Peek((PeekWord(address + 1) + nes.RegY) & 0xFFFF).ToString("X2"));
                    break;
                case OpInfo.AddrIndirectAbs:
                    line.AppendFormat("(${0}) = {1}              ", PeekWord(address + 1).ToString("X4"), PeekWordWrap(PeekWord(address + 1)).ToString("X4"));
                    break;
                case OpInfo.AddrRelative:
                    int addr = Peek(address + 1);
                    if (addr < 0x80)
                        addr += (address + size);
                    else
                        addr += (address + size) - 256;
                    line.AppendFormat("${0}                       ", addr.ToString("X4"));
                    break;
                case OpInfo.AddrIndirectX:
                    addr = val1 = Peek(address + 1);
                    addr += nes.RegX;
                    addr &= 0xFF;
                    val2 = addr;
                    addr = val3 = Peek(addr) + (Peek((addr + 1) & 0xFF) << 8);
                    addr = val4 = Peek(addr);
                    line.AppendFormat("(${0},X) @ {1} = {2} = {3}    ", val1.ToString("X2"), val2.ToString("X2"), val3.ToString("X4"), val4.ToString("X2"));
                    break;
                case OpInfo.AddrIndirectY:
                    addr = val1 = Peek(address + 1);
                    addr = val2 = Peek(addr) + (Peek((addr + 1) & 0xFF) << 8);
                    addr += nes.RegY;
                    addr &= 0xFFFF;
                    val3 = addr;
                    addr = val4 = Peek(addr & 0xFFFF);
                    line.AppendFormat("(${0}),Y = {1} @ {2} = {3}  ", val1.ToString("X2"), val2.ToString("X4"), val3.ToString("X4"), val4.ToString("X2"));
                    break;
            }
            line.AppendFormat("A:{0} X:{1} Y:{2} P:", nes.RegA.ToString("X2"), nes.RegX.ToString("X2"), nes.RegY.ToString("X2"));
            if (nes.FlagCarry != 0)
                line.Append("C");
            else
                line.Append("c");
            if (nes.FlagZero == 0)
                line.Append("Z");
            else
                line.Append("z");
            if (nes.FlagIRQ != 0)
                line.Append("I");
            else
                line.Append("i");
            if (nes.FlagDecimal != 0)
                line.Append("D");
            else
                line.Append("d");
            if (nes.FlagBreak != 0)
                line.Append("B");
            else
                line.Append("b");
            if (nes.FlagNotUsed != 0)
                line.Append("-");
            else
                line.Append("_");
            if (nes.FlagOverflow != 0)
                line.Append("V");
            else
                line.Append("v");
            if ((nes.FlagSign >> 7) != 0)
                line.Append("N");
            else
                line.Append("n");
            line.AppendFormat(" S:{0} CYC:{1} SL:{2}", nes.RegS.ToString("X2"), (nes.PPU.scanlineCycle).ToString().PadLeft(3), nes.PPU.scanline.ToString().PadLeft(3));
            return line.ToString();
        }
        public byte Peek(int address)
        {
            address = address & 0xFFFF;
            byte nextByte = nes.Memory[nes.MirrorMap[address]];
            return nextByte;
        }
        public int PeekWord(int address)
        {
            int highAddress = (address + 1) & 0xFFFF;
            return (Peek(address) + (Peek(highAddress) << 8)) & 0xFFFF;
        }
        public int PeekWordWrap(int address)
        {
            int highAddress = (address & 0xFF00) + ((address + 1) & 0xFF);
            return (Peek(address) + (Peek(highAddress) << 8)) & 0xFFFF;
        }
        public ushort PeekMirror(ushort address)
        {
            return nes.MirrorMap[address];
        }
    }
}
