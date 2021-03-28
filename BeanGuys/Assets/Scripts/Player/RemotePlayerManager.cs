using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemotePlayerManager : MonoBehaviour
{
    private PlayerController pController;

    [Header("Player info")]
    public int id;
    public string username;

    private Queue<InputMessage> inputMessage;
    private int last_corrected_tick = -1;
    private Inputs currentInput;

    [Header("States")]
    public bool isRunning;
    public bool x, y, jump, dive, ragdolled;


    void Start()
    {
        pController = GetComponent<PlayerController>();

        inputMessage = new Queue<InputMessage>();
        currentInput = new Inputs(0, 0, false, false);

        //Only for now, change later to false
        isRunning = true;
        pController.StartController(false);
    }


    private void FixedUpdate()
    {
        if (isRunning)
        {
            while (inputMessage.Count > 0)
            {
                Debug.Log("Got mail");
                InputMessage input_msg = inputMessage.Dequeue();
                currentInput = new Inputs(input_msg.x, input_msg.y, input_msg.jump, input_msg.dive);

                if (input_msg.tick_number > last_corrected_tick)
                {
                    //Add this later
                    //last_corrected_tick = input_msg.tick_number;
                    //Movement
                    pController.FixedUpdateController(currentInput);
                }
            }
        }
    }

    //Add inputs received to the queue
    public void AddInputMessage(int x, int y, bool jump, bool dive, int tick_number)
    {
        InputMessage input_msg = new InputMessage(x, y, jump, dive, tick_number);
        inputMessage.Enqueue(input_msg);
    }
}

public class InputMessage
{
    public int x, y;
    public bool jump, dive;
    public int tick_number;

    public InputMessage(int x, int y, bool jump, bool dive, int tick_number)
    {
        this.x = x;
        this.y = y;
        this.jump = jump;
        this.dive = dive;
        this.tick_number = tick_number;
    }
}