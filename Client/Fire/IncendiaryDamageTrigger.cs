using System.Collections.Generic;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using UnityEngine;

namespace IncendiaryGrenade;

public class IncendiaryDamageTrigger : DamageTrigger
{
    private const float DamagePerTick = 15f;
    private const float TickInterval = 0.5f;

    private static readonly HashSet<BodyPartCollider> DamagedThisFrame = new HashSet<BodyPartCollider>();
    private static readonly Dictionary<BodyPartCollider, float> FlameTimers = new Dictionary<BodyPartCollider, float>();
    private static int _lastFrame = -1;

    private bool _extinguished;

    public override string Description => "IncendiaryFlame";

    public override bool IsStatic => true;

    public override void ProceedDamage(IObserverToPlayerBridge player, BodyPartCollider bodyPart)
    {
        if (_extinguished) return;

        if (Time.frameCount != _lastFrame)
        {
            _lastFrame = Time.frameCount;
            DamagedThisFrame.Clear();
        }

        if (!DamagedThisFrame.Add(bodyPart)) return;

        Burn(bodyPart);
    }

    private static void Burn(BodyPartCollider bodyPart)
    {
        FlameTimers.TryGetValue(bodyPart, out float timer);
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            bodyPart.ApplyEnvironmentalDamage(new DamageInfo
            {
                DamageType = EDamageType.Flame,
                Damage = DamagePerTick,
                Direction = Vector3.zero,
                HitCollider = bodyPart.Collider,
                HitNormal = Vector3.zero,
                HitPoint = Vector3.zero,
                HittedBallisticCollider = bodyPart,
                Player = null
            });
            timer = TickInterval;
        }

        FlameTimers[bodyPart] = timer;
    }

    public override void AddPenalty(IObserverToPlayerBridge player) { }

    public override void RemovePenalty(IObserverToPlayerBridge player) { }

    public override void PlaySound(bool useOcclusion = false) { }

    public static IncendiaryDamageTrigger Create(Transform parent, Vector3 position, float radius)
    {
        var host = new GameObject("IncendiaryFireTrigger");
        host.transform.SetParent(parent, false);
        host.transform.position = position;

        SphereCollider sphere = host.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius;

        return host.AddComponent<IncendiaryDamageTrigger>();
    }

    public void Extinguish()
    {
        _extinguished = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public static void ResetStatics()
    {
        DamagedThisFrame.Clear();
        FlameTimers.Clear();
        _lastFrame = -1;
    }
}
