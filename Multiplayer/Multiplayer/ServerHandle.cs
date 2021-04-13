using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;

namespace Multiplayer
{
    class ServerHandle
    {
        public static void WelcomeReceived(int idFromClient, Packet packet)
        {
            int clientIdCheck = packet.ReadInt();
            string username = packet.ReadString();

            Console.WriteLine($"{Server.clients[idFromClient].tcp.socket.Client.RemoteEndPoint} ({username}) connected successfully and is now player {idFromClient}.");
            if(idFromClient != clientIdCheck)
            {
                Console.WriteLine($"Player \"{username}\" (ID: {idFromClient}) has assumed the wrong client ID ({clientIdCheck}).");
            }

            //////////
            if (idFromClient > 1)
            {
                string peerPort = ((IPEndPoint)Server.clients[idFromClient].tcp.socket.Client.RemoteEndPoint).Port.ToString();
                string peerIP = ((IPEndPoint)Server.clients[idFromClient].tcp.socket.Client.RemoteEndPoint).Address.ToString();
                ServerSend.Peer(idFromClient, username, peerIP, peerPort, 1);
                Console.WriteLine("Sent peer");
            }
        }
    }
}
