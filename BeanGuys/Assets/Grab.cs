using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grab : MonoBehaviour
{
    public bool grabbing;
    public GameObject grabbedObj;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Mouse0) && !grabbing)
        {
            if (!grabbedObj)
                return;

            /*FixedJoint fj = grabbedObj.AddComponent<FixedJoint>();
            fj.connectedBody = gameObject.transform.root.GetComponent<Rigidbody>();
            fj.breakForce = 9001;*/
            grabbing = true;

            ClientSend.PlayerGrab(grabbedObj.GetComponent<RemotePlayerManager>().Id, 1);

            transform.root.GetComponent<PlayerController>().Grab(true);
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0) && grabbing)
        {
            grabbing = false;

            ClientSend.PlayerLetGo(grabbedObj.GetComponent<RemotePlayerManager>().Id, 1);

            transform.root.GetComponent<PlayerController>().Grab(false);
        }

        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            if (!grabbedObj)
                return;

            ClientSend.PlayerPush(grabbedObj.GetComponent<RemotePlayerManager>().Id, 1);

            transform.root.GetComponent<PlayerController>().Push();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.root.gameObject.layer.Equals(LayerMask.NameToLayer("RemotePlayer")))
        {
            grabbedObj = other.gameObject.transform.root.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Exit");
        if (other.gameObject.transform.root.gameObject.layer.Equals(LayerMask.NameToLayer("RemotePlayer")))
        {
            grabbedObj = null;
        }
    }
}
