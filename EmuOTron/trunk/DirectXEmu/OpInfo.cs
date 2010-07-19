using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectXEmu
{
    class OpInfo
    {
        public const int InstrADC = 0;
        public const int InstrAND = 1;
        public const int InstrASL = 2;
        public const int InstrBIT = 3;
        public const int InstrBCC = 4;
        public const int InstrBCS = 5;
        public const int InstrBEQ = 6;
        public const int InstrBMI = 7;
        public const int InstrBNE = 8;
        public const int InstrBPL = 9;
        public const int InstrBRK = 10;
        public const int InstrBVC = 11;
        public const int InstrBVS = 12;
        public const int InstrCLC = 13;
        public const int InstrCLD = 14;
        public const int InstrCLI = 15;
        public const int InstrCLV = 16;
        public const int InstrCMP = 17;
        public const int InstrCPX = 18;
        public const int InstrCPY = 19;
        public const int InstrDEC = 20;
        public const int InstrDEX = 21;
        public const int InstrDEY = 22;
        public const int InstrEOR = 23;
        public const int InstrINC = 24;
        public const int InstrINX = 25;
        public const int InstrINY = 26;
        public const int InstrJMP = 27;
        public const int InstrJSR = 28;
        public const int InstrLDA = 29;
        public const int InstrLDX = 30;
        public const int InstrLDY = 31;
        public const int InstrLSR = 32;
        public const int InstrNOP = 33;
        public const int InstrORA = 34;
        public const int InstrPHA = 35;
        public const int InstrPHP = 36;
        public const int InstrPLA = 37;
        public const int InstrPLP = 38;
        public const int InstrROL = 39;
        public const int InstrROR = 40;
        public const int InstrRTI = 41;
        public const int InstrRTS = 42;
        public const int InstrSBC = 43;
        public const int InstrSEC = 44;
        public const int InstrSED = 45;
        public const int InstrSEI = 46;
        public const int InstrSTA = 47;
        public const int InstrSTX = 48;
        public const int InstrSTY = 49;
        public const int InstrTAX = 50;
        public const int InstrTAY = 51;
        public const int InstrTSX = 52;
        public const int InstrTXA = 53;
        public const int InstrTXS = 54;
        public const int InstrTYA = 55;

        public const int IllInstrAHX = 56;
        public const int IllInstrANC = 57;
        public const int IllInstrALR = 58;
        public const int IllInstrARR = 59;
        public const int IllInstrAXS = 60;
        public const int IllInstrDCP = 61;
        public const int IllInstrISC = 62;
        public const int IllInstrKIL = 63;
        public const int IllInstrLAS = 64;
        public const int IllInstrLAX = 65;
        public const int IllInstrNOP = 66;
        public const int IllInstrRLA = 67;
        public const int IllInstrRRA = 68;
        public const int IllInstrSAX = 69;
        public const int IllInstrSBC = 70;
        public const int IllInstrSHX = 71;
        public const int IllInstrSHY = 72;
        public const int IllInstrSLO = 73;
        public const int IllInstrSRE = 74;
        public const int IllInstrTAS = 75;
        public const int IllInstrXAA = 76;

        public const int InstrDummy = 255;

        public const int AddrNone = 0;           // 
        public const int AddrZeroPage = 1;       //aa
        public const int AddrImmediate = 2;      //#aa
        public const int AddrAccumulator = 3;    //A
        public const int AddrZeroPageX = 4;      //aa,X
        public const int AddrZeroPageY = 5;      //aa,Y
        public const int AddrAbsolute = 6;       //aaaa
        public const int AddrAbsoluteX = 7;      //aaaa,X
        public const int AddrAbsoluteY = 8;      //aaaa,Y
        public const int AddrIndirectAbs = 9;    //(aaaa) Jump
        public const int AddrRelative = 10;      //Branch
        public const int AddrIndirectX = 11;     //(aa,X)
        public const int AddrIndirectY = 12;     //(aa),Y

        public static int[] opListing;
        public static string[] opNames;

        // opcode + ( Addr << 8) + (Size << 16) + (Cycles << 24)

        public static int[] GetOps()
        {
            if (opListing == null)
                LoadOps();
            return opListing;
        }
        public static string[] GetOpNames()
        {
            if (opNames == null)
                LoadOpNames();
            return opNames;
        }
        public static void LoadOps()
        {
            opListing = new int[256];

            for (int i = 0; i < 256; i++)
                opListing[i] = 255 + (0 << 8) + (1 << 16) + (1 << 24);

            SetOp(0xA8, InstrTAY, AddrNone, 1, 2);
            SetOp(0xAA, InstrTAX, AddrNone, 1, 2);
            SetOp(0xBA, InstrTSX, AddrNone, 1, 2);
            SetOp(0x98, InstrTYA, AddrNone, 1, 2);
            SetOp(0x8A, InstrTXA, AddrNone, 1, 2);
            SetOp(0x9A, InstrTXS, AddrNone, 1, 2);

            SetOp(0xA9, InstrLDA, AddrImmediate, 2, 2);
            SetOp(0xA5, InstrLDA, AddrZeroPage, 2, 3);
            SetOp(0xB5, InstrLDA, AddrZeroPageX, 2, 4);
            SetOp(0xAD, InstrLDA, AddrAbsolute, 3, 4);
            SetOp(0xBD, InstrLDA, AddrAbsoluteX, 3, 4);
            SetOp(0xB9, InstrLDA, AddrAbsoluteY, 3, 4);
            SetOp(0xA1, InstrLDA, AddrIndirectX, 2, 6);
            SetOp(0xB1, InstrLDA, AddrIndirectY, 2, 5);

            SetOp(0xA2, InstrLDX, AddrImmediate, 2, 2);
            SetOp(0xA6, InstrLDX, AddrZeroPage, 2, 3);
            SetOp(0xB6, InstrLDX, AddrZeroPageY, 2, 4);
            SetOp(0xAE, InstrLDX, AddrAbsolute, 3, 4);
            SetOp(0xBE, InstrLDX, AddrAbsoluteY, 3, 4);

            SetOp(0xA0, InstrLDY, AddrImmediate, 2, 2);
            SetOp(0xA4, InstrLDY, AddrZeroPage, 2, 3);
            SetOp(0xB4, InstrLDY, AddrZeroPageX, 2, 4);
            SetOp(0xAC, InstrLDY, AddrAbsolute, 3, 4);
            SetOp(0xBC, InstrLDY, AddrAbsoluteX, 3, 4);

            SetOp(0x85, InstrSTA, AddrZeroPage, 2, 3);
            SetOp(0x95, InstrSTA, AddrZeroPageX, 2, 4);
            SetOp(0x8D, InstrSTA, AddrAbsolute, 3, 4);
            SetOp(0x9D, InstrSTA, AddrAbsoluteX, 3, 5);
            SetOp(0x99, InstrSTA, AddrAbsoluteY, 3, 5);
            SetOp(0x81, InstrSTA, AddrIndirectX, 2, 6);
            SetOp(0x91, InstrSTA, AddrIndirectY, 2, 6);

            SetOp(0x86, InstrSTX, AddrZeroPage, 2, 3);
            SetOp(0x96, InstrSTX, AddrZeroPageY, 2, 4);
            SetOp(0x8E, InstrSTX, AddrAbsolute, 3, 4);

            SetOp(0x84, InstrSTY, AddrZeroPage, 2, 3);
            SetOp(0x94, InstrSTY, AddrZeroPageX, 2, 4);
            SetOp(0x8C, InstrSTY, AddrAbsolute, 3, 4);

            SetOp(0x48, InstrPHA, AddrNone, 1, 3);
            SetOp(0x08, InstrPHP, AddrNone, 1, 3);
            SetOp(0x68, InstrPLA, AddrNone, 1, 4);
            SetOp(0x28, InstrPLP, AddrNone, 1, 4);

            SetOp(0x69, InstrADC, AddrImmediate, 2, 2);
            SetOp(0x65, InstrADC, AddrZeroPage, 2, 3);
            SetOp(0x75, InstrADC, AddrZeroPageX, 2, 4);
            SetOp(0x6D, InstrADC, AddrAbsolute, 3, 4);
            SetOp(0x7D, InstrADC, AddrAbsoluteX, 3, 4);
            SetOp(0x79, InstrADC, AddrAbsoluteY, 3, 4);
            SetOp(0x61, InstrADC, AddrIndirectX, 2, 6);
            SetOp(0x71, InstrADC, AddrIndirectY, 2, 5);

            SetOp(0xE9, InstrSBC, AddrImmediate, 2, 2);
            SetOp(0xE5, InstrSBC, AddrZeroPage, 2, 3);
            SetOp(0xF5, InstrSBC, AddrZeroPageX, 2, 4);
            SetOp(0xED, InstrSBC, AddrAbsolute, 3, 4);
            SetOp(0xFD, InstrSBC, AddrAbsoluteX, 3, 4);
            SetOp(0xF9, InstrSBC, AddrAbsoluteY, 3, 4);
            SetOp(0xE1, InstrSBC, AddrIndirectX, 2, 6);
            SetOp(0xF1, InstrSBC, AddrIndirectY, 2, 5);

            SetOp(0x29, InstrAND, AddrImmediate, 2, 2);
            SetOp(0x25, InstrAND, AddrZeroPage, 2, 3);
            SetOp(0x35, InstrAND, AddrZeroPageX, 2, 4);
            SetOp(0x2D, InstrAND, AddrAbsolute, 3, 4);
            SetOp(0x3D, InstrAND, AddrAbsoluteX, 3, 4);
            SetOp(0x39, InstrAND, AddrAbsoluteY, 3, 4);
            SetOp(0x21, InstrAND, AddrIndirectX, 2, 6);
            SetOp(0x31, InstrAND, AddrIndirectY, 2, 5);

            SetOp(0x49, InstrEOR, AddrImmediate, 2, 2);
            SetOp(0x45, InstrEOR, AddrZeroPage, 2, 3);
            SetOp(0x55, InstrEOR, AddrZeroPageX, 2, 4);
            SetOp(0x4D, InstrEOR, AddrAbsolute, 3, 4);
            SetOp(0x5D, InstrEOR, AddrAbsoluteX, 3, 4);
            SetOp(0x59, InstrEOR, AddrAbsoluteY, 3, 4);
            SetOp(0x41, InstrEOR, AddrIndirectX, 2, 6);
            SetOp(0x51, InstrEOR, AddrIndirectY, 2, 5);

            SetOp(0x09, InstrORA, AddrImmediate, 2, 2);
            SetOp(0x05, InstrORA, AddrZeroPage, 2, 3);
            SetOp(0x15, InstrORA, AddrZeroPageX, 2, 4);
            SetOp(0x0D, InstrORA, AddrAbsolute, 3, 4);
            SetOp(0x1D, InstrORA, AddrAbsoluteX, 3, 4);
            SetOp(0x19, InstrORA, AddrAbsoluteY, 3, 4);
            SetOp(0x01, InstrORA, AddrIndirectX, 2, 6);
            SetOp(0x11, InstrORA, AddrIndirectY, 2, 5);

            SetOp(0xC9, InstrCMP, AddrImmediate, 2, 2);
            SetOp(0xC5, InstrCMP, AddrZeroPage, 2, 3);
            SetOp(0xD5, InstrCMP, AddrZeroPageX, 2, 4);
            SetOp(0xCD, InstrCMP, AddrAbsolute, 3, 4);
            SetOp(0xDD, InstrCMP, AddrAbsoluteX, 3, 4);
            SetOp(0xD9, InstrCMP, AddrAbsoluteY, 3, 4);
            SetOp(0xC1, InstrCMP, AddrIndirectX, 2, 6);
            SetOp(0xD1, InstrCMP, AddrIndirectY, 2, 5);
            SetOp(0xE0, InstrCPX, AddrImmediate, 2, 2);
            SetOp(0xE4, InstrCPX, AddrZeroPage, 2, 3);
            SetOp(0xEC, InstrCPX, AddrAbsolute, 3, 4);
            SetOp(0xC0, InstrCPY, AddrImmediate, 2, 2);
            SetOp(0xC4, InstrCPY, AddrZeroPage, 2, 3);
            SetOp(0xCC, InstrCPY, AddrAbsolute, 3, 4);

            SetOp(0x24, InstrBIT, AddrZeroPage, 2, 3);
            SetOp(0x2C, InstrBIT, AddrAbsolute, 3, 4);

            SetOp(0xE6, InstrINC, AddrZeroPage, 2, 5);
            SetOp(0xF6, InstrINC, AddrZeroPageX, 2, 6);
            SetOp(0xEE, InstrINC, AddrAbsolute, 3, 6);
            SetOp(0xFE, InstrINC, AddrAbsoluteX, 3, 7);
            SetOp(0xE8, InstrINX, AddrNone, 1, 2);
            SetOp(0xC8, InstrINY, AddrNone, 1, 2);

            SetOp(0xC6, InstrDEC, AddrZeroPage, 2, 5);
            SetOp(0xD6, InstrDEC, AddrZeroPageX, 2, 6);
            SetOp(0xCE, InstrDEC, AddrAbsolute, 3, 6);
            SetOp(0xDE, InstrDEC, AddrAbsoluteX, 3, 7);
            SetOp(0xCA, InstrDEX, AddrNone, 1, 2);
            SetOp(0x88, InstrDEY, AddrNone, 1, 2);

            SetOp(0x0A, InstrASL, AddrAccumulator, 1, 2);
            SetOp(0x06, InstrASL, AddrZeroPage, 2, 5);
            SetOp(0x16, InstrASL, AddrZeroPageX, 2, 6);
            SetOp(0x0E, InstrASL, AddrAbsolute, 3, 6);
            SetOp(0x1E, InstrASL, AddrAbsoluteX, 3, 7);

            SetOp(0x4A, InstrLSR, AddrAccumulator, 1, 2);
            SetOp(0x46, InstrLSR, AddrZeroPage, 2, 5);
            SetOp(0x56, InstrLSR, AddrZeroPageX, 2, 6);
            SetOp(0x4E, InstrLSR, AddrAbsolute, 3, 6);
            SetOp(0x5E, InstrLSR, AddrAbsoluteX, 3, 7);

            SetOp(0x2A, InstrROL, AddrAccumulator, 1, 2);
            SetOp(0x26, InstrROL, AddrZeroPage, 2, 5);
            SetOp(0x36, InstrROL, AddrZeroPageX, 2, 6);
            SetOp(0x2E, InstrROL, AddrAbsolute, 3, 6);
            SetOp(0x3E, InstrROL, AddrAbsoluteX, 3, 7);

            SetOp(0x6A, InstrROR, AddrAccumulator, 1, 2);
            SetOp(0x66, InstrROR, AddrZeroPage, 2, 5);
            SetOp(0x76, InstrROR, AddrZeroPageX, 2, 6);
            SetOp(0x6E, InstrROR, AddrAbsolute, 3, 6);
            SetOp(0x7E, InstrROR, AddrAbsoluteX, 3, 7);

            SetOp(0x4C, InstrJMP, AddrAbsolute, 3, 3);
            SetOp(0x6C, InstrJMP, AddrIndirectAbs, 3, 5);
            SetOp(0x20, InstrJSR, AddrAbsolute, 3, 6);
            SetOp(0x40, InstrRTI, AddrNone, 1, 6);
            SetOp(0x60, InstrRTS, AddrNone, 1, 6);

            SetOp(0x10, InstrBPL, AddrRelative, 2, 2);
            SetOp(0x30, InstrBMI, AddrRelative, 2, 2);
            SetOp(0x50, InstrBVC, AddrRelative, 2, 2);
            SetOp(0x70, InstrBVS, AddrRelative, 2, 2);
            SetOp(0x90, InstrBCC, AddrRelative, 2, 2);
            SetOp(0xB0, InstrBCS, AddrRelative, 2, 2);
            SetOp(0xD0, InstrBNE, AddrRelative, 2, 2);
            SetOp(0xF0, InstrBEQ, AddrRelative, 2, 2);

            SetOp(0x00, InstrBRK, AddrNone, 1, 7);

            SetOp(0x18, InstrCLC, AddrNone, 1, 2);
            SetOp(0x58, InstrCLI, AddrNone, 1, 2);
            SetOp(0xD8, InstrCLD, AddrNone, 1, 2);
            SetOp(0xB8, InstrCLV, AddrNone, 1, 2);
            SetOp(0x38, InstrSEC, AddrNone, 1, 2);
            SetOp(0x78, InstrSEI, AddrNone, 1, 2);
            SetOp(0xF8, InstrSED, AddrNone, 1, 2);

            SetOp(0xEA, InstrNOP, AddrNone, 1, 2);

            SetOp(0x87, IllInstrSAX, AddrZeroPage, 2, 3);
            SetOp(0x97, IllInstrSAX, AddrZeroPageY, 2, 4);
            SetOp(0x8F, IllInstrSAX, AddrAbsolute, 3, 4);
            SetOp(0x83, IllInstrSAX, AddrIndirectX, 2, 6);

            SetOp(0xA7, IllInstrLAX, AddrZeroPage, 2, 3);
            SetOp(0xB7, IllInstrLAX, AddrZeroPageY, 2, 4);
            SetOp(0xAF, IllInstrLAX, AddrAbsolute, 3, 4);
            SetOp(0xBF, IllInstrLAX, AddrAbsoluteY, 3, 4);
            SetOp(0xA3, IllInstrLAX, AddrIndirectX, 2, 6);
            SetOp(0xB3, IllInstrLAX, AddrIndirectY, 2, 5);

            SetOp(0x07, IllInstrSLO, AddrZeroPage, 2, 5);
            SetOp(0x17, IllInstrSLO, AddrZeroPageX, 2, 6);
            SetOp(0x03, IllInstrSLO, AddrIndirectX, 2, 8);
            SetOp(0x13, IllInstrSLO, AddrIndirectY, 2, 8);
            SetOp(0x0F, IllInstrSLO, AddrAbsolute, 3, 6);
            SetOp(0x1F, IllInstrSLO, AddrAbsoluteX, 3, 7);
            SetOp(0x1B, IllInstrSLO, AddrAbsoluteY, 3, 7);

            SetOp(0x27, IllInstrRLA, AddrZeroPage, 2, 5);
            SetOp(0x37, IllInstrRLA, AddrZeroPageX, 2, 6);
            SetOp(0x23, IllInstrRLA, AddrIndirectX, 2, 8);
            SetOp(0x33, IllInstrRLA, AddrIndirectY, 2, 8);
            SetOp(0x2F, IllInstrRLA, AddrAbsolute, 3, 6);
            SetOp(0x3F, IllInstrRLA, AddrAbsoluteX, 3, 7);
            SetOp(0x3B, IllInstrRLA, AddrAbsoluteY, 3, 7);

            SetOp(0x47, IllInstrSRE, AddrZeroPage, 2, 5);
            SetOp(0x57, IllInstrSRE, AddrZeroPageX, 2, 6);
            SetOp(0x43, IllInstrSRE, AddrIndirectX, 2, 8);
            SetOp(0x53, IllInstrSRE, AddrIndirectY, 2, 8);
            SetOp(0x4F, IllInstrSRE, AddrAbsolute, 3, 6);
            SetOp(0x5F, IllInstrSRE, AddrAbsoluteX, 3, 7);
            SetOp(0x5B, IllInstrSRE, AddrAbsoluteY, 3, 7);

            SetOp(0x67, IllInstrRRA, AddrZeroPage, 2, 5);
            SetOp(0x77, IllInstrRRA, AddrZeroPageX, 2, 6);
            SetOp(0x63, IllInstrRRA, AddrIndirectX, 2, 8);
            SetOp(0x73, IllInstrRRA, AddrIndirectY, 2, 8);
            SetOp(0x6F, IllInstrRRA, AddrAbsolute, 3, 6);
            SetOp(0x7F, IllInstrRRA, AddrAbsoluteX, 3, 7);
            SetOp(0x7B, IllInstrRRA, AddrAbsoluteY, 3, 7);

            SetOp(0xC7, IllInstrDCP, AddrZeroPage, 2, 5);
            SetOp(0xD7, IllInstrDCP, AddrZeroPageX, 2, 6);
            SetOp(0xC3, IllInstrDCP, AddrIndirectX, 2, 8);
            SetOp(0xD3, IllInstrDCP, AddrIndirectY, 2, 8);
            SetOp(0xCF, IllInstrDCP, AddrAbsolute, 3, 6);
            SetOp(0xDF, IllInstrDCP, AddrAbsoluteX, 3, 7);
            SetOp(0xDB, IllInstrDCP, AddrAbsoluteY, 3, 7);

            SetOp(0xE7, IllInstrISC, AddrZeroPage, 2, 5);
            SetOp(0xF7, IllInstrISC, AddrZeroPageX, 2, 6);
            SetOp(0xE3, IllInstrISC, AddrIndirectX, 2, 8);
            SetOp(0xF3, IllInstrISC, AddrIndirectY, 2, 8);
            SetOp(0xEF, IllInstrISC, AddrAbsolute, 3, 6);
            SetOp(0xFF, IllInstrISC, AddrAbsoluteX, 3, 7);
            SetOp(0xFB, IllInstrISC, AddrAbsoluteY, 3, 7);

            SetOp(0x0B, IllInstrANC, AddrImmediate, 2, 2);
            SetOp(0x2B, IllInstrANC, AddrImmediate, 2, 2);
            SetOp(0x4B, IllInstrALR, AddrImmediate, 2, 2);
            SetOp(0x6B, IllInstrARR, AddrImmediate, 2, 2);
            SetOp(0x8B, IllInstrXAA, AddrImmediate, 2, 2);
            SetOp(0xAB, IllInstrLAX, AddrImmediate, 2, 2);
            SetOp(0xCB, IllInstrAXS, AddrImmediate, 2, 2);
            SetOp(0xEB, IllInstrSBC, AddrImmediate, 2, 2);
            SetOp(0x93, IllInstrAHX, AddrIndirectY, 2, 6);
            SetOp(0x9F, IllInstrAHX, AddrAbsoluteY, 3, 5);
            SetOp(0x9C, IllInstrSHY, AddrAbsoluteX, 3, 5);
            SetOp(0x9E, IllInstrSHX, AddrAbsoluteY, 3, 5);
            SetOp(0x9B, IllInstrTAS, AddrAbsoluteY, 3, 5);
            SetOp(0xBB, IllInstrLAS, AddrAbsoluteY, 3, 4);

            SetOp(0x1A, IllInstrNOP, AddrNone, 1, 2);
            SetOp(0x3A, IllInstrNOP, AddrNone, 1, 2);
            SetOp(0x5A, IllInstrNOP, AddrNone, 1, 2);
            SetOp(0x7A, IllInstrNOP, AddrNone, 1, 2);
            SetOp(0xDA, IllInstrNOP, AddrNone, 1, 2);
            SetOp(0xFA, IllInstrNOP, AddrNone, 1, 2);

            SetOp(0x80, IllInstrNOP, AddrImmediate, 2, 2);
            SetOp(0x82, IllInstrNOP, AddrImmediate, 2, 2);
            SetOp(0x89, IllInstrNOP, AddrImmediate, 2, 2);
            SetOp(0xC2, IllInstrNOP, AddrImmediate, 2, 2);
            SetOp(0xE2, IllInstrNOP, AddrImmediate, 2, 2);

            SetOp(0x04, IllInstrNOP, AddrZeroPage, 2, 3);
            SetOp(0x44, IllInstrNOP, AddrZeroPage, 2, 3);
            SetOp(0x64, IllInstrNOP, AddrZeroPage, 2, 3);

            SetOp(0x14, IllInstrNOP, AddrZeroPageX, 2, 4);
            SetOp(0x34, IllInstrNOP, AddrZeroPageX, 2, 4);
            SetOp(0x54, IllInstrNOP, AddrZeroPageX, 2, 4);
            SetOp(0x74, IllInstrNOP, AddrZeroPageX, 2, 4);
            SetOp(0xD4, IllInstrNOP, AddrZeroPageX, 2, 4);
            SetOp(0xF4, IllInstrNOP, AddrZeroPageX, 2, 4);

            SetOp(0x0C, IllInstrNOP, AddrAbsolute, 3, 4);

            SetOp(0x1C, IllInstrNOP, AddrAbsoluteX, 3, 4);
            SetOp(0x3C, IllInstrNOP, AddrAbsoluteX, 3, 4);
            SetOp(0x5C, IllInstrNOP, AddrAbsoluteX, 3, 4);
            SetOp(0x7C, IllInstrNOP, AddrAbsoluteX, 3, 4);
            SetOp(0xDC, IllInstrNOP, AddrAbsoluteX, 3, 4);
            SetOp(0xFC, IllInstrNOP, AddrAbsoluteX, 3, 4);

            SetOp(0x02, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0x12, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0x22, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0x32, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0x42, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0x52, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0x62, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0x72, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0x92, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0xB2, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0xD2, IllInstrKIL, AddrNone, 1, 0);
            SetOp(0xF2, IllInstrKIL, AddrNone, 1, 0);
        }

        public static void SetOp(int op, int instr, int addr, int size, int cycles)
        {
            if ((opListing[op] & 0xFF) != 255)
                throw new Exception("Overlap");
            opListing[op] = (instr & 0xFF) + ((addr & 0xFF) << 8) + ((size & 0xFF) << 16) + ((cycles & 0xFF) << 24);
        }

        public static void LoadOpNames()
        {
            opNames = new string[256];
            opNames[0] = "ADC";
            opNames[1] = "AND";
            opNames[2] = "ASL";
            opNames[3] = "BIT";
            opNames[4] = "BCC";
            opNames[5] = "BCS";
            opNames[6] = "BEQ";
            opNames[7] = "BMI";
            opNames[8] = "BNE";
            opNames[9] = "BPL";
            opNames[10] = "BRK";
            opNames[11] = "BVC";
            opNames[12] = "BVS";
            opNames[13] = "CLC";
            opNames[14] = "CLD";
            opNames[15] = "CLI";
            opNames[16] = "CLV";
            opNames[17] = "CMP";
            opNames[18] = "CPX";
            opNames[19] = "CPY";
            opNames[20] = "DEC";
            opNames[21] = "DEX";
            opNames[22] = "DEY";
            opNames[23] = "EOR";
            opNames[24] = "INC";
            opNames[25] = "INX";
            opNames[26] = "INY";
            opNames[27] = "JMP";
            opNames[28] = "JSR";
            opNames[29] = "LDA";
            opNames[30] = "LDX";
            opNames[31] = "LDY";
            opNames[32] = "LSR";
            opNames[33] = "NOP";
            opNames[34] = "ORA";
            opNames[35] = "PHA";
            opNames[36] = "PHP";
            opNames[37] = "PLA";
            opNames[38] = "PLP";
            opNames[39] = "ROL";
            opNames[40] = "ROR";
            opNames[41] = "RTI";
            opNames[42] = "RTS";
            opNames[43] = "SBC";
            opNames[44] = "SEC";
            opNames[45] = "SED";
            opNames[46] = "SEI";
            opNames[47] = "STA";
            opNames[48] = "STX";
            opNames[49] = "STY";
            opNames[50] = "TAX";
            opNames[51] = "TAY";
            opNames[52] = "TSX";
            opNames[53] = "TXA";
            opNames[54] = "TXS";
            opNames[55] = "TYA";
            opNames[56] = "*AHX";
            opNames[57] = "*ANC";
            opNames[58] = "*ALR";
            opNames[59] = "*ARR";
            opNames[60] = "*AXS";
            opNames[61] = "*DCP";
            opNames[62] = "*ISC";
            opNames[63] = "*KIL";
            opNames[64] = "*LAS";
            opNames[65] = "*LAX";
            opNames[66] = "*NOP";
            opNames[67] = "*RLA";
            opNames[68] = "*RRA";
            opNames[69] = "*SAX";
            opNames[70] = "*SBC";
            opNames[71] = "*SHX";
            opNames[72] = "*SHY";
            opNames[73] = "*SLO";
            opNames[74] = "*SRE";
            opNames[75] = "*TAS";
            opNames[76] = "*XAA";
            opNames[255] = "Dummy";
        }/*
        public static void NewCPU()
        {
            int RegX;
            int RegY;
            int RegA;
            int RegPC = 0;
            int RegSP;
            int FlagZero;
            int FlagSign;
            int FlagCarry;
            int FlagDecimal;
            int FlagOverflow;
            int FlagBreak;
            int FlagIRQ;
            int FlagNotUsed;
            int op = 10;
            int opInfo;
            int opCycles;
            int opCycleAdd;
            int addressing;
            int instruction;
            int opAddr;
            int addr;
            int temp;
            int value;
            while (true)
            {
                op = Read(RegPC);
                opInfo = opListing[op];
                opCycles = (opInfo >> 24) & 0xFF;
                opCycleAdd = 0;
                addressing = (opInfo >> 8) & 0xFF;
                instruction = opInfo & 0xFF;
                opAddr = RegPC;
                RegPC += (opInfo >> 16) & 0xFF;
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
                        addr = ReadWord(ReadWord(opAddr + 1));
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
                        addr = ReadWord(addr);
                        break;
                    case OpInfo.AddrIndirectY:
                        addr = ReadWord(Read(opAddr));
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
                        RegA = FlagZero = FlagSign = temp & 0xFF;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrAND:
                        RegA &= Read(addr);
                        FlagZero = FlagSign = RegA;
                        opCycles += opCycleAdd;
                        break;
                    case OpInfo.InstrASL:
                        if (addressing == OpInfo.AddrAccumulator)
                        {
                            FlagCarry = (RegA >> 7) & 1;
                            RegA = (RegA << 1) & 0xFF;
                            FlagZero = FlagSign = RegA;
                        }
                        else
                        {
                            value = Read(addr);
                            FlagCarry = (value >> 7) & 1;
                            value = (value << 1) & 0xFF;
                            FlagZero = FlagSign = value;
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
                        RegA = FlagZero = (RegA - 1) & 0xFF;
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
                        value = 0;
                        if (FlagCarry != 0) value |= 1;
                        if (FlagZero == 0) value |= 2;
                        if (FlagIRQ != 0) value |= 4;
                        if (FlagDecimal != 0) value |= 8;
                        value |= 0x30;
                        if (FlagOverflow != 0) value |= 0x40;
                        if (FlagSign != 0) value |= 0x80;
                        PushByteStack(value);
                        break;
                    case OpInfo.InstrPLA:
                        RegA = FlagZero = PopByteStack();
                        FlagSign = RegA >> 7;
                        break;
                    case OpInfo.InstrPLP:
                        value = PopByteStack();
                        FlagCarry = value & 1;
                        FlagZero = (value & 2) == 0 ? 1 : 0;
                        FlagIRQ = value & 4;
                        FlagDecimal = value & 8;
                        FlagBreak = 1;
                        FlagNotUsed = 1;
                        FlagOverflow = value & 0x40;
                        FlagSign = value & 0x80;
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
                        FlagCarry = value & 1;
                        FlagZero = (value & 2) == 0 ? 1 : 0;
                        FlagIRQ = value & 4;
                        FlagDecimal = value & 8;
                        FlagBreak = 1;
                        FlagNotUsed = 1;
                        FlagOverflow = value & 0x40;
                        FlagSign = value & 0x80;
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
                        RegX = FlagZero = RegSP;
                        FlagSign = RegX >> 7;
                        break;
                    case OpInfo.InstrTXA:
                        RegA = FlagZero = RegX;
                        FlagSign = RegA >> 7;
                        break;
                    case OpInfo.InstrTXS:
                        RegSP = RegX;
                        break;
                    case OpInfo.InstrTYA:
                        RegA = FlagZero = RegY;
                        FlagSign = RegA >> 7;
                        break;
                    default:
                        switch (instruction) //Illegal Ops
                        {
                            case OpInfo.InstrDummy:
                                throw new Exception("Missing OP: " + op + " Program Counter: " + RegPC);
                                break;
                        }
                }
            }
        }
          */
    }
}
