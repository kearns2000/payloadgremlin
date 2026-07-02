using System.Text;

namespace PayloadGremlin;

/// <summary>Summary report of a generation run.</summary>
public sealed class GremlinReport
{
    internal GremlinReport(
        int totalCases,
        int invalidJsonCases,
        int seed,
        IReadOnlyDictionary<MutationType, int> mutationTypeCounts,
        IReadOnlyList<string> pathsTouched,
        IReadOnlyList<string> pathsNeverTouched)
    {
        TotalCases = totalCases;
        InvalidJsonCases = invalidJsonCases;
        Seed = seed;
        MutationTypeCounts = mutationTypeCounts;
        PathsTouched = pathsTouched;
        PathsNeverTouched = pathsNeverTouched;
    }

    public int TotalCases { get; }
    public int InvalidJsonCases { get; }
    public int Seed { get; }
    public IReadOnlyDictionary<MutationType, int> MutationTypeCounts { get; }
    public IReadOnlyList<string> PathsTouched { get; }
    public IReadOnlyList<string> PathsNeverTouched { get; }

    /// <summary>Returns a markdown summary of the generation run.</summary>
    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PayloadGremlin Report");
        sb.AppendLine();
        sb.AppendLine($"- **Total cases:** {TotalCases}");
        sb.AppendLine($"- **Invalid JSON cases:** {InvalidJsonCases}");
        sb.AppendLine($"- **Reproduction seed:** {Seed}");
        sb.AppendLine();
        sb.AppendLine("## Mutation types used");
        sb.AppendLine();
        if (MutationTypeCounts.Count == 0)
        {
            sb.AppendLine("_None_");
        }
        else
        {
            foreach (var (type, count) in MutationTypeCounts.OrderBy(kv => kv.Key.ToString()))
            {
                sb.AppendLine($"- `{type}`: {count}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## JSON paths touched");
        sb.AppendLine();
        if (PathsTouched.Count == 0)
        {
            sb.AppendLine("_None_");
        }
        else
        {
            foreach (var path in PathsTouched)
            {
                sb.AppendLine($"- `{path}`");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## JSON paths never touched");
        sb.AppendLine();
        if (PathsNeverTouched.Count == 0)
        {
            sb.AppendLine("_None_");
        }
        else
        {
            foreach (var path in PathsNeverTouched)
            {
                sb.AppendLine($"- `{path}`");
            }
        }

        return sb.ToString();
    }
}

/// <summary>Result of a <see cref="PayloadGremlin.Generate"/> call.</summary>
public sealed class GremlinResult
{
    internal GremlinResult(IReadOnlyList<GremlinCase> cases, GremlinReport report, string originalJson)
    {
        Cases = cases;
        Report = report;
        OriginalJson = originalJson;
    }

    public IReadOnlyList<GremlinCase> Cases { get; }
    public GremlinReport Report { get; }
    internal string OriginalJson { get; }
}
