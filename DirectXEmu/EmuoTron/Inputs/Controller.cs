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
        int shiftCount;
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
                    if (shiftCount < 0)
                    {
                        value |= 1; //Should be a stream of 1s when control data is used up with official controllers
                    }
                    else
                    {
                        value |= (byte)(controlReg & 1);
                        controlReg >>= 1;
                        shiftCount--;
                    }
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
                    shiftCount = 0;
                    if (nes.fourScore)
                    {
                        //Third Read, Signature
                        controlReg |= 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= (playerNum == 0) ? 1 : 0; //0x10 for player 1
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= (playerNum == 1) ? 1 : 0; //0x20 for player 2
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= 0;
                        controlReg <<= 1;
                        shiftCount++;

                        //Second Read, Players 3/4
                        controlReg |= nes.players[playerNum + 2].right ? 1 : 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= nes.players[playerNum + 2].left ? 1 : 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= nes.players[playerNum + 2].down ? 1 : 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= nes.players[playerNum + 2].up ? 1 : 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= nes.players[playerNum + 2].start ? 1 : 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= nes.players[playerNum + 2].select ? 1 : 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= nes.players[playerNum + 2].b ? 1 : 0;
                        controlReg <<= 1;
                        shiftCount++;
                        controlReg |= nes.players[playerNum + 2].a ? 1 : 0;
                        controlReg <<= 1;
                        shiftCount++;
                    }
                    //First Read, Players 1/2
                    controlReg |= nes.players[playerNum].right ? 1 : 0;
                    controlReg <<= 1;
                    shiftCount++;
                    controlReg |= nes.players[playerNum].left ? 1 : 0;
                    controlReg <<= 1;
                    shiftCount++;
                    controlReg |= nes.players[playerNum].down ? 1 : 0;
                    controlReg <<= 1;
                    shiftCount++;
                    controlReg |= nes.players[playerNum].up ? 1 : 0;
                    controlReg <<= 1;
                    shiftCount++;
                    controlReg |= nes.players[playerNum].start ? 1 : 0;
                    controlReg <<= 1;
                    shiftCount++;
                    controlReg |= nes.players[playerNum].select ? 1 : 0;
                    controlReg <<= 1;
                    shiftCount++;
                    controlReg |= nes.players[playerNum].b ? 1 : 0;
                    controlReg <<= 1;
                    shiftCount++;
                    controlReg |= nes.players[playerNum].a ? 1 : 0;
                    if (nes.filterIllegalInput)
                    {
                        controlReg = (controlReg & 0xFFFF00) | FilterJoypad(controlReg & 0xFF);
                    }
                    controlReady = false;
                }
                else
                {
                    controlReady = true;
                }
            }
        }

        int x_axis = 0xc0;
        int y_axis = 0x30;
        int mask = ~0x50;
        int prev;

        private int FilterJoypad(int joypad) //Code from Blargg, http://nesdev.parodius.com/bbs/viewtopic.php?t=711 , I think ideally I would handle this in te UI code of my various ports but this seems far more simple.
        {
            int changed = prev ^ joypad;
            int hidden = joypad & ~mask;

            if (((changed & x_axis) != 0) && ((hidden & x_axis) != 0))
                mask ^= x_axis;

            if (((changed & y_axis) != 0) && ((hidden & y_axis) != 0))
                mask ^= y_axis;

            prev = joypad;
            return joypad & mask;
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(controlReady);
            writer.Write(controlReg);
            writer.Write(shiftCount);
        }
        public override void StateLoad(BinaryReader reader)
        {
            controlReady = reader.ReadBoolean();
            controlReg = reader.ReadInt32();
            shiftCount = reader.ReadInt32();
        }
    }
}
