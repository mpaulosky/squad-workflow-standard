---
name: self-authored-pr-gate
confidence: high
description: >
  Lead-gate workflow for PRs where the reviewer account is also the PR author
  and GitHub blocks self-approval.
domain: "PR governance, code review, CI/CD"
source: "earned"
---

## Self-Authored PR Gate

Use this when GitHub returns `422: Can not approve your own pull request` during lead review.

### Required Checks

1. CI is fully green (build, tests, security, coverage checks).
2. Copilot automated review has no unresolved bug/security findings.
3. Codecov shows no material regression, or any significant coverage decrease (≥1%) is investigated and explained.
4. At least one domain-specialist review perspective is documented.

### Workflow

1. Verify PR body has a valid issue closure reference (`Closes #N`).
2. Record lead and specialist verdicts in the review notes.
3. If all checks pass, perform squash merge.
4. Sync local `dev` and prune merged `squad/*` branches.

### Do Not Use This Path When

- Any CI check is failing or pending.
- Copilot flags unresolved logic/security defects.
- Codecov shows unexplained coverage decreases (no investigation provided).
- Required domain reviewer perspective is missing.
