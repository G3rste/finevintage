using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace FineVintage
{
    public class WinePatch
    {
        public static void Patch(Harmony harmony)
        {
            harmony.Patch(methodInfo()
                , transpiler: new HarmonyMethod(typeof(WinePatch).GetMethod("Transpile", BindingFlags.Static | BindingFlags.Public)));
        }

        public static void Unpatch(Harmony harmony)
        {
            harmony.Unpatch(methodInfo()
                , HarmonyPatchType.Transpiler, "oats.finevintage");
        }

        public static MethodInfo methodInfo()
        {
            return typeof(BlockLiquidContainerBase).GetMethod("GetNutritionProperties", BindingFlags.Instance | BindingFlags.Public);
        }
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            int found = 0;
            List<CodeInstruction> result = new List<CodeInstruction>();
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && instruction.LoadsField(typeof(WaterTightContainableProps).GetField("NutritionPropsPerLitre")) && found++ == 1)
                {
                    result.RemoveAt(result.Count - 1);
                    result.Add(new CodeInstruction(OpCodes.Ldloc_0));
                    result.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WinePortion), "GetNutritionPropertiesPerLitre")));
                }
                else
                {
                    result.Add(instruction);
                }
            }
            if (found != 2)
                throw new Exception("Could not patch nutrition properties of wine!");
            return result;
        }
    }
}