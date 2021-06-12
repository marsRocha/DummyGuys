using System;
using UnityEngine;

public class MessageCaster : MonoBehaviour
{
    public PhysicsSceneManager simulationsManager;
    public Guid roomId;

    // Update is called once per frame
    void Update()
    {
        //SEND ROOM'S MAP
        if (Input.GetKeyDown(KeyCode.O))
        {
            foreach (Room room in Server.Rooms.Values)
            {
                roomId = room.Id;
                break;
            }

            //Add new room scene
            simulationsManager.AddSimulation(roomId, "Level1");

            Server.Rooms[roomId].Map("Level1"); //TODO: DEBUG INPUT
        }

        //SEND ROOM'S STARTGAME
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (roomId == null)
                return;

            Server.Rooms[roomId].StartGame();
        }
    }
}
