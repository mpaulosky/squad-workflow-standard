---
name: build-repair
confidence: high
description: "Iterative validation and repair workflow for this repository. WHEN: \"build is broken\", \"run build-repair\", \"before git push\". INVOKES: dotnet restore/build/test, markdownlint, sync/check scripts."
---

## Build Repair Skill

### Authoritative Source

The full build repair process is defined in:

> **`.github/prompts/build-repair.prompt.md`**

Always follow that prompt. This skill provides supplementary context.

### Quick Reference

1. `dotnet restore`
2. `dotnet build --no-restore` — must show **0 Error(s), 0 Warning(s)**
3. `dotnet test --configuration Release` — must show **Failed: 0**
4. `npx --yes markdownlint-cli2 "**/*.md"`
5. For standard-pack validation in a target repo, run:

- `bash scripts/squad/sync-git-gh-standard.sh /absolute/path/to/target-repo`
- `bash scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo`

### Repository-Specific Notes

- **Solution file:** `squad-workflow-standard.slnx` (repo root)
- **Primary test project:** `tests/SquadWorkflowStandard.Tests`
- **This repo is a standard-pack publisher:** validate sync/check scripts against
  a concrete target repo when changing workflow-standard content.
- **Zero warning tolerance:** treat warnings as errors before pushing.

### Common Failures and Fixes

| Symptom                | Root Cause                               | Fix                                                 |
| ---------------------- | ---------------------------------------- | --------------------------------------------------- |
| `MSB4019` on Linux CI  | `%USERPROFILE%` path in NuGet.config     | Remove `<config>` block from NuGet.config           |
| Markdown lint failures | Rule drift in docs/prompts/skills        | Run markdownlint and apply minimal formatting fixes |
| check script exits `3` | Standard version drift in target repo    | Re-sync standard pack, then rerun check             |
| check script exits `4` | Missing required adapters in target repo | Add required adapter blocks, rerun check            |
