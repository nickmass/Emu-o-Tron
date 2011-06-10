using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron
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
        public static int[] dummyReads;

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
            LoadDummyReads();
        }

        public static void SetOp(int op, int instr, int addr, int size, int cycles)
        {
            if ((opListing[op] & 0xFF) != 255)
                throw new Exception("Overlap");
            opListing[op] = (instr & 0xFF) + ((addr & 0xFF) << 8) + ((size & 0xFF) << 16) + ((cycles & 0xFF) << 24);
        }
        public const int DummyNever = 0;
        public const int DummyOnCarry = 1;
        public const int DummyAlways = 2;
        public static void LoadDummyReads()
        {
            dummyReads = new int[256];
            dummyReads[0x1D] = DummyOnCarry;
            dummyReads[0x19] = DummyOnCarry;
            dummyReads[0x11] = DummyOnCarry;
            dummyReads[0x3D] = DummyOnCarry;
            dummyReads[0x39] = DummyOnCarry;
            dummyReads[0x31] = DummyOnCarry;
            dummyReads[0x5D] = DummyOnCarry;
            dummyReads[0x59] = DummyOnCarry;
            dummyReads[0x51] = DummyOnCarry;
            dummyReads[0x7D] = DummyOnCarry;
            dummyReads[0x79] = DummyOnCarry;
            dummyReads[0x71] = DummyOnCarry;
            dummyReads[0x9D] = DummyAlways;
            dummyReads[0x99] = DummyAlways;
            dummyReads[0x91] = DummyAlways;
            dummyReads[0xBD] = DummyOnCarry;
            dummyReads[0xB9] = DummyOnCarry;
            dummyReads[0xB1] = DummyOnCarry;
            dummyReads[0xDD] = DummyOnCarry;
            dummyReads[0xD9] = DummyOnCarry;
            dummyReads[0xD1] = DummyOnCarry;
            dummyReads[0xFD] = DummyOnCarry;
            dummyReads[0xF9] = DummyOnCarry;
            dummyReads[0xF1] = DummyOnCarry;
            dummyReads[0x1E] = DummyAlways;
            dummyReads[0x3E] = DummyAlways;
            dummyReads[0x5E] = DummyAlways;
            dummyReads[0x7E] = DummyAlways;
            dummyReads[0xDE] = DummyAlways;
            dummyReads[0xFE] = DummyAlways;
            dummyReads[0xBC] = DummyOnCarry;
            dummyReads[0xBE] = DummyOnCarry;


            //Illegals
            dummyReads[0x1C] = DummyOnCarry;
            dummyReads[0x3C] = DummyOnCarry;
            dummyReads[0x5C] = DummyOnCarry;
            dummyReads[0x7C] = DummyOnCarry;
            dummyReads[0xDC] = DummyOnCarry;
            dummyReads[0xFC] = DummyOnCarry;

            //Not positive about this block
            dummyReads[0x1F] = DummyAlways;
            dummyReads[0x1B] = DummyAlways;
            dummyReads[0x13] = DummyAlways;
            dummyReads[0x3F] = DummyAlways;
            dummyReads[0x3B] = DummyAlways;
            dummyReads[0x33] = DummyAlways;
            dummyReads[0x5F] = DummyAlways;
            dummyReads[0x5B] = DummyAlways;
            dummyReads[0x53] = DummyAlways;
            dummyReads[0x7F] = DummyAlways;
            dummyReads[0x7B] = DummyAlways;
            dummyReads[0x73] = DummyAlways;
            dummyReads[0xDF] = DummyAlways;
            dummyReads[0xDB] = DummyAlways;
            dummyReads[0xD3] = DummyAlways;
            dummyReads[0xFF] = DummyAlways;
            dummyReads[0xFB] = DummyAlways;
            dummyReads[0xF3] = DummyAlways;

            dummyReads[0x9C] = DummyAlways;
            dummyReads[0xBF] = DummyOnCarry;
            dummyReads[0x9B] = DummyAlways;
            dummyReads[0x9E] = DummyAlways;
            dummyReads[0x9F] = DummyAlways;
            dummyReads[0xBB] = DummyOnCarry;
            dummyReads[0x93] = DummyAlways;
            dummyReads[0xB3] = DummyOnCarry;
        }
        public static void LoadOpNames()
        {
            opNames = new string[256];
            opNames[InstrADC] = "ADC";
            opNames[InstrAND] = "AND";
            opNames[InstrASL] = "ASL";
            opNames[InstrBIT] = "BIT";
            opNames[InstrBCC] = "BCC";
            opNames[InstrBCS] = "BCS";
            opNames[InstrBEQ] = "BEQ";
            opNames[InstrBMI] = "BMI";
            opNames[InstrBNE] = "BNE";
            opNames[InstrBPL] = "BPL";
            opNames[InstrBRK] = "BRK";
            opNames[InstrBVC] = "BVC";
            opNames[InstrBVS] = "BVS";
            opNames[InstrCLC] = "CLC";
            opNames[InstrCLD] = "CLD";
            opNames[InstrCLI] = "CLI";
            opNames[InstrCLV] = "CLV";
            opNames[InstrCMP] = "CMP";
            opNames[InstrCPX] = "CPX";
            opNames[InstrCPY] = "CPY";
            opNames[InstrDEC] = "DEC";
            opNames[InstrDEX] = "DEX";
            opNames[InstrDEY] = "DEY";
            opNames[InstrEOR] = "EOR";
            opNames[InstrINC] = "INC";
            opNames[InstrINX] = "INX";
            opNames[InstrINY] = "INY";
            opNames[InstrJMP] = "JMP";
            opNames[InstrJSR] = "JSR";
            opNames[InstrLDA] = "LDA";
            opNames[InstrLDX] = "LDX";
            opNames[InstrLDY] = "LDY";
            opNames[InstrLSR] = "LSR";
            opNames[InstrNOP] = "NOP";
            opNames[InstrORA] = "ORA";
            opNames[InstrPHA] = "PHA";
            opNames[InstrPHP] = "PHP";
            opNames[InstrPLA] = "PLA";
            opNames[InstrPLP] = "PLP";
            opNames[InstrROL] = "ROL";
            opNames[InstrROR] = "ROR";
            opNames[InstrRTI] = "RTI";
            opNames[InstrRTS] = "RTS";
            opNames[InstrSBC] = "SBC";
            opNames[InstrSEC] = "SEC";
            opNames[InstrSED] = "SED";
            opNames[InstrSEI] = "SEI";
            opNames[InstrSTA] = "STA";
            opNames[InstrSTX] = "STX";
            opNames[InstrSTY] = "STY";
            opNames[InstrTAX] = "TAX";
            opNames[InstrTAY] = "TAY";
            opNames[InstrTSX] = "TSX";
            opNames[InstrTXA] = "TXA";
            opNames[InstrTXS] = "TXS";
            opNames[InstrTYA] = "TYA";
            opNames[IllInstrAHX] = "*AHX";
            opNames[IllInstrANC] = "*ANC";
            opNames[IllInstrALR] = "*ALR";
            opNames[IllInstrARR] = "*ARR";
            opNames[IllInstrAXS] = "*AXS";
            opNames[IllInstrDCP] = "*DCP";
            opNames[IllInstrISC] = "*ISC";
            opNames[IllInstrKIL] = "*KIL";
            opNames[IllInstrLAS] = "*LAS";
            opNames[IllInstrLAX] = "*LAX";
            opNames[IllInstrNOP] = "*NOP";
            opNames[IllInstrRLA] = "*RLA";
            opNames[IllInstrRRA] = "*RRA";
            opNames[IllInstrSAX] = "*SAX";
            opNames[IllInstrSBC] = "*SBC";
            opNames[IllInstrSHX] = "*SHX";
            opNames[IllInstrSHY] = "*SHY";
            opNames[IllInstrSLO] = "*SLO";
            opNames[IllInstrSRE] = "*SRE";
            opNames[IllInstrTAS] = "*TAS";
            opNames[IllInstrXAA] = "*XAA";
            opNames[InstrDummy] = "Dummy";
        }
    }
}
