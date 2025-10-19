using System;
using System.IO;
using System.Text;

namespace takip
{
    public static class DetailedLogger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "detailed_log.txt");
        private static readonly object LockObject = new object();

        public static void Log(string message, string category = "INFO")
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{category}] {message}";
                
                lock (LockObject)
                {
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
                }
                
                // Console'a da yazdır
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log yazma hatası: {ex.Message}");
            }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            var errorMessage = message;
            if (ex != null)
            {
                errorMessage += $"\nException: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
                }
                errorMessage += $"\nStack Trace: {ex.StackTrace}";
            }
            Log(errorMessage, "ERROR");
        }

        public static void LogInfo(string message)
        {
            Log(message, "INFO");
        }

        public static void LogWarning(string message)
        {
            Log(message, "WARNING");
        }

        public static void LogDebug(string message)
        {
            Log(message, "DEBUG");
        }

        public static void ClearLog()
        {
            try
            {
                lock (LockObject)
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.Delete(LogFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log temizleme hatası: {ex.Message}");
            }
        }
    }
}


