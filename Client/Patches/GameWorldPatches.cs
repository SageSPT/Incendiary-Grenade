using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace IncendiaryGrenade.Patches;

internal class Patch_GameWorld_OnGameStarted : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));

    [PatchPostfix]
    private static void Postfix()
    {
        ResetRaidState();
        IncendiaryAssets.Load();
    }

    internal static void ResetRaidState()
    {
        IncendiaryDamageTrigger.ResetStatics();
        IncendiaryGrenades.Reset();
        BotFireAvoidance.ClearAll();
    }
}

internal class Patch_GameWorld_Dispose : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GameWorld), nameof(GameWorld.Dispose));

    [PatchPrefix]
    private static void Prefix()
    {
        IncendiaryInstance.DestroyAll();
        IncendiaryAssets.Unload();
        Patch_GameWorld_OnGameStarted.ResetRaidState();
    }
}
