---
name: "release-process"
description: "MyBlog-specific release coordination for dev→main promotion, manual tagging, and hotfix backports."
domain: "release-workflow"
confidence: "high"
status: "active"
source: "bound to MyBlog repo workflow"
---

## Release Process — MyBlog

Use this skill only for MyBlog release coordination.

### When to use

- Preparing a release PR from `dev` to `main`
- Tagging a release on `main`
- Creating optional GitHub Release notes for a tagged commit
- Releasing a `hotfix/*` branch and backporting it to `dev`

### When not to use

- Normal feature PRs to `dev`
- CI/CD workflow authoring or deployment automation changes
- Generic release design work across multiple repositories

### MyBlog release facts

- Authoritative steps live in `.squad/playbooks/release-myblog.md`
- Versioning is defined by `GitVersion.yml`
- The only active release-adjacent workflows today are `.github/workflows/ci.yml`
  and `.github/workflows/hotfix-backport-reminder.yml`
- MyBlog does **not** currently have `squad-release.yml`, `squad-promote.yml`,
  package publishing, or automated production deployment
- Release owner is Aragorn (approval, scope, gate) with Boromir (execution,
  workflow verification)

### Required output

A release task should leave behind one of these outcomes:

1. A reviewed `dev` → `main` release PR ready to merge
2. A tagged `main` commit with optional GitHub Release notes
3. A merged hotfix on `main` with a confirmed backport path to `dev`

### Related assets

- `.squad/playbooks/release-myblog.md`
- `GitVersion.yml`
- `.github/workflows/ci.yml`
- `.github/workflows/hotfix-backport-reminder.yml`
- `.squad/skills/release-process-base/SKILL.md` (quarantined; do not inject for
  normal MyBlog work)
