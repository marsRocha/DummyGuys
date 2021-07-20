using UnityEngine;

public class RingInstancer : MonoBehaviour
{
    [SerializeField] 
    private GameObject ringsInstance;
    [SerializeField]
    private float time;

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("InstantiateRings", 1, time);
    }

    void InstantiateRings()
    {
        GameObject rings = Instantiate(ringsInstance, transform);
    }
}
