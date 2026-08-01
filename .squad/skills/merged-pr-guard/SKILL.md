---
name: merged-pr-guard
description: "Avoid stale squad branch commit drift by checking whether the related PR is already merged. WHEN: \"stale squad branch commit\", \"merged PR guard\", \"branch guard\" INVOKES: \"merged-pr-guard\", \"run merged-pr-guard\", \"merged-pr-guard skill\""
---

# Merged PR guard

If the related PR is already merged, switch to the correct base branch before continuing work.
