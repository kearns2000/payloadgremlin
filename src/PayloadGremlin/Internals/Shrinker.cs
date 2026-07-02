using PayloadGremlin.Mutations;

namespace PayloadGremlin.Internals;

/// <summary>Produces smaller failing cases by removing one mutation at a time.</summary>
internal sealed class Shrinker
{
    private readonly GremlinOptions _options;
    private readonly string _originalJson;

    public Shrinker(GremlinOptions options, string originalJson)
    {
        _options = options;
        _originalJson = originalJson;
    }

    public IReadOnlyList<GremlinCase> Shrink(GremlinCase failingCase)
    {
        if (failingCase.Mutations.Count <= 1)
        {
            return [];
        }

        var tree = JsonTree.Parse(_originalJson);
        var planner = new MutationPlanner(_options);
        var allOpportunities = planner.DiscoverOpportunities(tree);
        var executor = new MutationExecutor(_options, tree);
        var smallerCases = new List<GremlinCase>();

        for (var i = 0; i < failingCase.Mutations.Count; i++)
        {
            var remaining = failingCase.Mutations
                .Where((_, idx) => idx != i)
                .ToList();

            var planned = MapMutationsToOpportunities(remaining, allOpportunities);
            if (planned.Count == 0)
            {
                continue;
            }

            var shrunk = executor.ExecuteCase(1000 + i, planned);
            smallerCases.Add(shrunk with
            {
                Name = $"shrunk-{failingCase.Name}-without-mutation-{i}"
            });
        }

        return smallerCases;
    }

    private static IReadOnlyList<MutationOpportunity> MapMutationsToOpportunities(
        IReadOnlyList<AppliedMutation> mutations,
        IReadOnlyList<MutationOpportunity> opportunities)
    {
        var result = new List<MutationOpportunity>();

        foreach (var mutation in mutations)
        {
            var match = opportunities.FirstOrDefault(o =>
                o.JsonPath == mutation.JsonPath && o.Mutation.Type == mutation.MutationType);

            if (match is not null)
            {
                result.Add(match);
            }
        }

        return result;
    }
}
