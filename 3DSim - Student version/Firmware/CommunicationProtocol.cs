using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
namespace Firmware
{
    class CommunicationProtocol
    {
        /// <summary>
        /// Read data from host. Blocks the thread until the expected bytes are received.
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="expectedBytes">The number of bytes expected</param>
        /// <returns></returns>
        public byte[] ReadBlocking(Hardware.PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            while(pc.ReadSerialFromHost(data,expectedBytes) == 0) { }
            return data;
        }

        /// <summary>
        /// Read data from host. Returns a byte array of length 0 if no data can be read.
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="expectedBytes">The number of bytes expected</param>
        /// <returns></returns>
        public byte[] Read(Hardware.PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            if(pc.ReadSerialFromHost(data, expectedBytes) == 0) { return new byte[0]; }
            return data;
        }

        /// <summary>
        /// Attempts to read the given number of bytes from the host for the given amount of time. Returns a byte array of length 0 if no data was read.
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="expectedBytes">The number of bytes expected</param>
        /// <param name="milliseconds">Time to wait in miliseconds</param>
        /// <returns></returns>
        public byte[] ReadWait(Hardware.PrinterControl pc, int expectedBytes, int milliseconds)
        {
            byte[] data = new byte[expectedBytes];
            //[] d = ReadBlocking(pc, 1);
            //if(expectedBytes-1 == 0) { return d; }
            Timer timer = new Timer(milliseconds);
            timer.AutoReset = false;
            timer.Start();
            while(timer.Enabled)
            {
                int i = pc.ReadSerialFromHost(data, expectedBytes);
                if(i != 0)
                {
                    timer.Stop();
                    return data;
                }
            }
            return new byte[0];
        }
    }

    public enum CommunicationCommand
    {
        Laser = 0,
        ResetBuildPlatform = 1,
        RaiseBuildPlatform = 2,
        ToTop = 3,
        AimLaser = 4,
        GetFirmwareVersion = 5
    }
}
