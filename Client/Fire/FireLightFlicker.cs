using UnityEngine;

namespace IncendiaryGrenade;

[RequireComponent(typeof(Light))]
internal sealed class FireLightFlicker : MonoBehaviour
{
    private const float MinIntensity = 0.7f, MaxIntensity = 1.3f, IntensitySpeed = 20f;
    private const float MinRange = 0.9f, MaxRange = 1.1f, RangeSpeed = 10f;
    private const float MinInterval = 0.03f, MaxInterval = 0.12f;
    private const float BurstChance = 0.15f, BurstStrength = 0.2f;

    private Light _light;
    private float _baseIntensity, _baseRange;
    private float _intensity = 1f, _targetIntensity = 1f;
    private float _range = 1f, _targetRange = 1f;
    private float _nextRetarget;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
        _baseRange = _light.range;
    }

    private void Update()
    {
        if (!_light.enabled) return;

        if (Time.time >= _nextRetarget)
        {
            _nextRetarget = Time.time + Random.Range(MinInterval, MaxInterval);
            _targetIntensity = Random.Range(MinIntensity, MaxIntensity);
            if (Random.value < BurstChance) _targetIntensity += BurstStrength;
            _targetRange = Random.Range(MinRange, MaxRange);
        }

        _intensity = Mathf.Lerp(_intensity, _targetIntensity, 1f - Mathf.Exp(-IntensitySpeed * Time.deltaTime));
        _range = Mathf.Lerp(_range, _targetRange, 1f - Mathf.Exp(-RangeSpeed * Time.deltaTime));

        _light.intensity = _baseIntensity * _intensity;
        _light.range = _baseRange * _range;
    }
}
