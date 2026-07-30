# Sprint 19 — Execution Rails: Markdown Editor Migration

**Owner:** Aragorn (Lead / Architect)
**Issue:** #322 — [Sprint 19] Milestone/worktree execution rails for markdown migration
**Parent PRD:** #320
**Scope Lock:** `.squad/decisions/inbox/aragorn-scope-lock-markdown-editor.md` (closes #321)
**Last Updated:** 2026-06-02

---

## Purpose

This playbook provides implementers with the explicit, dependency-aware execution sequence for
Sprint 19 markdown editor migration. It defines branch names, worktree setup, per-slice validation
gates, and merge ordering so each squad member can proceed in an isolated branch without merge
conflicts or scope drift.

---

## Dependency Graph

```text
#321 [CLOSED] Scope lock + baseline path
  │
  └─► #322 [this] Execution rails (closes now)
        │
        └─► #323  Restore compile path end-to-end
              │
              ├─► #324  Create flow markdown authoring slice
              │     │
              │     └─►  #326  Shared UX parity + editor presentation hardening
              │           │
              │           └─► #327  Test / interop / lifecycle vertical validation
              │
              └─► #325  Edit flow markdown authoring slice
                    │
                    └─►  #326  (same — #324 AND #325 must complete before #326)
```

**Rule:** No issue may begin until every issue it depends on has a **merged PR** on `dev`.
Start of implementation coding ≠ merge of the previous slice. Check `gh issue view` state.

---

## Issue → Branch Map

| Issue | Slice | Branch Name | Owner | Blocked By |
|-------|-------|-------------|-------|------------|
| #322 | Execution rails (this) | `squad/322-milestone-worktree-execution-rails` | Aragorn | #321 ✅ |
| #323 | Restore compile path | `squad/323-restore-markdown-editor-compile-path` | Sam / Legolas | #322 |
| #324 | Create flow authoring | `squad/324-create-flow-markdown-authoring` | Legolas | #323 |
| #325 | Edit flow authoring | `squad/325-edit-flow-markdown-authoring` | Legolas | #323 |
| #326 | UX parity + hardening | `squad/326-shared-ux-parity-editor-hardening` | Legolas | #324 + #325 |
| #327 | Test / interop / lifecycle | `squad/327-test-interop-lifecycle-validation` | Gimli | #324 + #325 |

> Branch naming follows the repo convention: `squad/{issue-number}-{kebab-case-slug}`.
> All branches are cut from `dev` at the time the upstream dependency merges.

---

## Worktree Setup

Each slice runs in its own git worktree so branch isolation is physical, not just logical.

### Create a worktree for a slice

```bash
# Replace {N} and {slug} with the issue number and slug from the map above
git worktree add ../MyBlog-{N} -b squad/{N}-{slug} dev
cd ../MyBlog-{N}
```

### Active worktrees for Sprint 19

| Directory | Branch | Notes |
|-----------|--------|-------|
| `/home/mpaulosky/Repos/MyBlog` | `dev` (primary) | Integration and review |
| `/home/mpaulosky/Repos/MyBlog-{N}` | `squad/{N}-{slug}` | One per active slice |

**Teardown after merge:**

```bash
cd /home/mpaulosky/Repos/MyBlog   # return to primary
git worktree remove ../MyBlog-{N} --force
git branch -d squad/{N}-{slug}
```

---

## Per-Slice Validation Expectations

Every branch must pass the full pre-push gate before PR creation. The gate is enforced by the
local pre-push hook and must not be bypassed with `--no-verify`.

### Gate checklist (all slices)

| Gate | Command | Required result |
|------|---------|-----------------|
| 0 | Branch name check (auto) | `squad/{issue}-{slug}` format passes automatically |
| 1 | Untracked source warning | No new `.razor` / `.cs` files left untracked |
| 2 | Format verify | `dotnet format --verify-no-changes` — zero violations |
| 3 | Release build | `dotnet build MyBlog.slnx --configuration Release` — zero errors, zero warnings |
| 4 | Unit / arch / bUnit tests | All pass; coverage ≥ 89% line threshold in `Unit.Tests.csproj` |
| 5 | Integration tests | All pass (Docker required for MongoDB fixture) |

### Slice-specific validation notes

#### #323 — Restore compile path

- Primary gate: **Release build passes** with markdown editor NuGet reference in place.
- Confirm `Directory.Packages.props` has the markdown editor package version pinned.
- Confirm `App.razor` script/CSS ordering is correct (no 404s in browser console).
- RTBlazorfied removal must not leave orphaned `@using` or `@inject` references.
- No page-level feature code in this slice — compile path only.

#### #324 — Create flow markdown authoring

- `Create.razor` renders markdown editor component; plain `<InputTextArea>` is removed.
- `CreateBlogPostCommand.Content` receives raw markdown string (not HTML).
- Required-content validation correctly rejects empty/whitespace-only editor value.
- Existing bUnit tests for `Create.razor` updated to account for editor component.
- No changes to `CreateBlogPostHandler`, `BlogPost` entity, or MongoDB schema.

#### #325 — Edit flow markdown authoring

- `Edit.razor` loads existing `BlogPost.Content` into markdown editor on mount.
- Existing post content round-trips through save without corruption or transformation.
- `EditBlogPostCommand.Content` receives raw markdown string.
- Authorization and concurrency-aware save path behavior is unchanged.
- Existing bUnit tests for `Edit.razor` updated for editor component.

#### #326 — Shared UX parity + hardening

- `Create.razor` and `Edit.razor` use the same editor component with identical toolbar config.
- Toolbar affordances, value binding semantics, and validation signals are consistent.
- No regression in light/dark theme rendering around the editor container.
- CSS class integration with existing Tailwind layout is verified (no overflow, no z-index clashes).

#### #327 — Test / interop / lifecycle validation

- JS interop behavior for the markdown editor is covered (mock or verified via Playwright if needed).
- Lifecycle navigation between Create → Edit preserves editor reset behavior.
- Full bUnit coverage for component states: empty, populated, validation failure, submission.
- Architecture test suite continues to pass (no new cross-feature dependencies introduced).

---

## Merge Sequencing

Merges happen into `dev` via squash-merge PRs after the Aragorn PR gate passes.

```text
dev
 ├── ← squad/322-milestone-worktree-execution-rails  (this PR)
 ├── ← squad/323-restore-markdown-editor-compile-path
 ├── ← squad/324-create-flow-markdown-authoring        ┐ parallel OK after #323 merges
 ├── ← squad/325-edit-flow-markdown-authoring          ┘
 ├── ← squad/326-shared-ux-parity-editor-hardening       (after #324 AND #325)
 └── ← squad/327-test-interop-lifecycle-validation        (after #324 AND #325)
```

**Merge rules:**

1. CI must be green before Aragorn reviews.
2. Aragorn reads Copilot automated review comments before approving.
3. Aragorn checks Codecov bot comment — flag any coverage decrease ≥ 1%.
4. Squash merge only (no merge commits on `dev`).
5. After merge: delete the remote branch and tear down the worktree.

---

## Scope Reminder

Per scope lock (`.squad/decisions/inbox/aragorn-scope-lock-markdown-editor.md`):

- ✅ Markdown editor component in Create and Edit flows
- ✅ Markdown string storage (no HTML, no Markdig pipeline in this sprint)
- ❌ Backend image-upload pipeline — **blocked for this sprint**
- ❌ Markdown-to-HTML rendering redesign — **out of scope**
- ❌ RTBlazorfied kept for non-content purposes — evaluate during #323

---

## Squad Contacts

| Question | Who |
|----------|-----|
| Scope clarification, architecture decision | Aragorn |
| Backend wiring, NuGet, App.razor assets | Sam |
| Blazor component implementation, UI | Legolas |
| Tests, coverage, lifecycle assertions | Gimli |
| Pre-push hook failures, CI/CD | Boromir |
