using System;

namespace LR2_CSaN
{
    class ICMPPacket
    {
        const int ICMP_HEADER_SIZE = 4;
        const int IP_HEADER_SIZE = 20;
        const int MESSAGE_SIZE = 128;
        const int TYPE_ECHO_REQUEST = 8;

        private byte code;
        private UInt16 checksum;
        private UInt16 seq;
        private UInt16 id;
        private int messageSize;
        private byte[] message = new byte[MESSAGE_SIZE];
        public byte Type { get; set; }
        public int PacketSize { get; set; }

        public ICMPPacket(byte[] data)
        {
            Type = TYPE_ECHO_REQUEST;
            code = 0;
            Random random = new Random();
            seq = (UInt16)random.Next(0, UInt16.MaxValue);
            id = (UInt16)random.Next(0, UInt16.MaxValue);
            Buffer.BlockCopy(data, 0, message, ICMP_HEADER_SIZE, data.Length);
            messageSize = data.Length + 4;
            PacketSize = messageSize + ICMP_HEADER_SIZE;
            Buffer.BlockCopy(BitConverter.GetBytes(id), 0, message, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(seq), 0, message, 2, 2);
            checksum = getChecksum();
        }

        public ICMPPacket(byte[] data, int size)
        {
            Type = data[IP_HEADER_SIZE];
            code = data[IP_HEADER_SIZE + 1];
            checksum = BitConverter.ToUInt16(data, IP_HEADER_SIZE + 2);
            PacketSize = size - IP_HEADER_SIZE;
            messageSize = size - IP_HEADER_SIZE - ICMP_HEADER_SIZE;
            Buffer.BlockCopy(data, IP_HEADER_SIZE + ICMP_HEADER_SIZE, message, 0, messageSize);
        }

        public byte[] getBytes()
        {
            byte[] data = new byte[PacketSize + 1];
            Buffer.BlockCopy(BitConverter.GetBytes(Type), 0, data, 0, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(code), 0, data, 1, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(checksum), 0, data, 2, 2);
            Buffer.BlockCopy(message, 0, data, ICMP_HEADER_SIZE, messageSize);
            return data;
        }

        public void incSeq()
        {
            UInt16 inc = 1;
            byte[] bytes = BitConverter.GetBytes(inc);
            Array.Reverse(bytes, 0, bytes.Length);
            inc = BitConverter.ToUInt16(bytes, 0);
            seq += inc;

            Buffer.BlockCopy(BitConverter.GetBytes(seq), 0, message, 2, 2);
            checksum = 0;
            checksum = getChecksum();
        }

        public UInt16 getChecksum()
        {
            UInt32 checksum = 0;
            byte[] data = getBytes();
            int index = 0;

            while (index < PacketSize)
            {
                checksum += Convert.ToUInt32(BitConverter.ToUInt16(data, index));
                index += 2;
            }
            checksum = (checksum >> 16) + (checksum & 0xffff);
            checksum += (checksum >> 16);
            return (UInt16)(~checksum);
        }
    }
}