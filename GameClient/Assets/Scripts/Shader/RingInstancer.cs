using UnityEngine;

public class RingInstancer : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] 
    private GameObject ringsInstance;
    [SerializeField]
    private float time;
#pragma warning restore 0649

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
