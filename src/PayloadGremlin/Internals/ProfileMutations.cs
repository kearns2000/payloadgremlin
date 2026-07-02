namespace PayloadGremlin.Internals;

/// <summary>Maps profiles to their default enabled mutation sets.</summary>
internal static class ProfileMutations
{
    private static readonly MutationType[] RealisticApiDrift =
    [
        MutationType.NullProperty,
        MutationType.NumberToString,
        MutationType.StringToNumber,
        MutationType.BooleanToString,
        MutationType.RenamePropertyCasing,
        MutationType.StringWhitespace,
        MutationType.DateFormatChange,
        MutationType.EnumCasing,
        MutationType.RemoveProperty,
        MutationType.AddUnexpectedProperty
    ];

    private static readonly MutationType[] StrictClientBreaker =
    [
        MutationType.RemoveProperty,
        MutationType.NullProperty,
        MutationType.ObjectToArray,
        MutationType.ArrayToObject,
        MutationType.EmptyArray,
        MutationType.EmptyObject,
        MutationType.AddUnexpectedProperty,
        MutationType.StringToNumber,
        MutationType.NumberToString,
        MutationType.DateInvalid
    ];

    private static readonly MutationType[] LegacySystemWeirdness =
    [
        MutationType.StringWhitespace,
        MutationType.StringUnicode,
        MutationType.DecimalCommaSeparator,
        MutationType.DateFormatChange,
        MutationType.RenamePropertyCasing,
        MutationType.BooleanToString,
        MutationType.NumberVeryLarge,
        MutationType.NumberToString,
        MutationType.EnumWhitespace
    ];

    private static readonly MutationType[] DateAndMoneyChaos =
    [
        MutationType.DateFormatChange,
        MutationType.DateInvalid,
        MutationType.DateTimezoneShift,
        MutationType.DecimalCommaSeparator,
        MutationType.NumberToString,
        MutationType.NumberZero,
        MutationType.NumberNegative,
        MutationType.NumberVeryLarge,
        MutationType.StringToNumber
    ];

    private static readonly MutationType[] AggressiveBase =
    [
        MutationType.RemoveProperty,
        MutationType.NullProperty,
        MutationType.AddUnexpectedProperty,
        MutationType.RenamePropertyCasing,
        MutationType.StringToNumber,
        MutationType.NumberToString,
        MutationType.BooleanToString,
        MutationType.ObjectToArray,
        MutationType.ArrayToObject,
        MutationType.EmptyArray,
        MutationType.EmptyObject,
        MutationType.StringEmpty,
        MutationType.StringWhitespace,
        MutationType.StringUnicode,
        MutationType.NumberZero,
        MutationType.NumberNegative,
        MutationType.NumberVeryLarge,
        MutationType.DecimalCommaSeparator,
        MutationType.BooleanFlip,
        MutationType.DateFormatChange,
        MutationType.DateInvalid,
        MutationType.DateTimezoneShift,
        MutationType.EnumCasing,
        MutationType.EnumUnknownValue,
        MutationType.EnumWhitespace,
        MutationType.EnumEmpty
    ];

    private static readonly MutationType[] InvalidJsonMutations =
    [
        MutationType.TruncatedJson,
        MutationType.TrailingComma,
        MutationType.BrokenStringQuote
    ];

    public static HashSet<MutationType> GetForProfile(GremlinProfile profile, bool allowInvalidJson)
    {
        var set = profile switch
        {
            GremlinProfile.RealisticApiDrift => new HashSet<MutationType>(RealisticApiDrift),
            GremlinProfile.StrictClientBreaker => new HashSet<MutationType>(StrictClientBreaker),
            GremlinProfile.LegacySystemWeirdness => new HashSet<MutationType>(LegacySystemWeirdness),
            GremlinProfile.DateAndMoneyChaos => new HashSet<MutationType>(DateAndMoneyChaos),
            GremlinProfile.Aggressive => new HashSet<MutationType>(AggressiveBase),
            _ => new HashSet<MutationType>(RealisticApiDrift)
        };

        if (allowInvalidJson)
        {
            foreach (var m in InvalidJsonMutations)
            {
                set.Add(m);
            }
        }

        return set;
    }
}
