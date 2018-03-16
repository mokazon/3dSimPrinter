using System;
using System.Collections.Generic;
using System.Text;
using Hardware;
namespace PrinterSimulator
{
    class CommunicationProtocol
    {
        static public bool SendPacket(PrinterControl pc, Packet pkt)
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
                if(result == "SUCCESS") { return true; }
                return false;
            }
            pc.WriteSerialToFirmware(new byte[] { 0xFF }, 1);
            return false;
        }


        public static byte[] Read(Hardware.PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            if (pc.ReadSerialFromFirmware(data, expectedBytes) == 0) { return new byte[0]; }
            return data;
        }

        static public byte[] ReadBlocking(PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            while (pc.ReadSerialFromFirmware(data, expectedBytes) == 0) { }
            return data;
        }
    }
}
