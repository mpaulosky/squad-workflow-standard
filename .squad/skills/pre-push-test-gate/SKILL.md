---
name: pre-push-test-gate
confidence: high
description: >
  Enforces build cleanliness and test passage before any git push.
  Delegates to the build-repair prompt (.github/prompts/build-repair.prompt.md)
  as the authoritative gate. Established after the Shared project test batch
  (04714a4) shipped two broken tests directly to main.
---

## Pre-Push Test Gate

### Why This Exists

On 2026-02-25, two unit tests were pushed directly to `main` without local verification.
Both tests had wrong expectations and failed in CI. This skill enforces the gate that
prevents that from recurring.

### The Gate

Before any `git push`, an agent MUST run the **Build Repair Skill**:

> **`.github/prompts/build-repair.prompt.md`**

That prompt already defines the full gate:

1. Restore dependencies (`dotnet restore`)
2. Build the solution (`dotnet build --no-restore`) — zero errors, zero warnings
3. Fix any build errors before continuing
4. Run unit tests — all must pass
5. Fix test failures before continuing

Only push when the build-repair prompt reports **"Build succeeded"** with **zero warnings**
and **all tests pass**.

### Agent Checklist

Before any `git push`, an agent MUST:

- [ ] Open `.github/prompts/build-repair.prompt.md` and execute it fully
- [ ] Confirm final output: `Build succeeded. 0 Warning(s). 0 Error(s).`
- [ ] Run ALL test projects including `AppHost.Tests` — confirm `Passed! Failed: 0` for every suite
- [ ] Only then execute `git push`

**⚠️ AppHost.Tests (Playwright E2E) is MANDATORY.** It must be run locally before every push,
even though it takes longer. Skipping it or claiming "all tests pass" without running it is a
false statement — failures will surface in CI on the PR. Docker must be running (Aspire boots
internally via `DistributedApplicationTestingBuilder`).

Do NOT push if any test suite reports failures. Fix first.

### Hook (Local Enforcement)

The `.git/hooks/pre-push` hook enforces all four gates locally.
Install once per clone — **Shell (Linux/macOS/Git Bash)**:

```bash
# Copy the full hook from the canonical source:
cp .git/hooks/pre-push .git/hooks/pre-push.bak 2>/dev/null || true
# The hook runs: build → unit tests → integration tests (including AppHost.Tests/Playwright)
# All four gates must pass. Docker required for Gate 4.
chmod +x .git/hooks/pre-push
```

**Gate summary (current hook):**

- Gate 0: Block direct push to `main`
- Gate 1: Warn on untracked `.razor`/`.cs` files
- Gate 2: `dotnet format --verify-no-changes` (formatting check; offers auto-fix)
- Gate 3: Release build (0 warnings, 0 errors)
- Gate 4: Unit + bUnit + Architecture tests (6 projects, no Docker)
- Gate 5: Integration + Playwright E2E — **AppHost.Tests included** (Docker required)

**PowerShell (Windows):**

```powershell
@'
#!/usr/bin/env bash
set -euo pipefail
echo "🔎 pre-push: running build-repair gate (Domain.Tests + Web.Tests)…"
if dotnet test tests/Domain.Tests tests/Web.Tests --configuration Release --verbosity quiet 2>&1; then
  echo "✅ Gate passed — push allowed."
else
  echo "❌ Gate FAILED. Run .github/prompts/build-repair.prompt.md and fix before pushing."
  exit 1
fi
'@ | Set-Content -NoNewline .git/hooks/pre-push
```

> The hook is not committed — install on every fresh clone. The build-repair prompt
> is the authoritative process; the hook is a fast local tripwire.

### Failure Taxonomy (known patterns)

| Symptom | Root Cause | Fix |
| --------- | ----------- | ----- |
| `DateTime` equality failure in `*.Empty` tests | `Empty` property calls `DateTime.UtcNow` each time — two calls produce different values | Assert individual fields, not whole-record equality |
| Unexpected trailing `_` in slug tests | `GenerateSlug` appends `_` when string ends with punctuation AND has internal punctuation | Verify actual output against implementation before asserting |
| Record equality fails on nested DTO | Nested DTO `Empty` also uses `UtcNow` — same root cause | Flatten assertions to field-level |
