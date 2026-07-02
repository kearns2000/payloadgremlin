using PayloadGremlin.Mutations;

namespace PayloadGremlin.Internals;

/// <summary>A planned mutation opportunity at a specific JSON path.</summary>
internal sealed record MutationOpportunity(
    string JsonPath,
    IJsonMutation Mutation,
    int SortKey);

/// <summary>Discovers and plans mutation opportunities deterministically.</summary>
internal sealed class MutationPlanner
{
    private readonly GremlinOptions _options;
    private readonly IReadOnlyList<IJsonMutation> _mutations;

    public MutationPlanner(GremlinOptions options)
    {
        _options = options;
        _mutations = MutationRegistry.GetEnabled(options.GetEffectiveMutations());
    }

    public IReadOnlyList<MutationOpportunity> DiscoverOpportunities(JsonTree tree)
    {
        var opportunities = new List<MutationOpportunity>();
        var sortKey = 0;

        foreach (var (path, node, parent, propertyName) in tree.Walk())
        {
            foreach (var mutation in _mutations)
            {
                var context = new MutationContext
                {
                    Options = _options,
                    JsonPath = path,
                    Node = node,
                    Parent = parent,
                    PropertyName = propertyName,
                    CaseIndex = 0,
                    MutationIndex = 0,
                    Seed = _options.Seed
                };

                if (mutation.CanApply(context))
                {
                    opportunities.Add(new MutationOpportunity(path, mutation, sortKey++));
                }
            }
        }

        return opportunities
            .OrderBy(o => o.JsonPath, StringComparer.Ordinal)
            .ThenBy(o => o.Mutation.SignatureKey, StringComparer.Ordinal)
            .ThenBy(o => o.SortKey)
            .ToList();
    }

    public IReadOnlyList<IReadOnlyList<MutationOpportunity>> PlanCases(IReadOnlyList<MutationOpportunity> opportunities)
    {
        if (opportunities.Count == 0)
        {
            return [];
        }

        var random = new DeterministicRandom(_options.Seed);
        var cases = new List<IReadOnlyList<MutationOpportunity>>();
        var usedSignatures = new HashSet<string>(StringComparer.Ordinal);
        var singleCaseLimit = _options.MaxCaseCount <= 1
            ? _options.MaxCaseCount
            : Math.Max(1, _options.MaxCaseCount / 2);

        foreach (var opportunity in opportunities)
        {
            if (cases.Count >= singleCaseLimit)
            {
                break;
            }

            var signature = OpportunitySignature(opportunity);
            if (!usedSignatures.Add(signature))
            {
                continue;
            }

            cases.Add([opportunity]);
        }

        var index = 0;
        var maxPairAttempts = Math.Max(1, opportunities.Count * opportunities.Count);
        var attempts = 0;
        while (cases.Count < _options.MaxCaseCount && opportunities.Count >= 2 && attempts < maxPairAttempts)
        {
            attempts++;
            var first = opportunities[index % opportunities.Count];
            var offset = opportunities.Count == 2 ? 1 : 1 + random.Next(opportunities.Count - 1);
            var second = opportunities[(index + offset) % opportunities.Count];
            index++;

            if (first.JsonPath == second.JsonPath)
            {
                continue;
            }

            if (JsonPath.IsAncestorOrDescendant(first.JsonPath, second.JsonPath)
                && !IsInvalidJsonPair(first, second))
            {
                continue;
            }

            var pairSignature = string.Join(";", new[] { first, second }
                .OrderBy(o => o.JsonPath, StringComparer.Ordinal)
                .ThenBy(o => o.Mutation.SignatureKey, StringComparer.Ordinal)
                .Select(OpportunitySignature));

            if (!usedSignatures.Add(pairSignature))
            {
                continue;
            }

            cases.Add([first, second]);
        }

        return cases.Take(_options.MaxCaseCount).ToList();
    }

    private static string OpportunitySignature(MutationOpportunity opportunity) =>
        $"{opportunity.JsonPath}|{opportunity.Mutation.SignatureKey}";

    private static bool IsInvalidJsonPair(MutationOpportunity a, MutationOpportunity b) =>
        IsInvalidJsonType(a.Mutation.Type) || IsInvalidJsonType(b.Mutation.Type);

    private static bool IsInvalidJsonType(MutationType type) =>
        type is MutationType.TruncatedJson or MutationType.TrailingComma or MutationType.BrokenStringQuote;
}
