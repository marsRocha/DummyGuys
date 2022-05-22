using System;
using System.Collections.Generic;

namespace Multiplayer
{

    public class RoomHandle
    {
        public delegate void PacketHandler(Guid RoomId, Guid ClientId, Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        public static void InitializeData()
        {
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.test, Test },
            };
        }

        public static void Test(Guid _roomId, Guid _clientId, Packet packet)
        {
            Console.WriteLine($"Got test message from {_clientId}");
        }
    }
}
