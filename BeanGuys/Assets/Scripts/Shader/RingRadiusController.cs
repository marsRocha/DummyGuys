using UnityEngine;

public class RingRadiusController : MonoBehaviour
{
    Material material;
    private float time;
    [SerializeField]
    private float speed;
    private float radius = 0;
    private float randomValue;

    // Start is called before the first frame update
    void Start()
    {
        material = this.gameObject.GetComponent<MeshRenderer>().material;
        material.SetFloat("_radius_A", radius);

        float random = Random.Range(0.01f, 0.04f);
        material.SetFloat("_radius_B", random);
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime * speed;
        if(time < 1)
        {
            radius = Mathf.Lerp(0, 1, time);
            material.SetFloat("_radius_A", radius);
        }

        if (radius >= 0.65f)
        {
            Destroy(this.gameObject);
        }
    }
}
