namespace GitGhStandardCli.Models;

internal sealed record SyncOptions(
    string SourceRepo,
    string TargetRepo,
    bool DryRun = false);
