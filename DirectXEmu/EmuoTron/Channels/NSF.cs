using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmuoTron.Channels
{
    class NSF : Channel
    {
        byte FlagVRC6 = 0x01;
        byte FlagVRC7 = 0x02;
        byte FlagFDS = 0x04;
        byte FlagMMC5 = 0x08;
        byte FlagN163 = 0x10;
        byte FlagFME7 = 0x20;

        int chipCount;

        bool hasMMC5 = false;
        byte multiplicand;
        byte multiplier;

        Channel VRC6;
        Channel VRC7;
        Channel FDS;
        Channel MMC5;
        Channel N163;
        Channel FME7;

        public NSF(NESCore nes, int specialChips)
        {
            this.nes = nes;
            chipCount = 0;
            if ((FlagVRC6 & specialChips) != 0)
            {
                VRC6 = new Channels.VRC6(0, 1, 2, 3);
                chipCount++;
            }
            else
            {
                VRC6 = new Channels.External();
            }
            if ((FlagVRC7 & specialChips) != 0)
            {
                VRC7 = new Channels.External();
                chipCount++;
            }
            else
            {
                VRC7 = new Channels.External();
            }
            if ((FlagFDS & specialChips) != 0)
            {
                FDS = new Channels.FDS();
                chipCount++;
            }
            else
            {
                FDS = new Channels.External();
            }
            if ((FlagMMC5 & specialChips) != 0)
            {
                hasMMC5 = true;
                MMC5 = new Channels.MMC5(nes);
                chipCount++;
            }
            else
            {
                MMC5 = new Channels.External();
            }
            if ((FlagN163 & specialChips) != 0)
            {
                N163 = new Channels.N163();
                chipCount++;
            }
            else
            {
                N163 = new Channels.External();
            }
            if ((FlagFME7 & specialChips) != 0)
            {
                FME7 = new Channels.FME7();
                chipCount++;
            }
            else
            {
                FME7 = new Channels.External();
            }
        }

        public override byte Read(byte value, ushort address)
        {
            value = FDS.Read(value, address);
            value = MMC5.Read(value, address);
            value = N163.Read(value, (ushort)(address & 0xF800));
            if (hasMMC5)//Apparently some MMC5 NSFs need this.
            {
                switch (address)
                {
                    case 0x5205:
                        value = (byte)((multiplicand * multiplier) & 0xFF);
                        break;
                    case 0x5206:
                        value = (byte)((multiplicand * multiplier) >> 8);
                        break;
                }
            }
            return value;
        }

        public override void Write(byte value, ushort address)
        {
            VRC6.Write(value, address);
            FDS.Write(value, address);
            MMC5.Write(value, address);
            FME7.Write(value, (ushort)(address & 0xE000));
            N163.Write(value, (ushort)(address & 0xF800));
            if (hasMMC5)
            {
                switch (address)
                {
                    case 0x5205:
                        multiplicand = value;
                        break;
                    case 0x5206:
                        multiplier = value;
                        break;
                }
            }
        }

        public override void Power()
        {
            VRC6.Power();
            VRC7.Power();
            FDS.Power();
            MMC5.Power();
            N163.Power();
            FME7.Power();
        }
        
        public override void Reset()
        {
            VRC6.Reset();
            VRC7.Reset();
            FDS.Reset();
            MMC5.Reset();
            N163.Reset();
            FME7.Reset();
        }
        
        public override void HalfFrame()
        {
            VRC6.HalfFrame();
            VRC7.HalfFrame();
            FDS.HalfFrame();
            MMC5.HalfFrame();
            N163.HalfFrame();
            FME7.HalfFrame();
        }

        public override void QuarterFrame()
        {
            VRC6.QuarterFrame();
            VRC7.QuarterFrame();
            FDS.QuarterFrame();
            MMC5.QuarterFrame();
            N163.QuarterFrame();
            FME7.QuarterFrame();
        }

        public override byte Cycle()
        {
            byte volume = 0;
            volume += VRC6.Cycle();
            volume += VRC7.Cycle();
            volume += FDS.Cycle();
            volume += MMC5.Cycle();
            volume += N163.Cycle();
            volume += FME7.Cycle();
            volume = (byte)(volume / (chipCount * 1.0));
            if (hasMMC5)
            {
                nes.mapper.interruptMapper = ((Channels.MMC5)MMC5).interrupt;
            }
            return volume;
        }

        public override void StateSave(System.IO.BinaryWriter writer)
        {
            VRC6.StateSave(writer);
            VRC7.StateSave(writer);
            FDS.StateSave(writer);
            MMC5.StateSave(writer);
            N163.StateSave(writer);
            FME7.StateSave(writer);
        }

        public override void StateLoad(System.IO.BinaryReader reader)
        {
            VRC6.StateLoad(reader);
            VRC7.StateLoad(reader);
            FDS.StateLoad(reader);
            MMC5.StateLoad(reader);
            N163.StateLoad(reader);
            FME7.StateLoad(reader);
        }
    }
}
