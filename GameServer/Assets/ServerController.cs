using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        //#if UNITY_EDITOR
        //Debug.Log("Build the project to start the server!");
        //#else
        Server.Start(26950);
        //#endif
    }
}
