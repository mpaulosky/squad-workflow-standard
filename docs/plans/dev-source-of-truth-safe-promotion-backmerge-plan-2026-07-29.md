---
post_title: Dev Source of Truth Safe Promotion and Back-merge Plan
author1: mpaulosky
post_slug: dev-source-of-truth-safe-promotion-backmerge-plan-2026-07-29
microsoft_alias: n/a
featured_image: https://via.placeholder.com/1200x630?text=Dev+Source+of+Truth+Plan
categories:
  - General
tags:
  - branching
  - governance
  - backmerge
  - promotion
  - incident-response
ai_note: Drafted with AI assistance for internal planning and governance.
summary: Strategy to prevent noisy main to dev back-merges while preserving dev as source of truth through guarded promotion, measurable gates, and divergence recovery.
post_date: 2026-07-29
---

## Problem Statement

PR #119 exposed a recurring failure mode: a main to dev synchronization attempt
carried noisy, broad churn that obscured intent, increased review burden, and
risked overwriting dev-owned state. The incident showed that branch protection
alone is not enough when the synchronization branch is composed without strict
ancestry, scope, and size controls.

The plan below preserves dev as the operational source of truth for active
integration work while still allowing controlled synchronization from released
state in main.

## Design Principles

1. Dev remains the source of truth for active integration and team state.
2. Promotion is directional and staged: dev to preview to main.
3. Back-merge is corrective synchronization, not a second delivery channel.
4. Synchronization changes must be explainable by commit ancestry, not inferred
   by file diff noise.
5. Guardrails must fail closed by default and require explicit override trails.
6. Recovery must be rehearsable, bounded, and measurable.

## Explicit Decision on Base Merges

Decision: do not use routine base merges for main to dev synchronization.

Rationale:

- Base merges inflate churn by pulling unrelated history into the sync branch.
- They reduce reviewer signal by mixing intended sync with incidental drift.
- They are the fastest path to repeating the PR #119 noise pattern.

Allowed exception:

- A one-time, explicitly approved divergence repair may use a base merge only
  under override governance, with pre-merge dry-run evidence and full audit
  notes in the PR body.

## End-state Branch Policy

1. Feature and issue work merges into dev only.
2. Promotion to preview occurs only through the sanctioned promotion branch.
3. Promotion to main occurs only through preview to main PR flow.
4. Main to dev synchronization is controlled back-merge PR flow only.
5. Dev-owned state remains protected during back-merge composition and review.

Operational sequence:

1. dev -> automation/promote-preview -> preview
2. preview -> main
3. automation/backmerge-main-to-dev -> dev (only when ahead and gated)

## Guardrails

### Ancestry Guardrail

- Sync branch must be cut from current dev head.
- Main changes are replayed into that branch through controlled composition.
- If ancestry check cannot prove expected parentage, fail and require manual
  divergence recovery playbook.

### Scope Guardrail

- Back-merge PR must include only synchronization-intent paths.
- Dev-owned state paths must remain protected by policy checks.
- Any unrelated path change causes hard failure until excluded.

### Size and Churn Guardrail

- Warning threshold: over 40 files changed or over 1200 net line delta.
- Hard block threshold: over 80 files changed or over 2500 net line delta.
- Hard block threshold also triggers if over 15 percent of changed files are
  outside expected synchronization path classes.

### Forbidden Path Symmetry Guardrail

- Paths forbidden for upward promotion into main must have symmetric controls
  for downward synchronization into dev where ownership differs.
- If policy blocks a path in one direction but allows silent overwrite in the
  opposite direction, the PR fails policy validation.

### Override Governance Guardrail

- Overrides are time-boxed and single-use.
- Requires named approver, incident reference, and expiration timestamp.
- Requires at least two approvals, including one governance owner.
- Auto-merge is disabled for any override-tagged PR.

## Phase Rollout

### Immediate Phase (0 to 2 days)

1. Freeze noisy back-merge auto-merge behavior.
2. Enforce ancestry, scope, and protected-path checks as required statuses.
3. Add PR template fields for sync intent, churn metrics, and override reason.

### Short-term Phase (3 to 10 days)

1. Add churn threshold checks and outside-scope percentage checks.
2. Add forbidden-path symmetry validation into both source and generated
   workflow baselines.
3. Add dashboard visibility for back-merge PR volume, failure reason, and mean
   time to merge.

### Hardening Phase (2 to 6 weeks)

1. Add drift forecasting to detect likely divergence before main release merge.
2. Add periodic replay simulation in CI against synthetic high-churn scenarios.
3. Run quarterly policy game-day using incident recovery playbook.

## Acceptance Criteria

1. Zero back-merge PRs merged with protected-path violations for 60 days.
2. At least 95 percent of back-merge PRs stay below warning churn threshold.
3. 100 percent of back-merge PRs include ancestry proof and scope declaration.
4. Mean reviewer turnaround for back-merge PRs is less than 8 business hours.
5. Zero override PRs merged without dual approval and expiration metadata.
6. No repeated PR #119-class noisy incident across two release cycles.

## Incident Recovery Playbook for Divergence

1. Detect divergence and label incident severity within 30 minutes.
2. Pause auto back-merge workflow and lock override path.
3. Capture comparison artifacts: ancestry graph, path-class diff, churn metrics.
4. Choose recovery mode:
   - Mode A: targeted replay of missing commits into dev-derived sync branch.
   - Mode B: controlled one-time base merge under override governance.
5. Run dry-run validation and publish artifacts in PR conversation.
6. Require dual approval and merge only after all guardrails pass or approved
   override evidence is present.
7. Post-merge verify branch parity on defined path classes within 15 minutes.
8. Conduct incident review within 2 business days and update thresholds if
   false positives or false negatives are found.

## Rubber-duck Review

1. Q: If dev is source of truth, why synchronize from main at all?
   A: Main can contain release-only changes; synchronization prevents hidden
   long-term drift that later explodes into larger conflicts.

2. Q: Are churn thresholds arbitrary?
   A: They are initial control limits and must be tuned from observed metrics
   after two release cycles.

3. Q: Could thresholds block legitimate large syncs?
   A: Yes, which is why warning and hard-block tiers exist plus governed
   override for exceptional, auditable cases.

4. Q: Why reject routine base merges if they are simpler?
   A: Simplicity at composition time creates complexity at review and incident
   time; PR #119 proved that cost is too high.

5. Q: What is the largest false-positive risk?
   A: Large but valid release synchronization bursts may trip hard blocks.
   Mitigation is evidence-backed override with dual approval.

6. Q: What is the largest false-negative risk?
   A: Small but dangerous protected-path edits hidden in low-churn PRs.
   Mitigation is strict path-class and symmetry checks, independent of size.

7. Q: Does this slow releases?
   A: Slightly for edge cases, but it reduces high-cost rollback and review
   fatigue, improving total lead time reliability.

8. Q: Can automation itself fail operationally?
   A: Yes. Expected modes are stale branch refs, race conditions, and check
   misconfiguration; each is handled by fail-closed checks and manual playbook.

9. Q: What happens if override governance is abused?
   A: Abuse is constrained by expiration, named ownership, dual approval, and
   mandatory post-incident audit trail.

10. Q: Why require symmetry of forbidden paths?
    A: Asymmetric policy creates one-way safety and opposite-way corruption
    risk; symmetry closes that loophole.

11. Q: Could reviewer burden still increase?
    A: Temporarily, during rollout, but standardized evidence fields reduce
    cognitive load and improve consistency.

12. Q: How do we know the plan worked?
    A: The measurable acceptance criteria define success using violations,
    churn distribution, review latency, and recurrence rate.

## Decision Summary

The strategy keeps dev authoritative for integration, maintains directional
promotion through preview and main, and constrains back-merge to a guarded,
auditable synchronization path. Routine base merges are explicitly disallowed,
with a narrow override path for controlled divergence repair.