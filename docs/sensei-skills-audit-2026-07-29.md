---
post_title: "Sensei Skills Audit 2026-07-29"
author1: "copilot"
post_slug: "sensei-skills-audit-2026-07-29"
microsoft_alias: "n/a"
featured_image: "n/a"
categories:
  - engineering
tags:
  - sensei
  - skills
  - compliance
ai_note: "Generated with GitHub Copilot"
summary: "Frontmatter and trigger-routing compliance snapshot for .squad/skills after targeted remediation."
post_date: "2026-07-29"
---

## Scope

Reviewed all skill files under .squad/skills and focused remediation on files that lacked YAML frontmatter.

## Before and After

| Metric | Before | After |
| --- | ---: | ---: |
| Skills scanned | 26 | 26 |
| Skills with frontmatter | 21 | 26 |
| Skills missing frontmatter | 5 | 0 |
| Skills with WHEN triggers | 4 | 26 |
| Skills with INVOKES | 0 | 26 |
| Skills with DO NOT USE FOR | 0 | 0 |
| Descriptions >= 150 chars | 11 | 26 |
| Descriptions <= 60 words | 21 | 26 |

## Files Remediated

- .squad/skills/auth0-management-api/SKILL.md
- .squad/skills/auth0-management-security/SKILL.md
- .squad/skills/copilot-sdk-csharp-usage/SKILL.md
- .squad/skills/labels-feature-patterns/SKILL.md
- .squad/skills/merged-pr-guard/SKILL.md
- .squad/skills/blazor-tailwind-theme-persistence/SKILL.md
- .squad/skills/building-protection/SKILL.md
- .squad/skills/build-repair/SKILL.md
- .squad/skills/copilot-review-outdated-filter/SKILL.md
- .squad/skills/gh-pr-comments-fallback/SKILL.md
- .squad/skills/git-workflow-standard/SKILL.md
- .squad/skills/issue-branch-alignment/SKILL.md
- .squad/skills/microsoft-code-reference/SKILL.md
- .squad/skills/mongodb-dba-patterns/SKILL.md
- .squad/skills/mongodb-filter-pattern/SKILL.md
- .squad/skills/post-build-validation/SKILL.md
- .squad/skills/pre-push-test-gate/SKILL.md
- .squad/skills/release-process-base/SKILL.md
- .squad/skills/release-process/SKILL.md
- .squad/skills/self-authored-pr-gate/SKILL.md
- .squad/skills/sprint-planning/SKILL.md
- .squad/skills/squad-conventions/SKILL.md
- .squad/skills/static-config-pattern/SKILL.md
- .squad/skills/testcontainers-shared-fixture/SKILL.md
- .squad/skills/unit-test-conventions/SKILL.md
- .squad/skills/webapp-testing/SKILL.md

## Residual Gaps

- No open frontmatter routing compliance gaps remain.

## Recommended Next Pass

1. Expand the lightweight harness to assert additional domain-specific trigger phrases for more skills.
2. Add a CI step that runs the filtered skill routing tests (`SkillRoutingFrontmatterTests`) on every PR.

## Harness Added

- Test file: `tests/SquadWorkflowStandard.Tests/SkillRoutingFrontmatterTests.cs`
- Verifies every `.squad/skills/*/SKILL.md` description includes `WHEN:` and `INVOKES:`
- Verifies no description includes `DO NOT USE FOR:`
- Verifies at least three quoted `WHEN` trigger phrases per skill
- Verifies stable trigger phrases for top-priority skills:
  - `git-workflow-standard`
  - `build-repair`
  - `pre-push-test-gate`
  - `release-process`
  - `sprint-planning`
  - `merged-pr-guard`

## CI Integration

- Added explicit workflow job: `Skill Routing Frontmatter`
- Files updated:
  - `source/workflows/squad-test.yml`
  - `.github/workflows/squad-test.yml`
- Job command:
  - `dotnet test tests/SquadWorkflowStandard.Tests/SquadWorkflowStandard.Tests.csproj --filter "FullyQualifiedName~SkillRoutingFrontmatterTests" --configuration Release`
