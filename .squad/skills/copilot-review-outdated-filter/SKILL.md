---
name: copilot-review-outdated-filter
description: Filter outdated Copilot review suggestions after merge conflicts or rebases to focus on still-applicable feedback
domain: github-workflow
confidence: high
source: earned
tools:
  - name: github-github-mcp-server-pull_request_read
    description: Fetch PR review threads with is_outdated flag
    when: After conflict resolution or rebase to determine which suggestions still apply
---

## Context

After resolving merge conflicts or rebasing a PR branch, many Copilot review suggestions become **outdated** because they reference line numbers and file states that no longer exist. Copilot marks these as `is_outdated: true` in the review thread metadata.

**Problem:** Addressing outdated suggestions wastes time and may introduce incorrect changes based on stale context.

**Solution:** Filter review threads by `is_outdated` flag and focus only on still-applicable suggestions.

## Pattern

### Step 1 — Fetch Review Threads

After conflict resolution or rebase:

```bash
# Using GitHub CLI
gh pr view {PR_NUMBER} --json reviews

# Or via MCP tool
github-github-mcp-server-pull_request_read(
  method: "get_review_comments",
  owner: "{owner}",
  repo: "{repo}",
  pullNumber: {number}
)
```

### Step 2 — Parse and Filter

Review threads have three key fields:

- `is_outdated`: Boolean — `true` if file/line changed since review
- `is_resolved`: Boolean — `true` if author resolved the thread
- `is_collapsed`: Boolean — UI hint (not actionable)

**Filter logic:**

```javascript
// Keep only non-outdated, unresolved threads
const actionable = review_threads.filter(t => 
  !t.is_outdated && !t.is_resolved
);

// Count what you're skipping
const outdated = review_threads.filter(t => t.is_outdated).length;
console.log(`Skipped ${outdated} outdated suggestions`);
```

### Step 3 — Address Still-Applicable Suggestions

For each actionable thread:

1. Read the current file state (not the diff view)
2. Verify the suggestion still makes sense
3. Apply the fix if valid
4. Commit with reference: `"docs: resolve Copilot review suggestions on PR #{number}"`

### Step 4 — Document Outcomes

In PR comment or commit message:

```markdown
## Copilot Suggestions Resolved

**Fixed:** {N} still-applicable suggestions
**Skipped:** {M} outdated suggestions after {conflict-resolution|rebase}

### Fixed Items
1. {file}: {what was changed}
2. ...

### Skipped (outdated)
- {M} review threads marked stale after {commit SHA}
```

## Examples

### Example 1 — Post-Conflict Filter (PR #17)

**Scenario:** Gandalf resolved merge conflicts on PR #17 with commit 89bcf1c. Many Copilot suggestions became outdated.

**Action:**

```javascript
// Fetch reviews
const threads = pullRequest.getReviewComments();

// Filter
const current = threads.filter(t => !t.is_outdated);
const stale = threads.filter(t => t.is_outdated);

console.log(`${current.length} still apply, ${stale.length} outdated`);
// Output: "2 still apply, 24 outdated"

// Fix only current ones
current.forEach(thread => {
  // Apply suggestion to current file state
  applyFix(thread.path, thread.body);
});
```

**Outcome:** Fixed 2 suggestions (YAML front matter, legacy warning banner), skipped 24 outdated ones.

## Key Considerations

**When to skip outdated suggestions:**

- ✅ File content changed significantly (conflict resolution, rebase)
- ✅ Line numbers shifted due to insertions/deletions
- ✅ Copilot marked `is_outdated: true` automatically

**When to still address outdated suggestions:**

- ⚠️ Suggestion highlights conceptual issue (e.g., "missing validation") even if line changed
- ⚠️ Same issue exists in new code (e.g., "no error handling" still true after rebase)
- Use judgment: outdated ≠ automatically invalid

**Fresh re-review cycle:**

- After addressing current suggestions, push commit
- CI runs + Copilot re-reviews on new commit SHA
- New review catches any issues missed by filtering

## Anti-Patterns

❌ **Manually applying outdated suggestions** — Suggestion references line 42, but file now has 100 lines after rebase

❌ **Ignoring all Copilot suggestions post-conflict** — Some suggestions are still valid (e.g., metadata issues, repo-fit warnings)

❌ **Resolving threads without fixing** — Let GitHub track outdated status; don't manually resolve stale ones

## Success Criteria

✅ Only non-outdated suggestions addressed  
✅ Commit message documents fix count + skip count  
✅ PR comment explains what was fixed vs skipped  
✅ CI passing before requesting human re-review  
✅ Fresh Copilot review cycle triggered on new commit

## Related Patterns

- **PR Merge Process** — `.squad/playbooks/pr-merge-process.md` (Critical Rule #8: read Copilot review before verdict)
- **Pre-Push Test Gate** — `.squad/skills/pre-push-test-gate/SKILL.md` (verify fixes locally before push)
- **Skill Template** — `.squad/templates/skill.md` (metadata requirements)

## Reference

- GitHub PR Review API: `is_outdated` field — https://docs.github.com/en/rest/pulls/reviews
- Copilot PR Review: https://docs.github.com/en/copilot/using-github-copilot/code-review/using-copilot-code-review
- `.squad/agents/aragorn/charter.md` — Critical Rule #8 (read Copilot review before posting verdict)
