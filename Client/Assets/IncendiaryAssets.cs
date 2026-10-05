using System;
using System.IO;
using SPT.Custom.Utils;
using UnityEngine;

namespace IncendiaryGrenade;

internal static class IncendiaryAssets
{
    private const string FxBundleKey = "assets/content/weapons/saga6/incendiary_fx.bundle";
    private const string AudioBundleKey = "assets/content/weapons/saga6/incendiary_audio.bundle";

    public const float FireNodeBaseRadius = 0.2f;

    private static AssetBundle _fxBundle;
    private static AssetBundle _audioBundle;

    public static GameObject FireNodePrefab { get; private set; }
    public static GameObject SagaFirePrefab { get; private set; }
    public static AudioClip IgnitionClip { get; private set; }
    public static AudioClip BurnLoopClip { get; private set; }
    public static AudioClip ExtinguishClip { get; private set; }

    public static void Load()
    {
        _fxBundle = LoadBundle(FxBundleKey);
        if (_fxBundle != null)
        {
            FireNodePrefab = LoadPrefab("/FireNodeEffect.prefab");
            SagaFirePrefab = LoadPrefab("/SagaFire.prefab");
        }

        _audioBundle = LoadBundle(AudioBundleKey);
        if (_audioBundle != null)
        {
            AudioClip[] clips = _audioBundle.LoadAllAssets<AudioClip>();
            IgnitionClip = FindClip(clips, "inc_grenade_detonate_1");
            BurnLoopClip = FindClip(clips, "fire_loop_1");
            ExtinguishClip = FindClip(clips, "inc_grenade_pop_03");
        }
    }

    public static void Unload()
    {
        if (_fxBundle != null) _fxBundle.Unload(false);
        if (_audioBundle != null) _audioBundle.Unload(false);

        _fxBundle = null;
        _audioBundle = null;
        FireNodePrefab = null;
        SagaFirePrefab = null;
        IgnitionClip = null;
        BurnLoopClip = null;
        ExtinguishClip = null;
    }

    private static GameObject LoadPrefab(string file)
    {
        string path = Array.Find(_fxBundle.GetAllAssetNames(),
            n => n.EndsWith(file, StringComparison.OrdinalIgnoreCase));
        if (path != null) return _fxBundle.LoadAsset<GameObject>(path);

        IncendiaryPlugin.Log.LogError($"FX bundle is missing {file.TrimStart('/')}.");
        return null;
    }

    private static AssetBundle LoadBundle(string key)
    {
        if (!BundleManager.Bundles.TryGetValue(key, out var item))
        {
            IncendiaryPlugin.Log.LogError($"Bundle '{key}' is not registered. Is the IncendiaryGrenade server mod installed?");
            return null;
        }

        string path = Path.GetFullPath(BundleManager.GetBundleFilePath(item));
        AssetBundle bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) IncendiaryPlugin.Log.LogError($"Could not load bundle '{path}'.");
        return bundle;
    }

    private static AudioClip FindClip(AudioClip[] clips, string name)
    {
        AudioClip clip = Array.Find(clips, c => c != null && string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase));
        if (clip == null) IncendiaryPlugin.Log.LogError($"Audio bundle is missing clip '{name}'.");
        return clip;
    }
}
