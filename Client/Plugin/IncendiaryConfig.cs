namespace IncendiaryGrenade;

internal static class IncendiaryConfig
{
    public const string TemplateId = "6aabad0ff49370bbdc709173";

    public const int GroundLayerMask = (1 << 9) | (1 << 11) | (1 << 18);

    public const int MaxNodes = 35;
    public const float SpreadRadius = 0.8f;
    public const float FireRadius = 1.0f;
    public const float TimeBetweenNodes = 0.15f;
    public const float MaxStepHeight = 0.5f;
    public const float MaxDropHeight = 2.0f;
    public const float MaxSpreadDistance = 12.0f;

    public const float BurnDuration = 15.0f;
    public const float CleanupDelay = 5.0f;
    public const float ContactExplodeDelay = 0.25f;

    public const float IgnitionVolume = 0.8f;
    public const float BurnVolume = 1.0f;
    public const int IgnitionMaxDistance = 100;
    public const int BurnMaxDistance = 100;
}
