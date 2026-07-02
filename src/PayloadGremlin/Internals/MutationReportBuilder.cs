namespace PayloadGremlin.Internals;

/// <summary>Builds summary reports from generated cases.</summary>
internal static class MutationReportBuilder
{
    public static GremlinReport Build(
        IReadOnlyList<GremlinCase> cases,
        int seed,
        IReadOnlyList<string> allPaths)
    {
        var mutationCounts = new Dictionary<MutationType, int>();
        var touchedPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var gremlinCase in cases)
        {
            foreach (var mutation in gremlinCase.Mutations)
            {
                mutationCounts.TryGetValue(mutation.MutationType, out var count);
                mutationCounts[mutation.MutationType] = count + 1;
                touchedPaths.Add(mutation.JsonPath);
            }
        }

        var neverTouched = allPaths
            .Where(p => !touchedPaths.Contains(p))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var invalidCount = cases.Count(c => !c.IsValidJson);

        return new GremlinReport(
            cases.Count,
            invalidCount,
            seed,
            mutationCounts,
            touchedPaths.OrderBy(p => p, StringComparer.Ordinal).ToList(),
            neverTouched);
    }
}
