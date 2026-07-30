# MyBlog Release Playbook

**Owner:** Aragorn (Lead) + Boromir (DevOps)  
**Ref:** `GitVersion.yml`, `.github/workflows/ci.yml`, `.github/workflows/hotfix-backport-reminder.yml`  
**Status:** Active, manual-first release guidance  
**Last Updated:** 2026-04-19

---

## When to use this playbook

Use this playbook when MyBlog is ready to move validated work from `dev` to
`main`, or when a `hotfix/*` branch must go straight to `main`.

## Current MyBlog release model

| Parameter | Value | Notes |
| ----------- | ------- | ------- |
| **Owner / Repo** | `mpaulosky/MyBlog` | GitHub repository |
| **Integration Branch** | `dev` | All `squad/*` PRs target `dev` |
| **Release Branch** | `main` | Release-only branch |
| **Hotfix Branches** | `hotfix/*` | Branch from `main`, then backport to `dev` |
| **Versioning** | `GitVersion.yml` | SemVer labels from branch + git history |
| **Active Workflows** | `ci.yml`, `hotfix-backport-reminder.yml` | No automated release/promote workflow today |
| **Published Artifacts** | None automated | No NuGet, Docker, docs, or deploy workflow yet |
| **GitHub Release** | Optional manual step | Useful for notes and tags; does not deploy anything |

## Guardrails

- Do **not** assume `squad-release.yml`, `squad-promote.yml`, or any publish
  workflow exists in this repo
- Do **not** promise automated deployment from a tag or GitHub Release
- Use this playbook with Aragorn leading the approval gate and Boromir verifying
  workflow and branch-state details
- Do **not** reset or fast-forward `dev` after a normal `dev` → `main` release;
  `dev` already contains the source changes. Only hotfixes merged to `main`
  need a backport to `dev`

## Standard release path (`dev` → `main`)

### 1. Verify `dev` is release-ready

Before opening a release PR, confirm:

- All intended `squad/*` work is already merged into `dev`
- The latest `dev` commit is green in GitHub Actions (`ci.yml`)
- Any release notes summary is ready for the PR body or GitHub Release notes
- No emergency hotfix backports are still missing from `dev`

### 2. Open the release PR

Create a PR from `dev` to `main`:

```bash
gh pr create \
  --base main \
  --head dev \
  --title "[RELEASE] Promote dev to main" \
  --body "## Release Checklist
- [ ] Latest dev CI is green
- [ ] Release scope reviewed
- [ ] Breaking changes documented
- [ ] Release notes drafted" 
```

### 3. Review and merge the release PR

- Aragorn reviews the scope and merge readiness
- Boromir confirms branch state and workflow health
- Wait for PR CI to pass before merging
- Merge to `main` with a squash merge unless a specific release PR needs a
  different strategy and Aragorn approves it

```bash
gh pr merge <PR-number> --squash
```

### 4. Wait for `main` CI and tag the release

After the merge, wait for the push to `main` to finish running `ci.yml`. When it
is green, tag the release commit manually:

```bash
git fetch origin
git checkout main
git reset --hard origin/main
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z
```

**Notes:**

- Use a `vX.Y.Z` tag that matches the release decision for the branch state
- The tag is bookkeeping only today; it does not trigger package publishing or
  deployment on its own

### 5. Optional: create a GitHub Release

If the team wants a release page and notes:

```bash
gh release create vX.Y.Z \
  --repo mpaulosky/MyBlog \
  --title "vX.Y.Z" \
  --notes "{manual release notes}" \
  --target main
```

This is optional and currently does **not** run additional deployment workflows.

### 6. Post-release cleanup

```bash
git fetch origin
git checkout dev
git pull origin dev
git checkout main
git pull origin main
```

Normal releases do not need a follow-up sync from `main` back into `dev`.

## Hotfix release path (`hotfix/*` → `main`)

Use this path only for urgent fixes that cannot wait for the next normal release.

1. Branch from `main`:

```bash
git checkout main
git pull origin main
git checkout -b hotfix/<slug>
```

1. Open a PR from `hotfix/<slug>` to `main`
2. Wait for CI and required review
3. Merge to `main`
4. Wait for `hotfix-backport-reminder.yml` to comment, or backport manually right
   away:

```bash
git checkout dev
git pull origin dev
git cherry-pick <hotfix-merge-commit>
git push origin dev
```

1. Tag the updated `main` commit only after `main` CI is green

## Out of scope for this playbook

These are not implemented today and should be treated as future work, not active
release promises:

- Automated release PR creation
- Automated tag creation
- Docker or NuGet publishing
- Docs deployment
- Environment promotion or production deployment

## Related assets

- `.squad/skills/release-process/SKILL.md`
- `.squad/skills/release-process-base/SKILL.md` (quarantined)
- `.squad/playbooks/pr-merge-process.md`
