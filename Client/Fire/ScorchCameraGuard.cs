using UnityEngine;

namespace IncendiaryGrenade;

internal sealed class ScorchCameraGuard : MonoBehaviour
{
    private const float EyeClearance = 0.08f;
    private const float MinHeight = 0.1f;

    private DynamicDeferredDecalRenderer _decal;
    private Vector3 _position;
    private float _height;
    private float _applied;

    public static void Attach(DynamicDeferredDecalRenderer decal, float height)
    {
        var host = new GameObject("SagaScorchGuard");
        var guard = host.AddComponent<ScorchCameraGuard>();
        guard._decal = decal;
        guard._position = decal.transform.position;
        guard._height = height;
        guard._applied = height;
    }

    private void LateUpdate()
    {
        if (_decal == null || !_decal.enabled || _decal.transform.position != _position)
        {
            Destroy(gameObject);
            return;
        }

        Camera camera = Camera.main;
        if (camera == null) return;

        Transform t = _decal.transform;
        Vector3 local = t.InverseTransformPoint(camera.transform.position);
        bool insideFootprint = Mathf.Abs(local.x) < 0.5f && Mathf.Abs(local.z) < 0.5f;

        float wanted = _height;
        if (insideFootprint)
        {
            float above = Mathf.Abs(local.y) * _applied;
            wanted = Mathf.Clamp((above - EyeClearance) * 2f, MinHeight, _height);
        }

        if (Mathf.Approximately(wanted, _applied)) return;

        _applied = wanted;
        Vector3 scale = t.localScale;
        t.localScale = new Vector3(scale.x, wanted, scale.z);
        ScorchDecal.MarkDirty();
    }
}
