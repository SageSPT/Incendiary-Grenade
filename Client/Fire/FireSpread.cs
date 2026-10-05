using System.Collections.Generic;
using UnityEngine;

namespace IncendiaryGrenade;

public static class FireSpread
{
    private const float MaxGroundDrop = 30f;
    private const int Mask = IncendiaryConfig.GroundLayerMask;

    private static readonly Vector3[] Directions =
    {
        new Vector3(1f, 0f, 0f),
        new Vector3(0.5f, 0f, 0.866f),
        new Vector3(-0.5f, 0f, 0.866f),
        new Vector3(-1f, 0f, 0f),
        new Vector3(-0.5f, 0f, -0.866f),
        new Vector3(0.5f, 0f, -0.866f)
    };

    public static List<FireNode> Generate(Vector3 origin)
    {
        var nodes = new List<FireNode>(IncendiaryConfig.MaxNodes);
        if (!TryFindGround(origin, out Vector3 startPos, out Vector3 startNormal)) return nodes;

        var queue = new Queue<(Vector3 Position, Vector3 Normal, int Depth)>();
        var visited = new HashSet<Vector3Int> { Quantize(startPos) };
        queue.Enqueue((startPos, startNormal, 0));

        while (queue.Count > 0 && nodes.Count < IncendiaryConfig.MaxNodes)
        {
            var current = queue.Dequeue();

            nodes.Add(new FireNode
            {
                Position = current.Position,
                Rotation = Quaternion.FromToRotation(Vector3.up, current.Normal),
                Radius = IncendiaryConfig.FireRadius,
                TimeOffset = current.Depth * IncendiaryConfig.TimeBetweenNodes
            });

            float upOffset = IncendiaryConfig.MaxStepHeight + 0.1f;
            if (Physics.Raycast(current.Position + Vector3.up * 0.05f, Vector3.up, out RaycastHit ceiling, upOffset, Mask))
            {
                upOffset = Mathf.Max(0.01f, ceiling.distance - 0.05f);
            }

            foreach (Vector3 dir in Directions)
            {
                if (nodes.Count + queue.Count >= IncendiaryConfig.MaxNodes) break;

                Vector3 rayStart = current.Position + Vector3.up * upOffset;
                if (Physics.Raycast(rayStart, dir, IncendiaryConfig.SpreadRadius, Mask)) continue;

                Vector3 downStart = rayStart + dir * IncendiaryConfig.SpreadRadius;
                if (!Physics.Raycast(downStart, Vector3.down, out RaycastHit ground,
                        upOffset + IncendiaryConfig.MaxDropHeight, Mask)) continue;

                Vector3 newPos = ground.point;
                if (Vector3.Distance(startPos, newPos) > IncendiaryConfig.MaxSpreadDistance) continue;

                float heightDiff = newPos.y - current.Position.y;
                if (heightDiff > IncendiaryConfig.MaxStepHeight || heightDiff < -IncendiaryConfig.MaxDropHeight) continue;

                if (!visited.Add(Quantize(newPos))) continue;

                queue.Enqueue((newPos, ground.normal, current.Depth + 1));
            }
        }

        return nodes;
    }

    private static bool TryFindGround(Vector3 origin, out Vector3 point, out Vector3 normal)
    {
        float upOffset = 0.5f;
        if (Physics.Raycast(origin + Vector3.up * 0.05f, Vector3.up, out RaycastHit ceiling, 0.5f, Mask))
        {
            upOffset = Mathf.Max(0f, ceiling.distance - 0.05f);
        }

        if (Physics.Raycast(origin + Vector3.up * upOffset, Vector3.down, out RaycastHit hit, MaxGroundDrop, Mask))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        point = Vector3.zero;
        normal = Vector3.up;
        return false;
    }

    private static Vector3Int Quantize(Vector3 position)
    {
        float cell = IncendiaryConfig.SpreadRadius * 0.8f;
        return new Vector3Int(
            Mathf.RoundToInt(position.x / cell),
            Mathf.RoundToInt(position.y),
            Mathf.RoundToInt(position.z / cell));
    }
}
