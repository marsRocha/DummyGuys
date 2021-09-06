using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Server.Start();

            Console.WriteLine("[Server console initiated]\n-Press 'Any Key' to send a message\n-Press 'Enter' to exit\n");

            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    Server.SendTcp();

                    //Send to client
                    Server.SendUdp();

                    //Multicast
                    Server.SendMulticast();
                }
            }
        }
    }
}

