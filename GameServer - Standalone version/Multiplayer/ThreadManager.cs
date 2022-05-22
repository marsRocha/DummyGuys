using System;
using System.Collections.Generic;
using System.Text;

namespace Multiplayer
{
    class ThreadManager
    {
        private static readonly List<MainThread> threads = new List<MainThread>();

        public static void Update()
        {
            foreach (MainThread t in threads)
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
}
