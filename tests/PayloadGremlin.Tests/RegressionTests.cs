namespace PayloadGremlin.Tests;

public class RegressionTests
{
    private const string NestedJson = """
        {
          "customer": { "id": "123", "name": "Acme" },
          "order": { "total": 99.99, "items": [{ "sku": "A1" }] },
          "metadata": { "traceId": "abc" }
        }
        """;

    [Theory]
    [InlineData(GremlinProfile.RealisticApiDrift)]
    [InlineData(GremlinProfile.StrictClientBreaker)]
    [InlineData(GremlinProfile.LegacySystemWeirdness)]
    [InlineData(GremlinProfile.DateAndMoneyChaos)]
    [InlineData(GremlinProfile.Aggressive)]
    public void NestedJson_AllProfiles_DoNotCrashAcrossSeeds(GremlinProfile profile)
    {
        for (var seed = 0; seed < 100; seed++)
        {
            var ex = Record.Exception(() =>
                GremlinEngine.Create(o =>
                {
                    o.WithSeed(seed);
                    o.UseProfile(profile);
                    o.MaxCases(20);
                }).Generate(NestedJson));

            Assert.Null(ex);
        }
    }

    [Theory]
    [InlineData(MutationType.StringEmpty)]
    [InlineData(MutationType.NumberToString)]
    [InlineData(MutationType.BooleanFlip)]
    public void RootScalar_DoesNotCrash(MutationType mutationType)
    {
        var json = mutationType switch
        {
            MutationType.StringEmpty => "\"hello\"",
            MutationType.NumberToString => "42",
            MutationType.BooleanFlip => "true",
            _ => throw new ArgumentOutOfRangeException(nameof(mutationType))
        };

        var ex = Record.Exception(() =>
            GremlinEngine.Create(o =>
            {
                o.WithSeed(1);
                o.Enable(mutationType);
                o.MaxCases(1);
            }).Generate(json));

        Assert.Null(ex);
    }

    [Fact]
    public void RootArray_EmptyArray_DoesNotCrash()
    {
        var ex = Record.Exception(() =>
            GremlinEngine.Create(o =>
            {
                o.WithSeed(1);
                o.Enable(MutationType.EmptyArray);
                o.MaxCases(1);
            }).Generate("[{\"id\":1}]"));

        Assert.Null(ex);
    }

    [Fact]
    public void ExtremeSeed_DoesNotOverflow()
    {
        var ex = Record.Exception(() =>
            GremlinEngine.Create(o =>
            {
                o.WithSeed(int.MaxValue);
                o.Enable(MutationType.AddUnexpectedProperty);
                o.MaxCases(5);
            }).Generate("""{"a":{"b":"c"}}"""));

        Assert.Null(ex);
    }

    [Fact]
    public void DateFormatChange_ProducesMultipleVariants()
    {
        var json = """{"startDate":"2026-01-01","endDate":"2026-06-15"}""";
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(7);
            o.Enable(MutationType.DateFormatChange);
            o.MaxCases(50);
        }).Generate(json);

        var formats = result.Cases
            .SelectMany(c => c.Mutations)
            .Where(m => m.MutationType == MutationType.DateFormatChange)
            .Select(m => m.AfterValue)
            .Distinct()
            .ToList();

        Assert.True(formats.Count >= 2, $"Expected multiple date formats, got: {string.Join(", ", formats)}");
    }

    [Fact]
    public void RenamePropertyCasing_SkipsCollidingKeys()
    {
        var json = """{"name":"lower","Name":"upper"}""";
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(1);
            o.Enable(MutationType.RenamePropertyCasing);
            o.MaxCases(10);
        }).Generate(json);

        Assert.DoesNotContain(
            result.Cases.SelectMany(c => c.Mutations),
            m => m.MutationType == MutationType.RenamePropertyCasing && m.JsonPath == "$.Name");
    }

    [Fact]
    public void InvalidJsonWithValidMutation_PayloadReflectsBoth()
    {
        var json = """{"a":"one","b":"two"}""";
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(3);
            o.AllowInvalidJson();
            o.Enable(MutationType.RemoveProperty);
            o.Enable(MutationType.TruncatedJson);
            o.MaxCases(30);
        }).Generate(json);

        var combined = result.Cases.FirstOrDefault(c =>
            c.Mutations.Any(m => m.MutationType == MutationType.RemoveProperty)
            && c.Mutations.Any(m => m.MutationType == MutationType.TruncatedJson));

        if (combined is null)
        {
            return;
        }

        var removed = combined.Mutations.First(m => m.MutationType == MutationType.RemoveProperty);
        var removedKey = removed.JsonPath.TrimStart('$', '.').Split('.')[^1];
        Assert.DoesNotContain($"\"{removedKey}\"", combined.Payload);
        Assert.False(combined.IsValidJson);
    }

    [Theory]
    [InlineData("2026-02-30")]
    [InlineData("2026-13-01")]
    [InlineData("2026-01-32")]
    [InlineData("0000-01-01")]
    public void DateMutation_DateShapedButInvalidValue_DoesNotCrash(string value)
    {
        var json = $"{{\"d\":\"{value}\"}}";
        var ex = Record.Exception(() =>
            GremlinEngine.Create(o =>
            {
                o.WithSeed(1);
                o.Enable(MutationType.DateFormatChange);
                o.Enable(MutationType.DateInvalid);
                o.Enable(MutationType.DateTimezoneShift);
                o.MaxCases(10);
            }).Generate(json));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData("1e29")]
    [InlineData("1e308")]
    [InlineData("-1e308")]
    public void NumberNegative_ValueExceedingDecimalRange_DoesNotOverflow(string number)
    {
        var json = $"{{\"v\":{number}}}";
        var ex = Record.Exception(() =>
            GremlinEngine.Create(o =>
            {
                o.WithSeed(1);
                o.Enable(MutationType.NumberNegative);
                o.MaxCases(3);
            }).Generate(json));

        Assert.Null(ex);
    }

    [Fact]
    public void DecimalCommaSeparator_WorksUnderInvariantGlobalization()
    {
        var previous = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
            var ex = Record.Exception(() =>
                GremlinEngine.Create(o =>
                {
                    o.WithSeed(2);
                    o.Enable(MutationType.DecimalCommaSeparator);
                    o.MaxCases(1);
                }).Generate("""{"amount":"1200.50"}"""));

            Assert.Null(ex);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", previous);
        }
    }

    [Fact]
    public void Shrink_ProducesUniqueNames()
    {
        var json = """{"a":"one","b":"two","c":"three","d":"four","active":true}""";
        var gremlin = GremlinEngine.Create(o =>
        {
            o.WithSeed(20);
            o.UseProfile(GremlinProfile.Aggressive);
            o.MaxCases(30);
        });

        var result = gremlin.Generate(json);
        var multi = result.Cases.First(c => c.Mutations.Count >= 2);
        var shrunk = gremlin.Shrink(multi);

        Assert.Equal(shrunk.Select(s => s.Name).Distinct().Count(), shrunk.Count);
    }
}
