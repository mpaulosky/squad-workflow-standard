---
id: ddac7da2-7e16-491c-85d5-2b0fcb3dac1a
class: POLICY
loadGuidance: [ALWAYS]
title: "PR base branch policy update"
author: "Squad"
createdAt: 2026-07-24T20:29:06.153Z
metadata: {}
---

### 2026-07-24: Branching policy correction
**By:** mpaulosky (via Copilot)
**What:** Work from squad branches, sprints, and worktrees must target `dev`; only `dev` may target `main`.
**Why:** Prevent `dev` and `main` from drifting and preserve a staged promotion flow.
