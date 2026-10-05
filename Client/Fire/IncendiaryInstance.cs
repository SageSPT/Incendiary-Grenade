using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IncendiaryGrenade;

public class IncendiaryInstance : MonoBehaviour
{
    private const float ScorchEdge = 0.8f;
    private const float ScorchMinHeight = 0.6f;
    private const float ScorchMaxHeight = 2.0f;

    private const float CarpetEmission = 0.35f;
    private const float SmokeEmission = 0.5f;

    private const float BigFireSpacing = 2.5f;
    private const float BigFireScale = 0.5f;
    private const float BigFireLightIntensity = 1.1f;
    private const float BigFireLightRange = 7f;
    private const float BigFireLightHeight = 0.6f;
    private const string SmokeColumnName = "Smoke (1)";

    private static readonly List<IncendiaryInstance> Active = new List<IncendiaryInstance>();

    private readonly List<IncendiaryDamageTrigger> _triggers = new List<IncendiaryDamageTrigger>();
    private readonly List<Vector3> _bigFirePoints = new List<Vector3>();

    private List<FireNode> _nodes;
    private float _startTime;
    private FireAudio _audio;
    private string _zoneId;

    private float Elapsed => Time.time - _startTime;

    public static void Detonate(Vector3 position)
    {
        List<FireNode> nodes = FireSpread.Generate(position);
        if (nodes.Count == 0) return;

        var root = new GameObject("IncendiaryFire");
        root.transform.position = position;
        root.AddComponent<IncendiaryInstance>().Initialize(nodes);
    }

    public static void DestroyAll()
    {
        foreach (IncendiaryInstance instance in Active.ToArray())
        {
            if (instance != null) Destroy(instance.gameObject);
        }
        Active.Clear();
    }

    private void Initialize(List<FireNode> nodes)
    {
        _nodes = nodes;
        _startTime = Time.time;
        Active.Add(this);

        Vector3 centre = Centroid(nodes);

        _audio = new FireAudio(centre);
        _audio.Ignite();

        StampScorch(centre);

        _zoneId = "saga6_fire_" + GetInstanceID();
        BotFireAvoidance.Mark(_zoneId, centre, nodes, IncendiaryConfig.BurnDuration);

        StartCoroutine(SpawnNodesRoutine());
        StartCoroutine(BurnoutRoutine());
    }

    private IEnumerator SpawnNodesRoutine()
    {
        _nodes.Sort((a, b) => a.TimeOffset.CompareTo(b.TimeOffset));

        foreach (FireNode node in _nodes)
        {
            float delay = node.TimeOffset - Elapsed;
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (Elapsed >= IncendiaryConfig.BurnDuration) yield break;

            SpawnCarpet(node);
            TryBigFire(node);
            _triggers.Add(IncendiaryDamageTrigger.Create(transform, node.Position, node.Radius));
        }
    }

    private void SpawnCarpet(FireNode node)
    {
        GameObject prefab = IncendiaryAssets.FireNodePrefab;
        if (prefab == null) return;

        GameObject fire = Instantiate(prefab, node.Position, node.Rotation, transform);
        fire.SetActive(false);

        fire.transform.Rotate(0f, Random.Range(0f, 360f), 0f, Space.Self);
        fire.transform.localScale = Vector3.one *
                                    (node.Radius / IncendiaryAssets.FireNodeBaseRadius * Random.Range(0.85f, 1.15f));

        foreach (ParticleSystem ps in fire.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;
            main.simulationSpeed *= Random.Range(0.8f, 1.25f);
            main.startLifetimeMultiplier *= Random.Range(0.8f, 1.2f);
            main.startSizeMultiplier *= Random.Range(0.8f, 1.2f);
            main.startDelayMultiplier += Random.Range(0f, 0.1f);

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTimeMultiplier *= CarpetEmission * SmokeFactor(ps);
        }

        foreach (Light light in fire.GetComponentsInChildren<Light>(true))
        {
            Destroy(light);
        }

        fire.SetActive(true);
    }

    private void TryBigFire(FireNode node)
    {
        GameObject prefab = IncendiaryAssets.SagaFirePrefab;
        if (prefab == null) return;

        foreach (Vector3 point in _bigFirePoints)
        {
            if ((point - node.Position).sqrMagnitude < BigFireSpacing * BigFireSpacing) return;
        }
        _bigFirePoints.Add(node.Position);

        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * prefab.transform.rotation;
        GameObject fire = Instantiate(prefab, node.Position, rotation, transform);
        fire.SetActive(false);

        float scale = BigFireScale * Random.Range(0.9f, 1.1f);
        fire.transform.localScale = Vector3.one * scale;

        foreach (ParticleSystem ps in fire.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.gameObject.name == SmokeColumnName)
            {
                ps.gameObject.SetActive(false);
                continue;
            }

            ParticleSystem.MainModule main = ps.main;
            main.startSize = Scaled(main.startSize, scale);
            main.startSpeed = Scaled(main.startSpeed, scale);
            main.simulationSpeed *= Random.Range(0.9f, 1.1f);

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTimeMultiplier *= SmokeFactor(ps);
        }

        var lightHost = new GameObject("SagaFireLight");
        lightHost.transform.SetParent(fire.transform, false);
        lightHost.transform.position = node.Position + Vector3.up * BigFireLightHeight;

        Light fireLight = lightHost.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.55f, 0.25f);
        fireLight.intensity = BigFireLightIntensity;
        fireLight.range = BigFireLightRange;
        fireLight.shadows = LightShadows.None;
        lightHost.AddComponent<FireLightFlicker>();

        fire.SetActive(true);
    }

    private static ParticleSystem.MinMaxCurve Scaled(ParticleSystem.MinMaxCurve curve, float scale)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                curve.constant *= scale;
                break;
            case ParticleSystemCurveMode.TwoConstants:
                curve.constantMin *= scale;
                curve.constantMax *= scale;
                break;
            default:
                curve.curveMultiplier *= scale;
                break;
        }
        return curve;
    }

    private static float SmokeFactor(ParticleSystem ps) =>
        ps.gameObject.name.StartsWith("Smoke") ? SmokeEmission : 1f;

    private void StampScorch(Vector3 centre)
    {
        Vector3 normal = Vector3.zero;
        float radius = 0f;
        foreach (FireNode node in _nodes)
        {
            normal += node.Rotation * Vector3.up;
            radius = Mathf.Max(radius, Vector3.Distance(centre, node.Position) + node.Radius * ScorchEdge);
        }
        normal = normal.normalized;

        float deviation = 0f;
        foreach (FireNode node in _nodes)
        {
            deviation = Mathf.Max(deviation, Mathf.Abs(Vector3.Dot(node.Position - centre, normal)));
        }

        float height = Mathf.Clamp(deviation * 2f + ScorchMinHeight, ScorchMinHeight, ScorchMaxHeight);
        ScorchDecal.StampArea(centre, normal, radius, height);
    }

    private IEnumerator BurnoutRoutine()
    {
        yield return new WaitForSeconds(IncendiaryConfig.BurnDuration);

        foreach (IncendiaryDamageTrigger trigger in _triggers)
        {
            if (trigger != null) trigger.Extinguish();
        }

        BotFireAvoidance.Clear(_zoneId);

        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        foreach (Light light in GetComponentsInChildren<Light>(true))
        {
            light.enabled = false;
        }

        _audio.Extinguish();

        yield return new WaitForSeconds(IncendiaryConfig.CleanupDelay);
        Destroy(gameObject);
    }

    private static Vector3 Centroid(List<FireNode> nodes)
    {
        Vector3 sum = Vector3.zero;
        foreach (FireNode node in nodes) sum += node.Position;
        return sum / nodes.Count;
    }

    private void OnDestroy()
    {
        Active.Remove(this);
        _audio?.Release();
        if (_zoneId != null) BotFireAvoidance.Clear(_zoneId);
    }
}
