using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Inputs
{
    class Paddle : Input
    {
        int readAddress;
        bool controlReady;
        int paddleValue;
        int playerNum;
        public Paddle(NESCore nes, Port port)
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
                value = 0;
                if (nes.players[playerNum].zapper.triggerPulled)
                    value |= 0x8;
                if (controlReady)
                {
                    if ((paddleValue & 0x80) == 0)
                        value |= 0x10;
                    paddleValue = paddleValue << 1;
                }
            }
            return value;
        }
        public override void Write(byte value, ushort address)
        {
            if (address == 0x4016)
            {
                if ((value & 1) != 0)
                {
                    paddleValue = ((160 * nes.players[playerNum].zapper.x) / 256) + 84;
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
            writer.Write(paddleValue);
        }
        public override void StateLoad(BinaryReader reader)
        {
            controlReady = reader.ReadBoolean();
            paddleValue = reader.ReadInt32();
        }
    }
}
