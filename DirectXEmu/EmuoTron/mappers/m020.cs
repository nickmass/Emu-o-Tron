using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.mappers
{
    class m020 : Mapper
    {
        private byte[,] diskData;
        private int diskPointer;
        private bool irqEnable;
        private int irqReload;
        private int irqCounter;
        private bool dataIRQ;
        private bool timerIRQ;
        private bool soundControl;
        private bool dataControl;
        private byte externalConnector;
        private bool diskInserted;
        private bool diskWriteProtected;
        private bool lostData;
        private byte nextData;
        private bool dataHandled;
        private bool readWrite;
        private bool dataIRQTrigger;
        private bool driveMotor;
        private int diskOperationTime = 152; //96.4khz / 60 fps / 29780 cycles / frame * 8 bits

        public override bool interruptMapper
        {
            get
            {
                return dataIRQ || timerIRQ;
            }
        }
        public m020(NESCore nes)
        {
            this.nes = nes;
        }
        public override void Init()
        {
            nes.Memory.Swap8kROM(0xE000, 0);
            nes.Memory.Swap8kRAM(0x8000, 1);
            nes.Memory.Swap8kRAM(0xA000, 2);
            nes.Memory.Swap8kRAM(0xC000, 3);
            nes.Memory.SetReadOnly(0x6000, 8, false);
            nes.PPU.PPUMemory.Swap8kRAM(0, 0);
        }
        public override void Write(byte value, ushort address)
        {
            if((address & 0xFF00) == 0x4000)
            {
                switch (address)
                {
                    case 0x4020:
                        irqReload = (irqReload & 0xFF00) | value;
                        break;
                    case 0x4021:
                        irqReload = (irqReload & 0x00FF) | (value << 8);
                        break;
                    case 0x4022:
                        irqEnable = ((value & 1) != 0);
                        if(!irqEnable)
                            timerIRQ = false;
                        irqCounter = irqReload;
                        break;
                    case 0x4023:
                        dataControl = ((value & 1) != 0);
                        soundControl = ((value & 2) != 0);
                        break;
                    case 0x4024:
                        nextData = value;
                        dataHandled = true;
                        dataIRQ = false;
                        break;
                    case 0x4025:
                        driveMotor = ((value & 1) != 0);
                        readWrite = ((value & 4) != 0);
                        if ((value & 8) != 0)
                            nes.PPU.PPUMemory.HorizontalMirroring();
                        else
                            nes.PPU.PPUMemory.VerticalMirroring();
                        dataIRQTrigger = ((value & 0x80) != 0);
                        break;
                    case 0x4026:
                        externalConnector = value;
                        break;
                }
            }
        }
        public override byte Read(byte value, ushort address)
        {
            if ((address & 0xFF00) == 0x4000)
            {
                switch (address)
                {
                    case 0x4030:
                        value = 0;
                        if (timerIRQ)
                            value |= 1;
                        if (dataIRQ) 
                            value |= 2;
                        if (lostData)
                            value |= 0x40;
                        if(diskInserted)
                            value |= 0x80; //reable or writable
                        dataIRQ = false;
                        timerIRQ = false;
                        break;
                    case 0x4031:
                        value = nextData;
                        dataHandled = true;
                        dataIRQ = false;
                        nextData = 0;
                        break;
                    case 0x4032:
                        value = 0;
                        if (diskInserted)
                            value |= 3;
                        if (diskWriteProtected || !diskInserted)
                            value |= 4;
                        break;
                    case 0x4033:
                        value = externalConnector;
                        break;
                }
            }
            return value;
        
        }
        public override void IRQ(int cycles, int vblank)
        {
            if (irqEnable)
            {
                irqCounter -= cycles;
                if (irqCounter <= 0)
                {
                    timerIRQ = true;
                    irqCounter += irqReload;
                }
            }
        }
        public override void StateLoad(BinaryReader reader) { }
        public override void StateSave(BinaryWriter writer) { }
    }
}
