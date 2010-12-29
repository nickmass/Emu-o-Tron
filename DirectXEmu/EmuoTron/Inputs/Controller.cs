using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Inputs
{
    class Controller : Input
    {
        int readAddress;
        bool controlReady;
        int controlReg;
        int playerNum;
        public Controller(NESCore nes, Port port)
        {
            this.nes = nes;
            this.port = port;
            if (port == Port.PortOne)
            {
                readAddress = 0x4016;
                playerNum = 0;
            }
            else if (port == Port.PortTwo)
            {
                readAddress = 0x4017;
                playerNum = 1;
            }
        }
        public override byte Read(byte value, ushort address)
        {
            if (address == readAddress)
            {
                if (controlReady)
                {
                    value |= (byte)(controlReg & 1);
                    controlReg >>= 1;
                }
            }
            return value;
        }
        public override void Write(byte value, ushort address)
        {
            if (address == 0x4016)
            {
                if ((value & 0x01) == 1)
                {
                    controlReg = 0;
                    if (nes.fourScore)
                    {
                        controlReg |= 1;
                        controlReg <<= 1;
                        controlReg |= 0;
                        controlReg <<= 1;
                        controlReg |= 0;
                        controlReg <<= 1;
                        controlReg |= 0;
                        controlReg <<= 1;
                        controlReg |= nes.players[playerNum + 2].right ? 1 : 0;
                        controlReg <<= 1;
                        controlReg |= nes.players[playerNum + 2].left ? 1 : 0;
                        controlReg <<= 1;
                        controlReg |= nes.players[playerNum + 2].down ? 1 : 0;
                        controlReg <<= 1;
                        controlReg |= nes.players[playerNum + 2].up ? 1 : 0;
                        controlReg <<= 1;
                        controlReg |= nes.players[playerNum + 2].start ? 1 : 0;
                        controlReg <<= 1;
                        controlReg |= nes.players[playerNum + 2].select ? 1 : 0;
                        controlReg <<= 1;
                        controlReg |= nes.players[playerNum + 2].b ? 1 : 0;
                        controlReg <<= 1;
                        controlReg |= nes.players[playerNum + 2].a ? 1 : 0;
                        controlReg <<= 1;
                    }
                    controlReg |= nes.players[playerNum].right ? 1 : 0;
                    controlReg <<= 1;
                    controlReg |= nes.players[playerNum].left ? 1 : 0;
                    controlReg <<= 1;
                    controlReg |= nes.players[playerNum].down ? 1 : 0;
                    controlReg <<= 1;
                    controlReg |= nes.players[playerNum].up ? 1 : 0;
                    controlReg <<= 1;
                    controlReg |= nes.players[playerNum].start ? 1 : 0;
                    controlReg <<= 1;
                    controlReg |= nes.players[playerNum].select ? 1 : 0;
                    controlReg <<= 1;
                    controlReg |= nes.players[playerNum].b ? 1 : 0;
                    controlReg <<= 1;
                    controlReg |= nes.players[playerNum].a ? 1 : 0;
                    controlReady = false;
                }
                else
                {
                    controlReady = true;
                }
            }
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(controlReady);
            writer.Write(controlReg);
        }
        public override void StateLoad(BinaryReader reader)
        {
            controlReady = reader.ReadBoolean();
            controlReg = reader.ReadInt32();
        }
    }
}
