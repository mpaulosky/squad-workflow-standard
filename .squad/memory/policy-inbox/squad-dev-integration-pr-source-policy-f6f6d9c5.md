---
id: f6f6d9c5-1ed5-41e7-8a96-19d042697886
class: POLICY
loadGuidance: [ALWAYS]
title: "Dev integration PR source policy"
author: "Squad"
createdAt: 2026-07-24T21:15:08.147Z
metadata: {}
---

### 2026-07-24: Corrected integration flow
**By:** mpaulosky (via Copilot)
**What:** Changes must be pushed from a squad/work branch and opened as a PR targeting `dev`; direct pushes to `dev` are not the normal workflow. Only `dev` can later promote to `main` via PR.
**Why:** Keep integration review and branch hygiene intact while preserving `dev -> main` promotion control.
