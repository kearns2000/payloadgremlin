using System.Text.Json.Nodes;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal sealed class AddUnexpectedPropertyMutation : PathMutationBase
{
    private static readonly string[] UnexpectedNames =
    [
        "_extra", "legacyField", "UNKNOWN", "meta", "debugInfo", "tempValue"
    ];

    public override MutationType Type => MutationType.AddUnexpectedProperty;
    public override MutationSeverity DefaultSeverity => MutationSeverity.Medium;

    public override bool CanApply(MutationContext context) =>
        base.CanApply(context)
        && context.Node is JsonObject;

    public override AppliedMutation Apply(JsonTree tree, MutationContext context)
    {
        var obj = (JsonObject)context.Node;
        var nameIndex = (int)((uint)(context.Seed + context.CaseIndex) % (uint)UnexpectedNames.Length);
        var name = UnexpectedNames[nameIndex] + context.MutationIndex;
        var value = "unexpected";
        obj[name] = value;

        return new AppliedMutation(
            CreateMutationId(context.Seed, context.CaseIndex, context.MutationIndex, Type),
            context.JsonPath,
            Type,
            null,
            $"\"{value}\"",
            DefaultSeverity,
            true,
            $"Added unexpected property '{name}' at {context.JsonPath}");
    }
}
