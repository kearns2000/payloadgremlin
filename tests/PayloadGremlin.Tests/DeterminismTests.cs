namespace PayloadGremlin.Tests;

public class DeterminismTests
{
    private const string SampleJson = """
        {
          "customerId": "123",
          "premium": 1200.50,
          "active": true,
          "startDate": "2026-01-01"
        }
        """;

    [Fact]
    public void SameSeedProducesIdenticalCases()
    {
        var result1 = CreateGremlin(42).Generate(SampleJson);
        var result2 = CreateGremlin(42).Generate(SampleJson);

        Assert.Equal(result1.Cases.Count, result2.Cases.Count);
        for (var i = 0; i < result1.Cases.Count; i++)
        {
            Assert.Equal(result1.Cases[i].Name, result2.Cases[i].Name);
            Assert.Equal(result1.Cases[i].Payload, result2.Cases[i].Payload);
            Assert.Equal(result1.Cases[i].Mutations.Count, result2.Cases[i].Mutations.Count);
        }
    }

    [Fact]
    public void DifferentSeedsProduceDifferentCases()
    {
        var result1 = CreateGremlin(1).Generate(SampleJson);
        var result2 = CreateGremlin(999).Generate(SampleJson);

        Assert.NotEqual(
            string.Join('|', result1.Cases.Select(c => c.Payload)),
            string.Join('|', result2.Cases.Select(c => c.Payload)));
    }

    [Fact]
    public void CasesAreGeneratedInStableOrder()
    {
        var gremlin = CreateGremlin(100);
        var names1 = gremlin.Generate(SampleJson).Cases.Select(c => c.Name).ToList();
        var names2 = gremlin.Generate(SampleJson).Cases.Select(c => c.Name).ToList();

        Assert.Equal(names1, names2);
    }

    private static GremlinEngine CreateGremlin(int seed) =>
        GremlinEngine.Create(o =>
        {
            o.WithSeed(seed);
            o.UseProfile(GremlinProfile.RealisticApiDrift);
            o.MaxCases(10);
        });
}
