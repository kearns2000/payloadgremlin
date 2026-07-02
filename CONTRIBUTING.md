# Contributing to PayloadGremlin

Thanks for your interest in contributing. PayloadGremlin is a small, focused library — contributions that improve realistic JSON mutation testing are welcome.

## Before you start

- Search [existing issues](https://github.com/kearns2000/payloadgremlin/issues) to avoid duplicate work.
- For large changes (new profiles, API changes, architecture), open an issue first to discuss approach.
- Keep pull requests focused. One feature or fix per PR is easier to review.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Any editor (VS, VS Code, Rider)

## Getting started

```bash
git clone https://github.com/kearns2000/payloadgremlin.git
cd PayloadGremlin
dotnet build
dotnet test
```

Run the sample:

```bash
dotnet run --project samples/PayloadGremlin.Sample
```

## Project layout

```text
src/PayloadGremlin/           # Library
  Mutations/                  # One class per mutation type (or family)
  Internals/                  # Planner, executor, JSON tree, etc.
tests/PayloadGremlin.Tests/   # xUnit tests
samples/PayloadGremlin.Sample/
```

## Making changes

### Bug fixes

1. Add a failing test in `tests/PayloadGremlin.Tests/` that reproduces the bug.
2. Fix the issue in `src/PayloadGremlin/`.
3. Ensure `dotnet test -c Release` passes.

### New mutation types

1. Add a value to `MutationType` in `src/PayloadGremlin/MutationType.cs`.
2. Implement `IJsonMutation` (usually extend `PathMutationBase`) in `src/PayloadGremlin/Mutations/`.
3. Register the mutation in `src/PayloadGremlin/Internals/MutationRegistry.cs`.
4. Add to relevant profiles in `src/PayloadGremlin/Internals/ProfileMutations.cs` if appropriate.
5. Add tests covering `CanApply`, metadata, and at least one generated case.
6. If the mutation uses a variant (like `DateFormatChange`), override `SignatureKey` so planner deduplication works correctly.

### New profiles

1. Add enum value to `GremlinProfile`.
2. Define mutation set in `ProfileMutations.cs`.
3. Add a test in `ProfileTests.cs` or `RegressionTests.cs`.

### Public API changes

- Keep the public surface small.
- Update `README.md` for any user-visible API change.
- Avoid breaking changes in patch/minor releases without discussion.

## Code guidelines

- Use nullable reference types; avoid suppressing null warnings without reason.
- Prefer deterministic behaviour — use the seeded `DeterministicRandom`, not uncontrolled `Random`.
- New mutations should produce useful `AppliedMutation` metadata (path, before/after, description).
- Do not mutate every field in one case by default; planner targets one or two mutations per case.
- Match existing naming and file structure.

## Testing expectations

All PRs should pass:

```bash
dotnet build -c Release
dotnet test -c Release
```

Add tests when you:

- Fix a bug
- Add a mutation or profile
- Change planner/executor behaviour
- Touch path configuration or shrinking

Use nested JSON in regression tests when changes affect tree walking or pairing.

## Pull request checklist

- [ ] `dotnet build -c Release` succeeds
- [ ] `dotnet test -c Release` passes
- [ ] Tests added or updated for the change
- [ ] README updated if public API or behaviour changed
- [ ] No unrelated formatting or drive-by refactors

## Code of conduct

This project follows the [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold it.

## Questions

Open a [GitHub issue](https://github.com/kearns2000/payloadgremlin/issues) for questions or ideas.
