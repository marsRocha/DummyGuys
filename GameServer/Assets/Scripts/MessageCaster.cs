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
            foreach(Room room in Server.Rooms.Values)
            {
                room.Stop();
            }

            Debug.Log(Server.Rooms.Count);
        }
    }
}
