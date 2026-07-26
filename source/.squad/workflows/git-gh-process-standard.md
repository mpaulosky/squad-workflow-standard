# Git + GH Process Standard

Standard-Version: 2026.07.3
Owner: Squad process governance

## Purpose

Define one canonical process for issue-driven squad work, including branch/worktree
selection, PR flow, cleanup, and hard gates.

## Non-negotiables

1. No direct pushes to `main`, `preview`, or `dev`.
2. Every file-producing issue change must go through a PR.
3. PR review is mandatory before merge.
4. Pre-push checks are mandatory.
5. Cleanup is mandatory after merge (branch deletion, plus worktree cleanup if used).

## Branch model (default)

- Integration branch: `dev`
- Release branch: `preview`
- Promotion branch: `main`
- Preview promotion branch: `automation/promote-preview`
- Issue branch: `squad/{issue-number}-{kebab-slug}`
- Feature/work branch PR target: `dev`
- Preview PR target: `preview`
- Preview PRs open from a sanitized branch derived from `preview`
- Release PR target: `main`
- Release PRs to `main` open from `preview`
- After each successful work-branch push, immediately open/update a PR to `dev`
- Do not auto-open `dev` -> `preview` after routine work pushes; promotion PRs are separate
- Preview-side automation must open or reuse an `automation/promote-preview` -> `preview` PR instead of pushing directly to `preview`
- Main-side automation must open or reuse a `preview` -> `main` PR instead of pushing directly to `main`
- Back-merge sync (`main` -> `dev`) is automated via `squad-main-to-dev-backmerge.yml`:
  - Triggered on push to `main` (and manual dispatch)
  - Opens a `main` -> `dev` PR only when `main` is ahead
  - Reuses existing open back-merge PR (no duplicates)
  - No-ops when `main` and `dev` are already in sync

## GitHub enforcement baseline (branch protection / rulesets)

To enforce this branch model in GitHub, configure protections/rulesets for
`dev`, `preview`, and `main`:

1. Protect `dev`, `preview`, and `main` from direct pushes.
2. Require pull requests before merge on all three branches.
3. Require at least one approval before merge on all three branches.
4. Require status checks before merge (at minimum: CI/test workflows used in the
   repo).
5. Restrict `preview` merge source to the sanctioned promotion branch:
   - Keep `squad-preview-guard.yml` enabled and required for `preview`.
   - In rulesets, limit allowed source branch pattern for `preview` to
     `automation/promote-preview` when your ruleset plan supports source-branch
     restrictions.
6. Restrict `main` merge source to `preview` for release PRs:
   - Keep `squad-main-guard.yml` enabled and required for `main`.
   - In rulesets, limit allowed source branch pattern for `main` to `preview`
     when your ruleset plan supports source-branch restrictions.
   - If the repo uses emergency hotfix branches, document the explicit
     `hotfix/*` exception alongside that ruleset.
7. Ensure issue/work branches merge into `dev`, not `preview` or `main`:
   - Set repository defaults/templates (`gh pr create --base dev`) and
     contributor guidance accordingly.
   - Optionally restrict direct branch creation in UI to preserve
     `squad/{issue-number}-{kebab-slug}` convention.

## Flow selection

- **Single issue / one-off:** standard branch flow.
- **Two or more concurrent issues:** worktree flow (one worktree per issue).

## Standard flow

```bash
git checkout dev
git pull origin dev
git checkout -b squad/{issue-number}-{kebab-slug}
git push -u origin squad/{issue-number}-{kebab-slug}
```

Immediately after push, open draft PR to `dev`:

```bash
gh pr create --base dev --title "{title}" --body "Closes #{issue-number}" --draft
```

Mark ready after checks pass:

```bash
gh pr ready
```

Cleanup after merge:

```bash
git checkout dev
git pull origin dev
git branch -d squad/{issue-number}-{kebab-slug}
git push origin --delete squad/{issue-number}-{kebab-slug}
```

If the repo uses an orphan state branch, remove it after its state has been
promoted or archived elsewhere:

```bash
git checkout dev
git pull origin dev
git branch -D {state-branch}
git push origin --delete {state-branch}
```

## Worktree flow (parallel issues)

From the primary clone:

```bash
git fetch origin dev
git worktree add ../{repo-name}-{issue-number} \
  -b squad/{issue-number}-{kebab-slug} \
  origin/dev
```

Inside the worktree:

```bash
cd ../{repo-name}-{issue-number}
git push -u origin squad/{issue-number}-{kebab-slug}
gh pr create --base dev --title "{title}" --body "Closes #{issue-number}" --draft
```

Do not auto-open `dev` -> `preview` from this step; promotion stays on the
separate sanitized `automation/promote-preview` -> `preview` path, followed by
the `preview` -> `main` release PR path.

Cleanup after merge:

```bash
git worktree remove ../{repo-name}-{issue-number}
git worktree prune
git branch -d squad/{issue-number}-{kebab-slug}
git push origin --delete squad/{issue-number}-{kebab-slug}
```

## Required pre-push behavior

Run required tests/validation configured by the repo before push.
If any gate fails, fix and rerun before pushing.

## Required hook activation

- Repositories must set `git config core.hooksPath .github/hooks`.
- Required hooks are `pre-commit`, `pre-push`, and `post-checkout`.
- Hook files in `.github/hooks/` must remain executable.

## Phased rollout plan (mandatory adoption)

### Phase 0: Preparation

1. Confirm canonical source files are current in ArticlesSite.
2. Record current state in each pilot repo:
   - `scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo`
3. Announce pilot start date, owner, and freeze window for process changes.

### Phase 1: Pilot scope

Pilot must run in exactly two repos:

1. `ArticlesSite`
2. One high-activity squad repo (highest recent issue/PR volume), referenced
   in rollout notes as `<high-activity-repo>`

Pilot duration: 10 business days of normal issue throughput.

### Phase 2: Pilot pass/fail criteria

Pilot is **pass** only if all criteria are true:

1. `check-git-gh-standard.sh` exits `0` in both pilot repos for 5 consecutive
   business days.
2. 100% of pilot issue work uses `squad/{issue-number}-{kebab-slug}` branches
   and PRs to `dev`.
3. 0 direct pushes to `main`, `preview`, or `dev` in pilot repos.
4. 0 unresolved Sev1/Sev2 incidents caused by workflow standard adoption.

Pilot is **fail** if any criterion is missed or if Sev1/Sev2 workflow breakage
remains unresolved for more than one business day.

### Phase 3: Mandatory rollout transition

If pilot passes, rollout is mandatory for all active squad repos:

1. Begin rollout within 2 business days of pilot sign-off.
2. Complete rollout across all active squad repos within 2 sprints.
3. No permanent opt-out is allowed; temporary exceptions require named owner,
   expiration date, and weekly revalidation.

### Phase 4: Rollback path

Trigger rollback on pilot fail or post-rollout Sev1/Sev2 workflow regression.

1. Pin to the last known good standard version in source control.
2. Re-sync affected repos from that version using:
   - `scripts/squad/sync-git-gh-standard.sh /absolute/path/to/target-repo`
3. Re-run:
   - `scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo`
4. Open a corrective issue before retrying rollout.

## Drift and upgrade posture

- Local enforcement must live in preserved files (`routing.md`, `ceremonies.md`,
  `templates/issue-lifecycle.md`, `.squad/skills/*`).
- On standard-version mismatch, coordinator should prompt: update now, defer, or
  view differences.

## Upgrade-safe enforcement adapters

The following files are the required enforcement adapters in each repo:

- `.squad/routing.md`
- `.squad/ceremonies.md`
- `.squad/templates/issue-lifecycle.md`
- `.squad/skills/git-workflow-standard/SKILL.md`

These adapters are user-owned and expected to persist through `squad upgrade`.
They must keep the canonical binding and hard-gate language intact.

Validation command:

```bash
scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo
```
