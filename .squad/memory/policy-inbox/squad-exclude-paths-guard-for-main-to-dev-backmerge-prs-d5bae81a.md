---
id: d5bae81a-4b46-4e45-817b-5807c71d59e5
class: POLICY
loadGuidance: [ALWAYS]
title: "Exclude paths guard for main-to-dev backmerge PRs"
author: "Squad"
createdAt: 2026-07-24T21:56:25.158Z
metadata: {}
---

### 2026-07-24: Guard workflow scope exception
**By:** mpaulosky (via Copilot)
**What:** PRs from `main` to `dev` (back-merge sync PRs) should not run `squad-paths-guard`.
**Why:** Avoid blocking automated back-merge reconciliation with path guard checks intended for other PR flows.
