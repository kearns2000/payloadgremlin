using System.Text.Json.Nodes;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal sealed class RenamePropertyCasingMutation : PathMutationBase
{
    public override MutationType Type => MutationType.RenamePropertyCasing;
    public override MutationSeverity DefaultSeverity => MutationSeverity.Medium;

    public override bool CanApply(MutationContext context) =>
        base.CanApply(context)
        && PassesPathRules(context, isTypeChange: true)
        && context.Parent is JsonObject obj
        && context.PropertyName is { } name
        && name != ToAlternateCasing(name)
        && !obj.ContainsKey(ToAlternateCasing(name));

    public override AppliedMutation Apply(JsonTree tree, MutationContext context)
    {
        var obj = (JsonObject)context.Parent!;
        var oldName = context.PropertyName!;
        var newName = ToAlternateCasing(oldName);
        var value = obj[oldName]!.DeepClone();
        var before = ValueToString(value);
        obj.Remove(oldName);
        obj[newName] = value;

        return new AppliedMutation(
            CreateMutationId(context.Seed, context.CaseIndex, context.MutationIndex, Type),
            context.JsonPath,
            Type,
            before,
            before,
            DefaultSeverity,
            true,
            $"Renamed property '{oldName}' to '{newName}' at {JsonPath.ParentPath(context.JsonPath)}");
    }

    private static string ToAlternateCasing(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (char.IsLower(name[0]))
        {
            return char.ToUpperInvariant(name[0]) + name[1..];
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
