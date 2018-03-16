using System;
using System.Collections.Generic;
using System.Text;

namespace Firmware
{
    class CommunicationProtocol
    {
        public  static byte[] Read(Hardware.PrinterControl pc, int expectedBytes)
        {
            byte[] data = new byte[expectedBytes];
            while(pc.ReadSerialFromHost(data,expectedBytes) == 0) { }
            return data;
        }
    }
}
