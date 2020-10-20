using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;

namespace Server
{
    public partial class ServerController
    {
        private void Pong(IPEndPoint endPoint)
        {
            string playerJson = JsonConvert.SerializeObject("Pong");
            byte[] msg = Encoding.ASCII.GetBytes(playerJson);
            server.Send(msg, msg.Length, endPoint);
            Console.WriteLine("Pong");
        }

        private void NewPlayer(Player p, IPEndPoint endPoint)
        {
            p.Id = Guid.NewGuid();
            p.GameState = GameState.Connecting;
            p.MulticastIP = multicastIP.ToString();
            p.MulticastPort = multicastPort;
            _players.Add(p.Id, p);
            Console.WriteLine("New player: " + p.Name);

            //Send player back with generated ID
            string playerJson = JsonConvert.SerializeObject(p);
            byte[] msg = Encoding.ASCII.GetBytes(playerJson);
                
            server.Send(msg, msg.Length, endPoint);
        }

        private void SyncNewPlayer(Player p, IPEndPoint endPoint)
        {
            _players[p.Id] = p;
            Console.WriteLine($"My GameState: {p.GameState}");
            Console.WriteLine("Sending Players");
            Message message = new Message();
            message.MessageType = MessageType.LoadGameInfo;
            message.Description = JsonConvert.SerializeObject(_players);
            string messageJson = JsonConvert.SerializeObject(message);
            byte[] msg = Encoding.ASCII.GetBytes(messageJson);
            server.Send(msg, msg.Length, endPoint);
            Console.WriteLine("laod gameinfo sent");
        }

        private void JoinPlayer(Player p, IPEndPoint endPoint)
        {
            p.GameState = GameState.GameSync;
            _players[p.Id] = p;
            Message message = new Message();
            message.MessageType = MessageType.PlayerJoined;
            message.Description = JsonConvert.SerializeObject(p);
            string messageJson = JsonConvert.SerializeObject(message);
            byte[] msg = Encoding.ASCII.GetBytes(messageJson);
            //server.Send(msg, msg.Length, endPoint);
            server.Send(msg, msg.Length, multicastChannel);
            Console.WriteLine("New player joined sent");
        }

        private void SyncPlayerMovement(Player player, IPEndPoint endPoint)
        {
            if (_players[player.Id].GameState != GameState.GameSync)
                _players[player.Id].GameState = GameState.GameSync;

            foreach (Message m in player.Messages)
            {
                string messageJson = JsonConvert.SerializeObject(m);

                byte[] msg = Encoding.ASCII.GetBytes(messageJson);
                server.Send(msg, msg.Length, multicastChannel);
            }
            player.Messages.Clear();
        }
    }
}
