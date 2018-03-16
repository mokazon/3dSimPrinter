using System;
using System.Collections.Generic;
using System.Text;

namespace Firmware
{
    class CommunicationProtocol
    {
        public static byte[] ReadBlocking(Hardware.PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            while(pc.ReadSerialFromHost(data,expectedBytes) == 0) { }
            return data;
        }

        public static byte[] Read(Hardware.PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            if(pc.ReadSerialFromHost(data, expectedBytes) == 0) { return new byte[0]; }
            return data;
        }
    }
}
