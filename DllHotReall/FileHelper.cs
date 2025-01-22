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
    //[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    //public class ReloadableAttribute : Attribute { }


    [StaticConstructorOnStartup]
    // 插入按钮的 Harmony Patch
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.zilong.dllreload");
            harmony.PatchAll();
        }
    }



    [StaticConstructorOnStartup]

    public class FileHelps
    {

        private static  FileSystemWatcher watcher;
        public static event Action<List<string>> OnFilesChanged;



        private static int namespaceNum=0;
        static  FileHelps()
        {
            LogFilePath=   DateTime.Now.ToString("yyyyMMdd_HHmmssfff")+".log";
            Write("开始启动111");
            Run();
            Run();
            StartFileWatcher();
            //HandleFileChange("C:\\Users\\ZZL\\Desktop\\MOD类型\\dnspy\\DllHotReload.dll");
        }
        private static readonly object LockObject = new object();
        private static string LogFilePath = "log.txt"; // 默认日志文件路径
        public static void Write(string message)
        {
            lock (LockObject)
            {
                try
                {
                    string logEntry = $"{DateTime.Now: HH:mm:ss} {message}";
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                    Console.WriteLine(logEntry); // 可选：同时输出到控制台
                }
                catch (Exception ex)
                {
                    Log.Message($"写入日志时   出错: {ex.Message}");
                }
            }
        }


        private static void StartFileWatcher()
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
            watcher.Changed += OnChanged;
            watcher.EnableRaisingEvents = true;
            Log.Message($"监听文件变更   {modsDirectory} ");
        }
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Log.Message($"发生监听 事件 {e.FullPath} ");

            HandleFileChange(e.FullPath);
        }


        private static void HandleFileChange(string filePath)
        {
            try
            {
                if (!filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    return;
                    Upload(filePath);
  
                Log.Message($"发生监5听事件3 4");
            }
            catch (Exception ex)
            {
                Write(ex.Message);
                Write(ex.StackTrace);
                // 输出详细的异  常 信息，包括堆栈信息
                Log.Message("An error occurred:");
                Log.Message($"Message: {ex.Message}");
                Log.Message($"Stack Trace: {ex.StackTrace}");

                // 如果需要记录内部异常信息
                if (ex.InnerException != null)
                {
                    Log.Message("Inner Exception:");
                    Log.Message($"Message: {ex.InnerException.Message}");
                    Log.Message($"Stack Trace: {ex.InnerException.StackTrace}");
                }
            }
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


        [HarmonyPatch(typeof(Pawn), "GetGizmos")]
        public static class Pawn_GetGizmos_Patch
        {

            public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
            {
                var pawn = __instance;

                // 标志变量：检查是否已经存在征召按钮
                bool alreadyHasVanillaDraftButton = false;

                // 遍历原始按钮集合
                foreach (var g in __result)
                {
                    if (g is Command_Toggle command && command.defaultDesc == "CommandToggleDraftDesc".Translate().ToString())
                    {
                        alreadyHasVanillaDraftButton = true;
                    }

                    // 保留原始按钮
                    yield return g;
                }

                // 如果已经有征召按钮，则继续添加自定义按钮
                if (alreadyHasVanillaDraftButton)
                {
                    // 自定义按钮
                    Command_Toggle customCommand = new Command_Toggle
                    {
                        defaultLabel = "测试按 钮 ", // 按钮显示的标签
                        defaultDesc = "测试按钮说明  ", // 按钮描述
                        hotKey = KeyBindingDefOf.Command_ColonistDraft, // 按钮快捷键
                        toggleAction = delegate
                        {
                            // 调用任务分配逻辑
                            //SitOnSpecificChairUtility.AssignSitJob();
                            //Log.Message($"{pawn.Name} 的坐椅任务已分配。");
                            // 自定义按钮的逻辑
                            //
                            Log.Message("更新测试4");
                        },
                        isActive = () => false, // 按钮的状态（例如激活/禁用）
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft", true) // 按钮的图标
                    };
                    yield return customCommand;
                }
            }
        }

        public static void Run() {
            //加载目标Dll清单
            string[] dllfiles = GetRuningDll(); // 模拟获取已加载的DLL文件路径
            MoveFiles(dllfiles);
            MarkMethhodMap();


        }
        static void SaveModifiedAssembly(AssemblyDefinition assemblyDefinition, string outputPath)
        {
            using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                assemblyDefinition.Write(fileStream);
            }
        }
        static Assembly LoadAssemblyFromFile(string path)
        {
            byte[] dllBytes = File.ReadAllBytes(path);
            return Assembly.Load(dllBytes);
        }
        //更新程序  集
        public static void Upload(string path)
        {


            //修正DLL.只能通过外部辅助了.
            //ModPath("runing");
            var dpath = ModPath("Definition");
            var outfile = dpath + "out.dll";

            //HandleFileChange 
            //byte[] dllBytes = File.ReadAllBytes(path);




            string exePath = System.IO.Path.Combine(dpath, "Definition.exe");



            // 构建参数字符串，确保参数用引号括起来
            string arguments = $"\"{path}\" \"{outfile}\" \"patch_cls{namespaceNum++}\"";
            Log.Message($"执行命令行 参数为 "+ arguments);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath, // 可执行文件路径
                Arguments = arguments, // 命令行参数
                UseShellExecute = false, // 使用外壳程序执行
                RedirectStandardOutput = true, // 重定向标准输出
                RedirectStandardError = true, // 重定向标准错误
                CreateNoWindow = true // 不创建窗口
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                // 读取输出
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                // 输出结果
                System.Console.WriteLine("Output:");
                System.Console.WriteLine(output);
                Log.Message("执行结果" + output);
                if (!string.IsNullOrEmpty(error))
                {
                    System.Console.WriteLine("Error:");
                    System.Console.WriteLine(error);
                }
            }

            //var assemblyDefinition = LoadAndModifyAssembly(path);
            //Write("文件assembly Definition");

            //SaveModifiedAssembly(assemblyDefinition, newFileName);
            Write("文件assemblyDefinitionSave ");
           var loadedAssembly = LoadAssemblyFromFile(outfile);
           Write("文件loadedAssembly" );
           Write($"Loaded   Assembly: {loadedAssembly.FullName}");



           
           // 加载程序集
           // 获取程序集的 IL 映射 
           var ilMap = GetIlMap(loadedAssembly);
            Log.Message($"更新key长度 aaa  : " + ilMap.Count);
           var logstr = "";
           var originakeys = originalMethodMap.Keys.ToList();
           Write($"新文件接 口信息");
            
           foreach (var kvp in ilMap) {
               Write(kvp.Key);
                Log.Message($"更新key长度   : " + kvp.Key);
            }
               foreach (var kvp in originakeys)
           {
               string originaId = kvp;

               if (ilMap.ContainsKey(originaId))
               {

                   MethodIlInfo newMethodInfo = ilMap[originaId];

                    //DetourMethod(kvp.Value.Method,) 
                    // 键已存在，检查哈希是否一 致 

                    MethodIlInfo existingMethodInfo = originalMethodMap[originaId];
                    //Log.Message(" 找到" + originaId + " 对比结果" + object.ReferenceEquals(existingMethodInfo.Method, newMethodInfo.Method).ToString());
                   if (existingMethodInfo.IlHash != newMethodInfo.IlHash)
                   {
                        // 触发判断逻辑
                        //Log.Message($"方 法哈   希 不一致: {originaId}");
                        //Log.Message($"原始哈希: {existingMethodInfo.IlHash}");
                        //Log.Message($"新哈希: {newMethodInfo.IlHash}");
                        // 可以在这里添加更多处理逻辑，如日志记录或更新
                        Log.Message("更新函数"+ originaId);
                        DetourMethod(existingMethodInfo.Method, newMethodInfo.Method);
                   }
               }
           }
        }


        public static void MoveFiles(string[] dllfiles)
        {
            foreach (var dllFile in dllfiles)
            { 
                Log.Message("移动文件: " + dllFile);
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
                    if (!originalMethodMap.ContainsKey(methodId))
                    {
                        // 如果不存在，则添加到 originalMethodMap
                        originalMethodMap[methodId] = kvp.Value;
                    }
                    else
                    {

                    }
                }
            }
            Log.Message($"方法Map总量为: {originalMethodMap.Count}");
            Log.Message("keys: "+originalMethodMap.Keys.ToArray().ToString());
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
                Write("type循环开始");
                // 遍历类型的所有方法
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {


                    string methodId;
                    try
                    {
                        methodId = MethhodId(method);
                        ilMap[methodId] = new MethodIlInfo
                        {
                            IlHash = ComputeHash(method.GetMethodBody().GetILAsByteArray()),
                            Method = method
                        };
                    }
                    catch (Exception ex)
                    {
                        // 记录异常日志 
                        Write($"An error   occurred: {ex.Message}");
                        // 处理异常或提供默认值
                    }



                    /*
                    // 获取方法体的 IL 代码
                    var methodBody = method.GetMethodBody();
                    if (methodBody != null)
                    {
                        var ilBytes = methodBody.GetILAsByteArray();

                        // 计算 IL 代码的哈希
                        string ilHash = ComputeHash(ilBytes);

                        // 获取方法 ID
                        
                        if (methodId.Contains("<Postfix>") ||methodId.Contains("+")) {
                            continue;
                        }
                        // 添加到字典中
                        ilMap[methodId] = new MethodIlInfo
                        {
                            IlHash = ilHash,
                            Method = method
                        };
                    }
                    */
                }
                Write("type循环结束");
            }
            return ilMap;
        }

        //获取methodbase

        public static string MethhodId( MethodBase member)
        {
            //Write("aaa0");
            var sb = new StringBuilder(128);
            //Write("aaa1");
            // 拼接类全名、方法名、方法参数类型等信息
            sb.Append(member.DeclaringType.FullName);
            //Write("aaa2");
            sb.Append('.');
            sb.Append(member.Name);
            //Write("aaa3");
            sb.Append('(');
            sb.Append(string.Join(", ", member.GetParameters().Select(p => p.ParameterType.FullName)));
            //Write("aaa4");
            sb.Append(')');
            // 返回方法的唯一标识符
            return sb.ToString();
        }
        
        //获得 哈希
        static string ComputeHash(byte[] data)
        {
            var a =1;
            a = a;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
        public static string ModPath(string f) {
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string modDirectory = Path.GetDirectoryName(assemblyPath);
            return Path.Combine(modDirectory, "../" + f+"/");
        }
        public static void MoveFile(string dllFile)
        {
            try
            {
                
                string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string modDirectory = Path.GetDirectoryName(assemblyPath);
                string runingSubdirectory = ModPath("runing");



                // 如果子目录不存在，创建它
                if (!Directory.Exists(runingSubdirectory))
                {
                    Directory.CreateDirectory(runingSubdirectory);
                }

                if (!File.Exists(dllFile))
                {
                    Log.Message($" 文件 未找到: {dllFile}");
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
                    Log.Message($"已生成 .run 文件: {runFilePath}");
                }
            }
            catch (Exception ex)
            {
                Log.Message($"处理文件 {dllFile} 时发 生错误 : {ex.Message}");
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

                        // 判断路径是否包含 modsPath
                        if (!string.IsNullOrEmpty(location) && location.StartsWith(modsPath, StringComparison.OrdinalIgnoreCase))
                        {
                            string fileName = Path.GetFileName(location);
                            // 添加到静态字典
                            if (!dllAssemblyMap.ContainsKey(fileName))
                            {
                                Write("启动加载DLL文件"+ fileName);
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
