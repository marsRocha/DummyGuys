
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class MessageQueue
{
    private static readonly List<Action> queue = new List<Action>();
    private static readonly List<Action> queueCopy = new List<Action>();

    /// <summary>Adds a message to be executed.</summary>
    public void ExecuteOnMain(Action _action)
    {
        if (_action == null)
            return;

        lock (queue)
        {
            queue.Add(_action);
        }
    }

    /// <summary>Executes all messages and the code inside them.</summary>
    /// Call ONLY from the main thread.
    public int UpdateMain()
    {
        lock (queue)
        {
            if (queue.Count == 0)
                return 0;
        }

        queueCopy.Clear();
        lock (queue)
        {
            queueCopy.AddRange(queue);
            queue.Clear();
        }

        int processed = queueCopy.Count;
        //Stopwatch stopWatch = new Stopwatch();
        //double _accumulator = 0;

        for (int i = 0; i < queueCopy.Count; i++)
        {
            //stopWatch.Restart();

            // Do action
            queueCopy[i]();

            //stopWatch.Stop();

            // Get elapsed time
            //long elapsedTime = stopWatch.ElapsedMilliseconds;
            //_accumulator += elapsedTime;
        }

        //UnityEngine.Debug.Log($"avg. request time:{_accumulator/processed}");

        return processed;
    }

    /// <summary>Clears all messages added to the queue.</summary>
    /// Call ONLY from the main thread.
    public void Clear()
    {
        queueCopy.Clear();
        lock (queue)
        {
            queue.Clear();
            queueCopy.Clear();
        }
    }
}
