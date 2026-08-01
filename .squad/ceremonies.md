# Ceremonies

> Team meetings that happen before or after work. Each squad configures their own.

## Feature Work Kickoff

| Field | Value |
| ------- | ------- |
| **Trigger** | auto |
| **When** | before |
| **Condition** | any agent picks up a feature or fix issue that touches production code |
| **Facilitator** | coordinator |
| **Participants** | all-relevant |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Agenda:**

1. Feature author reads the issue and identifies files to be changed
2. Gimli is spawned in parallel to write tests from the issue's acceptance criteria
3. Frodo is spawned in parallel to note doc/README impact
4. If new architectural patterns are involved → rubber duck BEFORE coding starts
5. All agents confirm scope before the first file is opened

**Hard Rule:** No feature code is written without Gimli already working on tests. Tests are a parallel deliverable, not a post-merge cleanup.

---

## Design Review

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | before |
| **Condition** | multi-agent task involving 2+ agents modifying shared systems |
| **Facilitator** | lead |
| **Participants** | all-relevant |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Agenda:**
1. Review the task and requirements
2. Agree on interfaces and contracts between components
3. Identify risks and edge cases
4. Assign action items

---

## Retrospective

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | after |
| **Condition** | build failure, test failure, or reviewer rejection |
| **Facilitator** | lead |
| **Participants** | all-involved |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Agenda:**

1. What happened? (facts only — no blame)
2. Root cause analysis (was it a guardrail gap? a bypass? missing agent activation?)
3. What should change? (update routing.md, ceremonies.md, or playbooks)
4. Action items with owner assigned

**Hard Rule:** Every CI failure triggers a retrospective. Ralph documents action items in `.squad/decisions/inbox/` within the same session. The squad does not move to the next sprint without closing all retrospective action items.

---

## Retrospective with Enforcement

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | weekly |
| **Condition** | No *retrospective* log in .squad/log/ within the last 7 days |
| **Facilitator** | lead |
| **Participants** | all |
| **Time budget** | focused |
| **Enabled** | yes |
| **Enforcement skill** | retro-enforcement |

**Agenda:**
1. What shipped this week? (closed issues, merged PRs)
2. What did not ship? (open issues, blockers)
3. Root cause on any failures
4. Action items -- each MUST become a GitHub Issue labeled retro-action

**Coordinator integration:**
At round start, call Test-RetroOverdue (see skill retro-enforcement). If overdue, run this ceremony before the work queue.

**Why GitHub Issues, not markdown:**
Production data: 0% completion across 6 retros using markdown checklists, 100% after switching to GitHub Issues.

## Workflow Standard Reference

Reference: `.squad/workflows/git-gh-process-standard.md`