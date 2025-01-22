using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using Verse;

namespace DllHotReall
{
    internal class Watcher
    {
        private static FileSystemWatcher watcher;
        //public static event Action<List<string>> OnFilesChanged;

        public static void StartFileWatcher(FileSystemEventHandler onChangedHandler)
        {
            string modsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");

            if (!Directory.Exists(modsDirectory))
            {
                throw new DirectoryNotFoundException($"目录不存在 : {modsDirectory}");
            }

            watcher = new FileSystemWatcher
            {
                Path = modsDirectory,
                Filter = "*.dll",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = true
            };

            if (onChangedHandler != null)
            {
                watcher.Changed += onChangedHandler;
            }

            watcher.EnableRaisingEvents = true;
            FileLog.Write($"文件监听启动{modsDirectory}");
        }
    }
}