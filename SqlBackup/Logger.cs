using System;
using System.IO;

namespace SqlBackup
{
    public static class Logger
    {
        public static void LogError(string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            Console.WriteLine($"[{DateTime.Now:T} ERR]: {formattedMessage}");

            var logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}:[ERROR] {formattedMessage}";
            Log(logLine);
        }

        public static void LogInformation(string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            Console.WriteLine($"[{DateTime.Now:T} INF]: {formattedMessage}");

            var logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}:[INFO]  {formattedMessage}";
            Log(logLine);
        }


        private static void Log(string logLine)
        {
            // Write the log line to a file
            var logFilePath =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt"); // Replace with your desired file path
            using (var writer = File.AppendText(logFilePath))
            {
                writer.WriteLine(logLine);
            }
        }
    }
}