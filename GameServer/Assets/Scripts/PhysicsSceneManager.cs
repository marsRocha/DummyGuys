using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

public class PhysicsSceneManager : MonoBehaviour
{
    public static PhysicsSceneManager instance;

    private static Dictionary<Guid, Scene> physicsScenes;

    private void Awake()
    {
        instance = this;
        physicsScenes = new Dictionary<Guid, Scene>();
    }

    public static void AddSimulation(Guid _roomId, string _physicsSceneName)
    {
        // Load the scene to place in a local physics scene
        LoadSceneParameters param = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        Scene scene = SceneManager.LoadScene(_physicsSceneName, param);

        // Get the scene's physics scene
        physicsScenes.Add(_roomId, scene);

        instance.StartCoroutine(WaitFrame(scene, _roomId, _physicsSceneName));
    }

    public static void RemoveSimulation(Guid _roomId)
    {
        // Unload the scene
        LoadSceneParameters param = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        SceneManager.UnloadSceneAsync(physicsScenes[_roomId]);
        //Remove the scene's physics scene
        physicsScenes.Remove(_roomId);
    }

    //necessary because we need to 1 frame to load scene, used by physicsSceneManager ienumerator
    private static IEnumerator WaitFrame(Scene _scene, Guid _roomId, string _mapName)
    {
        //returning 0 makes it wait 1 frame (needed to load scene)
        yield return 0;

        Server.Rooms[_roomId].roomScene = _scene.GetRootGameObjects()[0].GetComponent<RoomScene>();
        Server.Rooms[_roomId].InitializeMap(_scene);
    }
}
