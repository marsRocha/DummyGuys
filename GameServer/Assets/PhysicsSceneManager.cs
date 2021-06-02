using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

public class PhysicsSceneManager : MonoBehaviour
{
    private Dictionary<Guid, PhysicsScene> physicsScenes;
    private Scene main;

    private void Start()
    {
        physicsScenes = new Dictionary<Guid, PhysicsScene>();
        main = SceneManager.GetActiveScene();
    }

    private void Update()
    {

    }

    /*private void FixedUpdate()
    {
        //Simulate the scene on FixedUpdate
        if (physicsScenes.Count > 0)
        {
            foreach (PhysicsScene physicsScene in physicsScenes.Values)
                physicsScene.Simulate(Time.fixedDeltaTime);
        }
    }*/

    public void AddSimulation(Guid _roomId, string _physicsSceneName)
    {
        //Load the scene to place in a local physics scene
        LoadSceneParameters param = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        Scene scene = SceneManager.LoadScene("Level1", param);

        //Get the scene's physics scene
        physicsScenes.Add(_roomId, scene.GetPhysicsScene());

        StartCoroutine(WaitFrame(scene, _roomId));
    }

    public void RemoveSimulation(Guid _roomId)
    {
        /*//Unload the scene
        LoadSceneParameters param = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        Scene scene = SceneManager.UnloadScene(physicsScenes[_roomId].);
        //Remove the scene's physics scene
        physicsScenes.Remove(_roomId);*/
    }

    IEnumerator WaitFrame(Scene scene, Guid _roomId)
    {
        //returning 0 will make it wait 1 frame
        yield return 0;

        Server.Rooms[_roomId].roomObjects = scene.GetRootGameObjects()[0].GetComponent<RoomObjects>();
        Server.Rooms[_roomId].Initialize();
    }
}
