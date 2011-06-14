using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Inputs
{
    class Paddle : Input
    {
        bool controlReady;
        int paddleValue;
        int playerNum;
        public Paddle(NESCore nes, Port port)
        {
            this.nes = nes;
            this.port = port;
            if (port == Port.PortOne)
            {
                playerNum = 0;
            }
            else if (port == Port.PortTwo)
            {
                playerNum = 1;
            }
        }
        public override byte Read(ushort address)
        {
            byte value = 0;
            if (nes.players[playerNum].triggerPulled)
                value |= 0x8;
            if (controlReady)
            {
                if ((paddleValue & 0x80) == 0)
                    value |= 0x10;
                paddleValue = paddleValue << 1;
            }
            return value;
        }
        public override void Write(byte value, ushort address)
        {
            if (address == 0x4016)
            {
                if ((value & 1) != 0)
                {
                    paddleValue = ((160 * nes.players[playerNum].x) / 256) + 84;
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
