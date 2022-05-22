using System;
using System.Collections.Generic;
using UnityEngine;

public class MessageQueuer : MonoBehaviour
{
    private static readonly List<Action> queue = new List<Action>();
    private static readonly List<Action> queueCopy = new List<Action>();

    private void Update()
    {
        UpdateOnMain();
    }

    /// <summary>Executes all messages and the code inside them.</summary>
    /// Call ONLY from the main thread.
    private static void UpdateOnMain()
    {
        queueCopy.Clear();
        lock (queue)
        {
            queueCopy.AddRange(queue);
            queue.Clear();
        }

        for (int i = 0; i < queueCopy.Count; i++)
            queueCopy[i]();
    }

    /// <summary>Adds a message to be executed.</summary>
    public static void ExecuteOnMain(Action _action)
    {
        if (_action == null)
        {
            Debug.LogWarning("No message given to execute on main thread!");
            return;
        }

        lock (queue)
        {
            queue.Add(_action);
        }
    }
}
