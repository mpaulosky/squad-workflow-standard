# Contributing to Squad Workflow Standard

Thank you for your interest in contributing to this repository. This project is a
workflow-standard distribution repo, so most changes affect canonical assets,
validation scripts, or the C# CLI that syncs those assets into downstream repos.

## Initial setup

### 1. Clone and restore

```bash
git clone https://github.com/mpaulosky/squad-workflow-standard.git
cd squad-workflow-standard
dotnet restore squad-workflow-standard.slnx
```

### 2. Prerequisites

- .NET 10 SDK
- A working `git` installation
- Optional but recommended: `yamllint` and `npx` for local lint checks

## What the local pre-push gate does

The repository’s pre-push hook runs before `git push` and currently enforces:

- branch naming validation,
- YAML lint for changed workflow files,
- Markdown lint for changed Markdown files,
- the full test suite via `dotnet test squad-workflow-standard.slnx --nologo`.

It does not currently run `dotnet format` or an explicit Release build step.

## Branch naming

Work should be done on a branch that matches one of these patterns:

```bash
git checkout -b squad/42-fix-login-validation
git checkout -b sprint/3-release-automation
```

The hook rejects pushes from `main`, `preview`, `dev`, and from branches that do
not match `squad/{issue-number}-{kebab-slug}`, `sprint/{n}-{kebab-slug}`, or
`hotfix/{kebab-slug}` for release-style hotfix flows.

## Local validation commands

Run these before pushing:

```bash
# Build the solution
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet build squad-workflow-standard.slnx \
  --configuration Release

# Run the tests
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet test squad-workflow-standard.slnx \
  --nologo

# Lint Markdown files
npx --yes markdownlint-cli2 "**/*.md"
```

If you changed workflow YAML, run `yamllint` locally when available.

## Development workflow

### Working on canonical assets

Most repository changes land in one of these areas:

- [.github/workflows](../.github/workflows) for canonical workflow YAML
- [.github/hooks](../.github/hooks) for canonical hook scripts
- [source/.squad/workflows](../source/.squad/workflows) for policy and manifests
- [scripts/squad](../scripts/squad) for sync and validation scripts
- [src/GitGhStandardCli](../src/GitGhStandardCli) for the C# sync/check CLI
- [tests/Unit.Tests](../tests/Unit.Tests) for regression and contract tests

### Creating a branch

Start from `dev` and create a branch with a descriptive issue number and slug:

```bash
git checkout dev
git pull origin dev
git checkout -b squad/42-fix-login-validation
```

### Pushing your work

Before pushing:

1. Verify your branch name.
2. Run the local validation commands above.
3. If everything passes, push normally:

```bash
git push
```

If the hook reports an issue, fix it and try again.

## Pull requests

Create a pull request from your `squad/*` or `sprint/*` branch to `dev`.

A good PR description should include:

- what changed,
- why it changed,
- any validation that was run.

## Bypassing the hook

Bypassing the pre-push hook with `git push --no-verify` is discouraged and should
only be used for a specific, well-understood reason. Prefer running the local
validation commands first.

## Code standards

- Keep changes focused and easy to review.
- Prefer small, explicit updates to canonical assets and scripts.
- Add or update tests when changing CLI behavior or sync logic.
- Follow standard .NET naming conventions and keep C# code readable.

## Resources

- [ARCHITECTURE.md](ARCHITECTURE.md) — high-level project structure and design goals
- [README.md](../README.md) — repository purpose and sync model

## Questions?

Open an issue or reach out to [@mpaulosky](https://github.com/mpaulosky/squad-workflow-standard).
