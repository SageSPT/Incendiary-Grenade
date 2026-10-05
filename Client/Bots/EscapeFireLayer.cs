using System.Collections.Generic;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using UnityEngine;
using UnityEngine.AI;

namespace IncendiaryGrenade.Bots;

internal static class EscapeFireBrains
{
    private const int Priority = 200;

    private static readonly List<string> Brains = new List<string>
    {
        "ArenaFighter", "BossBully", "BossGluhar", "BossBoar", "BossPartisan", "Knight", "BossKojaniy",
        "BossSanitar", "BossKolontay", "Tagilla", "TagillaAgro", "BossTest", "Obdolbs", "ExUsec", "BigPipe",
        "BirdEye", "FollowerBully", "FollowerGluharAssault", "FollowerGluharProtect", "FollowerGluharScout",
        "FollowerKojaniy", "FollowerSanitar", "FlBoar", "FlBoarCl", "FlBoarSt", "FlKlnAslt", "KolonSec",
        "TagillaFollower", "HelperAgro", "Gifter", "Killa", "KillaAgro", "Marksman", "BoarSniper", "PMC",
        "SectantPriest", "SctPredvst", "PrizrakSt", "Oni", "SectantWarrior", "CursAssault", "Assault",
        "PmcBear", "PmcUsec"
    };

    public static void Register() => BrainManager.AddCustomLayer(typeof(EscapeFireLayer), Brains, Priority);
}

internal sealed class EscapeFireLayer : CustomLayer
{
    private const float CheckInterval = 0.25f;
    private const float StayActiveAfterLeaving = 1.5f;
    private const float EntryBuffer = 0.5f;

    private float _nextCheck;
    private bool _inFire;
    private float _leftFireAt = float.MinValue;

    public EscapeFireLayer(BotOwner botOwner, int priority) : base(botOwner, priority)
    {
    }

    public override string GetName() => "SAGA-6 Escape Fire";

    public override bool IsActive()
    {
        if (BotOwner == null || BotOwner.IsDead || BotOwner.BotState != EBotState.Active) return false;

        if (Time.time >= _nextCheck)
        {
            _nextCheck = Time.time + CheckInterval;
            bool inFire = BotFireAvoidance.FindFire(BotOwner.Position, EntryBuffer, out _, out _);
            if (_inFire && !inFire) _leftFireAt = Time.time;
            _inFire = inFire;
        }

        return _inFire || Time.time - _leftFireAt < StayActiveAfterLeaving;
    }

    public override Action GetNextAction() => new Action(typeof(EscapeFireLogic), "In fire");

    public override bool IsCurrentActionEnding() => false;
}

internal sealed class EscapeFireLogic : CustomLogic
{
    private const float RepathInterval = 1.0f;
    private const float ClearDistance = 3.0f;
    private const float SampleRange = 2.5f;

    private static readonly float[] Angles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f, 150f, -150f, 180f };

    private float _nextRepath;
    private Vector3? _target;

    public EscapeFireLogic(BotOwner botOwner) : base(botOwner)
    {
    }

    public override void Start()
    {
        _nextRepath = 0f;
        _target = null;
    }

    public override void Stop()
    {
        BotOwner.Mover.Sprint(false);
    }

    public override void Update(CustomLayer.ActionData data)
    {
        BotOwner.SetPose(1f);
        BotOwner.SetTargetMoveSpeed(1f);

        bool arrived = _target.HasValue && (BotOwner.Position - _target.Value).sqrMagnitude < 1f;
        if (Time.time >= _nextRepath || arrived || !BotOwner.Mover.HasPathAndNoComplete)
        {
            _nextRepath = Time.time + RepathInterval;
            PickTarget();
        }

        BotOwner.Mover.Sprint(BotOwner.CanSprintPlayer);
        BotOwner.Steering.LookToMovingDirection();
    }

    private void PickTarget()
    {
        Vector3 position = BotOwner.Position;
        if (!BotFireAvoidance.FindFire(position, 2f, out Vector3 centre, out float radius))
        {
            centre = position - BotOwner.LookDirection;
            radius = 0f;
        }

        Vector3 away = position - centre;
        if (new Vector2(away.x, away.z).sqrMagnitude < 0.01f) away = -BotOwner.LookDirection;
        away.y = 0f;
        away.Normalize();

        float distance = Mathf.Max(radius - Vector3.Distance(position, centre), 0f) + ClearDistance;

        foreach (float angle in Angles)
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * away;
            float reach = angle == 0f ? distance : distance + radius * 0.5f;
            Vector3 wanted = position + dir * reach;

            if (!NavMesh.SamplePosition(wanted, out NavMeshHit hit, SampleRange, NavMesh.AllAreas)) continue;
            if (BotFireAvoidance.IsInFire(hit.position)) continue;

            NavMeshPathStatus status = BotOwner.Mover.GoToPoint(hit.position, false, 0.75f, false, true, false, true);
            if (status != NavMeshPathStatus.PathComplete) continue;

            _target = hit.position;
            return;
        }

        _target = null;
    }
}
