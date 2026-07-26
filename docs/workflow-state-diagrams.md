---
post_title: Current Workflow State Diagrams
author1: Inara
post_slug: current-workflow-state-diagrams
microsoft_alias: unknown
featured_image: https://via.placeholder.com/1200x630?text=Workflow+Diagrams
categories:
  - General
tags:
  - workflow
  - github-actions
  - branching
  - documentation
ai_note: Drafted with AI assistance for internal repository documentation.
summary: Two diagrams describing the current branch promotion and automation PR flows used by the workflow standard.
post_date: 2026-07-26
---

## Purpose

This internal note captures the current branch movement and automation-driven
pull request paths used by the workflow standard as of 2026-07-26.

## Branch Promotion Flow

```mermaid
flowchart LR
  work["Issue work branch\nsquad/{issue-number}-{slug}"] -->|PR| dev[dev]
  dev -->|Sanitize and stage| promote["automation/promote-preview\npreview-derived promotion branch"]
  promote -->|PR| preview[preview]
  preview -->|PR| main[main]
  main -->|Backmerge PR| dev

  hotfix["Hotfix branch\nhotfix/{slug}"] -->|PR| main
  backport["Hotfix backport branch\nhotfix/backport-{pr-number}"] -->|PR| dev
  main -. reminder .-> backport
```

## Automation PR Flow

```mermaid
flowchart TD
  blog["Blog sync workflow\nautomation/blog-readme-sync"] -->|PR to dev| dev[dev]
  readme["README sync workflow\nautomation/sync-readme"] -->|PR to main| main[main]
  work[Regular issue work] -->|PR to dev| dev
  dev -->|Promote workflow prepares sanitized branch| promote[automation/promote-preview]
  promote -->|Only sanctioned PR source for preview| preview[preview]
  preview -->|PR| main
  main -->|Backmerge PR| dev
  hotfix["Hotfix branch\nhotfix/{slug}"] -->|PR to main| main
  backport["Hotfix backport branch\nhotfix/backport-{pr-number}"] -->|PR to dev| dev
  main -. reminder .-> backport
```

## Notes

- Normal delivery starts on a work branch and merges into `dev` by pull
  request.
- Promotion from `dev` to `preview` is workflow-driven through the sanitized
  `automation/promote-preview` branch, then merged by pull request into
  `preview`.
- PRs into `preview` are guarded so the sanctioned promotion branch is the only
  allowed source.
- Promotion from `preview` to `main` happens by pull request.
- PRs into `main` are guarded to allow `preview` for standard releases, with
  `hotfix/*` as the explicit exception path.
- Changes that reach `main` are returned to `dev` through a backmerge pull
  request.
- Blog sync automation targets `dev`, while README sync automation targets
  `main`.
- Hotfixes may go to `main` by pull request, then are backported to `dev` by a
  separate pull request from `hotfix/backport-{pr-number}`.
