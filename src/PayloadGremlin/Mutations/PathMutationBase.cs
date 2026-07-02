using System.Text.Json.Nodes;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal abstract class PathMutationBase : IJsonMutation
{
    public abstract MutationType Type { get; }
    public abstract MutationSeverity DefaultSeverity { get; }
    public virtual string SignatureKey => Type.ToString();

    public virtual bool CanApply(MutationContext context) =>
        !IsPathExcluded(context) && PassesPathRules(context);

    public abstract AppliedMutation Apply(JsonTree tree, MutationContext context);

    protected static bool IsPathExcluded(MutationContext context) =>
        JsonPath.IsExcluded(context.JsonPath, context.Options.ExcludedPaths);

    protected bool PassesPathRules(MutationContext context, bool isRemove = false, bool isNull = false, bool isTypeChange = false, bool isValueChange = false)
    {
        if (!context.Options.PathRules.TryGetValue(context.JsonPath, out var rules))
        {
            return true;
        }

        if (isRemove && !rules.AllowRemoveProperty) return false;
        if (isNull && !rules.AllowNullValue) return false;
        if (isTypeChange && !rules.AllowTypeChangeMutations) return false;
        if (isValueChange && !rules.AllowValueChangeMutations) return false;
        return true;
    }

    protected static string CreateMutationId(int seed, int caseIndex, int mutationIndex, MutationType type) =>
        $"mg-{seed}-{caseIndex}-{mutationIndex}-{type}";

    protected static string? ValueToString(JsonNode? node) =>
        node switch
        {
            null => null,
            JsonValue v => v.ToJsonString(),
            _ => node.ToJsonString()
        };
}
