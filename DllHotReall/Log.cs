using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DllHotReall
{
    internal class FileLog
    {
        private static readonly object LockObject = new object();

        private static string LogFilePath =Tools.ModPath("dllreload.log"); // 默认日志文件路径
        public static void Clear() {

            if (File.Exists(LogFilePath))
            {
                File.Delete(LogFilePath);
            }
        }
        public static void Write(string message)
        {
            lock (LockObject)
            {
                try
                {
                    string logEntry = $"{DateTime.Now: HH:mm:ss} {message}";
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Log.Message($"DllHotReload: {ex.Message}");
                }
            }
        }
    }
}
