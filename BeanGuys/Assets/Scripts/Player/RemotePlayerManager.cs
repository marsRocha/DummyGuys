using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemotePlayerManager : MonoBehaviour
{
    private PlayerController pController;
    private Rigidbody rb;
    private Animator anim;

    [Header("Player info")]
    public int id;
    public string username;

    private const int CACHE_SIZE = 1024;
    private Queue<RemoteState> player_state_msgs;
    private int last_corrected_tick = -1;

    [Header("States")]
    public bool isRunning;
    public bool x, y, jump, dive, ragdolled;


    void Start()
    {
        pController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        //For this entity's interpolation
        player_state_msgs = new Queue<RemoteState>();

        //Only for now, change later to false
        isRunning = true;
        pController.StartController(false);
        Debug.Log("Remote player initiated");
    }


    private void FixedUpdate()
    {
        if (isRunning)
        {
            /*if(player_state_msgs.Count > 0)
            {
                Debug.Log("Trying");
                RemoteState movement_msg = player_state_msgs.Dequeue();

                // Drop older updates
                while (player_state_msgs.Count >= 2 && movement_msg.tick_number <= MapController.instance.Game_Clock)
                    movement_msg = player_state_msgs.Dequeue();

                if (movement_msg.tick_number < MapController.instance.Game_Clock)
                {
                    RemoteState movement_msg0 = player_state_msgs.Dequeue();
                    RemoteState movement_msg1 = player_state_msgs.Dequeue();

                    //Position
                    rb.position = movement_msg0.position + (movement_msg1.position - movement_msg0.position)
                        * (MapController.instance.Game_Clock - movement_msg0.tick_number) / (movement_msg1.tick_number - movement_msg0.tick_number);
                }
            }

            
            if (player_state_msgs.Count > 0)
            {
                // Drop older updates
                while (player_state_msgs.Count > 2)
                    player_state_msgs.Dequeue();

                if (player_state_msgs.Count >= 2)
                {
                    RemoteState movement_msg0 = player_state_msgs.Dequeue();
                    RemoteState movement_msg1 = player_state_msgs.Dequeue();

                    //Position
                    rb.position = movement_msg0.position + (movement_msg1.position - movement_msg0.position)
                        * (MapController.instance.Game_Clock - movement_msg0.tick_number) / (movement_msg1.tick_number - movement_msg0.tick_number);
                }
            }*/
        }
    }

    public void SetIdentification(int id, string username)
    {
        this.id = id;
        this.username = username;
    }

    public void UpdateMovement(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, int tick_number)
    {
        //player_state_msgs.Enqueue(new RemoteState(position, rotation, tick_number));

        rb.position = position;
        rb.rotation = rotation;
        rb.velocity = velocity;
        rb.angularVelocity = angular_velocity;
    }

    public void UpdateAnimaiton(int animNum)
    {
        //MOVE TO PLAYER CONTROLLER
        animNum newAnim = (animNum)animNum;

        anim.SetBool("isRunning", false);
        anim.SetBool("isJumping", false);
        anim.SetBool("isDiving", false);

        Debug.Log($"CurrentAnimation:{newAnim}");

        switch (newAnim)
        {
            case global::animNum.idle:
                // do nothing
                break;
            case global::animNum.run:
                anim.SetBool("isRunning", true);
                break;
            case global::animNum.jump:
                anim.SetBool("isJumping", true);
                break;
            case global::animNum.dive:
                anim.SetBool("isDiving", true);
                break;
        }
    }

    public void Respawn(Vector3 position, Quaternion rotation)
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = rotation;

        pController.Respawn();
    }
}

public class RemoteState
{
    public Vector3 position;
    public Quaternion rotation;
    public int tick_number;

    public RemoteState()
    {
    }

    public RemoteState(Vector3 position, Quaternion rotation, int tick_number)
    {
        this.position = position;
        this.rotation = rotation;
        this.tick_number = tick_number;
    }
}