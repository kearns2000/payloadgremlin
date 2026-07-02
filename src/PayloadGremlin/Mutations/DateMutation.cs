using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using PayloadGremlin.Internals;

namespace PayloadGremlin.Mutations;

internal sealed class DateMutation : PathMutationBase
{
    private static readonly Regex IsoDateRegex = new(
        @"^\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})?)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly MutationType _type;
    private readonly int _variant;

    public DateMutation(MutationType type, int variant = 0)
    {
        if (type is not (MutationType.DateFormatChange or MutationType.DateInvalid or MutationType.DateTimezoneShift))
        {
            throw new ArgumentException($"Unsupported date mutation: {type}", nameof(type));
        }

        _type = type;
        _variant = variant;
    }

    public override MutationType Type => _type;
    public override string SignatureKey => $"{_type}:{_variant}";

    public override MutationSeverity DefaultSeverity => _type switch
    {
        MutationType.DateInvalid => MutationSeverity.High,
        _ => MutationSeverity.Medium
    };

    public override bool CanApply(MutationContext context) =>
        base.CanApply(context)
        && PassesPathRules(context, isValueChange: true)
        && context.Node is JsonValue v
        && v.TryGetValue<string>(out var s)
        && LooksLikeDate(s);

    public override AppliedMutation Apply(JsonTree tree, MutationContext context)
    {
        var before = context.Node!.AsValue().GetValue<string>();
        string after;
        string description;

        // CanApply/LooksLikeDate guarantees this parses; TryParseDate keeps Apply crash-safe.
        TryParseDate(before, out var dt);

        switch (_type)
        {
            case MutationType.DateFormatChange:
                after = (_variant % 3) switch
                {
                    0 => dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    1 => dt.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),
                    _ => dt.ToString("D", CultureInfo.InvariantCulture)
                };
                description = $"Changed date format at {context.JsonPath}";
                break;
            case MutationType.DateInvalid:
                after = $"{dt:yyyy}-{dt:MM}-32";
                description = $"Set invalid but plausible date at {context.JsonPath}";
                break;
            case MutationType.DateTimezoneShift:
                after = dt.ToUniversalTime().AddHours(5).ToString("o", CultureInfo.InvariantCulture);
                description = $"Timezone-shifted ISO date at {context.JsonPath}";
                break;
            default:
                throw new InvalidOperationException($"Unhandled date mutation {_type}");
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

    internal static bool LooksLikeDate(string value) =>
        (IsoDateRegex.IsMatch(value) || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        && TryParseDate(value, out _);

    private static bool TryParseDate(string value, out DateTime dt) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dt)
        || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
}
