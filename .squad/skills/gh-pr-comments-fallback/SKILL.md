---
name: gh-pr-comments-fallback
description: Reliable PR comment retrieval when gh pr view --comments fails on deprecated GraphQL fields
domain: github-workflow
confidence: high
source: earned
tools:
  - name: gh api
    description: Queries REST endpoints for issue comments, review comments, and reviews
    when: When PR gate evidence is needed and gh pr view --comments errors
---

## Context

During PR gate work, `gh pr view --comments` may fail due to GraphQL field deprecations (for example, classic project card access). Gate flow still requires Copilot/Codecov and reviewer-comment evidence.

## Patterns

### Fallback Retrieval Sequence

1. Use issue conversation comments:
   - `gh api repos/{owner}/{repo}/issues/{pr}/comments --paginate`
2. Use inline review comments:
   - `gh api repos/{owner}/{repo}/pulls/{pr}/comments --paginate`
3. Use review summaries/states:
   - `gh api repos/{owner}/{repo}/pulls/{pr}/reviews --paginate`

### Gate Usage

- Prefer this fallback when `gh pr view --comments` returns GraphQL/projectCards errors.
- Use output to classify blockers (bug/security/process-contract) vs advisory notes.

## Anti-Patterns

- Assuming no review comments exist just because `gh pr view --comments` failed.
- Merging without reading Copilot inline comments due to CLI query failures.
