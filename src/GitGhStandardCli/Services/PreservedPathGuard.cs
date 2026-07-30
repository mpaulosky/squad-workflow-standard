namespace GitGhStandardCli.Services;

/// <summary>
/// Guards against syncing files that are owned by the target repo's team.
/// These files are either squad-managed state or user-configured adapters.
/// </summary>
internal static class PreservedPathGuard
{
    /// <summary>
    /// Target-relative paths that are NEVER overwritten by sync.
    /// Prefix entries (ending with '/') match any path beneath that directory.
    /// </summary>
    private static readonly string[] PreservedPaths =
    [
        ".squad/routing.md",
        ".squad/ceremonies.md",
        ".squad/templates/issue-lifecycle.md",
        ".squad/team.md",
        ".squad/decisions.md",
        ".squad/agents/",      // entire tree
        ".squad/templates/",   // entire tree — squad upgrade territory
        ".github/copilot-instructions.md",  // top-level only; .github/instructions/ is managed
    ];

    /// <summary>
    /// Returns true if the target-relative path is preserved and must not be overwritten.
    /// </summary>
    public static bool IsPreserved(string targetRelativePath)
    {
        // Normalize separators for cross-platform comparison.
        var normalized = targetRelativePath.Replace('\\', '/');

        return PreservedPaths.Any(p =>
            p.EndsWith('/')
                ? normalized.StartsWith(p, StringComparison.OrdinalIgnoreCase)
                : normalized.Equals(p, StringComparison.OrdinalIgnoreCase));
    }
}
