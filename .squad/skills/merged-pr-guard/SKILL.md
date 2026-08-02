---
name: merged-pr-guard
description: "Guardrail to avoid committing to merged squad branches by checking merged PR state and switching to dev before committing. WHEN: \"stale squad branch commit\", \"merged-pr-guard\", \"run merged-pr-guard\". INVOKES: merged PR checks, branch switching."
confidence: high
---

## Merged-PR branch guard

- Check whether the related PR has already merged.
- If so, switch back to the active development branch before continuing.
- Avoid leaving commits stranded on an already-merged branch.
