namespace PayloadGremlin;

/// <summary>A named test case with a mutated JSON payload and applied mutation metadata.</summary>
public sealed record GremlinCase(
    string Name,
    string Payload,
    IReadOnlyList<AppliedMutation> Mutations,
    int Seed,
    bool IsValidJson);
