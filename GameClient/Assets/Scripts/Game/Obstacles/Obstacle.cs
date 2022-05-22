using UnityEngine;

public class Obstacle : MonoBehaviour
{
    protected MapController mapController;

    private void Start()
    {
        foreach (GameObject obj in gameObject.scene.GetRootGameObjects())
        {
            if (obj.GetComponent<MapController>())
            {
                mapController = obj.GetComponent<MapController>();
                break;
            }
        }
    }
}
