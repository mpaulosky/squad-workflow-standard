# Copilot instructions for `squad-workflow-standard`

## Build, test, and lint commands

This repository is a **workflow standard distribution repo**.
It does not contain an application build or unit-test suite of its own;
the primary executable checks are standard sync/validation scripts plus
CI lint workflows.

### Core validation commands

```bash
# Sync canonical workflow standard into a target repo
bash scripts/squad/sync-git-gh-standard.sh /absolute/path/to/target-repo

# Validate version + adapter enforcement in a target repo
bash scripts/squad/check-git-gh-standard.sh /absolute/path/to/target-repo
```

### Lint commands used by CI

```bash
# Markdown lint (same tool used in .github/workflows/squad-lint-markdown.yml)
npx --yes markdownlint-cli2 "**/*.md"

# YAML lint is enforced in CI by .github/workflows/squad-lint-yaml.yml (yamllint)
```

### Single-test guidance

There is no local test-project runner in this repository.
For targeted validation, run `check-git-gh-standard.sh` against one
specific target repository you are updating.

## High-level architecture

This repo is the **canonical source** for a reusable Git/GitHub workflow
standard pack that gets copied into other squad repositories.

1. Canonical policy and documentation live in:
   - `source/.squad/workflows/git-gh-process-standard.md`
   - `source/.squad/workflows/README.md`
   - `source/.squad/skills/git-workflow-standard/SKILL.md`
2. `source/.squad/workflows/workflow-baseline-manifest.txt` defines which
   `source/workflows/*.yml` files are centrally managed and must be synced
   to target repos at `.github/workflows/`.
3. `source/.squad/workflows/hook-baseline-manifest.txt` defines which
   `source/hooks/*` files are centrally managed and must be synced to target
   repos at `.github/hooks/`.
4. `scripts/squad/sync-git-gh-standard.sh` copies canonical
   files/workflows/hooks into a target repo and writes
   `.squad/workflows/.git-gh-standard-version`.
5. `scripts/squad/check-git-gh-standard.sh` detects version drift and
   verifies required enforcement adapters in target repo files
   (`.squad/routing.md`, `.squad/ceremonies.md`,
   `.squad/templates/issue-lifecycle.md`,
   `.squad/skills/git-workflow-standard/SKILL.md`).

Treat this repository as a **standard pack publisher**, not as an app/runtime repository.

## Key conventions

- `Standard-Version:` in `git-gh-process-standard.md` is the authoritative
  version value; synced repos must match it via
  `.git-gh-standard-version` and skill/template bindings.
- Branch/PR policy defined by the standard is hard-gated:
  no direct pushes to `main`/`dev`, issue branches use
  `squad/{issue-number}-{kebab-slug}`, PR target is `main`.
- Flow selection is explicit policy:
  single issue uses standard branch flow;
  concurrent issues use worktree flow.
- If you add/remove centrally managed workflows, update
  `workflow-baseline-manifest.txt` in the same change.
- `check-git-gh-standard.sh` exit codes are contractually meaningful for
  automation (`0` ok, `2` canonical metadata/source missing, `3` drift,
  `4` adapter/enforcement failure).
