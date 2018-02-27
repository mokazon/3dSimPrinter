using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace Testing
{
    class Packet
    {
        public byte Command;
        public uint Length;
        public byte LowChecksum = 0;
        public byte HighChecksum = 0;
        public byte[] Data;

        public Packet(byte Command, byte[] Data)
        {
            this.Command = Command;
            this.Data = Data;
            Length = 0;// (uint)Data.Length;
        }

        public byte[] GetHeaderBytes()
        {
            List<byte> result = new List<byte>();
            result.Add(Command);
            result.Add((byte)Length);
            result.Add(LowChecksum);
            result.Add(HighChecksum);
            return result.ToArray();
        }

        public static Tuple<byte,byte> CalculateChecksum(Packet pckt)
        {
            byte[] header = pckt.GetHeaderBytes();
            header[2] = 0;//Set High/Low checksums to 0
            header[3] = 0;
            byte[] all = header.Concat(pckt.Data).ToArray();
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
            byte highChecksum = BitConverter.GetBytes(sum)[0];
            byte lowChecksum = BitConverter.GetBytes(sum)[1];
            return Tuple.Create(highChecksum, lowChecksum);
        }
    }
}
