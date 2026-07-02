namespace PayloadGremlin;

/// <summary>Named mutation profiles that enable realistic API drift scenarios.</summary>
public enum GremlinProfile
{
    /// <summary>Common real-world API contract drift: nulls, type coercion, date formats, casing.</summary>
    RealisticApiDrift,

    /// <summary>Mutations that break strict deserializers: missing fields, type swaps, empty collections.</summary>
    StrictClientBreaker,

    /// <summary>Legacy system quirks: whitespace, unicode, comma decimals, string booleans.</summary>
    LegacySystemWeirdness,

    /// <summary>Focused chaos on dates, money, and numeric fields.</summary>
    DateAndMoneyChaos,

    /// <summary>Enables the widest set of mutations, including invalid JSON when allowed.</summary>
    Aggressive
}
