---
name: "git-workflow-standard"
description: "Canonical issue-to-branch/worktree-to-PR git + gh workflow with hard gates"
domain: "version-control"
confidence: "high"
source: "team-decision"
---

## Context

Use this as the authoritative execution pattern for issue-driven work.

Source of truth:
- `.squad/workflows/git-gh-process-standard.md`
- Standard version: `2026.07.1`

## Rules

1. No direct pushes to `main` or `dev`.
2. Every file-producing issue change goes through PR.
3. PR review approval is mandatory before merge.
4. Required pre-push checks must pass before push.
5. Cleanup is mandatory after merge.

## Flow Selection

- Single issue / one-off: standard branch flow.
- 2+ concurrent issues: worktree flow.

## Standard Flow

```bash
git checkout main
git pull origin main
git checkout -b squad/{issue-number}-{kebab-slug}
git push -u origin squad/{issue-number}-{kebab-slug}
gh pr create --base main --title "{title}" --body "Closes #{issue-number}" --draft
```

## Worktree Flow

```bash
git fetch origin main
git worktree add ../{repo-name}-{issue-number} -b squad/{issue-number}-{kebab-slug} origin/main
cd ../{repo-name}-{issue-number}
git push -u origin squad/{issue-number}-{kebab-slug}
gh pr create --base main --title "{title}" --body "Closes #{issue-number}" --draft
```

## Cleanup

Standard:

```bash
git checkout main
git pull origin main
git branch -d squad/{issue-number}-{kebab-slug}
git push origin --delete squad/{issue-number}-{kebab-slug}
```

Worktree:

```bash
git worktree remove ../{repo-name}-{issue-number}
git worktree prune
git branch -d squad/{issue-number}-{kebab-slug}
git push origin --delete squad/{issue-number}-{kebab-slug}
```
