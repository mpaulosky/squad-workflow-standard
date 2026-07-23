# squad-workflow-standard

Canonical distribution repository for Squad Git/GitHub workflow standards.  
This repo publishes the standard pack that other repositories sync and enforce.

## What this repository does

1. Defines the canonical workflow policy
   (branch model, PR flow, hard gates, cleanup).
2. Distributes policy assets into target repositories.
3. Validates drift and enforcement wiring in target repositories.
4. Ships a centrally managed GitHub Actions baseline for squad
   automation/linting/release ceremonies.

## Project structure

- `source/.squad/workflows/git-gh-process-standard.md`:
  Canonical process standard (`Standard-Version` source of truth).
- `source/.squad/workflows/README.md`:
  Rollout/retrofit playbook for adopting the standard in target repos.
- `source/.squad/workflows/workflow-baseline-manifest.txt`:
  List of centrally managed workflow files to sync into target repos.
- `source/.squad/workflows/hook-baseline-manifest.txt`:
  List of centrally managed hook files to sync into target repos.
- `source/.squad/skills/git-workflow-standard/SKILL.md`:
  Skill-facing executable guidance bound to the same standard version.
- `scripts/squad/sync-git-gh-standard.sh`:
  Syncs standard assets + baseline workflows into a target repo.
- `scripts/squad/check-git-gh-standard.sh`:
  Checks version drift and required adapter/enforcement bindings.
- `source/workflows/*.yml`:
  Canonical workflow YAML source that syncs into target
  `.github/workflows/`.
- `source/hooks/*`:
  Canonical hook source that syncs into target `.github/hooks/`.

## Core functionality

### 1. Standard pack publishing

The canonical pack consists of:

- process standard + rollout README
- skill binding file
- baseline workflow + hook manifests
- workflow YAML files listed in the manifest
- hook files listed in the hook manifest

### 2. Target-repo synchronization

`sync-git-gh-standard.sh` copies canonical assets into a target repository and writes:

- `.squad/workflows/.git-gh-standard-version`

It also syncs all workflow files listed in `workflow-baseline-manifest.txt`.
It also syncs all hook files listed in `hook-baseline-manifest.txt`.

### 3. Drift + enforcement validation

`check-git-gh-standard.sh` verifies:

- local standard version matches canonical `Standard-Version`
- required adapter files exist and contain required hard-gate bindings
- baseline workflow files match canonical copies

Exit codes are automation-safe:

- `0` = in sync
- `2` = canonical source/version metadata missing
- `3` = version drift detected
- `4` = enforcement/adapter mismatch

### 4. Baseline GitHub automation

The workflow set includes:

- linting (`squad-lint-yaml.yml`, `squad-lint-markdown.yml`)
- test/release templates (`squad-ci.yml`, `squad-test.yml`, `squad-release.yml`)
- policy guards (`squad-main-from-dev-guard.yml`)
- labeling/triage/assignment automation
- project board automation/audit
- milestone/release blog orchestration
- CodeQL + code metrics + Dependabot auto-merge variants

## How to use this repository

### Prerequisites

- `bash`
- `git`
- `gh` (for PR-driven workflows in target repos)

### Sync the standard into a target repo

```bash
bash scripts/squad/sync-git-gh-standard.sh /absolute/path/to/target-repo
```

```powershell
pwsh scripts/squad/sync-git-gh-standard.ps1 /absolute/path/to/target-repo
```

Optional canonical source override:

```bash
bash scripts/squad/sync-git-gh-standard.sh \
  /absolute/path/to/target-repo \
  --source-repo /absolute/path/to/canonical-repo
```

or set:

```bash
export SQUAD_STANDARD_SOURCE_REPO=/absolute/path/to/canonical-repo
```

### Validate drift and enforcement in a target repo

```bash
bash scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo
```

```powershell
pwsh scripts/squad/check-git-gh-standard.ps1 /absolute/path/to/target-repo
```

### Optional .NET CLI wrapper (coexists with scripts)

```bash
dotnet run --project src/GitGhStandardCli -- sync-git-gh-standard /absolute/path/to/target-repo
dotnet run --project src/GitGhStandardCli -- \
  check-git-gh-standard /absolute/path/to/target-repo
```

### Typical operator flow

1. Update canonical files in this repo.
2. Commit and push changes.
3. Run `sync-git-gh-standard.sh` for each target repo.
4. Run `check-git-gh-standard.sh` for each target repo.
5. Resolve any drift/enforcement failures before issue-work PRs proceed.

### Local linting (matching CI tools)

```bash
npx --yes markdownlint-cli2 "**/*.md"
```

YAML linting is enforced in CI via `squad-lint-yaml.yml`
(yamllint rules embedded in workflow config).
