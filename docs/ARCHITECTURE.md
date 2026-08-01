---
post_title: "Project Architecture Outline"
author1: "GitHub Copilot"
post_slug: "architecture-outline"
microsoft_alias: "copilot"
featured_image: "https://example.com/featured-image.png"
categories:
  - "engineering"
tags:
  - "architecture"
  - "squad"
  - "workflow-standard"
ai_note: "Yes"
summary: "High-level architecture outline for the squad-workflow-standard repository."
post_date: "2026-08-01"
---

## Overview

This repository is a distribution and governance layer for the Squad workflow standard. It does not host a user-facing application; instead, it publishes canonical workflow assets, automation rules, and supporting guidance for other repositories to consume.

## Primary responsibilities

- Publish a reusable Git and GitHub workflow standard.
- Sync canonical assets into downstream repositories without overwriting local project-specific customizations.
- Enforce branch, PR, and release policy through scripts, hooks, and GitHub Actions.
- Provide a testable validation surface for drift detection and policy compliance.

## High-level architecture

### 1. Canonical source of truth

The repository is organized around a small set of authoritative assets under the source tree:

- [source/workflows](../source/workflows) contains the canonical workflow YAML templates.
- [source/hooks](../source/hooks) contains the canonical Git hooks.
- [source/.squad/workflows](../source/.squad/workflows) contains policy documents and manifest definitions that drive synchronization behavior.
- [source/.squad/skills](../source/.squad/skills) contains the canonical Squad skills used by the standard.

These files define the policy and templates that other repositories receive.

### 2. Distribution layer

The distribution layer is implemented through scripts and the C# CLI:

- [scripts/squad/sync-git-gh-standard.sh](../scripts/squad/sync-git-gh-standard.sh) and [scripts/squad/sync-git-gh-standard.ps1](../scripts/squad/sync-git-gh-standard.ps1) provide shell-based sync entry points.
- [src/GitGhStandardCli](../src/GitGhStandardCli) contains the primary cross-platform implementation of the sync and validation workflow.

The CLI reads manifests, copies approved assets into a target repository, and preserves files that belong to the target project.

### 3. Validation and enforcement layer

Validation is handled by the check workflow and the companion CLI commands:

- [scripts/squad/check-git-gh-standard.sh](../scripts/squad/check-git-gh-standard.sh) and [scripts/squad/check-git-gh-standard.ps1](../scripts/squad/check-git-gh-standard.ps1) provide script-based validation.
- [src/GitGhStandardCli/Commands/CheckCommand.cs](../src/GitGhStandardCli/Commands/CheckCommand.cs) implements the equivalent logic in C#.

This layer verifies:

- whether the canonical source is present,
- whether the target repository has drifted from the canonical standard,
- whether required enforcement adapters are present.

### 4. Preservation and safety boundaries

A key architectural principle is that local repository-specific files remain intact. The system uses a guard mechanism to prevent accidental overwrite of protected paths such as:

- team configuration,
- local routing and ceremonies,
- agent charters and histories,
- local project-specific Copilot instructions.

This is a critical safety feature because the repository is intended to augment, not replace, project-local state.

## Runtime flow

### Sync flow

1. The operator or automation identifies a target repository.
2. The sync command reads manifests and source assets.
3. The system copies managed files into the target repository.
4. The system preserves protected paths and restores executable permissions where required.

### Validation flow

1. The check command inspects the target repository against the canonical standard.
2. It compares version metadata, file presence, and policy adapters.
3. It returns an exit code that can be used by automation and CI.

## Key components

### Command-line interface

The C# CLI is the main implementation surface for orchestration. Its responsibilities include:

- parsing commands,
- reading manifests,
- performing file copy and preserve operations,
- reporting deterministic validation results.

### Manifest-driven sync

Manifests are the architectural glue between the canonical repo and downstream repositories. They define which files are managed and which are excluded from overwrites.

### Tests

The test suite under [tests/Unit.Tests](../tests/Unit.Tests) validates:

- CLI behaviors,
- script integration,
- synchronization contracts,
- workflow-specific policy expectations.

## Design principles

- Separation of concerns between policy, distribution, and enforcement.
- Manifest-driven change management rather than hard-coded one-off copies.
- Safety-first behavior with explicit preservation rules.
- Automation-friendly validation with stable exit codes.
- Cross-platform compatibility for both shell and .NET implementations.

## Architectural summary

In short, this project acts as a policy publisher and synchronization engine. Its architecture is centered on a small number of canonical assets, a manifest-driven distribution mechanism, and a validation layer that ensures downstream repositories stay aligned with the standard while preserving local customizations.
