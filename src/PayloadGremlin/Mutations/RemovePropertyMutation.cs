using System.Text.Json.Nodes;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal sealed class RemovePropertyMutation : PathMutationBase
{
    public override MutationType Type => MutationType.RemoveProperty;
    public override MutationSeverity DefaultSeverity => MutationSeverity.High;

    public override bool CanApply(MutationContext context) =>
        base.CanApply(context)
        && PassesPathRules(context, isRemove: true)
        && context.Parent is JsonObject
        && context.PropertyName is not null
        && context.JsonPath != "$";

    public override AppliedMutation Apply(JsonTree tree, MutationContext context)
    {
        var obj = (JsonObject)context.Parent!;
        var before = ValueToString(context.Node);
        obj.Remove(context.PropertyName!);

        return new AppliedMutation(
            CreateMutationId(context.Seed, context.CaseIndex, context.MutationIndex, Type),
            context.JsonPath,
            Type,
            before,
            null,
            DefaultSeverity,
            true,
            $"Removed property at {context.JsonPath}");
    }
}
