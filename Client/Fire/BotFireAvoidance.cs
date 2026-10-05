using System.Collections.Generic;
using UnityEngine;

namespace IncendiaryGrenade;

internal static class BotFireAvoidance
{
    private const float Margin = 1.0f;

    private sealed class Zone
    {
        public Vector3 Centre;
        public float Radius;
        public float ExpiresAt;
        public readonly List<Vector3> Points = new List<Vector3>();
        public readonly List<float> Radii = new List<float>();
    }

    private static readonly Dictionary<string, Zone> Zones = new Dictionary<string, Zone>();

    public static void Mark(string zoneId, Vector3 centre, List<FireNode> nodes, float duration)
    {
        if (nodes.Count == 0 || duration <= 0f) return;

        var zone = new Zone { Centre = centre, ExpiresAt = Time.time + duration };
        foreach (FireNode node in nodes)
        {
            zone.Points.Add(node.Position);
            zone.Radii.Add(node.Radius + Margin);
            zone.Radius = Mathf.Max(zone.Radius, Vector3.Distance(centre, node.Position) + node.Radius + Margin);
        }

        Zones[zoneId] = zone;
    }

    public static void Clear(string zoneId) => Zones.Remove(zoneId);

    public static void ClearAll() => Zones.Clear();

    public static bool IsInFire(Vector3 position) => FindFire(position, 0f, out _, out _);

    public static bool FindFire(Vector3 position, float extra, out Vector3 centre, out float radius)
    {
        centre = Vector3.zero;
        radius = 0f;
        if (Zones.Count == 0) return false;

        float now = Time.time;
        foreach (Zone zone in Zones.Values)
        {
            if (zone.ExpiresAt < now) continue;
            if ((position - zone.Centre).sqrMagnitude > Sq(zone.Radius + extra)) continue;

            for (int i = 0; i < zone.Points.Count; i++)
            {
                Vector3 offset = position - zone.Points[i];
                if (Mathf.Abs(offset.y) > 2.5f) continue;
                offset.y = 0f;
                if (offset.sqrMagnitude > Sq(zone.Radii[i] + extra)) continue;

                centre = zone.Centre;
                radius = zone.Radius;
                return true;
            }
        }

        return false;
    }

    private static float Sq(float v) => v * v;
}
