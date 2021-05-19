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

    public RoomObjects AddSimulation(Guid _roomId, string _physicsSceneName)
    {
        //TODO: ONLY FOR NOW
        physicsScenes.Add(_roomId, SceneManager.GetActiveScene().GetPhysicsScene());
        Debug.Log(SceneManager.GetActiveScene().GetRootGameObjects().Length);
        return GameObject.Find("RoomObjects").GetComponent<RoomObjects>();

        /*//Load the scene to place in a local physics scene
        LoadSceneParameters param = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        Scene scene = SceneManager.LoadScene("1", param);
        scene.isSubScene = false;
        //Get the scene's physics scene
        physicsScenes.Add(_roomId, scene.GetPhysicsScene());
        Debug.Log(scene.GetRootGameObjects().Length);
        Debug.Log(scene.GetRootGameObjects().Length);

        return scene.GetRootGameObjects()[0].GetComponent<RoomObjects>();*/
    }

public void RemoveSimulation(Guid _roomId)
    {
        /*//Unload the scene
        LoadSceneParameters param = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        Scene scene = SceneManager.UnloadScene(physicsScenes[_roomId].);
        //Remove the scene's physics scene
        physicsScenes.Remove(_roomId);*/
    }
}
