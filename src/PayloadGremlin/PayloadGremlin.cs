using PayloadGremlin.Internals;

namespace PayloadGremlin;

/// <summary>
/// Makes good JSON payloads misbehave on purpose — realistic API drift for testing
/// deserializers, API clients, and downstream integration code.
/// </summary>
public sealed class PayloadGremlin
{
    private readonly GremlinOptions _options;

    private PayloadGremlin(GremlinOptions options)
    {
        _options = options;
    }

    /// <summary>Creates a configured PayloadGremlin instance.</summary>
    public static PayloadGremlin Create(Action<GremlinOptions> configure)
    {
        var options = new GremlinOptions();
        configure(options);
        return new PayloadGremlin(options);
    }

    /// <summary>Generates named test cases from a known-good JSON payload.</summary>
    public GremlinResult Generate(string json)
    {
        _lastOriginalJson = json;
        var tree = JsonTree.Parse(json);
        var planner = new MutationPlanner(_options);
        var opportunities = planner.DiscoverOpportunities(tree);
        var casePlans = planner.PlanCases(opportunities);
        var executor = new MutationExecutor(_options, tree);

        var cases = new List<GremlinCase>();
        for (var i = 0; i < casePlans.Count; i++)
        {
            cases.Add(executor.ExecuteCase(i, casePlans[i]));
        }

        var allPaths = tree.GetAllValuePaths();
        var report = MutationReportBuilder.Build(cases, _options.Seed, allPaths);

        return new GremlinResult(cases, report, json);
    }

    /// <summary>
    /// Produces smaller versions of a failing case by removing one applied mutation at a time.
    /// </summary>
    public IReadOnlyList<GremlinCase> Shrink(GremlinCase failingCase)
    {
        if (_lastOriginalJson is null)
        {
            throw new InvalidOperationException(
                "Shrink requires the original JSON from a prior Generate call on this instance.");
        }

        return new Shrinker(_options, _lastOriginalJson).Shrink(failingCase);
    }

    private string? _lastOriginalJson;
}
