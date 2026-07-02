using System.Globalization;
using System.Text.Json.Nodes;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal sealed class ValueMutation : PathMutationBase
{
    private readonly MutationType _type;

    public ValueMutation(MutationType type)
    {
        if (type is not (MutationType.StringEmpty or MutationType.StringWhitespace or MutationType.StringUnicode
            or MutationType.NumberZero or MutationType.NumberNegative or MutationType.NumberVeryLarge
            or MutationType.DecimalCommaSeparator or MutationType.BooleanFlip))
        {
            throw new ArgumentException($"Unsupported value mutation: {type}", nameof(type));
        }

        _type = type;
    }

    public override MutationType Type => _type;

    public override MutationSeverity DefaultSeverity => _type switch
    {
        MutationType.NumberVeryLarge => MutationSeverity.High,
        MutationType.StringUnicode => MutationSeverity.Medium,
        _ => MutationSeverity.Low
    };

    public override bool CanApply(MutationContext context)
    {
        if (!base.CanApply(context) || !PassesPathRules(context, isValueChange: true))
        {
            return false;
        }

        return _type switch
        {
            MutationType.StringEmpty or MutationType.StringWhitespace or MutationType.StringUnicode or MutationType.DecimalCommaSeparator
                => context.Node is JsonValue v && v.TryGetValue<string>(out _),
            MutationType.NumberZero or MutationType.NumberNegative or MutationType.NumberVeryLarge
                => context.Node is JsonValue v && (v.TryGetValue<decimal>(out _) || v.TryGetValue<int>(out _) || v.TryGetValue<double>(out _)),
            MutationType.BooleanFlip => context.Node is JsonValue v && v.TryGetValue<bool>(out _),
            _ => false
        };
    }

    public override AppliedMutation Apply(JsonTree tree, MutationContext context)
    {
        var before = ValueToString(context.Node);
        JsonNode afterNode;
        string description;

        switch (_type)
        {
            case MutationType.StringEmpty:
                afterNode = JsonValue.Create("");
                description = $"Set string to empty at {context.JsonPath}";
                break;
            case MutationType.StringWhitespace:
                afterNode = JsonValue.Create("  " + context.Node!.AsValue().GetValue<string>() + "  ");
                description = $"Added leading/trailing whitespace at {context.JsonPath}";
                break;
            case MutationType.StringUnicode:
                afterNode = JsonValue.Create(context.Node!.AsValue().GetValue<string>() + "\u200B\u00A0");
                description = $"Appended unexpected Unicode at {context.JsonPath}";
                break;
            case MutationType.NumberZero:
                afterNode = JsonValue.Create(0m);
                description = $"Set number to zero at {context.JsonPath}";
                break;
            case MutationType.NumberNegative:
                var num = GetNumeric(context.Node!);
                afterNode = JsonValue.Create(-Math.Abs(num));
                description = $"Set number to negative at {context.JsonPath}";
                break;
            case MutationType.NumberVeryLarge:
                afterNode = JsonValue.Create(999999999999999m);
                description = $"Set number to very large value at {context.JsonPath}";
                break;
            case MutationType.DecimalCommaSeparator:
                var original = context.Node!.AsValue().GetValue<string>();
                if (decimal.TryParse(original, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                {
                    afterNode = JsonValue.Create(d.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ','));
                }
                else
                {
                    afterNode = JsonValue.Create(original.Replace('.', ','));
                }
                description = $"Changed decimal separator to comma at {context.JsonPath}";
                break;
            case MutationType.BooleanFlip:
                var b = context.Node!.AsValue().GetValue<bool>();
                afterNode = JsonValue.Create(!b);
                description = $"Flipped boolean at {context.JsonPath}";
                break;
            default:
                throw new InvalidOperationException($"Unhandled mutation type {_type}");
        }

        MutationNodeReplacer.Replace(tree, context, afterNode);
        return new AppliedMutation(
            CreateMutationId(context.Seed, context.CaseIndex, context.MutationIndex, Type),
            context.JsonPath,
            Type,
            before,
            ValueToString(afterNode),
            DefaultSeverity,
            true,
            description);
    }

    private static decimal GetNumeric(JsonNode node)
    {
        var value = node.AsValue();
        if (value.TryGetValue<decimal>(out var dec)) return dec;
        if (value.TryGetValue<long>(out var l)) return l;
        if (value.TryGetValue<int>(out var i)) return i;
        if (value.TryGetValue<double>(out var dbl))
        {
            // Doubles can exceed the decimal range; clamp instead of overflowing.
            if (double.IsNaN(dbl)) return 0m;
            if (dbl >= (double)decimal.MaxValue) return decimal.MaxValue;
            if (dbl <= (double)decimal.MinValue) return decimal.MinValue;
            return (decimal)dbl;
        }
        throw new InvalidOperationException($"Cannot read numeric value at node.");
    }
}
