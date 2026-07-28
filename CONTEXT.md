# CONTEXT.md — squad-workflow-standard

## Purpose

`squad-workflow-standard` is a **standard-pack publisher** — a canonical distribution
repository that syncs a standardized set of GitHub workflows, Copilot skills, instructions,
prompts, agents, and Squad skills into target repos after `git init` + `squad init`.

It is not an application runtime. It has no deployable service. Its primary output is a
CLI tool (`GitGhStandardCli`) and a set of source files that are distributed into target
repos.

---

## Domain Vocabulary

### standard-pack

The collection of files this repo distributes into target repos. The standard-pack
consists of five asset categories: workflows, hooks, skills, instructions, prompts,
agents, and squad-skills.

### managed-file

A file that is owned by this standard-pack. Managed files are listed in a manifest.
On sync, managed files in the target repo are overwritten if their content differs from
the canonical source. Managed files MUST NOT be manually edited in target repos —
changes will be overwritten on the next sync.

### preserved-file

A file that is owned by the target repo's team and MUST NEVER be touched by sync.
Preserved files are typically user-configured adapters or Squad-owned state.

**Hard preserved list (never synced, never checked for drift):**

- `.squad/routing.md`
- `.squad/ceremonies.md`
- `.squad/templates/issue-lifecycle.md`
- `.squad/team.md`
- `.squad/decisions.md`
- `.squad/agents/**` (entire tree)
- `.squad/templates/**` (entire tree — squad upgrade territory)
- `.github/copilot-instructions.md` (top-level only; `.github/instructions/copilot-instructions.md` IS managed)

### target-repo

A repository that has been initialized with `git init` + `squad init` and then had this
standard-pack applied via `sync`. Target repos receive managed files and are checked for
drift by `check`.

### squad-init boundary

`squad init` installs a baseline set of files into a target repo:
- 4 GitHub workflows (heartbeat, issue-assign, triage, sync-squad-labels)
- 19 skills in `.github/skills/`
- `squad.agent.md` in `.github/agents/`
- 4 always-on agent dirs in `.squad/agents/` (scribe, ralph, Rai, fact-checker)
- `.squad/` scaffold: team.md, routing.md, ceremonies.md, decisions.md, templates/

This standard-pack adds on top of that baseline. It does NOT replace `squad init`.
The standard-pack's version of overlapping skills (e.g., `git-workflow`) supersedes
the `squad init` baseline version — the standard-pack is authoritative.

### sync

The operation that copies managed files from this repo into a target repo, creating
directories as needed. Sync is idempotent: running it multiple times on the same target
produces the same result. Sync never touches preserved files.

### drift

The condition where a managed file in a target repo has diverged from the canonical
source in this repo. Drift is detected by the `check` command (byte-for-byte comparison).
Drift exits with code `3`.

### enforcement-adapter

A block of text that must be present in a target repo file to confirm that the target
team has opted into a required workflow policy. Enforcement adapters are checked by the
`check` command. Missing adapters exit with code `4`.

**Required adapters:**

| File | Required block |
|---|---|
| `.squad/routing.md` | `Standard-Version:` binding |
| `.squad/ceremonies.md` | `Standard-Version:` binding |
| `.squad/templates/issue-lifecycle.md` | `Standard-Version:` binding |
| `.squad/skills/git-workflow-standard/SKILL.md` | Presence of the skill itself |

### version-stamp

The file `.squad/workflows/.git-gh-standard-version` written into the target repo after
a successful sync. Contains the version string from
`source/.squad/workflows/git-gh-process-standard.md` (`Standard-Version:` field).
The `check` command compares this stamp to the canonical version to detect version drift.

### asset-category

One of the five logical groups of files managed by the standard-pack:

| Category | Source root (this repo) | Target root |
|---|---|---|
| Workflows | `source/workflows/` | `.github/workflows/` |
| Hooks | `source/hooks/` | `.github/hooks/` |
| Skills | `.github/skills/` | `.github/skills/` |
| Instructions | `.github/instructions/` | `.github/instructions/` |
| Prompts | `.github/prompts/` | `.github/prompts/` |
| Agents | `.github/agents/` | `.github/agents/` |
| Squad Skills | `source/.squad/skills/` | `.squad/skills/` |

> Workflows and Hooks read from `source/` because they are not used locally in this repo.
> Skills, Instructions, Prompts, and Agents read directly from `.github/` — Option B,
> no dual maintenance.

### dry-run

A flag (`--dry-run`) passed to the `sync` command. In dry-run mode, sync reports what
it would do but writes no files. Used for previewing changes before applying them.

---

## Architecture

```
squad-workflow-standard/
├── source/                        # Distributable source files
│   ├── workflows/                 # GitHub workflow files (→ .github/workflows/)
│   ├── hooks/                     # Git hooks (→ .github/hooks/)
│   └── .squad/
│       ├── workflows/             # Manifests + process standard
│       └── skills/                # Squad skills (→ .squad/skills/)
├── .github/                       # Used both locally and as source for Skills/Instructions/Prompts/Agents
│   ├── skills/                    # → target .github/skills/
│   ├── instructions/              # → target .github/instructions/
│   ├── prompts/                   # → target .github/prompts/
│   └── agents/                    # → target .github/agents/ (beast.agent.md only)
├── src/GitGhStandardCli/          # C# CLI — primary implementation
│   ├── Commands/                  # SyncCommand, CheckCommand
│   ├── Models/                    # AssetCategory (record), SyncOptions, CheckResult
│   └── Services/                  # ManifestReader, FileSync, PreservedPathGuard, DirectoryEnsurer
├── scripts/squad/                 # Bash/PS1 — independent fallback implementation
└── tests/SquadWorkflowStandard.Tests/
```

---

## Invariants

1. **Sync is always safe to re-run.** Idempotent. No side effects on preserved files.
2. **`PreservedPathGuard` is compiled.** Cannot be bypassed by manifest accident.
3. **Exit code contract is stable:** `0` ok, `2` source missing, `3` drift, `4` adapter failure.
4. **"STATUS: OK"** appears in `check` stdout on exit code `0`. Existing tooling asserts this string.
5. **`squad.agent.md` is never distributed.** Only `beast.agent.md` is in the agent manifest.
6. **`.github/copilot-instructions.md` (top-level) is never distributed.** Only `.github/instructions/copilot-instructions.md` is managed.
