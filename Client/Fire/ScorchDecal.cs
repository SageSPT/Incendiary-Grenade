using System;
using System.Collections.Generic;
using Comfort.Common;
using DeferredDecals;
using HarmonyLib;
using Systems.Effects;
using UnityEngine;

namespace IncendiaryGrenade;

internal static class ScorchDecal
{
    private static readonly AccessTools.FieldRef<DeferredDecalRenderer, List<DynamicDeferredDecalRenderer>> DynamicDecals =
        AccessTools.FieldRefAccess<DeferredDecalRenderer, List<DynamicDeferredDecalRenderer>>("_dynamicDecals");

    private static readonly AccessTools.FieldRef<DeferredDecalRenderer, int> DynamicIndex =
        AccessTools.FieldRefAccess<DeferredDecalRenderer, int>("_currentDynamicDecalIndex");

    private static readonly AccessTools.FieldRef<DeferredDecalRenderer, int> MaxDynamic =
        AccessTools.FieldRefAccess<DeferredDecalRenderer, int>("_maxDynamicDecals");

    private static readonly AccessTools.FieldRef<DeferredDecalRenderer, BoundingSphere[]> Spheres =
        AccessTools.FieldRefAccess<DeferredDecalRenderer, BoundingSphere[]>("_dynamicDecalsBoundingSpheres");

    private static readonly AccessTools.FieldRef<DeferredDecalRenderer, Mesh> Cube =
        AccessTools.FieldRefAccess<DeferredDecalRenderer, Mesh>("_cube");

    private static readonly AccessTools.FieldRef<DeferredDecalRenderer, DeferredDecalRenderer.SingleDecal> GrenadeDecal =
        AccessTools.FieldRefAccess<DeferredDecalRenderer, DeferredDecalRenderer.SingleDecal>("_grenadeDecal");

    private static readonly AccessTools.FieldRef<DeferredDecalRenderer, Dictionary<Camera, DeferredDecalRenderer.CameraData>> Cameras =
        AccessTools.FieldRefAccess<DeferredDecalRenderer, Dictionary<Camera, DeferredDecalRenderer.CameraData>>("_cameras");

    public static void StampArea(Vector3 centre, Vector3 normal, float radius, float height)
    {
        try
        {
            DeferredDecalRenderer renderer = Renderer();
            if (renderer == null) return;

            List<DynamicDeferredDecalRenderer> decals = DynamicDecals(renderer);
            DeferredDecalRenderer.SingleDecal grenade = GrenadeDecal(renderer);
            if (decals == null || decals.Count == 0 || grenade?.DynamicDecalMaterial == null) return;

            int index = DynamicIndex(renderer);
            DynamicDeferredDecalRenderer decal = decals[index];
            if (decal == null) return;

            float diameter = radius * 2f;
            Transform t = decal.transform;
            t.localScale = new Vector3(diameter, height, diameter);
            t.up = normal;
            t.position = centre;
            t.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 359f), Space.Self);

            if (decal.TransformHelper != null)
            {
                decal.TransformHelper.position = centre;
                decal.TransformHelper.rotation = t.rotation;
                decal.TransformHelper.hasChanged = false;
            }

            BoundingSphere[] spheres = Spheres(renderer);
            if (spheres != null && decal.CullingGroupSphereIndex < spheres.Length)
                spheres[decal.CullingGroupSphereIndex] = new BoundingSphere(centre, radius * 1.5f);

            decal.enabled = true;
            var uv = new Vector4(0f, 0f, grenade.TileUSize, grenade.TileVSize);
            decal.Init(grenade.DynamicDecalMaterial, Cube(renderer), normal, uv,
                grenade.IsTiled, decal.CullingGroupSphereIndex);

            DynamicIndex(renderer) = (index + 1) % Mathf.Max(1, MaxDynamic(renderer));

            MarkDirty(renderer);
            ScorchCameraGuard.Attach(decal, height);
        }
        catch (Exception ex)
        {
            IncendiaryPlugin.Log.LogWarning($"Scorch decal failed: {ex.Message}");
        }
    }

    public static void MarkDirty()
    {
        DeferredDecalRenderer renderer = Renderer();
        if (renderer != null) MarkDirty(renderer);
    }

    private static void MarkDirty(DeferredDecalRenderer renderer)
    {
        foreach (DeferredDecalRenderer.CameraData camera in Cameras(renderer).Values)
            camera.IsDynamicBufferDirty = true;
    }

    private static DeferredDecalRenderer Renderer()
    {
        if (!Singleton<Effects>.Instantiated) return null;

        EFTHardSettings settings = EFTHardSettings.Instance;
        if (settings == null || !settings.DEFERRED_DECALS_ENABLED) return null;

        return Singleton<Effects>.Instance.DeferredDecals;
    }
}
