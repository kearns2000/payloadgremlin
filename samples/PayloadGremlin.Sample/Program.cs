using PayloadGremlin;

var json = """
{
  "customerId": "123",
  "premium": 1200.50,
  "active": true,
  "startDate": "2026-01-01",
  "status": "Active"
}
""";

var gremlin = global::PayloadGremlin.PayloadGremlin.Create(options =>
{
    options.WithSeed(12345);
    options.UseProfile(GremlinProfile.RealisticApiDrift);
    options.MaxCases(10);
});

var result = gremlin.Generate(json);

Console.WriteLine(result.Report.ToMarkdown());
Console.WriteLine();
Console.WriteLine("Generated cases:");
Console.WriteLine(new string('-', 60));

foreach (var testCase in result.Cases)
{
    Console.WriteLine();
    Console.WriteLine($"[{testCase.Name}] valid={testCase.IsValidJson}");
    Console.WriteLine(testCase.Payload);

    foreach (var mutation in testCase.Mutations)
    {
        Console.WriteLine($"  - {mutation.MutationType} @ {mutation.JsonPath}: {mutation.Description}");
    }
}

var multiCase = result.Cases.FirstOrDefault(c => c.Mutations.Count >= 2);
if (multiCase is not null)
{
    Console.WriteLine();
    Console.WriteLine(new string('-', 60));
    Console.WriteLine($"Shrinking case: {multiCase.Name}");

    foreach (var shrunk in gremlin.Shrink(multiCase))
    {
        Console.WriteLine($"  shrunk -> {shrunk.Name} ({shrunk.Mutations.Count} mutations)");
    }
}
