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

        Channel VRC6;
        Channel VRC7;
        Channel FDS;
        Channel MMC5;
        Channel N163;
        Channel FME7;

        public NSF(NESCore nes, int specialChips)
        {
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
                FDS = new Channels.External();
                chipCount++;
            }
            else
            {
                FDS = new Channels.External();
            }
            if ((FlagMMC5 & specialChips) != 0)
            {
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
            value = MMC5.Read(value, address);
            value = N163.Read(value, (ushort)(address & 0xF800));
            return value;
        }

        public override void Write(byte value, ushort address)
        {
            VRC6.Write(value, address);
            MMC5.Write(value, address);
            FME7.Write(value, (ushort)(address & 0xE000));
            N163.Write(value, (ushort)(address & 0xF800));
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

        public override void HalfFrame()
        {
            VRC6.HalfFrame();
            VRC7.HalfFrame();
            FDS.HalfFrame();
            MMC5.HalfFrame();
            N163.HalfFrame();
            FME7.HalfFrame();
        }

        public override void QuaterFrame()
        {
            VRC6.QuaterFrame();
            VRC7.QuaterFrame();
            FDS.QuaterFrame();
            MMC5.QuaterFrame();
            N163.QuaterFrame();
            FME7.QuaterFrame();
        }

        public override int Cycle()
        {
            int volume = 0;
            volume += VRC6.Cycle();
            volume += VRC7.Cycle();
            volume += FDS.Cycle();
            volume += MMC5.Cycle();
            volume += N163.Cycle();
            volume += FME7.Cycle();
            volume = (int)(volume / (chipCount * 1.0));
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
