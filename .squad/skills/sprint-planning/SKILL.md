---
name: "sprint-planning"
description: "Convert a plan.md into GitHub milestones, sprint issues, a project board, and worktrees for isolated sprint execution"
domain: "planning, sprint, github-projects, worktrees"
confidence: "high"
source: "manual"
tools:
  - name: "gh api"
    description: "Create milestones and update project board items via GitHub REST/GraphQL"
    when: "Creating milestones or moving issues between project columns"
  - name: "gh issue create"
    description: "Create GitHub issues for each sprint todo"
    when: "Aragorn creates sprint issues from plan.md todos"
  - name: "gh project"
    description: "Manage GitHub Projects v2 — add/update items, move columns"
    when: "Adding sprint issues to the MyBlog project board"
  - name: "git worktree"
    description: "Manage isolated sprint working directories"
    when: "Boromir sets up or tears down sprint worktrees"
---

## Context

This skill applies whenever a `plan.md` is created or materially updated. Sprint
Planning is **mandatory for ALL plans** — see `.squad/ceremonies.md` (Sprint
Planning ceremony) for the trigger and participants.

**Agents involved:**

- **Ralph** — decomposes `plan.md` into logical sprints
- **Aragorn** — creates GH milestones, issues, and project assignments
- **Boromir** — creates sprint branches and worktrees

**Full process:** `.squad/playbooks/sprint-planning.md`

---

## Patterns

### Decomposition (Ralph)

- Read `plan.md` and the SQL `todos` table
- Group todos by dependency, theme, or size into sprints
- Target **3–6 issues per sprint**
- Earlier dependencies → earlier sprints
- Document sprint groupings before Aragorn creates GH artifacts

### Milestone Creation (Aragorn)

```bash
gh api repos/mpaulosky/MyBlog/milestones \
  -f title="Sprint N: {Theme}" \
  -f description="{sprint goal}" \
  -f due_on="{YYYY-MM-DDT00:00:00Z}" \
  -f state="open"
```

Naming: `Sprint N: {Theme}` (e.g., `Sprint 1: Foundation`)

### Issue Creation (Aragorn)

```bash
gh issue create \
  --title "[Sprint N] {Verb} {Noun}" \
  --milestone "Sprint N: {Theme}" \
  --label "squad" \
  --body "## Goal
{description from plan.md}

## Acceptance Criteria
- [ ] {criteria}

## Sprint
Sprint N: {Theme}

## Plan Reference
Todo: {todo-id}"
```

Triage immediately: remove `squad` label, add `squad:{member}` label.

### Project Board (Aragorn)

```bash
# List projects to find the MyBlog project number
gh project list --owner mpaulosky

# Add an issue to the project
gh project item-add 4 --owner mpaulosky --url {issue-url}
```

New items land in **Backlog**. Move to **In Sprint** when sprint starts.

### Worktree Setup (Boromir)

```bash
git checkout dev && git pull origin dev
git checkout -b sprint/{N}-{slug}
git push -u origin sprint/{N}-{slug}
git checkout dev
git worktree add ../MyBlog-sprint-{N} sprint/{N}-{slug}
```

### Sprint Work (all squad members)

```bash
cd ../MyBlog-sprint-{N}           # enter sprint worktree
git checkout -b squad/{issue}-{slug}   # feature branch from sprint branch
# ... do work ...
gh pr create --base sprint/{N}-{slug}  # PR targets sprint branch, NOT dev
```

### Sprint Close (Ralph → Aragorn → Boromir)

```bash
# 1. Ralph verifies 100% milestone completion
gh api repos/mpaulosky/MyBlog/milestones/{N} | jq '{title,open_issues,closed_issues}'

# 2. Aragorn opens sprint → dev PR (standard PR review applies)
gh pr create --base dev --head sprint/{N}-{slug}

# 3. After merge, close milestone
gh api -X PATCH repos/mpaulosky/MyBlog/milestones/{N} -f state="closed"

# 4. Boromir removes worktree
git worktree remove ../MyBlog-sprint-{N}
git push origin --delete sprint/{N}-{slug}
git branch -d sprint/{N}-{slug}
```

---

## Anti-Patterns

- ❌ **Skipping sprint planning** when a plan is created — it is mandatory
- ❌ **`squad/{issue}` PRs targeting `dev`** during a sprint — must target the sprint branch
- ❌ **Starting sprint N+1** before sprint N's PR merges to `dev`
- ❌ **Deleting worktree** before sprint PR merges
- ❌ **Releasing from a sprint branch** — only release from `dev` → `main`
- ❌ **Skipping worktree** — always work inside `../MyBlog-sprint-{N}/` for isolation
