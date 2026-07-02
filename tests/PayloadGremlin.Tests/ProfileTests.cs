namespace PayloadGremlin.Tests;

public class ProfileTests
{
    private const string Json = """
        {
          "customerId": "123",
          "premium": 1200.50,
          "active": true,
          "startDate": "2026-01-01",
          "status": "Active"
        }
        """;

    [Theory]
    [InlineData(GremlinProfile.RealisticApiDrift)]
    [InlineData(GremlinProfile.StrictClientBreaker)]
    [InlineData(GremlinProfile.LegacySystemWeirdness)]
    [InlineData(GremlinProfile.DateAndMoneyChaos)]
    [InlineData(GremlinProfile.Aggressive)]
    public void Profiles_GenerateCases(GremlinProfile profile)
    {
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(1);
            o.UseProfile(profile);
            o.MaxCases(5);
        }).Generate(Json);

        Assert.NotEmpty(result.Cases);
        Assert.True(result.Cases.Count <= 5);
    }

    [Fact]
    public void DateAndMoneyChaos_IncludesDateOrNumberMutations()
    {
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(3);
            o.UseProfile(GremlinProfile.DateAndMoneyChaos);
            o.MaxCases(15);
        }).Generate(Json);

        var types = result.Cases.SelectMany(c => c.Mutations).Select(m => m.MutationType).ToHashSet();
        Assert.True(
            types.Contains(MutationType.DateFormatChange)
            || types.Contains(MutationType.NumberToString)
            || types.Contains(MutationType.NumberZero)
            || types.Contains(MutationType.DecimalCommaSeparator));
    }
}
