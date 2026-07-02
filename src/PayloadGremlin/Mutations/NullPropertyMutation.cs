using System.Text.Json.Nodes;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal sealed class NullPropertyMutation : PathMutationBase
{
    public override MutationType Type => MutationType.NullProperty;
    public override MutationSeverity DefaultSeverity => MutationSeverity.Medium;

    public override bool CanApply(MutationContext context) =>
        base.CanApply(context)
        && PassesPathRules(context, isNull: true)
        && context.Parent is JsonObject
        && context.PropertyName is not null
        && context.Node is JsonValue
        && context.JsonPath != "$";

    public override AppliedMutation Apply(JsonTree tree, MutationContext context)
    {
        var obj = (JsonObject)context.Parent!;
        var before = ValueToString(context.Node);
        obj[context.PropertyName!] = null;

        return new AppliedMutation(
            CreateMutationId(context.Seed, context.CaseIndex, context.MutationIndex, Type),
            context.JsonPath,
            Type,
            before,
            "null",
            DefaultSeverity,
            true,
            $"Set property at {context.JsonPath} to null");
    }
}
