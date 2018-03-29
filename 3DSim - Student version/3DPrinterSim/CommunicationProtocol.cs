using System;
using System.Collections.Generic;
using System.Text;
using Hardware;
using System.Timers;
namespace PrinterSimulator
{
    class CommunicationProtocol
    {
        static public string SendPacket(PrinterControl pc, Packet pkt)
        {
            byte[] header = pkt.GetHeaderBytes();
            //Console.WriteLine("Host - Sending header: " + header[0] + "," + header[1] + "," + header[2] + "," + header[3]);
            pc.WriteSerialToFirmware(header,header.Length);
            byte[] headerCheck = ReadBlocking(pc, header.Length);
            //Console.WriteLine("Host - Received header: " + headerCheck[0] + "," + headerCheck[1] + "," + headerCheck[2] + "," + headerCheck[3]);
            if (headerCheck[0] == header[0] && headerCheck[1] == header[1] && headerCheck[2] == header[2] && headerCheck[3] == header[3])
            {
                //Console.WriteLine("Host - Sending ACK");
                pc.WriteSerialToFirmware(new byte[] { 0xA5 },1);
                //Console.WriteLine("Host - Sent ACK");
                //Console.WriteLine("Host - Sending Data");
                //foreach(byte x in pkt.Data) { Console.WriteLine("H: "+x); }
                pc.WriteSerialToFirmware(pkt.Data,pkt.Data.Length);
                //Console.WriteLine("Host - Sent Data");
                //Console.WriteLine("Host - Waiting for response");
                byte[] partialResponse = Read(pc,1);
                List<byte> response = new List<byte>();
                while(partialResponse.Length == 0) { partialResponse = Read(pc, 1); }
                while (partialResponse[0] != 0)
                {
                    response.Add(partialResponse[0]);
                    partialResponse = Read(pc, 1);
                    if(partialResponse.Length == 0) { break; }
                }
                string result = ASCIIEncoding.ASCII.GetString(response.ToArray());
                //Console.WriteLine("Host - Got Response");
                return result;
            }
            //Console.WriteLine("Host - Sending NACK");
            pc.WriteSerialToFirmware(new byte[] { 0xFF }, 1);
            //Console.WriteLine("Host - SentNACK");
            return "Invalid Header";
        }
        /// <summary>
        /// Read data from firmware. Blocks the thread until the expected bytes are received.
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="expectedBytes">The number of bytes expected</param>
        /// <returns></returns>
        public static byte[] ReadBlocking(Hardware.PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            while (pc.ReadSerialFromFirmware(data, expectedBytes) == 0) { }
            return data;
        }

        /// <summary>
        /// Read data from firmware. Returns a byte array of length 0 if no data can be read.
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="expectedBytes">The number of bytes expected</param>
        /// <returns></returns>
        public static byte[] Read(Hardware.PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            if (pc.ReadSerialFromFirmware(data, expectedBytes) == 0) { return new byte[0]; }
            return data;
        }

        /// <summary>
        /// Attempts to read the given number of bytes from the firmware for the given amount of time. Returns a byte array of length 0 if no data was read.
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="expectedBytes">The number of bytes expected</param>
        /// <param name="milliseconds">Time to wait in miliseconds</param>
        /// <returns></returns>
        public static byte[] ReadWait(Hardware.PrinterControl pc, int expectedBytes, int milliseconds)
        {
            byte[] data = new byte[expectedBytes];
            Timer timer = new Timer(milliseconds);
            timer.AutoReset = false;
            timer.Start();
            while (timer.Enabled)
            {
                int i = pc.ReadSerialFromFirmware(data, expectedBytes);
                if (i!=0)
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
