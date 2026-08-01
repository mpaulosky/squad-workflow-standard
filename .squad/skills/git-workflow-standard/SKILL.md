---
<<<<<<< HEAD
name: "git-workflow-standard"
description: 'Canonical issue-to-branch/worktree-to-PR git + gh workflow with hard gates. WHEN: "standard branch flow", "worktree flow", "open PR to dev". INVOKES: git checkout/switch/worktree, gh pr create, cleanup scripts.'
domain: "version-control"
confidence: "high"
source: "team-decision"
=======
name: git-workflow-standard
description: "Standard branch flow and promotion guardrails for the workflow-standard repository. WHEN: \"standard branch flow\", \"branch promotion\", \"protected branch\" INVOKES: \"git-workflow-standard\", \"git workflow standard\", \"workflow standard skill\""
>>>>>>> origin/preview
---

# Git workflow standard

<<<<<<< HEAD
## Context

Use this as the authoritative execution pattern for issue-driven work.

Source of truth:

- `.squad/workflows/git-gh-process-standard.md`
- Standard version: `2026.07.3`

## Rules

1. No direct pushes to `main`, `preview`, or `dev`.
2. Every file-producing issue change goes through PR.
3. PR review approval is mandatory before merge.
4. Required pre-push checks must pass before push.
5. Cleanup is mandatory after merge.
6. Feature/work branches PR to `dev`; `dev` promotes to `preview` via the sanctioned promotion branch; `preview` opens PRs to `main`.
7. After pushing a work branch, immediately open/update PR to `dev`.
8. Do not auto-open `dev` -> `preview` after routine work pushes; promotion PRs are separate.
9. Back-merge sync from `main` to `dev` must be handled by `squad-main-to-dev-backmerge.yml` (create/reuse PR, no-op when in sync).
10. Back-merge PRs from `main` to `dev` must not modify `.squad/`; enforce with `squad-main-to-dev-backmerge-guard.yml`.
11. GitHub protections/rulesets must enforce the same model:

- `dev`, `preview`, and `main` require PRs + checks + approvals
- `preview` accepts PRs from `automation/promote-preview` only (plus required `squad-preview-guard`)
- `main` accepts PRs from `preview` only, with any explicit `hotfix/*` exception guarded by `squad-main-guard`
- `dev` requires `squad-main-to-dev-backmerge-guard` to block `.squad/` mutations in `main` -> `dev` sync PRs

## Flow Selection

- Single issue / one-off: standard branch flow.
- 2+ concurrent issues: worktree flow.

## Standard Flow

```bash
git checkout dev
git pull origin dev
git checkout -b squad/{issue-number}-{kebab-slug}
git push -u origin squad/{issue-number}-{kebab-slug}
gh pr create --base dev --title "{title}" --body "Closes #{issue-number}" --draft
```

## Worktree Flow

```bash
git fetch origin dev
git worktree add ../{repo-name}-{issue-number} \
  -b squad/{issue-number}-{kebab-slug} \
  origin/dev
cd ../{repo-name}-{issue-number}
git push -u origin squad/{issue-number}-{kebab-slug}
gh pr create --base dev --title "{title}" --body "Closes #{issue-number}" --draft
```

Do not auto-open `dev` -> `preview` from this step; preview/main promotion PRs remain separate.

## Cleanup

Standard:

```bash
# Dry-run (recommended first)
bash scripts/squad/cleanup-squad-branches.sh --repo {owner/repo}

# Apply local + remote cleanup
bash scripts/squad/cleanup-squad-branches.sh --repo {owner/repo} --apply --delete-remote
```

Worktree:

```bash
# Dry-run (recommended first)
bash scripts/squad/cleanup-squad-branches.sh --repo {owner/repo}

# Apply local + remote cleanup (including linked worktree cleanup)
bash scripts/squad/cleanup-squad-branches.sh --repo {owner/repo} --apply --delete-remote
```
=======
Follow the standard branch flow and protected-branch promotion rules.
>>>>>>> origin/preview
