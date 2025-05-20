using System;
using System.IO;

namespace SchoolManager.Core
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "log.txt");

        static Logger()
        {
            var logDirectory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в лог: {ex.Message}");
            }
        }
    }
}