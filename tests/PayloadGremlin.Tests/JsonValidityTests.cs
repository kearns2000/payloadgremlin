using System.Text.Json;

namespace PayloadGremlin.Tests;

public class JsonValidityTests
{
    private const string Json = """{"name":"test","value":42}""";

    [Fact]
    public void Default_GeneratesValidJson()
    {
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(1);
            o.UseProfile(GremlinProfile.Aggressive);
            o.MaxCases(15);
        }).Generate(Json);

        foreach (var gremlinCase in result.Cases.Where(c => c.IsValidJson))
        {
            var ex = Record.Exception(() => JsonDocument.Parse(gremlinCase.Payload));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void AllowInvalidJson_CanProduceInvalidPayloads()
    {
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(5);
            o.AllowInvalidJson();
            o.Enable(MutationType.TruncatedJson);
            o.Enable(MutationType.TrailingComma);
            o.Enable(MutationType.BrokenStringQuote);
            o.MaxCases(3);
        }).Generate(Json);

        Assert.Contains(result.Cases, c => !c.IsValidJson);
        Assert.True(result.Report.InvalidJsonCases > 0);
    }

    [Fact]
    public void InvalidInput_ThrowsClearException()
    {
        var ex = Assert.Throws<JsonGremlinException>(() =>
            GremlinEngine.Create(o => o.MaxCases(1)).Generate("{not json"));

        Assert.Contains("not valid JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
