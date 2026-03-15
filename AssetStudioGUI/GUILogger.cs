using AssetStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AssetStudioGUI
{
    class GUILogger : ILogger
    {
        public bool ShowErrorMessage = true;
        private Action<string> action;
        private readonly Dictionary<string, int> errorBuffer = new Dictionary<string, int>();
        private readonly object errorLock = new object();
        private static bool consoleAttachAttempted;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        public GUILogger(Action<string> action)
        {
            this.action = action;
        }

        public void Log(LoggerEvent loggerEvent, string message)
        {
            WriteToConsole(loggerEvent, message);

            switch (loggerEvent)
            {
                case LoggerEvent.Error:
                    action(message);
                    if (ShowErrorMessage)
                    {
                        // Group by the root cause (exception message), not the full context
                        // so that e.g. 50 "version stripped" errors from different CAB files
                        // collapse into a single entry.
                        var cause = ExtractCause(message);
                        lock (errorLock)
                        {
                            if (errorBuffer.ContainsKey(cause))
                                errorBuffer[cause]++;
                            else
                                errorBuffer[cause] = 1;
                        }
                    }
                    break;
                default:
                    action(message);
                    break;
            }
        }

        private static void WriteToConsole(LoggerEvent loggerEvent, string message)
        {
            EnsureConsoleAttached();

            var text = $"[{loggerEvent}] {message}";
            try
            {
                if (loggerEvent == LoggerEvent.Error)
                {
                    Console.Error.WriteLine(text);
                }
                else
                {
                    Console.WriteLine(text);
                }
            }
            catch
            {
                // Ignore console write failures for GUI-only launches.
            }
        }

        private static void EnsureConsoleAttached()
        {
            if (consoleAttachAttempted)
            {
                return;
            }

            consoleAttachAttempted = true;
            try
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
            }
            catch
            {
                // No console available (double-click launch, etc.).
            }
        }

        // Returns the innermost exception message lines (strips context prefix and stack frames).
        // e.g. "Error while reading ... \nSystem.Exception: The Unity version has been stripped..."
        //   → "The Unity version has been stripped, please set the version in the options"
        private static string ExtractCause(string message)
        {
            var lines = message.Split('\n');
            var causeLines = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("at ") || trimmed.StartsWith("--- End of"))
                    break;
                // Lines like "SomeException: message" or "  ---> SomeException: message"
                var exIdx = trimmed.IndexOf("Exception:", StringComparison.Ordinal);
                if (exIdx >= 0)
                {
                    causeLines.Add(trimmed.Substring(exIdx + "Exception:".Length).Trim());
                }
            }
            if (causeLines.Count > 0)
                return string.Join(" | ", causeLines);
            // Fallback: return first non-empty line (no stack trace)
            foreach (var line in lines)
            {
                var t = line.Trim();
                if (t.Length > 0)
                    return t;
            }
            return message.TrimEnd();
        }

        public void ShowErrorSummary()
        {
            Dictionary<string, int> errors;
            lock (errorLock)
            {
                if (errorBuffer.Count == 0)
                    return;
                errors = new Dictionary<string, int>(errorBuffer);
                errorBuffer.Clear();
            }

            const int maxLines = 8;
            var sb = new StringBuilder();
            int total = errors.Values.Sum();
            sb.AppendLine($"{total} error(s) occurred during loading:\n");

            int shown = 0;
            foreach (var kv in errors)
            {
                if (shown >= maxLines)
                {
                    sb.AppendLine($"... and {errors.Count - shown} more unique error(s).");
                    break;
                }
                sb.AppendLine(kv.Value > 1 ? $"[×{kv.Value}]  {kv.Key}" : kv.Key);
                shown++;
            }

            MessageBox.Show(sb.ToString().TrimEnd(), "Loading errors",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
