# Release Process — IssueTrackerApp Project Playbook

> ⚠️ **LEGACY REFERENCE:** This playbook documents the release process for the **IssueTrackerApp** repository (external project). It references workflows (`.github/workflows/squad-release.yml`, `squad-promote.yml`) that do **not exist** in the **MyBlog** repository. Use this as a **template/example** for establishing a release workflow in MyBlog when needed. Do not follow these exact commands or workflows in the MyBlog repo.

**Last Updated:** 2026-04-13  
**Original Project:** IssueTrackerApp  
**Owner (original):** Boromir (DevOps) + Aragorn (Release Approval)  
**Status in MyBlog:** Reference/Template Only

---

## Project Configuration

### Repository & Branches

| Parameter          | Value           | Notes                                                          |
| ------------------ | --------------- | -------------------------------------------------------------- |
| **Owner**          | mpaulosky       |                                                                |
| **Repo**           | IssueTrackerApp | Single-owner fork (no upstream)                                |
| **Dev Branch**     | dev             | Integration branch — all squad PRs target dev (squash merge)   |
| **Release Branch** | main            | Stable release branch — dev promotes to main via squad-promote |
| **Default Branch** | main            | Protected; receives merges from dev only                       |

**Decision:** IssueTrackerApp uses a **two-branch model** (dev + main). Squad branches (`squad/{issue}-{slug}`) target dev via squash merge. Promotion from dev → main uses the `squad-promote.yml` workflow with merge commits to preserve history.

### Version Management

| Parameter          | Value                             | Notes                                                   |
| ------------------ | --------------------------------- | ------------------------------------------------------- |
| **Version System** | GitVersion                        | Configured in `GitVersion.yml`                          |
| **Version File**   | `GitVersion.yml`                  | At repo root                                            |
| **Tag Prefix**     | `v` (lowercase only)              | e.g., `v1.0.0` — GitVersion accepts `[vV]` but release workflow triggers only on `v*.*.*` |
| **Package ID**     | IssueTrackerApp                   | From `.csproj`                                          |
| **Merge Strategy** | squash (to dev), merge (dev→main) | Squash for feature work, merge commit for promotion     |

**GitVersion.yml reference:** See [`GitVersion.yml`](../../GitVersion.yml) at repo root for the full, authoritative config. Key settings: `mode: ContinuousDelivery`, `tag-prefix: '[vV]'`, branches: `main` (Patch), `dev` (alpha, Minor), `feature/squad` (Inherit).

### Artifacts & Deployments

| Artifact               | Triggered By      | Produced By                               | Deployed To        |
| ---------------------- | ----------------- | ----------------------------------------- | ------------------ |
| **Build Verification** | release published | `.github/workflows/build.yml`             | (logs only)        |
| **Unit Tests**         | release published | `.github/workflows/build.yml`             | (logs only)        |
| **Integration Tests**  | release published | `.github/workflows/integration-tests.yml` | (logs only)        |
| **Docker Image**       | TBD               | (not yet configured)                      | (not yet deployed) |
| **Documentation**      | TBD               | (not yet configured)                      | (not yet deployed) |
| **NuGet Package**      | TBD               | (not yet configured)                      | (not yet deployed) |

**Status:** Minimal release pipeline. Extend as needed.

---

## Step-by-Step Release Process (IssueTrackerApp)

### Prerequisites

- [ ] All feature PRs merged to `dev` (two-branch model)
- [ ] `dev` branch CI passing (build + tests green)
- [ ] Dev promoted to `main` via squad-promote workflow
- [ ] `main` branch CI passing after promotion
- [ ] No unmerged feature branches
- [ ] Release notes prepared (in PR body or CHANGELOG.md)

### Phase 1 — Version Verification

GitVersion auto-computes versions from branch and tags. Verify the computed version:

```bash
# Check GitVersion output
dotnet tool run dotnet-gitversion | grep -E '"(SemVer|FullSemVer|MajorMinorPatch)"'

# Or use gittools/actions in CI — version is computed automatically
```

**Note:** No manual version file edits needed. GitVersion derives the version from git history, branch names, and tags.

### Phase 2 — Create Release PR

Promote `dev` to `main` using the squad-promote workflow:

```bash
# Option A: Trigger via GitHub Actions
gh workflow run squad-promote.yml

# Option B: Manual merge (merge commit, not squash)
git checkout main
git pull origin main
git merge --no-ff dev -m "chore: promote dev to main for release"
git push origin main
```

### Phase 3 — Tag and Release

After main is current and CI passes:

```bash
# Tag the release
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0

# Create GitHub Release (triggers CI/CD)
gh release create v1.0.0 \
  --repo mpaulosky/IssueTrackerApp \
  --title "v1.0.0" \
  --notes "Release v1.0.0

## What's Included
- Issue CRUD with Labels, Priorities, Due Dates
- Comment Threading
- Bulk Operations (Edit, Delete)
- User Dashboard
- Admin Panel (Categories, Statuses, Users, Audit Log)
- Email Notifications (SendGrid/SMTP)
- Dark Mode + Color Themes
- Auth0 RBAC
- Redis Caching
- Real-time Updates (SignalR)

## Breaking Changes
None

## Bug Fixes
- [#123] Fixed comment edit not reflecting immediately
- [#124] Resolved empty search result display

## Contributors
- Matthew Paulosky" \
  --target main
```

### Phase 4 — Verify CI/CD Pipeline

Visit <https://github.com/mpaulosky/IssueTrackerApp/releases/tag/v1.0.0> and confirm:

- ✅ **build.yml** job passed (Build + Unit Tests)
- ✅ **integration-tests.yml** job passed (Playwright E2E)
- ✅ No workflow failures

**If any job fails:**

```bash
# Delete tag and release
git tag -d v1.0.0
git push origin :v1.0.0
gh release delete v1.0.0 --confirm

# Fix the issue on main
git commit -m "Fix: [issue]"
git push origin main

# Retry release
# Repeat Phase 3
```

### Phase 5 — Post-Release

```bash
# Sync local main
git fetch origin
git checkout main
git reset --hard origin/main

# Verify GitVersion computes next dev version
dotnet tool run dotnet-gitversion | grep '"SemVer"'

# Document in CHANGELOG.md (optional)
echo "## v1.0.0 ($(date +%Y-%m-%d))" >> CHANGELOG.md
echo "" >> CHANGELOG.md
echo "- Issue CRUD with Labels, Priorities, Due Dates" >> CHANGELOG.md
git add CHANGELOG.md
git commit -m "docs: Update CHANGELOG for v1.0.0"
git push origin main
```

---

## Common Issues (IssueTrackerApp-Specific)

### Issue: Build Fails on Release Tag

**Symptom:** `v1.0.0` tag created, but build workflow fails

**Root Cause:** GitVersion configuration mismatch or tag prefix issue

**Fix:**

```bash
# Verify GitVersion.yml exists at repo root
ls -la GitVersion.yml

# Check tag prefix matches GitVersion config (v or V)
git tag -l 'v*' | head -5

# Verify GitVersion can compute version from current state
dotnet tool run dotnet-gitversion
```

### Issue: Integration Tests Timeout on Release

**Symptom:** `.github/workflows/integration-tests.yml` times out after 15 minutes

**Root Cause:** Playwright E2E test is slow; needs optimization or longer timeout

**Fix:** Contact Pippin (Tester E2E). May need to:

- Increase GitHub Actions timeout
- Skip E2E on release tags (if desired)
- Parallelize E2E tests

### Issue: Docker Image Not Built

**Symptom:** Release created but no Docker image attached

**Root Cause:** Docker workflow not configured for IssueTrackerApp; Dockerfile may not exist

**Fix:** Boromir to configure `.github/workflows/publish-container.yml` when Docker deployment is ready.

---

## Secrets & Permissions

| Secret                     | Used By               | Type     | Status           |
| -------------------------- | --------------------- | -------- | ---------------- |
| `GITHUB_TOKEN`             | CI/CD (auto-provided) | Built-in | ✅ Active         |
| `NUGET_API_KEY`            | (not used yet)        | Manual   | ⏸️ Not configured |
| `AZURE_WEBAPP_WEBHOOK_URL` | (not used yet)        | Manual   | ⏸️ Not configured |

**To Deploy Docker or NuGet Packages:**

1. Contact Boromir (DevOps)
2. Configure secrets in GitHub
3. Update release workflow to include new jobs

---

## Future Extensions

- [ ] **Docker Image Publishing:** Add `publish-container.yml` when container deployment is needed
- [ ] **NuGet Package Publishing:** Add `publish-nuget.yml` + configure `NUGET_API_KEY` secret
- [ ] **Documentation Deployment:** Add `docs.yml` when GitHub Pages docs site is ready
- [ ] **Release Branches:** Consider `release/*` branches for hotfix isolation if needed
- [ ] **Automated Release Notes:** Script CHANGELOG.md generation from PR titles

---

## Reference

- **GitVersion config:** `GitVersion.yml`
- **Release workflow:** `.github/workflows/squad-release.yml`
- **Promote workflow:** `.github/workflows/squad-promote.yml`
- **CI workflow:** `.github/workflows/squad-ci.yml`
- **GitHub Docs:** <https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository>

**Owner for Updates:** Aragorn (Lead) + Boromir (DevOps)  
**Last Reviewed:** 2026-04-12
