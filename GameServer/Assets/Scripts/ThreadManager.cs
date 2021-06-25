using System.Collections.Generic;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    private static readonly List<MainThread> threads = new List<MainThread>();

    private void FixedUpdate()
    {
        foreach(MainThread t in threads)
            t.UpdateMain();
    }

    public static void AddThread(MainThread _newThread)
    {
        threads.Add(_newThread);
    }

    public static void RemoveThread(MainThread _newThread)
    {
        threads.Remove(_newThread);
    }
}
