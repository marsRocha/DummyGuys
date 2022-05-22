using System;
using System.Collections.Generic;
using UnityEngine;

public class MessageQueuer : MonoBehaviour
{
    private static readonly List<MessageQueue> queues = new List<MessageQueue>();

    private bool show_processes;
    private int processed = 0;
    private float elapsed = 0f;
    private float countRate = 1f;

    private void Start()
    {
        show_processes = ServerData.COUNT_PROCESSES;
    }

    private void FixedUpdate()
    {
        if (show_processes)
        {
            foreach (MessageQueue t in queues)
                processed += t.UpdateMain();

            elapsed += Time.deltaTime;
            if (elapsed >= countRate)
            {
                Console.Write($"p:{processed}. ");
                elapsed = 0;
                processed = 0;
            }

            return;
        }

        foreach (MessageQueue t in queues)
            processed += t.UpdateMain();
    }

    public static void AddQueue(MessageQueue _queue)
    {
        queues.Add(_queue);
    }

    public static void RemoveQueue(MessageQueue _queue)
    {
        queues.Remove(_queue);
    }
}
