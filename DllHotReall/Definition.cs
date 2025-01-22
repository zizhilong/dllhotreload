using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DllHotReall
{
    internal class Definition
    {
        private static int namespaceNum = 0;
        public static Assembly Start(string path)
        {
            FileLog.Write("Definition"+path);
            //修正DLL.只能通过外部辅助了 . 
            //ModPath("runing");
            var dpath = Tools.ModPath("Definition/");
            var outfile = dpath + "out.dll";
            //HandleFileChange 
            //byte[] dllBytes = File.ReadAllBytes(path);
            string exePath = System.IO.Path.Combine(dpath, "Definition.exe");
            // 构建参数字符串，确保参数用引号括起来
            string arguments = $"\"{path}\" \"{outfile}\" \"patch_cls{namespaceNum++}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath, // 可执行文件路径
                Arguments = arguments, // 命令行参数
                UseShellExecute = false, // 使用外壳程序执行
                RedirectStandardOutput = true, // 重定向标准输出
                RedirectStandardError = true, // 重定向标准错误
                CreateNoWindow = true // 不创建窗口
            };
            FileLog.Write("Definition Process");
            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                // 读取输出
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();
                FileLog.Write("Definition WaitForExit"+ output);
                // 输出结果
                System.Console.WriteLine("Output:");
                System.Console.WriteLine(output);
                FileLog.Write("执行结果 " + output);
                if (!string.IsNullOrEmpty(error))
                {
                    System.Console.WriteLine("Error:");
                    System.Console.WriteLine(error);
                }
            }
            //返回对象 
            byte[] dllBytes = File.ReadAllBytes(outfile);
            FileLog.Write("去签名后的数据长度"+ dllBytes.Length);
            return Assembly.Load(dllBytes);
        }
    }
}
