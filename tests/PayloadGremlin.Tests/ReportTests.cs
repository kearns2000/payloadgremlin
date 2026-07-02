namespace PayloadGremlin.Tests;

public class ReportTests
{
    [Fact]
    public void Report_IncludesSummarySections()
    {
        var json = """{"id":"1","amount":10.5,"date":"2026-01-01"}""";
        var result = GremlinEngine.Create(o =>
        {
            o.WithSeed(99);
            o.UseProfile(GremlinProfile.RealisticApiDrift);
            o.MaxCases(8);
        }).Generate(json);

        var markdown = result.Report.ToMarkdown();

        Assert.Contains("Total cases", markdown);
        Assert.Contains("Mutation types used", markdown);
        Assert.Contains("JSON paths touched", markdown);
        Assert.Contains("JSON paths never touched", markdown);
        Assert.Contains("Reproduction seed", markdown);
        Assert.Contains("99", markdown);
        Assert.Equal(8, result.Report.TotalCases);
        Assert.Equal(99, result.Report.Seed);
    }
}
