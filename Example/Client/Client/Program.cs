using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    class Program
    {


        static void Main(string[] args)
        {
            Client.Start();

            Console.WriteLine("[Client console initiated]\n-Press 'Any Key' to send a message\n-Press 'Enter' to exit\n");


            while (true)
            {
                if(Console.ReadKey(true).Key == ConsoleKey.Spacebar)
                {
                    Client.SendTcpConnect();
                }
                
                if(Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    Client.SendTcp();

                    //Send to server
                    Client.udp.SendData();

                    //Multicast to clients
                    Client.SendUDPMulticast();
                }
            }
        }
    }
}