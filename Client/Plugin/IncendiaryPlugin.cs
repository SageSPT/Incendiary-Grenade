using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using IncendiaryGrenade.Bots;
using IncendiaryGrenade.Patches;
using UnityEngine;

namespace IncendiaryGrenade;

[BepInDependency(BigBrainGuid, BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("com.sage.incendiarygrenade", "Incendiary Grenade", "1.0.2")]
public class IncendiaryPlugin : BaseUnityPlugin
{
    private const string BigBrainGuid = "xyz.drakia.bigbrain";

    private const string MissingBigBrainMessage =
        "Incendiary Grenade requires BigBrain, which is not installed.\n\n" +
        "Install BigBrain, then start the game again.\n\n" +
        "The game will now close.";

    internal static ManualLogSource Log { get; private set; }

    private void Awake()
    {
        Log = Logger;

        if (!Chainloader.PluginInfos.ContainsKey(BigBrainGuid))
        {
            Log.LogFatal("BigBrain is not installed. Closing the game.");
            ShowError(MissingBigBrainMessage);
            Application.Quit();
            return;
        }

        IncendiaryConfig.Bind(Config);

        new Patch_Grenade_InvokeBlowUpEvent().Enable();
        new Patch_Grenade_Explosion().Enable();
        new Patch_Effects_EmitGrenade().Enable();
        new Patch_ThrowWeap_MinTimeToContactExplode().Enable();
        new Patch_GameWorld_OnGameStarted().Enable();
        new Patch_GameWorld_Dispose().Enable();

        RegisterBotLayers();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterBotLayers() => EscapeFireBrains.Register();

    private static void ShowError(string message)
    {
        try
        {
            MessageBox(IntPtr.Zero, message, "Incendiary Grenade", 0x10 | 0x40000);
        }
        catch (Exception ex)
        {
            Log.LogError($"Could not show the error window: {ex.Message}");
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
