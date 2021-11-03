using UnityEngine;

public class PlayerInput : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField]
    private string forwardAxis;
    [SerializeField]
    private string lateralAxis;
    [SerializeField]
    private KeyCode jumpKey;
    [SerializeField]
    private KeyCode diveKey;
    [SerializeField]
    private KeyCode grabKey;
    [SerializeField]
    private KeyCode pushKey;
    [SerializeField]
    private KeyCode respawnKey;
#pragma warning restore 0649

    public string ForwardAxis { get { return forwardAxis; } }
    public string LateralAxis { get { return lateralAxis; } }
    public KeyCode JumpKey { get { return jumpKey; } }
    public KeyCode DiveKey { get { return diveKey; } }
    public KeyCode GrabKey { get { return grabKey; } }
    public KeyCode PushKey { get { return pushKey; } }
    public KeyCode RespawnKey { get { return respawnKey; } }
}
