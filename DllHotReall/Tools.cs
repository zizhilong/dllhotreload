using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DllHotReall
{
    internal class Tools
    {
        public static string ModPath(string f)
        {
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string modDirectory = Path.GetDirectoryName(assemblyPath);
            return Path.Combine(modDirectory, "../" + f);
        }
        //获得 哈希
        public static string ComputeHash(byte[] data)
        {
            var a = 1;
            a = a;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
        public static bool InAssemblies(string path)
        {
            var directoryPath = Path.GetDirectoryName(path);
            return (directoryPath != null && Path.GetFileName(directoryPath) == "Assemblies" );
        }
        public static void ModLog(string str) {
            Log.Message("DllHotReload:"+str);
            //Messages.Message("DllHotReload: " + str, MessageTypeDefOf.PositiveEvent);

        }
        public static bool IsHarmony(string path) { 

            if(path.Contains("<Postfix>") || 
                path.Contains("<Apply>" ) || 
                path.Contains("<fixMorphAbility>") ||
                path.Contains("<ConfigErrors>")||
                path.Contains("<GetWarnings>")||
                path.Contains("<>")
                //
                ) {
                return true;
            }

            return false;
        }
        public static bool CheckMethodsForHarmonyPatch(MethodBase method)
        {
            // 检查方法是否带有 HarmonyPatch 属性
            var harmonyPatchAttributes = method.GetCustomAttributes(typeof(HarmonyPatch), false);
            if (harmonyPatchAttributes.Length > 0)
            {
                    return true;
            }
            return false;
        }
    }
}
