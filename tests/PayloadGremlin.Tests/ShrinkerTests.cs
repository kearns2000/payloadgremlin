namespace PayloadGremlin.Tests;

public class ShrinkerTests
{
    [Fact]
    public void Shrink_ProducesSmallerCases()
    {
        var json = """
            {
              "a": "one",
              "b": "two",
              "c": "three",
              "amount": 100,
              "active": true
            }
            """;
        var gremlin = GremlinEngine.Create(o =>
        {
            o.WithSeed(20);
            o.UseProfile(GremlinProfile.Aggressive);
            o.MaxCases(30);
        });

        var result = gremlin.Generate(json);
        var multiMutationCase = result.Cases.FirstOrDefault(c => c.Mutations.Count >= 2);
        Assert.NotNull(multiMutationCase);

        var shrunk = gremlin.Shrink(multiMutationCase);
        Assert.NotEmpty(shrunk);
        Assert.All(shrunk, c => Assert.True(c.Mutations.Count < multiMutationCase.Mutations.Count));
    }

    [Fact]
    public void Shrink_SingleMutation_ReturnsEmpty()
    {
        var json = """{"id":"1"}""";
        var gremlin = GremlinEngine.Create(o =>
        {
            o.WithSeed(1);
            o.Enable(MutationType.StringEmpty);
            o.MaxCases(1);
        });

        var result = gremlin.Generate(json);
        var shrunk = gremlin.Shrink(result.Cases[0]);
        Assert.Empty(shrunk);
    }
}
