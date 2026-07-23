## Squad Workflow Standards Distribution

This directory hosts the canonical git + `gh` process standard for squad
issue work.

## Distributed Assets

- `git-gh-process-standard.md` — canonical process (main-first, hard gates,
  standard-vs-worktree split)
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
   - `.squad/templates/issue-lifecycle.md` standard version + main-first policy
   - `.squad/ceremonies.md` pre-push hard gate + versioned source-of-truth

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
   PRs targeting `main`.
3. No direct push events to `main` or `dev`.
4. No unresolved Sev1/Sev2 incidents caused by workflow adoption.

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

When `check-git-gh-standard.sh` detects version drift, it must stop green status and
prompt the operator with three actions:

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
