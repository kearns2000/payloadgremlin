namespace PayloadGremlin;

/// <summary>Metadata describing a single mutation applied to a payload.</summary>
public sealed record AppliedMutation(
    string MutationId,
    string JsonPath,
    MutationType MutationType,
    string? BeforeValue,
    string? AfterValue,
    MutationSeverity Severity,
    bool IsValidJson,
    string Description);
