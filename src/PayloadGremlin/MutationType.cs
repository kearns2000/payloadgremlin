namespace PayloadGremlin;

/// <summary>Types of JSON mutations PayloadGremlin can apply.</summary>
public enum MutationType
{
    RemoveProperty,
    NullProperty,
    AddUnexpectedProperty,
    RenamePropertyCasing,
    StringToNumber,
    NumberToString,
    BooleanToString,
    ObjectToArray,
    ArrayToObject,
    EmptyArray,
    EmptyObject,
    StringEmpty,
    StringWhitespace,
    StringUnicode,
    NumberZero,
    NumberNegative,
    NumberVeryLarge,
    DecimalCommaSeparator,
    BooleanFlip,
    DateFormatChange,
    DateInvalid,
    DateTimezoneShift,
    EnumCasing,
    EnumUnknownValue,
    EnumWhitespace,
    EnumEmpty,
    TruncatedJson,
    TrailingComma,
    BrokenStringQuote
}
