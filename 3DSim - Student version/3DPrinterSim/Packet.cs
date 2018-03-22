﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace PrinterSimulator
{
    class Packet
    {
        public byte Command;
        public uint Length;
        public ushort Checksum = 0;
        public byte[] Data;

        public Packet(byte Command, byte[] Data)
        {
            this.Command = Command;
            this.Data = Data;
            Length = (uint)Data.Length;
            CalculateChecksum();
        }

        public byte[] GetHeaderBytes()
        {
            List<byte> result = new List<byte>();
            result.Add(Command);
            result.Add((byte)Length);
            result.Add(BitConverter.GetBytes(Checksum)[0]);
            result.Add(BitConverter.GetBytes(Checksum)[1]);
            return result.ToArray();
        }

        public void CalculateChecksum()
        {
            byte[] header = GetHeaderBytes();
            header[2] = 0;//Set High/Low checksums to 0
            header[3] = 0;
            byte[] all = header.Concat(Data).ToArray();
            ushort sum = 0;
            for (int i = 0; i < all.Length; i+=2)
            {
                if(i+1>=all.Length)
                {
                    sum += all[i];
                    continue;
                }
                sum += (ushort)BitConverter.ToInt16(all, i);
            }
            Checksum = sum;
        }

        public static Packet LaserOnOffCommand(bool onOff)
        {
            return new Packet((byte)CommunicationCommand.Laser, BitConverter.GetBytes(onOff));
        }

        public static Packet ResetBuildPlatformCommand()
        {
            return new Packet((byte)CommunicationCommand.ResetBuildPlatform, new byte[0]);
        }

        public static Packet RaiseBuildPlatformCommand()
        {
            return new Packet((byte)CommunicationCommand.RaiseBuildPlatform, new byte[0]);
        }

        public static Packet ToTopCommand()
        {
            return new Packet((byte)CommunicationCommand.ToTop, new byte[0]);
        }

        public static Packet AimLaserCommand(float x, float y)
        {
            byte[] xData = BitConverter.GetBytes(x);
            byte[] yData = BitConverter.GetBytes(y);
            return new Packet((byte)CommunicationCommand.AimLaser, xData.Concat(yData).ToArray());
        }

        public static Packet GetFirmwareVersionCommand()
        {
            return new Packet((byte)CommunicationCommand.GetFirmwareVersion, new byte[0]);
        }
    }
}
