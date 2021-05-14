using UnityEngine;
using UnityEngine.SceneManagement;

public class PhysicsSceneLoader : MonoBehaviour
{
    //Exposed to inspector
    public string physicsSceneName;
    public float physicsSceneTimeScale = 100000;
    private PhysicsScene physicsScene;

    private void Start()
    {
        //Load the scene to place in a local physics scene
        LoadSceneParameters param = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        Scene scene = SceneManager.LoadScene(physicsSceneName, param);
        //Get the scene's physics scene
        physicsScene = scene.GetPhysicsScene();
    }

    private void FixedUpdate()
    {
       //Simulate the scene on FixedUpdate
       if (physicsScene != null)
        {
            physicsScene.Simulate(Time.fixedDeltaTime * physicsSceneTimeScale);
        }
    }
}
