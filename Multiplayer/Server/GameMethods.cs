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
        private void SyncData(Player p)
        {
            foreach (Message m in p.Messages)
            {
                Console.WriteLine(m.MessageType);
                string messageJson = JsonConvert.SerializeObject(m);

                byte[] msg = Encoding.ASCII.GetBytes(messageJson);
                server.Send(msg, msg.Length, multicastChannel);
            }
            p.Messages.Clear();
        }
    }
}
