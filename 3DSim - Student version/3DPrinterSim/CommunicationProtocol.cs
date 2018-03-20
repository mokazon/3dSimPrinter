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
            pc.WriteSerialToFirmware(header,header.Length);
            byte[] headerCheck = ReadBlocking(pc, header.Length);
            if (headerCheck[0] == header[0] && headerCheck[1] == header[1] && headerCheck[2] == header[2] && headerCheck[3] == header[3])
            {
                pc.WriteSerialToFirmware(new byte[] { 0xA5 },1);
                pc.WriteSerialToFirmware(pkt.Data,pkt.Data.Length);
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
                return result;
                //if(result == "SUCCESS" || result.Contains("VERSION")) { return true; }
                //return false;
            }
            pc.WriteSerialToFirmware(new byte[] { 0xFF }, 1);
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
                pc.ReadSerialFromFirmware(data, expectedBytes);
                if (data.Length == expectedBytes)
                {
                    timer.Stop();
                    return data;
                }
            }
            return new byte[0];
        }
    }
}
