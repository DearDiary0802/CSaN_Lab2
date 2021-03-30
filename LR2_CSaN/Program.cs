using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LR2_CSaN
{
    class Program
    {
        const int TYPE_ECHO_RESPONSE = 0;
        const int TYPE_TTL_EXCEEDED = 11;
        const int MESSAGE_SIZE = 128;

        static bool showName = true;
        static byte[] message = Encoding.ASCII.GetBytes("0000000000000000000000000000000000000000000000000000000000000000");

        static void Main(string[] args)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            string name;
            if (args.Length == 1)
            {
                name = args[0];
            }
            else if (args.Length == 2 && args[0] == "-d")
            {
                name = args[1];
                showName = false;
            }
            else
            {
                Console.Write("Mytracert ");
                name = Console.ReadLine();
            }

            try
            {
                IPHostEntry ipHost = Dns.GetHostEntry(name);
                IPEndPoint destIP = new IPEndPoint(ipHost.AddressList[0], 0);
                for (int index = 1; index < ipHost.AddressList.Length; index++)
                {
                    if (ipHost.AddressList[index].ToString() == name)
                    {
                        destIP = new IPEndPoint(ipHost.AddressList[index], 0);
                    } 
                }

                ICMPPacket packet = new ICMPPacket(message);

                string ResultIP = destIP.ToString();
                ResultIP = ResultIP.Remove(ResultIP.IndexOf(':'), ResultIP.Length - ResultIP.IndexOf(':'));
                try
                {
                    if (showName)
                    {
                        string ResultHostName = Dns.GetHostEntry(ResultIP).HostName;
                        if (ResultHostName.IndexOf(name) != -1)
                            Console.WriteLine("Трассировка маршрута к {0} [{1}]", name, ResultIP);
                        else
                            Console.WriteLine("Трассировка маршрута к {0} [{1}]", ResultHostName, ResultIP);
                    }
                    else
                        Console.WriteLine("Трассировка маршрута к {0}", ResultIP);
                }
                catch (SocketException)
                {
                    Console.WriteLine("Трассировка маршрута к {0}", ResultIP);
                }
                Console.WriteLine("с максимальным числом прыжков 30: ");

                Traceroute(socket, packet, destIP);
            }
            catch (SocketException)
            {
                Console.WriteLine("Не удается разрешить системное имя узла {0}.", name);
            }            
            socket.Close();
        }

        static void Traceroute(Socket socket, ICMPPacket packet, IPEndPoint destIP)
        {
            int responseSize;
            DateTime timeStart, timeEnd;
            byte[] responseMessage;
        
            EndPoint hopIP = destIP;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);
            for (int i = 1; i < 31; i++)
            {
                EndPoint BufPoint = null;
                Console.Write("{0, 3} ", i);
                for (int j = 0; j < 3; j++)
                {
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, i);
                    timeStart = DateTime.Now;
                    socket.SendTo(packet.getBytes(), packet.PacketSize, SocketFlags.None, destIP);
                    try
                    {
                        responseMessage = new byte[MESSAGE_SIZE];
                        responseSize = socket.ReceiveFrom(responseMessage, ref hopIP);
                        timeEnd = DateTime.Now;
                        ICMPPacket response = new ICMPPacket(responseMessage, responseSize);
                        if ((response.Type == TYPE_ECHO_RESPONSE) || (response.Type == TYPE_TTL_EXCEEDED))
                        {
                            BufPoint = hopIP;
                            if ((timeEnd.Ticks - timeStart.Ticks) / 10000 > 0)
                                Console.Write("{0, 5} ms", (timeEnd.Ticks - timeStart.Ticks) / 10000);
                            else 
                                Console.Write("   <1 ms");
                            if (j == 2)
                            {
                                string IP = hopIP.ToString();
                                IP = IP.Remove(IP.IndexOf(':'), IP.Length - IP.IndexOf(':'));
                                try
                                {
                                    if (showName)
                                    {
                                        string HostName = Dns.GetHostEntry(IP).HostName;
                                        Console.WriteLine("   {0} [{1}]", HostName, IP);
                                    }
                                    else
                                        Console.WriteLine("   {0}", IP);
                                }
                                catch (SocketException)
                                {
                                    Console.WriteLine("   {0}", IP);
                                }
                            }
                        }
                        if ((response.Type == TYPE_ECHO_RESPONSE) && (j == 2))
                        {
                            Console.WriteLine("\nТрассировка завершена.");
                            return;
                        }
                    }
                    catch (SocketException)
                    {
                        Console.Write("    *   ");
                        if (j == 2 && BufPoint == null)
                        {
                            Console.WriteLine("   Превышен интервал ожидания для запроса.", i);
                        }
                        else if (j == 2)
                        {
                            string IP = hopIP.ToString();
                            IP = IP.Remove(IP.IndexOf(':'), IP.Length - IP.IndexOf(':'));
                            try
                            {
                                if (showName)
                                {
                                    string HostName = Dns.GetHostEntry(IP).HostName;
                                    Console.WriteLine("{0} [{1}]", IP, HostName);
                                }
                                else
                                    Console.WriteLine("{0}", IP);
                            }
                            catch (SocketException)
                            {
                                Console.WriteLine("{0}", IP);
                            }
                        }
                    }
                    packet.incSeq();
                }
                if (i == 30)
                {
                    Console.WriteLine("Невозможно связаться с удаленным хостом.");
                }
            }
        }
    }
}