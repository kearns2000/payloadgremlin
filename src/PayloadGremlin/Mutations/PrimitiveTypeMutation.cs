using System.Globalization;
using System.Text.Json.Nodes;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal sealed class PrimitiveTypeMutation : PathMutationBase
{
    private readonly MutationType _type;

    public PrimitiveTypeMutation(MutationType type)
    {
        if (type is not (MutationType.StringToNumber or MutationType.NumberToString or MutationType.BooleanToString
            or MutationType.ObjectToArray or MutationType.ArrayToObject or MutationType.EmptyArray or MutationType.EmptyObject))
        {
            throw new ArgumentException($"Unsupported primitive type mutation: {type}", nameof(type));
        }

        _type = type;
    }

    public override MutationType Type => _type;

    public override MutationSeverity DefaultSeverity => _type switch
    {
        MutationType.ObjectToArray or MutationType.ArrayToObject => MutationSeverity.High,
        MutationType.EmptyArray or MutationType.EmptyObject => MutationSeverity.Medium,
        _ => MutationSeverity.Medium
    };

    public override bool CanApply(MutationContext context)
    {
        if (!base.CanApply(context) || !PassesPathRules(context, isTypeChange: true))
        {
            return false;
        }

        return _type switch
        {
            MutationType.StringToNumber => context.Node is JsonValue v && v.TryGetValue<string>(out var s) && LooksNumeric(s),
            MutationType.NumberToString => context.Node is JsonValue v && (v.TryGetValue<decimal>(out _) || v.TryGetValue<int>(out _) || v.TryGetValue<double>(out _)),
            MutationType.BooleanToString => context.Node is JsonValue v && v.TryGetValue<bool>(out _),
            MutationType.ObjectToArray => context.Node is JsonObject && context.JsonPath != "$",
            MutationType.ArrayToObject => context.Node is JsonArray { Count: > 0 } && context.JsonPath != "$",
            MutationType.EmptyArray => context.Node is JsonArray { Count: > 0 },
            MutationType.EmptyObject => context.Node is JsonObject { Count: > 0 } && context.JsonPath != "$",
            _ => false
        };
    }

    public override AppliedMutation Apply(JsonTree tree, MutationContext context)
    {
        var before = ValueToString(context.Node);
        JsonNode? afterNode = null;
        string description;

        switch (_type)
        {
            case MutationType.StringToNumber:
                var str = context.Node!.AsValue().GetValue<string>();
                afterNode = decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out var dec)
                    ? JsonValue.Create(dec)
                    : JsonValue.Create(long.Parse(str, CultureInfo.InvariantCulture));
                description = $"Changed string to number at {context.JsonPath}";
                break;

            case MutationType.NumberToString:
                afterNode = JsonValue.Create(context.Node!.ToJsonString().Trim('"'));
                description = $"Changed number to string at {context.JsonPath}";
                break;

            case MutationType.BooleanToString:
                var boolVal = context.Node!.AsValue().GetValue<bool>();
                afterNode = JsonValue.Create(boolVal ? "true" : "false");
                description = $"Changed boolean to string at {context.JsonPath}";
                break;

            case MutationType.ObjectToArray:
                afterNode = new JsonArray(context.Node!.DeepClone());
                description = $"Wrapped object in array at {context.JsonPath}";
                break;

            case MutationType.ArrayToObject:
                var arr = (JsonArray)context.Node!;
                afterNode = arr[0]!.DeepClone();
                description = $"Replaced array with first element at {context.JsonPath}";
                break;

            case MutationType.EmptyArray:
                afterNode = new JsonArray();
                description = $"Emptied array at {context.JsonPath}";
                break;

            case MutationType.EmptyObject:
                afterNode = new JsonObject();
                description = $"Emptied object at {context.JsonPath}";
                break;

            default:
                throw new InvalidOperationException($"Unhandled mutation type {_type}");
        }

        MutationNodeReplacer.Replace(tree, context, afterNode!);
        var after = ValueToString(afterNode);

        return new AppliedMutation(
            CreateMutationId(context.Seed, context.CaseIndex, context.MutationIndex, Type),
            context.JsonPath,
            Type,
            before,
            after,
            DefaultSeverity,
            true,
            description);
    }

    private static bool LooksNumeric(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
        || long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
}
