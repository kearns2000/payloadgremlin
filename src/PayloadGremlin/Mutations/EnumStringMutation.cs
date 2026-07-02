using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal sealed class EnumStringMutation : PathMutationBase
{
    private static readonly Regex EnumLikeRegex = new(
        @"^[A-Za-z][A-Za-z0-9_]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly MutationType _type;

    public EnumStringMutation(MutationType type)
    {
        if (type is not (MutationType.EnumCasing or MutationType.EnumUnknownValue or MutationType.EnumWhitespace or MutationType.EnumEmpty))
        {
            throw new ArgumentException($"Unsupported enum mutation: {type}", nameof(type));
        }

        _type = type;
    }

    public override MutationType Type => _type;

    public override MutationSeverity DefaultSeverity => MutationSeverity.Medium;

    public override bool CanApply(MutationContext context) =>
        base.CanApply(context)
        && PassesPathRules(context, isValueChange: true)
        && context.Node is JsonValue v
        && v.TryGetValue<string>(out var s)
        && LooksLikeEnum(s)
        && !DateMutation.LooksLikeDate(s);

    public override AppliedMutation Apply(JsonTree tree, MutationContext context)
    {
        var before = context.Node!.AsValue().GetValue<string>();
        string after;
        string description;

        switch (_type)
        {
            case MutationType.EnumCasing:
                after = before == before.ToUpperInvariant()
                    ? before.ToLowerInvariant()
                    : before.ToUpperInvariant();
                description = $"Changed enum casing at {context.JsonPath}";
                break;
            case MutationType.EnumUnknownValue:
                after = "UNKNOWN_VALUE_" + context.MutationIndex;
                description = $"Replaced enum with unknown value at {context.JsonPath}";
                break;
            case MutationType.EnumWhitespace:
                after = " " + before + " ";
                description = $"Added whitespace around enum value at {context.JsonPath}";
                break;
            case MutationType.EnumEmpty:
                after = "";
                description = $"Replaced enum with empty string at {context.JsonPath}";
                break;
            default:
                throw new InvalidOperationException($"Unhandled enum mutation {_type}");
        }

        var afterNode = JsonValue.Create(after);
        MutationNodeReplacer.Replace(tree, context, afterNode);

        return new AppliedMutation(
            CreateMutationId(context.Seed, context.CaseIndex, context.MutationIndex, Type),
            context.JsonPath,
            Type,
            $"\"{before}\"",
            $"\"{after}\"",
            DefaultSeverity,
            true,
            description);
    }

    private static bool LooksLikeEnum(string value) =>
        value.Length is >= 2 and <= 40
        && EnumLikeRegex.IsMatch(value)
        && !bool.TryParse(value, out _);
}
