using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody))]

//Controls player's network related functions, while minimizing the impact on the game code 
public class PlayerManager : MonoBehaviour
{
    private PlayerController pController;
    private Rigidbody rb;

    //Store previous player stuff
    private const int CACHE_SIZE = 1024;
    private ClientState[] client_state_buffer;
    private Inputs[] client_input_buffer;
    private int buffer_slot;
    private int currentTick = 0;

    private Inputs currentInput;
    private Inputs lastInput;
    private animNum lastAnimSent;

    [Header("Correction")]
    public bool client_correction_smoothing;
    private Vector3 client_pos_error;
    private Quaternion client_rot_error;
    private Queue<StateMessage> client_state_msgs;

    [Header("States")]
    public bool isOnline;
    public bool isRunning;

    // Start is called before the first frame update
    void Start()
    {
        currentTick = 0;
        rb = GetComponent<Rigidbody>();
        pController = GetComponent<PlayerController>();

        client_state_buffer = new ClientState[CACHE_SIZE];
        client_input_buffer = new Inputs[CACHE_SIZE];
        currentInput = new Inputs(0, 0, false, false);
        client_pos_error = Vector3.zero;
        client_rot_error = Quaternion.identity;
        client_state_msgs = new Queue<StateMessage>();

        pController.StartController(true);
    }

    // Update is called once per frame
    private void Update()
    {
        if(isRunning)
            pController.UpdateController();
    }

    private void FixedUpdate()
    {
        if (isRunning)
        {
            lastInput = currentInput;
            currentInput = pController.pInput.GetInputs();

            //Update player's movement
            pController.FixedUpdateController(currentInput);

            if (isOnline)
            {
                //ClientSend.PlayerMovement(rb.position, rb.rotation, rb.velocity, rb.angularVelocity, MapController.instance.Game_Clock);
                ClientSend.PlayerMovement(currentInput.x, currentInput.y, currentInput.jump, currentInput.dive, rb.position, rb.rotation, rb.velocity, rb.angularVelocity, MapController.instance.Game_Clock);
                Debug.Log("Sent message");
                
                if (lastAnimSent != pController.currentAnim)
                {
                    ClientSend.PlayerAnim((int)pController.currentAnim);
                    lastAnimSent = pController.currentAnim;
                }
            }
        }
    }

    public void PlayerInput(Inputs inputs, int tick_number)
    {
        currentInput = inputs;
    }

    public void Respawn( Vector3 position, Quaternion rotation)
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = rotation;

        pController.Respawn();
    }
}

public class ClientState
{
    public Vector3 position;
    public Quaternion rotation;

    public ClientState(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }
}

public class StateMessage
{
    public float delivery_time;
    public int tick_number;
    public Vector3 position, velocity, angular_velocity;
    public Quaternion rotation;

    public StateMessage(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, int tick_number)
    {
        this.position = position;
        this.rotation = rotation;
        this.velocity = velocity;
        this.angular_velocity = angular_velocity;
        this.tick_number = tick_number;
    }
}

public class Inputs
{
    public int x, y;
    public bool jump, dive;

    public Inputs(int x, int y, bool jump, bool dive)
    {
        this.x = x;
        this.y = y;
        this.jump = jump;
        this.dive = dive;
    }
}
