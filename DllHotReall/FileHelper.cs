using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using static RimWorld.ColonistBar;
using static HarmonyLib.AccessTools;
using Verse.Noise;
using Mono.Cecil;

namespace DllHotReall
{

    [StaticConstructorOnStartup]

    public class FileHelps
    {

        
        static  FileHelps()
        {
            //Watcher.StartFileWatcher(Reload);
            FileMonitor.OnFileChanged = Reload;
            FileLog.Write("Start  V0.0.1");
            Run();
            //HandleFileChange("C:\\Users\\ZZL\\Desktop\\MOD类型\\dnspy\\DllHotReload.dll");
        }
        // 定义一个委托，用于替换方法的劫持
        internal delegate void DetourMethodDelegate(MethodBase method, MethodBase replacement);

        // 使用反射获取方法并将其赋值给委托，初始化 DetourMethod 委托 
        internal static readonly DetourMethodDelegate DetourMethod = MethodDelegate<DetourMethodDelegate>(Method("HarmonyLib.PatchTools:DetourMethod"));

        // 静态字典：文件名 -> Assembly
        private static readonly Dictionary<string, Assembly> dllAssemblyMap = new Dictionary<string, Assembly>();

        private static readonly  Dictionary<string, MethodIlInfo> originalMethodMap = new Dictionary<string, MethodIlInfo>();
        // 提供访问字典的方法
        public static Dictionary<string, Assembly> GetDllAssemblyMap()
        {
            return dllAssemblyMap;
        }


        public static void Run() {
            FileLog.Clear();
            //加载目标Dll清单
            string[] dllfiles = GetRuningDll(); // 模拟获取已加载的DLL文件路径
            ClearRuning();
            MoveFiles(dllfiles);
            //MoveFiles(dllfiles);
            MarkMethhodMap();
        }
        //更新程序  集
        public static void Reload(string path)
        {
            //去签名处理
           FileLog.Write("触发更新 "+ path);
           var loadedAssembly = Definition.Start(path);
            if (loadedAssembly == null)
            {
                FileLog.Write("去签名后返回程序集失败 ");
                return;
            }
            FileLog.Write($"Loaded Assembly: {loadedAssembly.FullName}");
           // 加载程序集
           // 获取程序集的 IL 映射 
           var ilMap = GetIlMap(loadedAssembly);
            FileLog.Write($"更新key长度: " + ilMap.Count);
           var logstr = "";
           var originakeys = originalMethodMap.Keys.ToList();
            FileLog.Write($"新Dll所有方法");
            
           foreach (var kvp in ilMap) {
                FileLog.Write(kvp.Key);
            }
           foreach (var kvp in originakeys)
           {
               string originaId = kvp;

               if (ilMap.ContainsKey(originaId))
               {

                   MethodIlInfo newMethodInfo = ilMap[originaId];
                   MethodIlInfo existingMethodInfo = originalMethodMap[originaId];
                    //if (existingMethodInfo.IlHash != newMethodInfo.IlHash)
                    //{
                    FileLog.Write("更新函数"+ originaId);
                        DetourMethod(existingMethodInfo.Method, newMethodInfo.Method);
                    //}
                }
            }

           //输出完成提示新小米
            var filename = Path.GetFileName(path);
            FileLog.Write(path + "完成");
            Messages.Message(filename + " Reload Over", MessageTypeDefOf.PositiveEvent);
            Tools.ModLog(filename+" Reload Over");
        }
        public static void MoveFiles(string[] dllfiles)
        {
            foreach (var dllFile in dllfiles)
            { 
                FileLog.Write("移动文件:" + dllFile);
                MoveFile(dllFile);
            }
        }
        //创建方法Map
        public static void MarkMethhodMap()
        {

            foreach (var entry in dllAssemblyMap)
            {
                // 调用 GetIlMap 获取当前程序集的方法映射
                var ilMap = GetIlMap(entry.Value);

                foreach (var kvp in ilMap)
                {
                    string methodId = kvp.Key;

                    // 检查键是否存在
                    if (!originalMethodMap.ContainsKey(methodId) && !Tools.IsHarmony(methodId))
                    {
                        // 如果不存在，则添加到 originalMethodMap
                        originalMethodMap[methodId] = kvp.Value;
                    }
                }
            }
            FileLog.Write($"方法Map总量为 : {originalMethodMap.Count}");
            FileLog.Write("keys: "+originalMethodMap.Keys.ToArray().ToString());
            Tools.ModLog($"Is Ready ,Method Count Is {originalMethodMap.Count}");
        }


        public struct MethodIlInfo
        {
            public string IlHash { get; set; }
            public MethodBase Method { get; set; }
        }


        /// <summary>
        /// 获取程序集中的方法 ID 和 IL 哈希的 映射
        /// </summary>
        /// <param name="assembly">目标程序集</param>
        /// <returns>方法 ID 和 IL 哈希的字典 </returns>
        public static Dictionary<string, MethodIlInfo> GetIlMap(Assembly assembly)
        {
            //Assembly.ReflectionOnlyLoadFrom(assembly.Location);
            var ilMap = new Dictionary<string, MethodIlInfo>();

            foreach (var type in assembly.GetTypes())
            {
                //FileLog.Write("type循环开始");
                // 遍历类型的所有方法
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    string methodId;
                    try
                    {
                        methodId = MethhodId(method);
                        if (methodId.Contains("<Postfix>") || methodId.Contains("+"))
                        {
                            //continue;
                        }
                        ilMap[methodId] = new MethodIlInfo
                        {
                            //IlHash = //Tools.ComputeHash(method.GetMethodBody().GetILAsByteArray()),
                            Method = method
                        };
                    }
                    catch (Exception ex)
                    {
                        // 记录异常日志 
                        //FileLog.Write($"An error   occurred: {ex.Message}");
                        // 处理异常或提供默认值
                    }
                }
            }
            FileLog.Write($"获取了 {ilMap.Count} 个 IL map");
            return ilMap;
        }

        //获取methodbase

        public static string MethhodId( MethodBase member)
        {
            var sb = new StringBuilder(128);
            sb.Append(member.DeclaringType.FullName);
            sb.Append('.');
            sb.Append(member.Name);
            sb.Append('(');
            sb.Append(string.Join(", ", member.GetParameters().Select(p => p.ParameterType.FullName)));
            sb.Append(')');
            return sb.ToString();
        }



        public static void ClearRuning()
        {
            string runingSubdirectory = Tools.ModPath("runing/");

            try
            {
                if (Directory.Exists(runingSubdirectory))
                {
                    // 获取目录中的所有文件
                    string[] files = Directory.GetFiles(runingSubdirectory);

                    // 删除每个文件
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }

                    Console.WriteLine("所有文件已成功删除。");
                }
                else
                {
                    Console.WriteLine("目录不存在： " + runingSubdirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("清理运行目录时发生错误：" + ex.Message);
            }
        }

        public static void MoveFile(string dllFile)
        {
            try
            {
                
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string modDirectory = Path.GetDirectoryName(assemblyPath);
                string runingSubdirectory = Tools.ModPath("runing/");

                // 如果子目录不存在，创建它
                if (!Directory.Exists(runingSubdirectory))
                {
                    Directory.CreateDirectory(runingSubdirectory);
                }

                if (!File.Exists(dllFile))
                {
                    FileLog.Write($" 移动文件时 文件 未找到: {dllFile}");
                    return;
                }

                string runFilePath = Path.ChangeExtension(dllFile, ".run");
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
                string runFileNameWithTimestamp = $"{Path.GetFileNameWithoutExtension(dllFile)}_{timeStamp}.run";
                string runTargetPath = Path.Combine(runingSubdirectory, runFileNameWithTimestamp);

                // 检查 .run 文件是否存在
                if (!File.Exists(runFilePath))
                {
                    // 将文件扩展名改为 .run
                    File.Move(dllFile, runTargetPath);
                    //在复制一份副本
                    File.Copy(runTargetPath, dllFile, true);
                    FileLog.Write($"已生成 .run 文件: {runFilePath}");
                }
            }
            catch (Exception ex)
            {
                FileLog.Write($"处理文件 {dllFile} 时发 生错误 : {ex.Message}");
            }
        }


        public static string[] GetRuningDll()
        {
            // 获取当前程序的主目录
            string mainDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // 定义 Mods 文件夹路径
            string modsPath = Path.Combine(mainDirectory, "Mods");


            // 检查 Mods 文件夹是否存在
            if (!Directory.Exists(modsPath))
            {
                Console.WriteLine("Mods 文件夹不存在。");
                return Array.Empty<string>();
            }

            // 获取当前应用程序域中加载的所有程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Console.WriteLine("已加载的程序集及其主体文件路径：");

            var matchingDlls = assemblies
                .Select(assembly =>
                {
                    try
                    {
                        // 获取程序集的主体文件路径
                        string location = assembly.Location;
                        ; 

                        // 判断路径是否包含 modsPath
                        if (!string.IsNullOrEmpty(location) && Path.GetFullPath(location).StartsWith(modsPath, StringComparison.OrdinalIgnoreCase))
                        {
                            string fileName = Path.GetFileName(location);
                            //if (fileName.StartsWith("System.")) return null;
                            if (fileName.StartsWith("HarmonyMod.")) return null;
                            if (fileName.StartsWith("0Harmony")) return null;
                            // 添加到静态字典
                            if (!dllAssemblyMap.ContainsKey(Path.GetFullPath(fileName)))
                            {
                                FileLog.Write("启动加载DLL文件"+ fileName);
                                FileMonitor.Add(location);
                                dllAssemblyMap.Add(fileName, assembly);
                            }
                            return location;
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    return null;
                })
                .Where(location => !string.IsNullOrEmpty(location))
                .ToArray();

            return matchingDlls;
        }
    }

}
