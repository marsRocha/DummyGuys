using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody))]

//Controls player's network related functions, while minimizing the impact on the game code 
public class PlayerManager : MonoBehaviour
{
    private PlayerController pController;

    //Store previous player stuff
    private const int CACHE_SIZE = 1024;
    private ClientState[] client_state_buffer;
    private Inputs[] client_input_buffer;
    private int buffer_slot;
    private int lastCorrectedFrame = 0;
    private Inputs currentInput;
    private Inputs lastInput;

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

            if (isOnline)
            {
                //buffer_slot = CSceneManager.instance.tick_number % CACHE_SIZE;
                //client_input_buffer[buffer_slot] = currentInput;
                //if (currentInput != lastInput)
                    ClientSend.PlayerInput(currentInput.x, currentInput.y, currentInput.jump, currentInput.dive, 0);  //CSceneManager.instance.tick_number);
                //client_state_buffer[buffer_slot] = new ClientState(rb.position, rb.rotation);
            }

            pController.FixedUpdateController(currentInput);
        }
    }

    public void PlayerInput(Inputs inputs, int tick_number)
    {
        currentInput = inputs;
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
