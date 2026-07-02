namespace PayloadGremlin;

/// <summary>Per-path mutation rules.</summary>
public sealed class PathOptions
{
    internal bool AllowRemoveProperty { get; private set; } = true;
    internal bool AllowNullValue { get; private set; } = true;
    internal bool AllowTypeChangeMutations { get; private set; } = true;
    internal bool AllowValueChangeMutations { get; private set; } = true;

    /// <summary>Prevents property removal at this path.</summary>
    public PathOptions DoNotRemove()
    {
        AllowRemoveProperty = false;
        return this;
    }

    /// <summary>Controls whether null can be set at this path.</summary>
    public PathOptions AllowNull(bool allow)
    {
        AllowNullValue = allow;
        return this;
    }

    /// <summary>Controls whether type-changing mutations are allowed at this path.</summary>
    public PathOptions AllowTypeChanges(bool allow)
    {
        AllowTypeChangeMutations = allow;
        return this;
    }

    /// <summary>Controls whether value-changing mutations are allowed at this path.</summary>
    public PathOptions AllowValueChanges(bool allow)
    {
        AllowValueChangeMutations = allow;
        return this;
    }
}

/// <summary>Configuration for a PayloadGremlin instance.</summary>
public sealed class GremlinOptions
{
  internal int Seed { get; private set; } = 42;
  internal int MaxCaseCount { get; private set; } = 10;
  internal GremlinProfile? Profile { get; private set; }
  internal bool InvalidJsonAllowed { get; private set; }
  internal HashSet<MutationType> EnabledMutations { get; } = [];
  internal HashSet<string> ExcludedPaths { get; } = new(StringComparer.Ordinal);
  internal Dictionary<string, PathOptions> PathRules { get; } = new(StringComparer.Ordinal);
  internal bool UseExplicitMutations { get; private set; }

  /// <summary>Sets the random seed for deterministic generation.</summary>
  public GremlinOptions WithSeed(int seed)
  {
    Seed = seed;
    return this;
  }

  /// <summary>Sets the maximum number of test cases to generate.</summary>
  public GremlinOptions MaxCases(int maxCases)
  {
    if (maxCases < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(maxCases), "MaxCases must be at least 1.");
    }

    MaxCaseCount = maxCases;
    return this;
  }

  /// <summary>Applies a named mutation profile.</summary>
  public GremlinOptions UseProfile(GremlinProfile profile)
  {
    Profile = profile;
    return this;
  }

  /// <summary>Enables a specific mutation type (overrides profile defaults for that type).</summary>
  public GremlinOptions Enable(MutationType mutationType)
  {
    UseExplicitMutations = true;
    EnabledMutations.Add(mutationType);
    return this;
  }

  /// <summary>Excludes a JSON path from mutation (e.g. <c>$.metadata.traceId</c>).</summary>
  public GremlinOptions ExcludePath(string jsonPath)
  {
    ExcludedPaths.Add(NormalizePath(jsonPath));
    return this;
  }

  /// <summary>Configures mutation rules for a specific JSON path.</summary>
  public GremlinOptions ForPath(string jsonPath, Action<PathOptions> configure)
  {
    var normalized = NormalizePath(jsonPath);
    if (!PathRules.TryGetValue(normalized, out var options))
    {
      options = new PathOptions();
      PathRules[normalized] = options;
    }

    configure(options);
    return this;
  }

  /// <summary>Allows generation of syntactically invalid JSON payloads.</summary>
  public GremlinOptions AllowInvalidJson()
  {
    InvalidJsonAllowed = true;
    return this;
  }

  internal IReadOnlySet<MutationType> GetEffectiveMutations()
  {
    if (UseExplicitMutations && EnabledMutations.Count > 0)
    {
      return EnabledMutations;
    }

    if (Profile is { } profile)
    {
      return global::PayloadGremlin.Internals.ProfileMutations.GetForProfile(profile, InvalidJsonAllowed);
    }

    return global::PayloadGremlin.Internals.ProfileMutations.GetForProfile(GremlinProfile.RealisticApiDrift, InvalidJsonAllowed);
  }

  internal static string NormalizePath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      throw new ArgumentException("JSON path cannot be empty.", nameof(path));
    }

    return path.StartsWith('$') ? path : "$." + path;
  }
}
