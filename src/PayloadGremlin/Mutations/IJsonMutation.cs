using PayloadGremlin.Internals;
using System.Text.Json.Nodes;

namespace PayloadGremlin.Mutations;

/// <summary>Context passed to mutations during planning and application.</summary>
internal sealed class MutationContext
{
    public required GremlinOptions Options { get; init; }
    public required string JsonPath { get; init; }
    public required JsonNode Node { get; init; }
    public required JsonNode? Parent { get; init; }
    public required string? PropertyName { get; init; }
    public required int CaseIndex { get; init; }
    public required int MutationIndex { get; init; }
    public required int Seed { get; init; }
}

/// <summary>Describes a mutation that can be applied to a JSON tree.</summary>
internal interface IJsonMutation
{
    MutationType Type { get; }
    MutationSeverity DefaultSeverity { get; }
    string SignatureKey { get; }

    bool CanApply(MutationContext context);
    AppliedMutation Apply(JsonTree tree, MutationContext context);
}
