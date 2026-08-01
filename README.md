# squad-workflow-standard

Canonical distribution repository for Squad augmentation assets and workflow standards.
This repo publishes the standard pack that other repositories sync after `git init` + `squad init`, along with the governance, documentation, and release-review material that explain how the standard evolves over time.

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![xUnit Tests](https://img.shields.io/badge/Tests-xUnit-blueviolet?logo=github)](https://github.com/mpaulosky/squad-workflow-standard/actions/workflows/squad-ci.yml)
[![Latest Release](https://img.shields.io/github/v/release/mpaulosky/squad-workflow-standard?logo=github&color=blue&label=Release)](https://github.com/mpaulosky/squad-workflow-standard/releases/latest)

[![CI/CD](https://github.com/mpaulosky/squad-workflow-standard/actions/workflows/squad-ci.yml/badge.svg)](https://github.com/mpaulosky/squad-workflow-standard/actions/workflows/squad-ci.yml)
[![Test Suite](https://github.com/mpaulosky/squad-workflow-standard/actions/workflows/squad-test.yml/badge.svg)](https://github.com/mpaulosky/squad-workflow-standard/actions/workflows/squad-test.yml)

[![CodeCov Coverage](https://codecov.io/gh/mpaulosky/squad-workflow-standard/branch/main/graph/badge.svg)](https://codecov.io/gh/mpaulosky/squad-workflow-standard)
[![Coverage Trend](https://img.shields.io/badge/Coverage-Trend-blue?logo=codecov)](https://codecov.io/gh/mpaulosky/squad-workflow-standard/commits/main)
[![Coverage Gate](https://img.shields.io/badge/Coverage%20Gate-≥80%25-brightgreen?logo=codecov)](https://github.com/mpaulosky/squad-workflow-standard/actions/workflows/squad-test.yml)

[![Open Issues](https://img.shields.io/github/issues/mpaulosky/squad-workflow-standard?color=0366d6)](https://github.com/mpaulosky/squad-workflow-standard/issues?q=is%3Aopen+is%3Aissue)
[![Closed Issues](https://img.shields.io/github/issues-closed/mpaulosky/squad-workflow-standard?color=6f42c1)](https://github.com/mpaulosky/squad-workflow-standard/issues?q=is%3Aclosed+is%3Aissue)
[![Open PRs](https://img.shields.io/github/issues-pr/mpaulosky/squad-workflow-standard?color=28a745)](https://github.com/mpaulosky/squad-workflow-standard/pulls?q=is%3Aopen+is%3Apr)
[![Closed PRs](https://img.shields.io/github/issues-pr-closed/mpaulosky/squad-workflow-standard?color=6f42c1)](https://github.com/mpaulosky/squad-workflow-standard/pulls?q=is%3Aclosed+is%3Apr)

## Documentation index

- [Docs landing page](docs/index.html) — a concise overview of the repository purpose and the main documentation entry points.
- [Architecture overview](docs/ARCHITECTURE.md) — explains the repository layout, workflow policy sources, and the sync boundary between canonical assets and preserved local adapters.
- [Contributing guide](docs/CONTRIBUTING.md) — describes the contribution workflow and expectations for updates to the standard.
- [Workflow diagrams](docs/workflow-state-diagrams.md) — visualizes the branch and release flow for the canonical workflow standard.
- [Release review blog index](docs/blogs/README.md) — links to the release review posts captured in this repository.

## Release review blogs

The release review posts in [docs/blogs](docs/blogs/README.md) summarize the rollout history of the workflow-standard and highlight the major changes in each release. Highlights include:

- [Release Review: v0.0.1-38](docs/blogs/release-review-v0-0-1-38.md) — initial release baseline and workflow groundwork.
- [Release Review: v0.0.1-43](docs/blogs/release-review-v0-0-1-43.md) — branch-aware guardrails and release verification tasks.
- [Release Review: v0.0.1-44](docs/blogs/release-review-v0-0-1-44.md) — automation improvements for releases and project boards.
- [Release Review: v0.0.1-58](docs/blogs/release-review-v0-0-1-58.md) — project board automation, cleanup, and guard fixes.
- [Release Review: v0.0.1-65](docs/blogs/release-review-v0-0-1-65.md) — branch-aware guard fixes and syntax cleanup.
- [Release Review: v0.0.1-67](docs/blogs/release-review-v0-0-1-67.md) — project board automation and auditing workflows.
- [Release Review: v0.0.1-72](docs/blogs/release-review-v0-0-1-72.md) — dev-first PR enforcement and branch policy documentation.
- [Release Review: v0.0.1-77](docs/blogs/release-review-v0-0-1-77.md) — main-to-dev back-merge workflow and hotfix cleanup.
- [Release Review: v0.0.1-83](docs/blogs/release-review-v0-0-1-83.md) — removal of stale workflow state and policy updates.
- [Release Review: v0.0.1-88](docs/blogs/release-review-v0-0-1-88.md) — dependabot auto-merge stability and setup-dotnet updates.
- [Release Review: v0.0.1-101](docs/blogs/release-review-v0-0-1-101.md) — CI, documentation, and skills library updates.
- [Release Review: v0.0.1-102](docs/blogs/release-review-v0-0-1-102.md) — preview-to-main promotion work.
- [Release Review: v0.0.1-104](docs/blogs/release-review-v0-0-1-104.md) — promotion fix after a JavaScript syntax blocker.
- [Release Review: v0.0.1-107](docs/blogs/release-review-v0-0-1-107.md) — hotfix for preview-to-main merge issues.
- [Release Review: v0.0.1-108](docs/blogs/release-review-v0-0-1-108.md) — final promotion with a reconciled workflow baseline.

## Purpose

After a new project runs `git init` and `squad init`, this standard syncs:

- The full **Squad augmentation layer** (skills, instructions, prompts, agents)
- The **git/GitHub workflow standard** (branch policy, PR flow, hard gates)
- A baseline of **GitHub Actions workflows** for CI/CD, labeling, and release automation
- **Git hooks** enforcing the branch policy

It standardizes how Squad works across all projects in the same portfolio while preserving every file that belongs to the local project.

## What squad init installs vs what this adds

| Asset | `squad init` installs | This standard adds |
|---|---|---|
| `.github/workflows/` | 4 (heartbeat, triage, issue-assign, sync-labels) | +31 (CI, release, lint, policy guards, …) |
| `.github/skills/` | 19 skills | +17 additional skills; upgrades shared 19 to canonical versions |
| `.github/instructions/` | none | 5 instruction files |
| `.github/prompts/` | none | 14 prompt files |
| `.github/agents/` | `squad.agent.md` | +`beast.agent.md` |
| `.squad/skills/` | `git-workflow-standard/` | +`build-repair/` |

## Managed vs preserved file boundary

### Managed — canonical source always wins on sync

- All files listed in the manifest files under `source/.squad/workflows/`
- `source/.squad/workflows/git-gh-process-standard.md`
- `source/.squad/skills/git-workflow-standard/SKILL.md`
- `source/.squad/skills/build-repair/SKILL.md`

### Preserved — never touched (user-owned adapters)

```
.squad/routing.md
.squad/ceremonies.md
.squad/templates/issue-lifecycle.md
.squad/team.md
.squad/decisions.md
.squad/agents/**              ← all agent charters and histories
.squad/templates/**           ← managed by squad upgrade, not this standard
.github/copilot-instructions.md   ← top-level project-specific config
```

A re-sync is always safe: user-owned files are never overwritten.

## Project structure

```
source/.squad/workflows/
  git-gh-process-standard.md       ← canonical workflow policy (Standard-Version source of truth)
  README.md                        ← rollout/retrofit playbook
  workflow-baseline-manifest.txt   ← GitHub Actions YAMLs to sync
  hook-baseline-manifest.txt       ← git hooks to sync
  skill-manifest.txt               ← .github/skills/ directories to sync
  instruction-manifest.txt         ← .github/instructions/ files to sync
  prompt-manifest.txt              ← .github/prompts/ files to sync
  agent-manifest.txt               ← .github/agents/ files to sync (beast.agent.md only)
  squad-skill-manifest.txt         ← .squad/skills/ directories to sync

source/.squad/skills/
  git-workflow-standard/SKILL.md   ← skill bound to the process standard version
  build-repair/SKILL.md            ← iterative build-repair skill

.github/skills/                    ← source of truth for skill distribution (Option B)
.github/instructions/              ← source of truth for instruction distribution
.github/prompts/                   ← source of truth for prompt distribution
.github/agents/                    ← source of truth for agent distribution

source/workflows/                  ← canonical workflow YAML source
source/hooks/                      ← canonical hook source

scripts/squad/
  sync-git-gh-standard.sh          ← bash sync script
  check-git-gh-standard.sh         ← bash check script
  sync-git-gh-standard.ps1         ← PowerShell sync script
  check-git-gh-standard.ps1        ← PowerShell check script

src/GitGhStandardCli/             ← C# CLI (primary implementation)
  Commands/
    SyncCommand.cs
    CheckCommand.cs
  Models/
    AssetCategory.cs               ← 7 category records with path metadata
    SyncOptions.cs
    CheckResult.cs
  Services/
    ManifestReader.cs
    FileSync.cs                    ← copy-if-distinct + EnsureExecutable
    PreservedPathGuard.cs          ← hard-coded never-touch guard (compiled)
    DirectoryEnsurer.cs
```

## How to use

### Prerequisites

The C# CLI requires .NET 10+. The bash/PS1 scripts work without .NET.

### Sync assets into a target repo

```bash
# C# CLI (primary, cross-platform)
dotnet run --project src/GitGhStandardCli -- \
  sync-git-gh-standard /absolute/path/to/target-repo \
  --source /path/to/squad-workflow-standard

# Bash fallback
bash scripts/squad/sync-git-gh-standard.sh /absolute/path/to/target-repo

# PowerShell fallback
pwsh scripts/squad/sync-git-gh-standard.ps1 /absolute/path/to/target-repo
```

### Validate drift and enforcement

```bash
# C# CLI
dotnet run --project src/GitGhStandardCli -- \
  check-git-gh-standard /absolute/path/to/target-repo \
  --source /path/to/squad-workflow-standard

# Bash fallback
bash scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo
```

Exit codes are automation-safe:

- `0` = in sync
- `2` = canonical source/version metadata missing
- `3` = version drift detected
- `4` = enforcement/adapter mismatch

### Dry-run sync (C# CLI only)

```bash
dotnet run --project src/GitGhStandardCli -- \
  sync-git-gh-standard /absolute/path/to/target-repo \
  --source /path/to/squad-workflow-standard \
  --dry-run
```

Prints what would be copied without writing any files.

### Optional source override (bash)

```bash
bash scripts/squad/sync-git-gh-standard.sh \
  /absolute/path/to/target-repo \
  --source-repo /absolute/path/to/canonical-repo

# or
export SQUAD_STANDARD_SOURCE_REPO=/absolute/path/to/canonical-repo
```

### Typical operator flow

1. Update canonical assets in this repo (skills, workflows, process doc, etc.)
2. Commit and push.
3. Run `sync-git-gh-standard` for each target repo.
4. Run `check-git-gh-standard` for each target repo.
5. Resolve any drift/enforcement failures before gated issue work proceeds.

### Local linting

```bash
npx --yes markdownlint-cli2 "**/*.md"
```

YAML linting is enforced in CI via `squad-lint-yaml.yml`.

## Implementation notes

- **Skills/instructions/prompts/agents** are read directly from `.github/` in this repo (no `source/skills/` mirror). The manifests control which files are distributed. This is Option B — single source of truth, no dual maintenance.
- **`squad.agent.md`** is intentionally excluded from the agent manifest — `squad init`/`squad upgrade` own it. Only `beast.agent.md` is distributed.
- **`PreservedPathGuard`** is a compiled C# guard — the never-touch list cannot be bypassed by a future manifest accident.
- **Hook executability** is restored on every sync via `FileSync.EnsureExecutable` (C# uses `File.SetUnixFileMode`; bash uses `chmod +x`).

