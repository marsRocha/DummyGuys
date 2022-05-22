using UnityEngine;

public class PlayerColor : MonoBehaviour
{
    public static PlayerColor instance;

    public Material[] materials;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }
}
