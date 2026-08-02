---
name: release-process
description: "Step-by-step release checklist for Squad — prevents release regressions. WHEN: \"prepare MyBlog release PR\", \"release-process\", \"run release-process\". INVOKES: release validation, version checks, publish steps."
domain: release-management
confidence: high
---

## Release process

- Validate version semantics.
- Verify authentication and release inputs.
- Run the normal validation suite.
- Prepare the release branch and PR only after checks pass.
