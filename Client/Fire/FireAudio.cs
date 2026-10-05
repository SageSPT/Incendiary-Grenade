using UnityEngine;

namespace IncendiaryGrenade;

internal sealed class FireAudio
{
    private const EOcclusionTest Occlusion = EOcclusionTest.ContinuousPropagated;

    private readonly Vector3 _position;
    private BetterSource _loopSource;

    public FireAudio(Vector3 position)
    {
        _position = position;
    }

    public void Ignite()
    {
        PlayOneShot(IncendiaryAssets.IgnitionClip, IncendiaryConfig.IgnitionVolume, IncendiaryConfig.IgnitionMaxDistance);

        if (!CanPlay(IncendiaryAssets.BurnLoopClip)) return;

        bool started = MonoBehaviourSingleton<BetterAudio>.Instance.TryPlayAtPoint(
            out _loopSource,
            _position,
            IncendiaryAssets.BurnLoopClip,
            BetterAudio.AudioSourceGroupType.Environment,
            IncendiaryConfig.BurnMaxDistance,
            IncendiaryConfig.BurnVolume,
            Occlusion,
            null,
            spatialize: true,
            oneShot: false,
            autoReleaseSource: false,
            enabledHighPassFilter: false);

        if (!started || _loopSource == null) return;

        _loopSource.Loop = true;
        _loopSource.VolumeFadeIn(1.5f);
    }

    public void Extinguish()
    {
        PlayOneShot(IncendiaryAssets.ExtinguishClip, IncendiaryConfig.BurnVolume, IncendiaryConfig.BurnMaxDistance);

        if (_loopSource != null && !_loopSource.VolumeFadeOut(1f, Release)) Release();
    }

    public void Release()
    {
        if (_loopSource == null) return;

        _loopSource.Loop = false;
        _loopSource.Release();
        _loopSource = null;
    }

    private void PlayOneShot(AudioClip clip, float volume, int maxDistance)
    {
        if (!CanPlay(clip)) return;

        MonoBehaviourSingleton<BetterAudio>.Instance.TryPlayAtPoint(
            out _,
            _position,
            clip,
            BetterAudio.AudioSourceGroupType.Environment,
            maxDistance,
            volume,
            Occlusion,
            null,
            spatialize: true,
            oneShot: true,
            autoReleaseSource: true,
            enabledHighPassFilter: true);
    }

    private static bool CanPlay(AudioClip clip) =>
        clip != null && MonoBehaviourSingleton<BetterAudio>.Instantiated;
}
