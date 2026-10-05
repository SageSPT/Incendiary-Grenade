using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace IncendiaryGrenade.Patches;

internal class Patch_ThrowWeap_MinTimeToContactExplode : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.PropertyGetter(typeof(ThrowWeap), nameof(ThrowWeap.MinTimeToContactExplode));

    [PatchPrefix]
    private static bool Prefix(ThrowWeap __instance, ref float __result)
    {
        if (__instance.StringTemplateId != IncendiaryConfig.TemplateId) return true;

        __result = IncendiaryConfig.ContactExplodeDelay;
        return false;
    }
}
