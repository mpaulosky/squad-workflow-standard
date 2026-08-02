---
name: pre-push-test-gate
description: "Before any push, the agent must run the full local test suite and ensure zero failures. WHEN: \"before git push\", \"run pre-push-test-gate\", \"pre-push-test-gate skill\". INVOKES: dotnet test, full validation suite."
confidence: high
---

## Steps

- Run the full local test suite before any push.
- Fix any failures before publishing changes.
- Avoid pushing when validation still has open issues.
