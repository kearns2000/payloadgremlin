namespace PayloadGremlin;

/// <summary>How disruptive a mutation is likely to be for downstream consumers.</summary>
public enum MutationSeverity
{
    Low,
    Medium,
    High,
    Critical
}
