namespace PayloadGremlin.Tests;

public class PathConfigurationTests
{
    private const string Json = """
        {
          "customerId": "123",
          "metadata": { "traceId": "abc-123" },
          "premium": 99.99
        }
        """;

    [Fact]
    public void ExcludePath_PreventsMutationsAtPath()
    {
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(10);
            o.UseProfile(GremlinProfile.Aggressive);
            o.ExcludePath("$.metadata.traceId");
            o.MaxCases(20);
        }).Generate(Json);

        Assert.DoesNotContain(
            result.Cases.SelectMany(c => c.Mutations),
            m => m.JsonPath == "$.metadata.traceId");
    }

    [Fact]
    public void ForPath_DoNotRemove_PreventsPropertyRemoval()
    {
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(10);
            o.Enable(MutationType.RemoveProperty);
            o.ForPath("$.customerId", p => p.DoNotRemove());
            o.MaxCases(10);
        }).Generate(Json);

        Assert.DoesNotContain(
            result.Cases.SelectMany(c => c.Mutations),
            m => m.MutationType == MutationType.RemoveProperty && m.JsonPath == "$.customerId");
    }

    [Fact]
    public void ForPath_AllowNullFalse_PreventsNulling()
    {
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(10);
            o.Enable(MutationType.NullProperty);
            o.ForPath("$.customerId", p => p.AllowNull(false));
            o.MaxCases(10);
        }).Generate(Json);

        Assert.DoesNotContain(
            result.Cases.SelectMany(c => c.Mutations),
            m => m.MutationType == MutationType.NullProperty && m.JsonPath == "$.customerId");
    }
}
