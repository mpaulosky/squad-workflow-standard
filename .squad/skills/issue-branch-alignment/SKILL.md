---
name: issue-branch-alignment
confidence: high
description: >
  Prevents issue work from starting on the wrong branch and documents the safe
  stash-and-rehome recovery flow when dirty changes already exist on `dev` or
  another branch.
---

## Issue Branch Alignment

### Why This Exists

Issue work must live on `squad/{issue}-{slug}` branches so commits, pushes, PRs,
and reviewer routing all line up with the owning GitHub issue. Dirty work on
`dev`, `main`, or the wrong `squad/*` branch creates avoidable recovery work and
risks mixing unrelated files into a PR.

### Start-of-Work Check

Before opening or editing files for an issue:

1. Confirm the issue exists and is sprint-stamped.
2. Confirm the active branch matches the issue number:

   ```bash
   git symbolic-ref --short HEAD
   # Must be: squad/{issue}-{slug}
   ```

3. If the branch does not match, switch before making changes.

### Recovery Flow: Dirty Work on the Wrong Branch

If issue files are already dirty on `dev`, `main`, or the wrong branch, recover
them without dragging unrelated files along:

```bash
# Example for issue 371
git stash push -u -m "issue-371-rehome" -- <issue-files>
git fetch origin
git switch -c squad/371-issue-slug origin/dev
git stash apply stash@{0}
git stash drop stash@{0}
```

Rules:

- Stash only the files that belong to the issue.
- Create the issue branch from `origin/dev` unless an existing issue branch
  already owns the work.
- After rehoming the files, return `dev` to a clean state if it is safe to do so.
- If resuming an older `squad/*` branch, also apply
  `.squad/skills/merged-pr-guard/SKILL.md` before committing.

### Outcome

This keeps PRs issue-scoped, leaves `dev` clean, and ensures the eventual PR
head branch tells reviewers exactly which issue it resolves.
