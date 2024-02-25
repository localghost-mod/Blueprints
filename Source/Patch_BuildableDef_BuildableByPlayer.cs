using HarmonyLib;
using Verse;

namespace Blueprints
{
    [HarmonyPatch(typeof(BuildableDef), nameof(BuildableDef.BuildableByPlayer), MethodType.Getter)]
    public class Patch_BuildableDef_BuildableByPlayer
    {
        public static void Postfix(BuildableDef __instance, ref bool __result) => __result = __result || Extensions.Props.Contains(__instance);
    }
}
