---
post_title: Dev Source of Truth Promotion and Back-merge Spec
author1: mpaulosky
post_slug: dev-source-of-truth-promotion-backmerge-spec-2026-07-29
microsoft_alias: n/a
featured_image: https://via.placeholder.com/1200x630?text=Promotion+Backmerge+Spec
categories:
  - General
tags:
  - specification
  - branching
  - governance
  - ci
  - release-management
ai_note: Synthesized with AI assistance from existing repository context and prior incident analysis.
summary: Product-style specification for a safe promotion and back-merge process that keeps dev authoritative while preventing noisy synchronization PRs.
post_date: 2026-07-29
---

## Problem Statement

The team needs a reliable branch governance model where dev remains the
operational source of truth, promotions to preview and main stay policy-safe,
and synchronization back into dev does not produce uncontrolled high-noise pull
requests.

A recent incident demonstrated that branch-source guards alone do not prevent
large, low-signal back-merge diffs when divergence accumulates. This creates
review fatigue, increases regression risk, and weakens confidence in release
traceability.

## Solution

Implement a guarded synchronization model with directional promotion and scoped
back-merge controls.

The user-facing behavior of the system is:

1. Normal feature delivery continues through dev.
2. Promotion remains staged from dev to preview and preview to main.
3. Back-merge from main to dev runs only through a controlled automation path.
4. Every synchronization PR must prove ancestry, scope, and acceptable size.
5. Exceptions are possible, but only through explicit, auditable override
   governance.

This preserves delivery speed while preventing another PR-119-style noisy merge
surface.

## User Stories

1. As a release manager, I want dev to remain the authoritative integration
   branch, so that all day-to-day feature convergence happens in one place.
2. As a release manager, I want preview to accept only sanctioned promotion
   sources, so that staging remains predictable and reproducible.
3. As a release manager, I want main to accept only approved release paths, so
   that production history reflects intentional promotion.
4. As a maintainer, I want back-merge PRs to include only synchronization
   intent, so that review effort is proportional to actual risk.
5. As a maintainer, I want automated ancestry validation, so that hidden
   divergence is detected before merge.
6. As a maintainer, I want scope validation on changed path classes, so that
   unrelated edits cannot piggyback into synchronization PRs.
7. As a maintainer, I want churn thresholds with warning and block levels, so
   that oversized PRs are surfaced and stopped consistently.
8. As a maintainer, I want forbidden path symmetry in both promotion and
   back-merge directions, so that protected state cannot leak across branches.
9. As a maintainer, I want override actions to require governance approvals, so
   that emergency exceptions remain controlled.
10. As a maintainer, I want override metadata to include reason and expiry, so
    that temporary exceptions do not become permanent loopholes.
11. As a reviewer, I want synchronization PRs to include evidence fields, so
    that I can approve based on traceable facts instead of intuition.
12. As a reviewer, I want auto-merge disabled on override PRs, so that humans
    explicitly accept elevated risk.
13. As a compliance owner, I want all synchronization checks to be required
    statuses, so that bypassing policy is difficult.
14. As a compliance owner, I want a clear incident recovery playbook, so that
    teams know exactly how to respond to divergence.
15. As a platform engineer, I want automation to fail closed when evidence is
    inconclusive, so that uncertainty does not silently ship.
16. As a platform engineer, I want deterministic check outputs, so that failure
    reasons are actionable.
17. As a product engineer, I want normal work branches to remain unchanged by
    this policy, so that developer flow stays simple.
18. As a product engineer, I want policy checks to run early in PR lifecycle,
    so that issues are found before final review.
19. As an on-call engineer, I want divergence severity labels, so that
    incidents can be triaged quickly.
20. As an engineering manager, I want measurable success criteria, so that we
    can evaluate whether the new process is working.
21. As an engineering manager, I want trend reporting for back-merge noise, so
    that process drift is visible over time.
22. As an architect, I want one primary seam for synchronization validation, so
    that policy logic stays cohesive and maintainable.
23. As an architect, I want policy thresholds to be tunable without redesign,
    so that the process can adapt to real data.
24. As a governance owner, I want replay-safe automation behavior, so that
    retried runs do not mutate branch state unexpectedly.
25. As a governance owner, I want explicit ownership boundaries between dev and
    release branches, so that synchronization respects team-state contracts.
26. As a QA lead, I want high-signal back-merge PRs, so that quality review
    focuses on behavior-impacting changes.
27. As a QA lead, I want non-functional diff noise reduced, so that regression
    risk is easier to assess.
28. As a docs owner, I want policy vocabulary to be consistent across process
    docs and workflow rules, so that onboarding is clearer.
29. As a repo admin, I want branch protection and required checks aligned, so
    that policy cannot be accidentally weakened.
30. As an executive stakeholder, I want fewer release friction incidents, so
    that promotion reliability improves over time.

## Implementation Decisions

- The process uses one primary policy seam: synchronization validation at the
  automation boundary before any back-merge PR is allowed to proceed.
- Directional branch flow remains unchanged for promotion:
  dev to preview to main.
- Back-merge becomes synchronization-only behavior, not a second delivery
  channel.
- Routine full base merges are disallowed for synchronization because they
  inflate noise and hide intent in large diffs.
- Merge-base logic is required for ancestry and scope proof, but it is a guard
  mechanism rather than the default composition strategy.
- Synchronization composition defaults to controlled replay of main-only intent
  into a dev-derived branch.
- Guardrails are split into mandatory classes:
  ancestry proof, scope proof, churn budget, and ownership path safety.
- Churn enforcement uses two levels:
  warning threshold and hard-stop threshold.
- Path safety is directional and symmetric where ownership differs, preventing
  one-way policy gaps.
- Override governance is explicit and auditable with time-bounded validity,
  named approval responsibility, and elevated approval count.
- Override PRs are never merged automatically.
- Incident mode supports a controlled one-time divergence repair path when
  strict synchronization cannot be achieved through the normal flow.
- Policy telemetry is required so threshold tuning is data-driven rather than
  anecdotal.
- The rollout is phased to reduce operational disruption:
  observe, enforce, then harden.

## Testing Decisions

- A good test validates externally observable policy behavior and decision
  outcomes, not implementation details of workflow steps.
- The highest seam is end-to-end workflow contract behavior for promotion and
  synchronization PR gating.
- Existing workflow validation seams are preferred over introducing many new
  scattered checks.
- Test coverage focuses on policy outcomes:
  pass when ancestry and scope are valid, fail when they are not.
- Churn budget tests validate warning and blocking boundaries around configured
  thresholds.
- Ownership path tests validate that protected path classes cannot cross branch
  boundaries through synchronization.
- Override tests validate governance mechanics:
  required approvals, required metadata, expiration handling, and auto-merge
  prohibition.
- Divergence recovery tests validate runbook correctness for both targeted
  replay and controlled exception flow.
- Regression tests validate that normal dev feature delivery remains unaffected
  by synchronization-specific hardening.
- Prior art should be drawn from existing repository checks that enforce branch
  source constraints, path constraints, and promotion invariants.

## Out of Scope

- Rewriting the full release branching model beyond the existing
  dev to preview to main structure.
- Replacing current CI platform or repository hosting capabilities.
- Expanding policy to unrelated repositories in this spec.
- Defining implementation-level scripts or code in this document.
- Altering team topology, review ownership model, or role definitions outside
  synchronization governance needs.

## Further Notes

- Threshold values are initial control limits and are expected to be tuned
  after observing at least two release cycles.
- Success depends on combining technical guardrails with governance discipline;
  either alone is insufficient.
- The immediate objective is prevention of another large noisy back-merge event
  while maintaining promotion velocity.
- Once stabilized, this specification can be converted into a reusable standard
  for downstream repositories consuming this workflow pack.
