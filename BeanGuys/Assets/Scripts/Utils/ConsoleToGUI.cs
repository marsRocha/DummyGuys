using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace DebugStuff
{
    public class ConsoleToGUI : MonoBehaviour
    {
        // Adjust via the Inspector
        public int maxLines = 8;
        private Queue<string> queue = new Queue<string>();
        [SerializeField]
        private TMP_Text display;
        private string currentText = "";

        void OnEnable()
        {
            Application.logMessageReceivedThreaded += HandleLog;
        }

        void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleLog;
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            ThreadManager.ExecuteOnMainThread(() =>
            {
                // Delete oldest message
                if (queue.Count >= maxLines) queue.Dequeue();

                queue.Enqueue(logString);

                var builder = new StringBuilder();
                foreach (string st in queue)
                {
                    builder.Append(st).Append("\n");
                }

                currentText = builder.ToString();

                display.text = currentText;
            });
        }
    }
}