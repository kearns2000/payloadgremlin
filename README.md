![PayloadGremlin](https://raw.githubusercontent.com/kearns2000/payloadgremlin/main/icon.png)

# PayloadGremlin

[![NuGet](https://img.shields.io/nuget/v/PayloadGremlin?style=flat&logo=nuget)](https://www.nuget.org/packages/PayloadGremlin)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Build](https://github.com/kearns2000/payloadgremlin/actions/workflows/ci.yml/badge.svg)](https://github.com/kearns2000/payloadgremlin/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/github/license/kearns2000/payloadgremlin)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-xUnit-5C2D91?style=flat&logo=xunit)](tests/PayloadGremlin.Tests)

**Target framework:** `net10.0` · **Language:** C# 14 · **Test runner:** xUnit

**PayloadGremlin makes good JSON payloads misbehave on purpose.**

PayloadGremlin is a small .NET library for generating realistic bad JSON variants from known-good payloads. Use it in unit and integration tests to find brittle deserializers, API clients, and downstream code before production does.

This is not a general random fuzzer. It focuses on the kind of contract drift that shows up in real API integrations: nulls where you expected strings, numbers sent as strings, date format changes, unexpected properties, and casing mismatches.

## Why it exists

Production APIs change. Legacy systems send odd formats. Deserializers fail in subtle ways. Random fuzzing can miss the failures that actually happen in the wild.

PayloadGremlin gives you:

- Named, realistic mutation profiles
- Deterministic output when you set a seed
- Per-path configuration
- Mutation metadata so failing tests are easy to understand
- Simple failure shrinking for multi-mutation cases

## Installation

```bash
dotnet add package PayloadGremlin
```

See [PUBLISHING.md](PUBLISHING.md) for how releases are published via NuGet trusted publishing.

## Quick start

```csharp
using PayloadGremlin;

var json = """
{
  "customerId": "123",
  "premium": 1200.50,
  "active": true,
  "startDate": "2026-01-01"
}
""";

var gremlin = global::PayloadGremlin.PayloadGremlin.Create(options =>
{
    options.WithSeed(12345);
    options.UseProfile(GremlinProfile.RealisticApiDrift);
    options.MaxCases(10);
});

var result = gremlin.Generate(json);

foreach (var testCase in result.Cases)
{
    Console.WriteLine(testCase.Name);
    Console.WriteLine(testCase.Payload);
}
```

## Profiles

Profiles enable sensible sets of mutations without listing each type manually.

| Profile | What it exercises |
|---|---|
| `RealisticApiDrift` | Common drift: nulls, type coercion, date formats, casing |
| `StrictClientBreaker` | Missing fields, type swaps, empty collections |
| `LegacySystemWeirdness` | Whitespace, unicode, comma decimals, string booleans |
| `DateAndMoneyChaos` | Dates, decimals, numeric edge cases |
| `Aggressive` | Widest mutation set (still valid JSON by default) |

```csharp
options.UseProfile(GremlinProfile.StrictClientBreaker);
```

You can also enable specific mutation types:

```csharp
options.Enable(MutationType.RemoveProperty);
options.Enable(MutationType.NumberToString);
options.Enable(MutationType.DateFormatChange);
```

## Path configuration

Target or protect specific JSON paths:

```csharp
var gremlin = global::PayloadGremlin.PayloadGremlin.Create(options =>
{
    options.WithSeed(12345);
    options.UseProfile(GremlinProfile.RealisticApiDrift);

    options.ForPath("$.customerId", path =>
    {
        path.DoNotRemove();
        path.AllowNull(false);
    });

    options.ExcludePath("$.metadata.traceId");
});
```

## Deterministic seeds

The same input JSON, configuration, and seed always produce the same cases in the same order.

```csharp
options.WithSeed(42);
```

When a test fails, use the seed and mutation metadata to reproduce the exact case.

## Mutation metadata

Every case includes `AppliedMutation` records with:

- Mutation id
- JSON path
- Mutation type
- Before / after values
- Severity
- Whether the output is valid JSON
- Human-readable description

The reproduction seed is on each `GremlinCase` (`testCase.Seed`), not on individual mutations.

```csharp
foreach (var mutation in testCase.Mutations)
{
    Console.WriteLine($"{mutation.MutationType} @ {mutation.JsonPath}");
    Console.WriteLine($"  {mutation.Description}");
}
```

## Shrinking

If a test fails on a case with multiple mutations, ask for smaller versions:

```csharp
var smallerCases = gremlin.Shrink(failingCase);
```

v1 removes one mutation at a time to help isolate the smallest failing payload.

## Reports

```csharp
var result = gremlin.Generate(json);
Console.WriteLine(result.Report.ToMarkdown());
```

The report summarises total cases, mutation types used, paths touched, paths never touched, invalid JSON count, and the reproduction seed.

## Invalid JSON mode

By default, generated payloads stay valid JSON. To include syntactic breakage:

```csharp
options.AllowInvalidJson();
```

This enables truncated JSON, trailing commas, and broken string quotes.

## Example xUnit usage

```csharp
[Theory]
[MemberData(nameof(GremlinCases))]
public void Deserialize_HandlesMutatedPayloads(string name, string payload)
{
    var result = MyDeserializer.TryParse(payload, out var model);
    Assert.True(result, $"Failed on case: {name}");
}

public static IEnumerable<object[]> GremlinCases()
{
    var json = File.ReadAllText("Fixtures/order.json");
    var result = global::PayloadGremlin.PayloadGremlin.Create(o =>
    {
        o.WithSeed(42);
        o.UseProfile(GremlinProfile.RealisticApiDrift);
        o.MaxCases(25);
    }).Generate(json);

    return result.Cases.Select(c => new object[] { c.Name, c.Payload });
}
```

## Building from source

```bash
dotnet build
dotnet test
dotnet run --project samples/PayloadGremlin.Sample
```

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for setup, project layout, and how to add mutations or profiles.

Please read our [Code of Conduct](CODE_OF_CONDUCT.md) before participating.

Quick start for contributors:

```bash
git clone https://github.com/kearns2000/payloadgremlin.git
cd PayloadGremlin
dotnet build -c Release
dotnet test -c Release
```

Open a pull request with tests for any behaviour change. CI runs build and test on every PR.

## License

MIT
