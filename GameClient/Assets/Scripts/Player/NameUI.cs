using TMPro;
using UnityEngine;

public class NameUI : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField]
    private Transform target;
    [SerializeField]
    private TMP_Text nameTxt;
    [SerializeField]
    private bool remotePlayer;
#pragma warning restore 0649

    private void Start()
    {
        if(!remotePlayer)
            nameTxt.text = transform.root.GetComponent<Player>().Username;
        else
            nameTxt.text = transform.root.GetComponent<RemotePlayerManager>().Username;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0);

        transform.position = target.position + (Vector3.up * 1.6f);
    }
}
