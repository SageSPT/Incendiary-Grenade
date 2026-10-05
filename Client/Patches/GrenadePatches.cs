using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using Systems.Effects;

namespace IncendiaryGrenade.Patches;

internal static class IncendiaryGrenades
{
    private static readonly HashSet<int> Ignited = new HashSet<int>();

    public static bool IsIncendiary(Grenade grenade) =>
        grenade != null && grenade.WeaponSource?.StringTemplateId == IncendiaryConfig.TemplateId;

    public static bool TryClaimIgnition(Grenade grenade) => Ignited.Add(grenade.GetInstanceID());

    public static void Reset() => Ignited.Clear();
}

internal class Patch_Grenade_InvokeBlowUpEvent : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(Grenade), nameof(Grenade.InvokeBlowUpEvent));

    [PatchPrefix]
    private static void Prefix(Grenade __instance)
    {
        if (IncendiaryGrenades.IsIncendiary(__instance) && IncendiaryGrenades.TryClaimIgnition(__instance))
        {
            IncendiaryInstance.Detonate(__instance.transform.position);
        }
    }
}

internal class Patch_Grenade_Explosion : ModulePatch
{
    public static bool MuteEffectSound;

    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(Grenade), nameof(Grenade.Explosion), Type.EmptyTypes);

    [PatchPrefix]
    private static void Prefix(Grenade __instance) => MuteEffectSound = IncendiaryGrenades.IsIncendiary(__instance);

    [PatchPostfix]
    private static void Postfix() => MuteEffectSound = false;
}

internal class Patch_Effects_EmitGrenade : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(Effects), nameof(Effects.EmitGrenade));

    [PatchPrefix]
    private static void Prefix(ref float volume)
    {
        if (Patch_Grenade_Explosion.MuteEffectSound) volume = 0f;
    }
}
