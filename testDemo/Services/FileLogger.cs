using System;
using System.IO;
using System.Text;

namespace GHPHandShake.Services
{
    public static class FileLogger
    {
        private static readonly object SyncRoot = new object();
        private static string _logDirectory = "Logs";

        public static void Initialize(string logDirectory = null)
        {
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                _logDirectory = logDirectory;
            }

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public static void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}";
            string filePath = Path.Combine(_logDirectory, $"ghp_{DateTime.Now:yyyyMMdd}.log");

            lock (SyncRoot)
            {
                try
                {
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }

                    File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // 文件写入失败时不影响主流程
                }
            }
        }
    }
}
