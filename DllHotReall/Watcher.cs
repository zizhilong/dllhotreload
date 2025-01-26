using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Verse;

namespace DllHotReall
{
    /*
    internal class Watcher
    {
        private static FileSystemWatcher watcher;
        private static Timer debounceTimer;
        private static DateTime lastTriggeredTime;
        private static readonly object lockObj = new object();

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
            watcher.Changed += (sender, e) => HandleFileChange(e, onChangedHandler);

            watcher.Created += OnFileCreated;

            watcher.EnableRaisingEvents = true;
            FileLog.Write($"文件监听启动{modsDirectory}");
        }

     private static void OnFileCreated(object sender, FileSystemEventArgs e)
    {
            // 获取文件的完整路径
            string rawPath = e.FullPath;

            // 解析并规范化路径（保留文件名）
            string resolvedPath = Path.GetFullPath(rawPath);

            // 输出结果
            FileLog.Write($"文件创建事件: {resolvedPath}");

    }

        private static void HandleFileChange(FileSystemEventArgs e, FileSystemEventHandler onChangedHandler)
        {

            FileLog.Write("触发Changed");

            var path = e.FullPath;
            if (!Tools.InAssemblies(path))
            {
                return;
            }
            lock (lockObj)
            {
                // 获取当前时间
                DateTime now = DateTime.Now;

                // 如果上次触发时间距离现在小于 2 秒，直接忽略
                if ((now - lastTriggeredTime).TotalSeconds < 2)
                {
                    FileLog.Write($"触发小于两秒忽略");
                    return;
                }

                // 如果防抖计时器已存在，重置它
                debounceTimer?.Dispose();

                // 创建新的防抖计时器，延迟 200 毫秒后执行
                debounceTimer = new Timer(_ =>
                {
                    FileLog.Write("立即执行"+ onChangedHandler.ToString());
                    lock (lockObj)
                    {
                        lastTriggeredTime = DateTime.Now;
                        onChangedHandler?.Invoke(null, e); // 执行处理逻辑

                    }
                }, null, 200, Timeout.Infinite);
            }
        }
    }
    */

}