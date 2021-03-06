﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class MMC5 : Channel
    {
        Square square1;
        Square square2;

        bool readMode;
        bool irqEnable;
        bool irqTrip;
        byte pcmData;

        public bool interrupt
        {
            get
            {
                return irqEnable && irqTrip;
            }
        }
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
                nes.APU.Update();
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
                    case 0x5010:
                        readMode = (value & 1) != 0;
                        irqEnable = (value & 0x80) != 0;
                        break;
                    case 0x5011:
                        if (!readMode)
                        {
                            if (value == 0)
                                irqTrip = true;
                            else
                            {
                                pcmData = value;
                            }
                        }
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
                case 0x5010:
                    value = 0;
                    value |= (byte)(interrupt ? 0x80 : 0x00);
                    irqTrip = false;
                    break;
                case 0x5015:
                    value = 0;
                    value |= square1.Read(0, 0);
                    value |= (byte)(square2.Read(0, 0) << 1);
                    break;
            }
            if (readMode && address >= 0x8000 && address <=0xBFFF)
            {
                if (value == 0)
                    irqTrip = true;
                else
                {
                    nes.APU.Update();
                    pcmData = value;
                }
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
        public override void QuarterFrame()
        {
            square1.QuarterFrame();
            square2.QuarterFrame();
        }
        public override byte Cycle()
        {
            byte volume = 0;
            volume += (byte)(square1.Cycle() * 4);
            volume += (byte)(square2.Cycle() * 4);
            volume += (byte)(pcmData >> 1);
            return volume;
        }
        public override void StateSave(System.IO.BinaryWriter writer)
        {
            square1.StateSave(writer);
            square2.StateSave(writer);
            writer.Write(readMode);
            writer.Write(irqEnable);
            writer.Write(irqTrip);
            writer.Write(pcmData);
        }
        public override void StateLoad(System.IO.BinaryReader reader)
        {
            square1.StateLoad(reader);
            square2.StateLoad(reader);
            readMode = reader.ReadBoolean();
            irqEnable = reader.ReadBoolean();
            irqTrip = reader.ReadBoolean();
            pcmData = reader.ReadByte();
        }
    }
}
