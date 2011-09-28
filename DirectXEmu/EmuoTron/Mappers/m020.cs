using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmuoTron.Mappers
{
    class m020 : Mapper
    {
        public uint crc;
        public int sideCount;
        public int currentSide;
        private byte[,] diskData;
        private int diskPointer;
        private bool irqEnable;
        private int irqReload;
        private int irqCounter;
        private bool dataIRQ;
        private bool timerIRQ;
        private bool soundControl;
        private bool dataControl;
        public bool diskInserted = true;
        private bool readWrite;
        private bool dataIRQTrigger;
        private bool driveMotor;
        private int diskOperationTime = 152; //96.4khz / 60 fps / 29780 cycles / frame * 8 bits = 152  this timing wouldnt translate to PAL but there was no disc system made for PAL systems so should I even try to emulate something that doesn't exist?
        private int diskOperationCounter;

        public override bool interruptMapper
        {
            get
            {
                return dataIRQ || timerIRQ;
            }
        }
        public m020(NESCore nes, Stream diskStream, bool ignoreFileCheck)
        {
            this.nes = nes;
            this.cycleIRQ = true;
            diskStream.Position = 0x0;
            if (!ignoreFileCheck)
            {
                if (diskStream.ReadByte() != 'F' || diskStream.ReadByte() != 'D' || diskStream.ReadByte() != 'S' || diskStream.ReadByte() != 0x1A)
                {
                    diskStream.Position = 0x0;
                    if (diskStream.ReadByte() != 0x01 || diskStream.ReadByte() != '*' || diskStream.ReadByte() != 'N' || diskStream.ReadByte() != 'I')
                    {
                        diskStream.Close();
                        throw (new BadHeaderException("Invalid File"));
                    }
                    else //NoIntro set uses many headerless roms which makes sense given how useless the header is.
                    {
                        diskStream.Position = 0x0;
                        sideCount = (int)(diskStream.Length / 65500);
                    }
                }
                else
                {
                    diskStream.Position = 0x4;
                    sideCount = diskStream.ReadByte();
                    diskStream.Position = 0x10;
                }
            }
            diskData = new byte[sideCount, 65500];
            crc = 0xFFFFFFFF;
            for (int side = 0; side < sideCount; side++)
            {
                for (int i = 0; i < 65500; i++)
                {
                    byte nextByte = (byte)diskStream.ReadByte();
                    diskData[side, i] = nextByte;
                    crc = CRC32.crc32_adjust(crc, nextByte);
                }
            }
            nes.debug.LogInfo("Disk Sides: " + sideCount.ToString());
            crc = crc ^ 0xFFFFFFFF;
            nes.APU.external = new Channels.FDS();
        }
        public override void Power()
        {
            nes.Memory.Swap8kROM(0xE000, 0);
            nes.Memory.Swap8kRAM(0x6000, 0, false);
            nes.Memory.Swap8kRAM(0x8000, 1, false);
            nes.Memory.Swap8kRAM(0xA000, 2, false);
            nes.Memory.Swap8kRAM(0xC000, 3, false);
            nes.PPU.PPUMemory.Swap8kRAM(0, 0, false);
            diskOperationCounter = diskOperationTime;
        }
        public override void Write(byte value, ushort address)
        {
            if((address & 0xFF00) == 0x4000)
            {
                nes.APU.external.Write(value, address);
                switch (address)
                {
                    case 0x4020:
                        irqReload = (irqReload & 0xFF00) | value;
                        timerIRQ = false;
                        break;
                    case 0x4021:
                        irqReload = (irqReload & 0x00FF) | (value << 8);
                        timerIRQ = false;
                        break;
                    case 0x4022:
                        irqEnable = ((value & 2) != 0);
                        timerIRQ = false;
                        irqCounter = irqReload;
                        break;
                    case 0x4023:
                        dataControl = ((value & 1) != 0);
                        soundControl = ((value & 2) != 0);
                        break;
                    case 0x4024:
                        if (diskInserted && dataControl && !readWrite)
                        {
                            if ((diskPointer >= 0) && (diskPointer < 65000))
                            {
                                diskData[currentSide, diskPointer] = value;
                                dataIRQ = false;
                                diskOperationCounter = diskOperationTime;
                                if (diskPointer < 64999)
                                    diskPointer++;
                            }
                        }
                        break;
                    case 0x4025:
                        driveMotor = ((value & 1) != 0);
                        readWrite = ((value & 4) != 0);
                        if ((value & 0x40) == 0)//http://nesdev.parodius.com/bbs/viewtopic.php?t=738&highlight=fds .fds files do not contain crc bytes so have to jump pointer back
                        {
                            diskPointer -= 2;
                            if (diskPointer < 0)
                                diskPointer = 0;
                        }
                        if (diskPointer < 0)
                            diskPointer = 0;
                        if ((value & 8) != 0)
                            nes.PPU.PPUMemory.HorizontalMirroring();
                        else
                            nes.PPU.PPUMemory.VerticalMirroring();
                        dataIRQTrigger = ((value & 0x80) != 0);
                        if ((value & 0x02) != 0)
                        {
                            diskPointer = 0;
                            diskOperationCounter = diskOperationTime;
                        }
                        break;
                    case 0x4026:
                        break;
                }
            }
        }
        public override byte Read(byte value, ushort address)
        {
            if ((address & 0xFF00) == 0x4000)
            {
                value = nes.APU.external.Read(value, address);
                switch (address)
                {
                    case 0x4030:
                        value = 0;
                        if (timerIRQ)
                            value |= 1;
                        if (dataIRQ) 
                            value |= 2;
                        if(diskInserted)
                            value |= 0x80; //reable or writable
                        dataIRQ = false;
                        timerIRQ = false;
                        break;
                    case 0x4031:
                        if (diskInserted)
                        {
                            value = diskData[currentSide, diskPointer];
                            dataIRQ = false;
                            diskOperationCounter = diskOperationTime;
                            if (diskPointer < 64999)
                                diskPointer++;
                        }
                        break;
                    case 0x4032:
                        value = 0;
                        if (!diskInserted)
                            value |= 5;
                        if (!diskInserted || !driveMotor)
                            value |= 2;
                        break;
                    case 0x4033:
                        value = 0x80;
                        break;
                }
            }
            return value;
        
        }
        public override void IRQ(int cycles)
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
            if (diskOperationCounter > 0)
                diskOperationCounter -= cycles;
            if (diskOperationCounter <= 0 && readWrite && dataIRQTrigger)
            {
                dataIRQ = true;
            }
        }

        public void EjectDisk(bool diskInserted)
        {
            this.diskInserted = diskInserted;
            diskPointer = 0;
            diskOperationCounter = diskOperationTime;
        }
        public void SetDiskSide(int diskSide)
        {
            this.currentSide = diskSide;
            diskPointer = 0;
            diskOperationCounter = diskOperationTime;
        }
        public override void StateLoad(BinaryReader reader)
        {
            currentSide = reader.ReadInt32();
            diskPointer = reader.ReadInt32();
            irqEnable = reader.ReadBoolean();
            irqReload = reader.ReadInt32();
            irqCounter = reader.ReadInt32();
            dataIRQ = reader.ReadBoolean();
            timerIRQ = reader.ReadBoolean();
            soundControl = reader.ReadBoolean();
            dataControl = reader.ReadBoolean();
            diskInserted = reader.ReadBoolean();
            readWrite = reader.ReadBoolean();
            dataIRQTrigger = reader.ReadBoolean();
            driveMotor = reader.ReadBoolean();
            diskOperationCounter = reader.ReadInt32();
        }
        public override void StateSave(BinaryWriter writer)
        {
            writer.Write(currentSide);
            writer.Write(diskPointer);
            writer.Write(irqEnable);
            writer.Write(irqReload);
            writer.Write(irqCounter);
            writer.Write(dataIRQ);
            writer.Write(timerIRQ);
            writer.Write(soundControl);
            writer.Write(dataControl);
            writer.Write(diskInserted);
            writer.Write(readWrite);
            writer.Write(dataIRQTrigger);
            writer.Write(driveMotor);
            writer.Write(diskOperationCounter);
        }
    }
}
