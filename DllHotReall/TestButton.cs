using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DllHotReall
{

    //[HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class TestButton
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
                    defaultLabel = "测试按钮 ", // 按钮显示的标签
                    defaultDesc = "测试按钮说明  ", // 按钮描述
                    hotKey = KeyBindingDefOf.Command_ColonistDraft, // 按钮快捷键
                    toggleAction = delegate
                    {
                        // 调用任务分配逻辑
                        //SitOnSpecificChairUtility.AssignSitJob();
                        Messages.Message("!!!!!", MessageTypeDefOf.PositiveEvent);
                        Log.Message($"{pawn.Name} 的坐椅任务已分配222。");
                        // 自定义按钮的逻辑
                        //
                        Log.Message(Tools.ModPath("dllreload.log"));
                    },
                    isActive = () => false, // 按钮的状态（例如激活/禁用）
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft", true) // 按钮的图标
                };
                yield return customCommand;
            }
        }
    }
}
