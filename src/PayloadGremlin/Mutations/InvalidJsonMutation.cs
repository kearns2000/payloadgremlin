using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal sealed class InvalidJsonMutation : PathMutationBase
{
    private readonly MutationType _type;

    public InvalidJsonMutation(MutationType type)
    {
        if (type is not (MutationType.TruncatedJson or MutationType.TrailingComma or MutationType.BrokenStringQuote))
        {
            throw new ArgumentException($"Unsupported invalid JSON mutation: {type}", nameof(type));
        }

        _type = type;
    }

    public override MutationType Type => _type;

    public override MutationSeverity DefaultSeverity => MutationSeverity.Critical;

    public override bool CanApply(MutationContext context) =>
        context.Options.InvalidJsonAllowed && context.JsonPath == "$";

    public override AppliedMutation Apply(JsonTree tree, MutationContext context)
    {
        var validJson = tree.ToJson();
        string invalid;
        string description;

        switch (_type)
        {
            case MutationType.TruncatedJson:
                invalid = validJson[..Math.Max(1, validJson.Length / 2)];
                description = "Truncated JSON payload";
                break;
            case MutationType.TrailingComma:
                invalid = validJson.TrimEnd('}').TrimEnd(']') + ",}";
                description = "Added trailing comma to JSON";
                break;
            case MutationType.BrokenStringQuote:
                var idx = validJson.IndexOf('"');
                invalid = idx >= 0
                    ? validJson[..idx] + validJson[(idx + 1)..]
                    : validJson + "\"";
                description = "Removed a string opening quote";
                break;
            default:
                throw new InvalidOperationException($"Unhandled invalid JSON mutation {_type}");
        }

        return new AppliedMutation(
            CreateMutationId(context.Seed, context.CaseIndex, context.MutationIndex, Type),
            "$",
            Type,
            validJson,
            invalid,
            DefaultSeverity,
            false,
            description);
    }
}
