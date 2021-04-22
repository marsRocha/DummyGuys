using System;
using System.Collections.Generic;
using System.Text;

namespace Multiplayer
{
    class GameLogic
    {
        public static void Update()
        {
            /*foreach (Room room in Server.Rooms.Values)
            {
                if (room.RoomState == RoomState.playing)
                {
                    //Update room logic

                    //Clients
                    foreach (Player player in room.Players.Values)
                    {
                        player.Update();
                    }
                }
            }*/

            ThreadManager.UpdateMain();
        }
    }
}
