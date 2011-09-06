//Doesnt suppport MMC5 PCM, but the MMC5 mapper doesnt support any of the games that use it so I have a ways to go.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class MMC5 : Channel
    {
        Square square1;
        Square square2;

        public MMC5(NESCore nes)
        {
            this.nes = nes;
            square1 = new Square(nes, false, false);
            square2 = new Square(nes, false, false);
        }

        public override void Write(byte value, ushort address)
        {
            if (address >= 0x5000 && address <= 0x5017)
            {
                switch (address)
                {
                    case 0x5000:
                        square1.Write(value, 0);
                        break;
                    case 0x5001:
                        square1.Write(value, 1);
                        break;
                    case 0x5002:
                        square1.Write(value, 2);
                        break;
                    case 0x5003:
                        square1.Write(value, 3);
                        break;
                    case 0x5004:
                        square2.Write(value, 0);
                        break;
                    case 0x5005:
                        square2.Write(value, 1);
                        break;
                    case 0x5006:
                        square2.Write(value, 2);
                        break;
                    case 0x5007:
                        square2.Write(value, 3);
                        break;
                    case 0x5015:
                        square1.Write((byte)(value & 1), 4);
                        square2.Write((byte)(value & 2), 4);
                        break;
                }
            }
        }
        public override byte Read(byte value, ushort address)
        {
            switch (address)
            {
                case 0x5015:
                    value = 0;
                    value |= square1.Read(0, 0);
                    value |= (byte)(square2.Read(0, 0) << 1);
                    break;
            }
            return value;
        }
        public override void Power()
        {
            square1.Power();
            square2.Power();
        }
        public override void HalfFrame()
        {
            square1.HalfFrame();
            square2.HalfFrame();
        }
        public override void QuaterFrame()
        {
            square1.QuaterFrame();
            square2.QuaterFrame();
        }
        public override int Cycle()
        {
            int volume = 0;
            volume += square1.Cycle();
            volume += square2.Cycle();
            return volume * 8;
        }
        public override void StateSave(System.IO.BinaryWriter writer)
        {
            square1.StateSave(writer);
            square2.StateSave(writer);
        }
        public override void StateLoad(System.IO.BinaryReader reader)
        {
            square1.StateLoad(reader);
            square2.StateLoad(reader);
        }
    }
}
