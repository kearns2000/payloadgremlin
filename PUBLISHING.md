# Publishing to NuGet

PayloadGremlin uses [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) from GitHub Actions. No long-lived API keys are stored in the repo.

## One-time setup (maintainers)

### 1. Create a trusted publishing policy on nuget.org

1. Sign in at [nuget.org](https://www.nuget.org)
2. Click your username → **Trusted Publishing**
3. **Add new policy** with:

| Field | Value |
|-------|-------|
| Policy name | `payloadgremlin` (or any label) |
| Package owner | Your nuget.org account |
| Repository owner | `kearns2000` |
| Repository | `payloadgremlin` |
| Workflow file | `publish.yml` |
| Environment | *(leave empty)* |

The workflow file must be exactly `publish.yml` — not the full path.

Docs: [Trusted Publishing on Microsoft Learn](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)

### 2. Add a GitHub repository secret

**Settings → Secrets and variables → Actions → New repository secret**

| Name | Value |
|------|-------|
| `NUGET_USER` | Your **nuget.org username** (profile name, not your email) |

Trusted publishing still needs your NuGet username for the login step; the temporary API key comes from OIDC.

### 3. Publish the first version

1. Ensure `Version` in `src/PayloadGremlin/PayloadGremlin.csproj` is correct (e.g. `1.0.0`)
2. Commit and push to `main`
3. Create and push a version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

4. Watch **Actions → Publish** on GitHub
5. After validation, the package appears at https://www.nuget.org/packages/PayloadGremlin

## Releasing a new version

1. Bump `<Version>` in `src/PayloadGremlin/PayloadGremlin.csproj`
2. Commit, push to `main`
3. Tag and push: `git tag v1.0.1 && git push origin v1.0.1`

Each tag triggers build → test → pack → trusted publish.

## Notes

- Tags must match `v*` (e.g. `v1.0.0`, `v1.2.3`)
- NuGet does not allow republishing the same version — bump the version for every release
- The temporary API key from `NuGet/login@v1` expires in about an hour; push immediately after login
- Do **not** store a `NUGET_API_KEY` secret — trusted publishing replaces that
