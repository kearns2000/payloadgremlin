namespace PayloadGremlin.Tests;

public class MutationMetadataTests
{
    [Fact]
    public void EachMutationIncludesRequiredMetadata()
    {
        var json = """{"customerId":"123","status":"Active","startDate":"2026-01-01"}""";
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(7);
            o.UseProfile(GremlinProfile.Aggressive);
            o.MaxCases(5);
        }).Generate(json);

        Assert.NotEmpty(result.Cases);

        foreach (var gremlinCase in result.Cases)
        {
            Assert.Equal(7, gremlinCase.Seed);
            Assert.False(string.IsNullOrWhiteSpace(gremlinCase.Name));

            foreach (var mutation in gremlinCase.Mutations)
            {
                Assert.False(string.IsNullOrWhiteSpace(mutation.MutationId));
                Assert.False(string.IsNullOrWhiteSpace(mutation.JsonPath));
                Assert.False(string.IsNullOrWhiteSpace(mutation.Description));
                Assert.Contains("mg-7-", mutation.MutationId);
            }
        }
    }

    [Fact]
    public void MutationRecordsBeforeAndAfterValues()
    {
        var json = """{"amount":1200.50}""";
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(5);
            o.Enable(MutationType.NumberToString);
            o.MaxCases(1);
        }).Generate(json);

        var mutation = result.Cases[0].Mutations[0];
        Assert.NotNull(mutation.BeforeValue);
        Assert.NotNull(mutation.AfterValue);
        Assert.NotEqual(mutation.BeforeValue, mutation.AfterValue);
    }
}
