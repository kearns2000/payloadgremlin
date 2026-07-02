using PayloadGremlin.Mutations;

namespace PayloadGremlin.Internals;

/// <summary>Applies planned mutations and builds test cases.</summary>
internal sealed class MutationExecutor
{
    private readonly GremlinOptions _options;
    private readonly JsonTree _originalTree;

    public MutationExecutor(GremlinOptions options, JsonTree originalTree)
    {
        _options = options;
        _originalTree = originalTree;
    }

    public GremlinCase ExecuteCase(int caseIndex, IReadOnlyList<MutationOpportunity> planned)
    {
        var tree = _originalTree.DeepClone();
        var applied = new List<AppliedMutation>();
        var mutationIndex = 0;

        var ordered = planned
            .OrderBy(o => IsInvalidJsonType(o.Mutation.Type) ? 1 : 0)
            .ThenBy(o => o.JsonPath, StringComparer.Ordinal)
            .ThenBy(o => o.Mutation.SignatureKey, StringComparer.Ordinal)
            .ToList();

        foreach (var opportunity in ordered)
        {
            if (!tree.TryGetNode(opportunity.JsonPath, out var node, out var parent, out var prop)
                && opportunity.JsonPath != "$")
            {
                continue;
            }

            var context = new MutationContext
            {
                Options = _options,
                JsonPath = opportunity.JsonPath,
                Node = node ?? tree.Root,
                Parent = parent ?? tree.Root,
                PropertyName = prop,
                CaseIndex = caseIndex,
                MutationIndex = mutationIndex,
                Seed = _options.Seed
            };

            var result = opportunity.Mutation.Apply(tree, context);
            applied.Add(result);
            mutationIndex++;
        }

        var isValidJson = applied.Count == 0 || applied.All(m => m.IsValidJson);
        var payload = isValidJson
            ? tree.ToJson()
            : applied.Last(m => !m.IsValidJson).AfterValue ?? tree.ToJson();

        var name = BuildCaseName(caseIndex, applied);

        return new GremlinCase(name, payload, applied, _options.Seed, isValidJson);
    }

    private static bool IsInvalidJsonType(MutationType type) =>
        type is MutationType.TruncatedJson or MutationType.TrailingComma or MutationType.BrokenStringQuote;

    private static string BuildCaseName(int caseIndex, IReadOnlyList<AppliedMutation> mutations)
    {
        if (mutations.Count == 0)
        {
            return $"case-{caseIndex}-unchanged";
        }

        if (mutations.Count == 1)
        {
            var m = mutations[0];
            return $"case-{caseIndex}-{m.MutationType}-at-{SanitizePath(m.JsonPath)}";
        }

        var parts = string.Join("-and-", mutations.Select(m => $"{m.MutationType}-at-{SanitizePath(m.JsonPath)}"));
        return $"case-{caseIndex}-{parts}";
    }

    private static string SanitizePath(string path) =>
        path.TrimStart('$', '.').Replace('.', '_');
}
