# Squad Team

> squad-workflow-standard

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Mal | Lead | .squad/agents/mal/charter.md | 🏗️ Active |
| Zoe | Backend/Automation Dev | .squad/agents/zoe/charter.md | 🔧 Active |
| Wash | DevOps Engineer | .squad/agents/wash/charter.md | ⚙️ Active |
| Kaylee | QA Engineer | .squad/agents/kaylee/charter.md | 🧪 Active |
| Inara | Docs/Process Writer | .squad/agents/inara/charter.md | 📝 Active |
| Scribe | Session Logger | .squad/agents/scribe/charter.md | 📋 Active |
| Ralph | Work Monitor | .squad/agents/ralph/charter.md | 🔄 Active |
| Rai | RAI Reviewer | .squad/agents/Rai/charter.md | 🛡️ Active |
| Fact Checker | Verifier | .squad/agents/fact-checker/charter.md | 🔍 Active |


## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs
- Boilerplate/scaffolding generation
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and system design
- Multi-system integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical changes (auth, encryption, access control)
- Performance-critical paths requiring benchmarking
- Changes requiring cross-team discussion

## Project Context

- **Project:** squad-workflow-standard
- **Created:** 2026-07-23
- **Requested by:** mpaulosky
- **Focus:** Standardized post-init Squad automation for workflows, hooks, and Git/GitHub process across repositories using bash, pwsh, and C#.

## Issue Source

| Field | Value |
|------|-------|
| **Repository** | mpaulosky/squad-workflow-standard |
| **Connected at** | 2026-07-23T08:38:36.263-07:00 |
| **Mode** | GitHub Issues |
| **Auto-assign @copilot** | false |
