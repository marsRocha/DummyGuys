using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemotePlayerManager : MonoBehaviour
{
    private PlayerController pController;
    private Rigidbody rb;
    private Animator anim;

    [Header("Player info")]
    public Guid id;
    public string username;

    private const int CACHE_SIZE = 1024;
    private List<RemoteState> player_state_msgs;
    private int last_corrected_tick = -1;
    private long render_timestamp;

    private RemoteState from, to;

    [Header("States")]
    public bool isRunning;
    public bool x, y, jump, dive, ragdolled;


    void Start()
    {
        pController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        //For this entity's interpolation
        player_state_msgs = new List<RemoteState>();

        //Only for now, change later to false
        isRunning = true;
        pController.StartController();
        Debug.Log("Remote player initiated");
    }


    private void FixedUpdate()
    {
        if (isRunning)
        {
            if (player_state_msgs.Count == 0)
                return;

            // Compute render timestamp.
            render_timestamp = (long)(MapController.instance.Game_Clock - (0 / 30));

            // Find the two positions surrounding the rendering timestamp.
            // Drop older updates
            while (player_state_msgs.Count >= 2 && player_state_msgs[1].tick_number <= render_timestamp)
            {
                player_state_msgs.RemoveAt(0);
            }

            // Interpolate between the two surrounding authoritative positions.
            if (player_state_msgs.Count >= 2 && player_state_msgs[0].tick_number <= render_timestamp && render_timestamp <= player_state_msgs[1].tick_number) // :: Addition (instead of nested array, use struct to access named fields)
            {
                RemoteState from = player_state_msgs[0];
                RemoteState to = player_state_msgs[1];


                //rb.position = (x0 + (x1 - x0) * (render_timestamp - t0) / (t1 - t0));
                rb.position = Vector3.Lerp(from.position, to.position, .5f ); //Mathf.Abs((.1f - t0) / (t1 - t0))
                rb.rotation = Quaternion.Lerp(from.rotation, to.rotation, .5f ); //Mathf.Abs((.1f - t0) / (t1 - t0))
                rb.velocity = Vector3.Lerp(from.velocity, to.velocity, .5f ); //Mathf.Abs((.1f - t0) / (t1 - t0))
                rb.angularVelocity = Vector3.Lerp(from.angular_velocity, to.angular_velocity, .5f ); //Mathf.Abs((.1f - t0) / (t1 - t0))
            }
        }
    }

    public void SetIdentification(Guid id, string username)
    {
        this.id = id;
        this.username = username;
    }

    public void UpdateMovement(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, float tick_number)
    {
        player_state_msgs.Add(new RemoteState(position, rotation, velocity, angular_velocity, tick_number));

        //Debug.Log($"packet timestamp:{tick_number}, local timestamp:{MapController.instance.Game_Clock}");

        /*rb.position = position;
        rb.rotation = rotation;
        rb.velocity = velocity;
        rb.angularVelocity = angular_velocity;*/
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

    public void ReceivedCorrectionState(SimulationState simulationState)
    {
        Debug.LogWarning("Correct remote player position, rot, vel");
        rb.position = simulationState.position;
        rb.rotation = simulationState.rotation;
        rb.velocity = simulationState.velocity;

        //TODO: REMOVE MESSAGES THAT ARE OLDER AND THE SAME AGE AS THIS ONE
    }
}

public class RemoteState
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angular_velocity;
    public float tick_number;

    public RemoteState(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, float tick_number)
    {
        this.position = position;
        this.rotation = rotation;
        this.velocity = velocity;
        this.angular_velocity = angular_velocity;
        this.tick_number = tick_number;
    }
}