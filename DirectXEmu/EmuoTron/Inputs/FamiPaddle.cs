using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Inputs
{
    class FamiPaddle : Input
    {
        bool controlReady;
        int paddleValue;
        int playerNum;
        public FamiPaddle(NESCore nes, Port port) //I know this plugged into the expandsion port, but if I dont have a player number how do I know who's keybinds to use?
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
        public override byte Read(byte value, ushort address)
        {
            if (address == 0x4016)
            {
                if (nes.players[playerNum].triggerPulled)
                    value |= 0x2;
            }
            else if (address == 0x4017)
            {
                if (controlReady)
                {
                    if ((paddleValue & 0x80) == 0)
                        value |= 0x2;
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
                    paddleValue = ((170 * nes.players[playerNum].x) / 256) + 74; //Tweaked range for Arkanoid 2, paddle can reach both playfield sides this way.
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
