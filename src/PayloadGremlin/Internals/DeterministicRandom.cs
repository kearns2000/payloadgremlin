namespace PayloadGremlin.Internals;

/// <summary>Deterministic pseudo-random number generator for reproducible case selection.</summary>
internal sealed class DeterministicRandom
{
    private readonly Random _random;

    public DeterministicRandom(int seed)
    {
        _random = new Random(seed);
    }

    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public double NextDouble() => _random.NextDouble();

    public bool NextBool(double probability = 0.5) => _random.NextDouble() < probability;

    public T Pick<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Cannot pick from an empty list.");
        }

        return items[Next(items.Count)];
    }

    public void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
