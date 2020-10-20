using Common;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityMonoTaskHandler : MonoBehaviour
{
    public void Move(Message m, Guid id, Dictionary<Guid, Player> players)
    {
        PlayerInfo p = JsonConvert.DeserializeObject<PlayerInfo>(m.Description);

        /*if (p.Id != id)
        {
            GameObject obj = players[p.Id].gameObject;

            Vector3 pos = new Vector3(p.X, p.Y, p.Z);

            obj.GetComponent<Slave>().GoToPosition(pos);
            Quaternion rot = Quaternion.Euler(p.RotX, p.RotY, p.RotZ);
            obj.transform.rotation = rot;
        }*/
    }

    public void Interact(Guid id, Dictionary<Guid, Player> players)
    {
        //GameObject obj = players[id].gameObject;
        //obj.GetComponent<Slave>().interact
    }
}