# Squad Workflow Standards Distribution

This directory hosts the canonical git + `gh` process standard for squad
issue work.

## Distributed Assets

- `git-gh-process-standard.md` — canonical process (dev-integration with
   PR-only promotion through preview, hard gates,
  standard-vs-worktree split)
- `workflow-baseline-manifest.txt` — canonical workflow list (maps to
  target `.github/workflows/`)
- `hook-baseline-manifest.txt` — canonical hook list (maps to
  target `.github/hooks/`)
- `.git-gh-standard-version` — version stamp written in each target repo
- `.squad/skills/git-workflow-standard/SKILL.md` — executable guidance surface
- `README.md` (this file) — bootstrap + retrofit playbook

## Bootstrap (new repo after `squad init`)

1. Initialize squad in the target repo.
2. From this source repo, install/update the standard pack:

   ```bash
   scripts/squad/sync-git-gh-standard.sh /absolute/path/to/target-repo
   ```

3. Validate required enforcement wiring:

   ```bash
   scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo
   ```

4. Resolve any failing checks before opening issue-work PRs.

Hook enforcement is part of sync/check:

- `sync-git-gh-standard.sh` sets `core.hooksPath=.github/hooks` and marks
  hook files executable.
- `check-git-gh-standard.sh` fails if hook activation or executability drifts.

## Retrofit (existing squad repo)

1. From this source repo, sync the latest standard pack:

   ```bash
   scripts/squad/sync-git-gh-standard.sh /absolute/path/to/target-repo
   ```

2. Run deterministic drift checks:

   ```bash
   scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo
   ```

3. If drift is reported, reconcile:
   - `.squad/routing.md` hard gates + flow split + canonical binding
   - `.squad/templates/issue-lifecycle.md` standard version + dev/main PR policy
   - `.squad/ceremonies.md` pre-push hard gate + versioned source-of-truth

## GitHub branch protection / ruleset migration checklist

Use this checklist in each consumer repo to align GitHub settings with the
standard (`work branches -> dev`, `dev -> preview`, `preview -> main`):

1. **Sync + validate standard assets**
   - `scripts/squad/sync-git-gh-standard.sh /absolute/path/to/target-repo`
   - `scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo`
2. **Protect `dev`, `preview`, and `main`**
   - Require pull requests before merge.
   - Block direct pushes to all three branches.
   - Require at least one approval.
3. **Require status checks on `dev`, `preview`, and `main`**
   - Mark CI/test checks as required.
   - Keep `squad-preview-guard` required for PRs into `preview`.
   - Keep `squad-main-guard` required for PRs into `main`.
4. **Enforce promotion source branch policy**
   - Allow merges to `preview` from `automation/promote-preview` only
     (ruleset source restriction where available).
   - Allow merges to `main` from `preview` only, plus any explicit
     `hotfix/*` exception you document.
   - Keep `squad-main-to-dev-backmerge` enabled to auto-open/reuse `main` -> `dev` sync PRs when `main` moves ahead.
5. **Set contributor defaults to `dev`**
   - PR templates/docs must use `--base dev`.
   - Branch naming must follow `squad/{issue-number}-{kebab-slug}`.
   - After pushing a work branch, the immediate next step is opening/updating PR to `dev`.
   - Do not auto-open a `dev -> preview` PR after routine work pushes; promotion PRs are separate.
6. **Verify with a live PR test**
   - Open `squad/* -> dev` PR (should pass policy checks).
   - Open non-`automation/promote-preview -> preview` PR (should fail policy checks).
   - Open non-`preview -> main` PR (should fail policy checks unless it is an approved `hotfix/*` exception).
7. **Lock in and monitor**
   - Add policy check to onboarding/runbooks.
   - Re-run `check-git-gh-standard.sh` after workflow/ruleset edits.

## Pilot and rollout execution playbook

### Pilot scope

- Repo 1: `ArticlesSite`
- Repo 2: one high-activity squad repo (`<high-activity-repo>`) selected by
  highest recent issue/PR volume
- Duration: 10 business days

### Pilot pass/fail gate

Pass requires all of the following:

1. Daily `check-git-gh-standard.sh` returns exit code `0` in both pilot repos
   for 5 consecutive business days.
2. All issue work uses `squad/{issue-number}-{kebab-slug}` branch naming and
   PRs targeting `dev`.
3. Promotion PRs into `preview` originate only from `automation/promote-preview`, and promotion PRs into `main` originate only from `preview` or an explicit `hotfix/*` exception.
4. No direct push events to `main`, `preview`, or `dev`.
5. No unresolved Sev1/Sev2 incidents caused by workflow adoption.

Fail on any missed criterion or unresolved Sev1/Sev2 workflow breakage longer
than one business day.

### Mandatory rollout transition

After pilot pass:

1. Start full rollout within 2 business days.
2. Roll out to all active squad repos within 2 sprints.
3. Allow only temporary exceptions with explicit owner and expiration.

### Rollback path

If pilot fails or post-rollout critical regression occurs:

1. Revert to the last known good workflow standard version in source control.
2. Re-run sync to each affected repo:

   ```bash
   scripts/squad/sync-git-gh-standard.sh /absolute/path/to/target-repo
   ```

3. Validate recovery:

   ```bash
   scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo
   ```

4. Log corrective issue and re-run pilot gate before reattempting rollout.

### Detect-and-prompt policy

When `check-git-gh-standard.sh` detects version drift, it must stop green
status and prompt the operator with three actions:

1. Update now (`sync-git-gh-standard.sh`)
2. Defer and rerun the check before the next gated issue workflow
3. View local vs canonical workflow diff

### Exit codes (automation-safe)

- `0` — Version and required enforcement adapters are in sync
- `2` — Canonical version metadata missing or unreadable
- `3` — Version drift detected
- `4` — Adapter wiring/enforcement checks failed

## Upgrade Resilience

Keep policy enforcement in preserved user-owned files:

- `.squad/routing.md`
- `.squad/ceremonies.md`
- `.squad/templates/issue-lifecycle.md`
- `.squad/skills/*`

These adapter surfaces should continue enforcing policy even when
template-managed files are refreshed by `squad upgrade`.
