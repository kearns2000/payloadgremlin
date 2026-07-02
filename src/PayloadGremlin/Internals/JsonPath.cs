namespace PayloadGremlin.Internals;

/// <summary>JSON Path parsing and matching utilities.</summary>
internal static class JsonPath
{
    public static string Format(IReadOnlyList<string> segments)
    {
        if (segments.Count == 0)
        {
            return "$";
        }

        return "$." + string.Join('.', segments);
    }

    public static IReadOnlyList<string> Parse(string path)
    {
        var normalized = path.StartsWith('$') ? path : "$." + path;
        if (normalized == "$")
        {
            return [];
        }

        var raw = normalized[2..]; // skip "$."
        if (string.IsNullOrEmpty(raw))
        {
            return [];
        }

        return raw.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static bool Matches(string path, string pattern)
    {
        var pathSegments = Parse(path);
        var patternSegments = Parse(pattern);
        return pathSegments.SequenceEqual(patternSegments, StringComparer.Ordinal);
    }

    public static bool IsExcluded(string path, IReadOnlySet<string> excludedPaths)
    {
        foreach (var excluded in excludedPaths)
        {
            if (Matches(path, excluded) || IsPrefixMatch(path, excluded) || IsPrefixMatch(excluded, path))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPrefixMatch(string path, string prefix)
    {
        var pathSegments = Parse(path);
        var prefixSegments = Parse(prefix);
        if (prefixSegments.Count > pathSegments.Count)
        {
            return false;
        }

        for (var i = 0; i < prefixSegments.Count; i++)
        {
            if (!string.Equals(pathSegments[i], prefixSegments[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static string ParentPath(string path)
    {
        var segments = Parse(path);
        if (segments.Count <= 1)
        {
            return "$";
        }

        return Format(segments.Take(segments.Count - 1).ToList());
    }

    public static string? LeafName(string path)
    {
        var segments = Parse(path);
        return segments.Count == 0 ? null : segments[^1];
    }

    /// <summary>True when paths are equal or one is an ancestor of the other.</summary>
    public static bool IsAncestorOrDescendant(string pathA, string pathB)
    {
        if (string.Equals(pathA, pathB, StringComparison.Ordinal))
        {
            return true;
        }

        return IsPrefixMatch(pathA, pathB) || IsPrefixMatch(pathB, pathA);
    }
}
