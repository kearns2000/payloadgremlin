using PayloadGremlin.Mutations;

namespace PayloadGremlin.Internals;

/// <summary>Registry of all available mutations.</summary>
internal static class MutationRegistry
{
    private static readonly IReadOnlyList<IJsonMutation> All =
    [
        new RemovePropertyMutation(),
        new NullPropertyMutation(),
        new AddUnexpectedPropertyMutation(),
        new RenamePropertyCasingMutation(),
        new PrimitiveTypeMutation(MutationType.StringToNumber),
        new PrimitiveTypeMutation(MutationType.NumberToString),
        new PrimitiveTypeMutation(MutationType.BooleanToString),
        new PrimitiveTypeMutation(MutationType.ObjectToArray),
        new PrimitiveTypeMutation(MutationType.ArrayToObject),
        new PrimitiveTypeMutation(MutationType.EmptyArray),
        new PrimitiveTypeMutation(MutationType.EmptyObject),
        new ValueMutation(MutationType.StringEmpty),
        new ValueMutation(MutationType.StringWhitespace),
        new ValueMutation(MutationType.StringUnicode),
        new ValueMutation(MutationType.NumberZero),
        new ValueMutation(MutationType.NumberNegative),
        new ValueMutation(MutationType.NumberVeryLarge),
        new ValueMutation(MutationType.DecimalCommaSeparator),
        new ValueMutation(MutationType.BooleanFlip),
        new DateMutation(MutationType.DateFormatChange, 0),
        new DateMutation(MutationType.DateFormatChange, 1),
        new DateMutation(MutationType.DateFormatChange, 2),
        new DateMutation(MutationType.DateInvalid),
        new DateMutation(MutationType.DateTimezoneShift),
        new EnumStringMutation(MutationType.EnumCasing),
        new EnumStringMutation(MutationType.EnumUnknownValue),
        new EnumStringMutation(MutationType.EnumWhitespace),
        new EnumStringMutation(MutationType.EnumEmpty),
        new InvalidJsonMutation(MutationType.TruncatedJson),
        new InvalidJsonMutation(MutationType.TrailingComma),
        new InvalidJsonMutation(MutationType.BrokenStringQuote)
    ];

    public static IReadOnlyList<IJsonMutation> GetEnabled(IReadOnlySet<MutationType> enabledTypes) =>
        All.Where(m => enabledTypes.Contains(m.Type)).ToList();
}
