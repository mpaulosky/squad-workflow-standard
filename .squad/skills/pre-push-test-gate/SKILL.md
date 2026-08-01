---
name: pre-push-test-gate
description: "Before git push, validate the shared workflow standard and run the full local test suite. WHEN: \"before git push\", \"push validation\", \"pre-push gate\" INVOKES: \"pre-push-test-gate\", \"run pre-push-test-gate\", \"pre-push-test-gate skill\""
---

# Pre-push test gate

Run the local validation suite and make sure it passes before pushing.
