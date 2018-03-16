using System;
using System.Collections.Generic;
using System.Text;
using Hardware;
namespace PrinterSimulator
{
    class CommunicationProtocol
    {
        public bool SendPacket(PrinterControl pc, Packet pkt)
        {
            byte[] header = pkt.GetHeaderBytes();
            pc.WriteSerialToFirmware(header,header.Length);
            byte[] headerCheck = Read(pc, header.Length);
            if (headerCheck[0] == header[0] && headerCheck[1] == header[1] && headerCheck[2] == header[2] && headerCheck[3] == header[3])
            {
                pc.WriteSerialToFirmware(new byte[] { 0xA5 },1);
                pc.WriteSerialToFirmware(pkt.Data,pkt.Data.Length);
                string result = Encoding.ASCII.GetString(Read(pc, Encoding.ASCII.GetByteCount("SUCCESS")));
                if(result == "SUCCESS") { return true; }
                return false;
            }
            pc.WriteSerialToFirmware(new byte[] { 0xFF }, 1);
            return false;
        }

        public byte[] Read(PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            while (pc.ReadSerialFromFirmware(data, expectedBytes) == 0) { }
            return data;
        }
    }
}
