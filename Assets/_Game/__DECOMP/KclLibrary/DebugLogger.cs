using System;
using System.Collections.Generic;
using System.Text;

namespace KclLibrary
{
    public class DebugLogger
    {
        private static string Value;

        public static EventHandler OnDebuggerUpdated;

        public static EventHandler OnProgressUpdated;

        public static bool IsCurrentError = false;

        public string GetLog() {
            return Value;
        }

        public static void WriteLine(string value)
        {
            Value += $"{value}\n";
            Console.WriteLine($"DebugLogger {value}");
            OnDebuggerUpdated?.Invoke(value, EventArgs.Empty);
        }

        public static void WriteError(string value)
        {
            Value += $"{value}\n";
            Console.WriteLine($"DebugLogger {value}");
            IsCurrentError = true;
            OnDebuggerUpdated?.Invoke(value, EventArgs.Empty);
            IsCurrentError = false;
        }


        public static void UpdateProgress(int value) {
            OnProgressUpdated?.Invoke(value, EventArgs.Empty);
        }
    }
}
