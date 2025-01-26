using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DllHotReall
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Threading;
    public static class FileMonitor
    {
        private static readonly ConcurrentDictionary<string, DateTime> fileMap = new ConcurrentDictionary<string, DateTime>();
        private static readonly Timer timer;
        //private static readonly object lockObject = new object();

        // 回调方法，可以通过外部设置
        public static Action<string> OnFileChanged;

        // 静态构造函数
        static FileMonitor()
        {
            // 每500ms触发一次扫描
            timer = new Timer(CheckFiles, null, 0, 500);
        }

        /// <summary>
        /// 添加文件路径到Map
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void Add(string filePath)
        {
            FileLog.Write("添加文件"+ filePath);
            if (File.Exists(filePath))
            {
                FileLog.Write("添加文件开始" + filePath);
                var lastWriteTime = File.GetLastWriteTime(filePath);
                FileLog.Write("添加文件结束" + filePath);
                fileMap[filePath] = lastWriteTime;
            }
            else
            {
                Console.WriteLine($"File not found: {filePath}");
            }
        }

        /// <summary>
        /// 定时检查文件的状态
        /// </summary>
        private static void CheckFiles(object state)
        {
            //FileLog.Write("定时执行");
            //lock (lockObject)
            //{
                //FileLog.Write("定时执行2"+fileMap.Count);
                foreach (var kvp in fileMap.ToList())
                {

                    var filePath = kvp.Key;
                    var lastKnownTime = kvp.Value;
                //FileLog.Write("定时执行文件"+ filePath);

                if (File.Exists(filePath)){
                        var currentLastWriteTime = File.GetLastWriteTime(filePath);

                        //FileLog.Write("定时执行文件" + currentLastWriteTime);
                        // 检查最后更新时间是否变更
                        if (currentLastWriteTime > lastKnownTime.AddSeconds(2))
                        {
                            fileMap[filePath] = currentLastWriteTime; // 更新Map中的时间

                            // 触发回调
                            if (OnFileChanged != null)
                            {
                                OnFileChanged(filePath);
                            }
                        }
                }
            }
        }
    }

}
