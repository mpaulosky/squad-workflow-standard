namespace GitGhStandardCli.Models;

/// <summary>
/// Result of a drift and adapter enforcement check.
/// Exit codes: 0 = ok, 2 = source missing, 3 = version drift, 4 = adapter/content failure.
/// </summary>
internal sealed record CheckResult(
    int ExitCode,
    IReadOnlyList<string> Messages);
