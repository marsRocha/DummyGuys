using System;
using System.Collections.Generic;
using System.Text;

namespace Multiplayer
{
    public class ClientInfo
    {
        public readonly Guid id;
        public readonly string username;

        public ClientInfo(Guid _id, string _username)
        {
            id = _id;
            username = _username;
        }
    }
}
