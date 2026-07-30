# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Workflow architecture | Mal | Define reusable process, sequence, and rollout strategy |
| Script implementation (bash/pwsh/C#) | Zoe | Build and refactor sync/check/update scripts |
| CI/hooks/platform reliability | Wash | Manage workflow files, hook safety, cross-platform behavior |
| Test/verification coverage | Kaylee | Add drift checks, failure-mode validation, regression guardrails |
| Documentation/process clarity | Inara | Update standards docs, setup instructions, migration guides |
| Code review | Mal | Review PRs, check quality, suggest improvements |
| Testing | Kaylee | Write tests, find edge cases, verify fixes |
| Scope & priorities | Mal | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |
| RAI review | Rai | Content safety, bias checks, credential detection, ethical review |
| Fact verification / challenge | Fact Checker | Verify claims, challenge assumptions, pre-mortem risks |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Mal |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Skills

When routing work in these domains, inject the listed asset into the agent's
spawn prompt:
`Relevant asset: {path} — read before starting.`

| Domain                                                                     | Asset                                                                                | When to Inject                                                                                                                                                                                                                                                                            |
| -------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Blazor Tailwind theming, dark/light mode, FOUC, localStorage, color themes | `.squad/skills/blazor-tailwind-theme-persistence/SKILL.md`                           | Any Legolas task touching App.razor, NavMenu, MainLayout, theme toggle, or `tailwind-color-theme` storage key                                                                                                                                                                             |
| Auth0 Management API, M2M, role operations                                 | `.squad/skills/auth0-management-api/SKILL.md`                                        | Any Gandalf or Legolas task touching UserManagementHandler, role operations, Management API integration, or Auth0 configuration changes. Owner: Gandalf (Security).                                                                                                                       |
| Auth0 security, secrets, authorization                                     | `.squad/skills/auth0-management-security/SKILL.md`                                   | Any security audit, secrets review, or auth configuration change. All squad members reference this for authorization boundary and secrets management rules. Owner: Gandalf (Security).                                                                                                    |
| MongoDB DBA patterns, runtime wiring, indexing                             | `.squad/skills/mongodb-dba-patterns/SKILL.md`                                        | Any Sam, Gimli, Boromir, or Gandalf task touching Mongo wiring, mapping, indexing, backups, upgrades, or shared environment hardening. Owner: Sam (Backend). Audience: Gimli (verification), Boromir (environment), Gandalf (secrets/TLS).                                                |
| MongoDB filter patterns, list queries, caching                             | `.squad/skills/mongodb-filter-pattern/SKILL.md`                                      | Any Sam or Gimli task touching query contracts, cache-key changes, list filtering, repository standardization, or handler-level caching. Owner: Sam (Backend). Supporting: Gimli (Tester).                                                                                                |
| Mongo-backed integration tests                                             | `.squad/skills/testcontainers-shared-fixture/SKILL.md`                               | Any Gimli or Sam task touching `tests/Integration.Tests/`, `MongoDbFixture`, collection definitions, or new repository/handler integration coverage against MongoDB. Owner: Gimli (Tester).                                                                                               |
| Running-browser UI verification                                            | `.squad/skills/webapp-testing/SKILL.md`                                              | Any Gimli or Legolas task that already has bUnit coverage but still needs runtime verification of JS interop, Auth0 redirects, or AppHost smoke behavior. Do **not** inject this for ordinary unit/bUnit work or to create a new browser-test project. Owner: Gimli (Tester).             |
| Test-driven development (TDD), red-green-refactor, behavior-first testing  | `.squad/skills/tdd/SKILL.md` + `.github/skills/tdd/tests.md`                         | Every Gimli testing task: unit, bUnit, integration, or architecture tests. TDD is Gimli's default approach. Tests are behavior-first (observable outcomes), not implementation-detail coupled. Owner: Gimli (Tester). See `.github/skills/tdd/SKILL.md` for tracer bullets, incremental loops, and anti-patterns. |
| Sprint planning, worktrees, sprint branch lifecycle                        | `.squad/playbooks/sprint-planning.md` + `.squad/skills/sprint-planning/SKILL.md`     | Any Ralph or Aragorn task triggered by plan creation, milestone creation, sprint issue creation, project board updates, or worktree setup/teardown. Inject both assets into the spawn prompt.                                                                                             |
| Push-capable squad work                                                    | `.squad/skills/pre-push-test-gate/SKILL.md` + `.squad/playbooks/pre-push-process.md` | Any task expected to end in `git push`, branch handoff, or local gate validation. This is the default for normal `squad/{issue}-{slug}` delivery after Sprint 1.1.                                                                                                                        |
| Build/test gate failures                                                   | `.squad/skills/build-repair/SKILL.md` + `.squad/skills/pre-push-test-gate/SKILL.md`  | Any task blocked by Release build failures, warning cleanup, failing tests, or a rejected pre-push Gates 2–4 run. Aragorn owns this route and can delegate the repair.                                                                                                                    |
| PR review, approval, merge, and post-merge cleanup                         | `.squad/playbooks/pr-merge-process.md`                                               | Any Aragorn-led PR gate once CI is green, including Copilot-review read, parallel reviewer fan-out, CHANGES_REQUESTED lockout, squash merge, and cleanup.                                                                                                                                 |
| Resumed work on an existing `squad/*` branch                               | `.squad/skills/merged-pr-guard/SKILL.md`                                             | Any agent about to `git commit` on a branch with prior PR activity or an uncertain session state. Check for an already-merged PR before committing.                                                                                                                                       |
| NuGet/Azure/Microsoft API reference during CI/CD                           | `.squad/skills/microsoft-code-reference/SKILL.md`                                    | Any Boromir task verifying NuGet package versions, Aspire AppHost resources, GitHub Actions patterns, .NET SDK/target framework compatibility, or NuGet package signatures. Owner: Boromir (DevOps). Sprint 2 rewrite: now focused on MyBlog DevOps/NuGet/GitHub Actions/Aspire patterns. |
| Release coordination, tagging, and hotfix backports                        | `.squad/skills/release-process/SKILL.md` + `.squad/playbooks/release-myblog.md`      | Any Aragorn or Boromir task preparing a `dev` → `main` release PR, tagging `main`, writing manual release notes, or backporting a hotfix from `main` to `dev`. Owner: Aragorn (release gate) + Boromir (execution).                                                                       |

## Workflow Guardrails

After Sprint 1.1, these process assets are part of normal squad flow:

1. **Before writing any code**, a GitHub issue MUST exist for the work AND it must be
   **sprint-stamped** — title starts with `[Sprint N]`, milestone is set to
   `Sprint N: {Theme}`, and it is added to Project #4. This is an absolute gate with
   no exceptions. If no issue exists, create it now; if an issue exists but lacks the
   sprint prefix or milestone, correct it before touching any file. See the Hard Gate
   section in `.squad/playbooks/sprint-planning.md`.

2. **Before opening or modifying any file for issue-owned work**, verify checkout
   alignment with that issue. Confirm all of the following first:

    - `git branch --show-current` is the issue branch `squad/{issue}-{slug}` for the
      active issue — never `dev`
    - `git rev-parse --show-toplevel` and your current directory point at the correct
      sprint or issue worktree for that same issue
    - if a `squad/{issue}-*` branch or issue worktree already exists, reuse it instead
      of continuing from a different checkout

   If any check fails, stop. Recover or stash local edits if needed, switch to the
   correct branch/worktree, and only then touch files. This applies to Ralph before
   spawning issue-owned work and to every routed agent before continuing it.

3. **Before any push-ready handoff**, route through the pre-push gate skill and
   pre-push playbook so agents respect the live MyBlog hook: `squad/{issue}-{slug}`
   branch naming, Release build, `Architecture.Tests`, `Unit.Tests`, and
   `Integration.Tests`.

4. **When build or test health is red**, route through build repair first. Do not
   treat a broken branch as normal feature work.

5. **When PR work starts**, Aragorn and any spawned reviewers use the PR merge
   playbook as the governing checklist.

6. **When a session resumes on an older squad branch**, apply the merged-PR guard
   before committing so work does not strand on a merged branch.

7. **Do not reintroduce deleted imports.** Only route assets with an explicit
   MyBlog owner, fit, and usage rule.

8. **When any `plan.md` is created or materially updated**, Ralph and Aragorn run
   the Sprint Planning ceremony: decompose into sprints, create milestones + issues,
   add to the MyBlog project board, and Boromir sets up worktrees. See
   `.squad/playbooks/sprint-planning.md`.

9. **When a user makes any coding request** (direct instruction, `[[PLAN]]`, or
   follow-on work), the very first agent action is to check whether a GitHub issue
   exists with a `[Sprint N]` title prefix and a sprint milestone set. If the issue
   is missing, create it. If it exists but lacks the prefix or milestone, fix those
   fields. Then verify branch/worktree alignment for that same issue before any file
   is opened or modified.

10. **When any production code is written or modified** (Domain, Web, Persistence,
    AppHost), Gimli MUST be spawned in parallel to write or update unit tests.
    No feature branch closes without corresponding test authoring. The coverage gate
    (currently 89% line threshold in `Unit.Tests.csproj`) must pass locally before
    push. This is not optional — test coverage is a first-class deliverable.

11. **`git push --no-verify` is PROHIBITED.** It bypasses all pre-push quality gates
    (build, tests, coverage) and wastes CI time when failures are discovered
    remotely. If the hook fails due to a local SDK mismatch, fix the root cause:
    install the SDK version pinned in `global.json` (e.g., `dotnet-install.sh`
    or download from https://dot.net). SDK mismatch is never a valid bypass reason.
    Any `--no-verify` push requires prior documented approval from Ralph + Aragorn.

12. **When new architectural patterns are introduced** (new CQRS handler type, new
    service abstraction, new Blazor rendering pattern, new repository strategy), a
    rubber duck review MUST be run before the branch is pushed. Document the
    pattern decision in `.squad/decisions/inbox/` and route to Aragorn for ADR.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.

## Workflow Standard Binding

- Source: `.squad/workflows/git-gh-process-standard.md`
- Template: `.squad/templates/issue-lifecycle.md`
- Flow policy: single issue uses standard branch flow; 2+ issues require worktree flow
- Policy: never push directly to `main`, `preview`, or `dev`