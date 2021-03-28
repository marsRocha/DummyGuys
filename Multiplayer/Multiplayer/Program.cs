using System;
using System.Threading;

namespace Multiplayer
{
    class Program
    {
        private static bool isRunning = false;
        static void Main(string[] args)
        {
            Console.Title = "Game Server";
            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            Server.Start(10, 26950);
        }

        private static void MainThread()
        {
            Console.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.");
            DateTime nextLoop = DateTime.UtcNow;

            while (isRunning)
            {
                while(nextLoop < DateTime.UtcNow)
                {
                    GameLogic.Update();

                    nextLoop = nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    //sleep in between ticks - lower CPU usage
                    if(nextLoop > DateTime.UtcNow)
                    {
                        Thread.Sleep(nextLoop - DateTime.UtcNow);
                    }
                }
            }
        }
    }
}
