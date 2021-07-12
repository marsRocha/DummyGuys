using System;
using UnityEngine;

public class MessageCaster : MonoBehaviour
{
    public PhysicsSceneManager simulationsManager;
    public Guid roomId;

    // Update is called once per frame
    void Update()
    {
        //SEND ROOM'S STARTGAME
        if (Input.GetKeyDown(KeyCode.O))
        {
            //Create new room
            Guid newGuid = Guid.NewGuid();
            Server.Rooms.Add(newGuid, new Room(newGuid, Server.GetNextAdress(), Server.multicastPort));
        }
    }
}
