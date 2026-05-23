using System;
using System.IO;
using System.Security.Principal;
using System.Text;

namespace ProjectZetaTeam
{

    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string _logFilePath;

        static Logger()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _logFilePath = Path.Combine(baseDir, "logs", "stego_log.txt");

                string? logDir = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                if (!File.Exists(_logFilePath))
                {
                    string header = "Timestamp;UserName;Operation;FileName;MessageLength;Status;Error\n";
                    File.AppendAllText(_logFilePath, header, Encoding.UTF8);
                }
            }
            catch
            {

            }
        }

        public static void LogOperation(
            string operation,
            string fileName,
            string message,
            bool success,
            string? error = null
        )
        {
            try
            {
                string userName = WindowsIdentity.GetCurrent()?.Name
                    ?? Environment.UserName
                    ?? "unknown";

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string safeFileName = Path.GetFileName(fileName);
                int msgLength = message?.Length ?? 0;
                string status = success ? "OK" : "FAIL";

            
                string safeMessage = (message ?? "").Replace(";", ",").Replace("\n", " ").Replace("\r", " ");
                string safeError = (error ?? "").Replace(";", ",").Replace("\n", " ").Replace("\r", " ");

                string logLine = $"[{timestamp}] [{userName}] [{operation}] File:{safeFileName} | Len:{msgLength} | {status}";
                if (!string.IsNullOrEmpty(safeError))
                    logLine += $" | Error:{safeError}";
                logLine += "\n";

                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, logLine, Encoding.UTF8);
                }
            }
            catch
            {
                
            }
        }
    }
}