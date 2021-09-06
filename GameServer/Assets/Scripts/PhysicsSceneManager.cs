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

        instance.StartCoroutine(WaitFrame(scene, _roomId));
    }

    public static void RemoveSimulation(Guid _roomId)
    {
        // Unload the scene
        LoadSceneParameters param = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        SceneManager.UnloadSceneAsync(physicsScenes[_roomId]);
        // Remove the scene's physics scene
        physicsScenes.Remove(_roomId);
    }

    // Necessary because we need to 1 frame to load scene, used by physicsSceneManager ienumerator
    private static IEnumerator WaitFrame(Scene _scene, Guid _roomId)
    {
        // returning 0 makes it wait 1 frame (needed to load scene)
        yield return 0;

        RoomScene roomScene = null;

        foreach (GameObject obj in _scene.GetRootGameObjects())
        {
            if (obj.GetComponent<RoomScene>())
            {
                roomScene = obj.GetComponent<RoomScene>();
                break;
            }
        }

        if(roomScene != null)
        {
            Server.Rooms[_roomId].roomScene = roomScene;
            Server.Rooms[_roomId].InitializeMap();
        }
        else
        {
            Debug.LogWarning("Roomscene has not loaded, deleting room");
            Server.Rooms[_roomId].Stop();
        }
    }
}
