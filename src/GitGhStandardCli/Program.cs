using System.Diagnostics;
using System.Runtime.InteropServices;
using GitGhStandardCli.Commands;
using GitGhStandardCli.Models;

static int PrintUsage()
{
    Console.WriteLine(
        """
        Usage:
          git-gh-standard-cli sync-git-gh-standard <target-repo> [--source <source-repo>] [--dry-run]
          git-gh-standard-cli check-git-gh-standard <target-repo> [--source <source-repo>]
          git-gh-standard-cli sync-mesh [--init] [mesh.json]

        Options:
          --source <path>    Path to the canonical source repo (defaults to auto-detected repo root)
          --dry-run          Preview changes without writing files (sync only)

        Environment:
          SQUAD_STANDARD_SOURCE_REPO    Alternative way to specify the source repo path

        Exit codes (sync/check):
          0  OK
          2  Canonical source missing
          3  Version drift detected
          4  Adapter or content enforcement failure
        """);
    return 1;
}

/// <summary>
/// Walks up from <paramref name="start"/> to find the standard-pack repo root.
/// Looks for the canonical version file as the marker.
/// </summary>
static string? FindSourceRepoRoot(string start)
{
    var current = new DirectoryInfo(Path.GetFullPath(start));
    while (current is not null)
    {
        var markerPath = Path.Combine(
            current.FullName, "source", ".squad", "workflows", "git-gh-process-standard.md");
        if (File.Exists(markerPath))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return null;
}

static string? FindGitRepoRoot(string start)
{
    var current = new DirectoryInfo(Path.GetFullPath(start));
    while (current is not null)
    {
        var gitPath = Path.Combine(current.FullName, ".git");
        if (Directory.Exists(gitPath) || File.Exists(gitPath))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return null;
}

/// <summary>
/// Parses common sync/check arguments: target path + optional --source and --dry-run.
/// Returns false if parsing fails.
/// </summary>
static bool ParseSyncCheckArgs(
    string[] remaining,
    out string targetRepo,
    out string? sourceOverride,
    out bool dryRun)
{
    targetRepo = string.Empty;
    sourceOverride = null;
    dryRun = false;

    for (var i = 0; i < remaining.Length; i++)
    {
        switch (remaining[i])
        {
            case "--source" or "--source-repo":
                if (i + 1 >= remaining.Length)
                {
                    Console.Error.WriteLine($"Missing value for {remaining[i]}");
                    return false;
                }
                sourceOverride = remaining[++i];
                break;

            case "--dry-run":
                dryRun = true;
                break;

            default:
                if (!remaining[i].StartsWith("--"))
                {
                    if (!string.IsNullOrEmpty(targetRepo))
                    {
                        Console.Error.WriteLine($"Unexpected argument: {remaining[i]}");
                        return false;
                    }
                    targetRepo = remaining[i];
                }
                else
                {
                    Console.Error.WriteLine($"Unknown option: {remaining[i]}");
                    return false;
                }
                break;
        }
    }

    return !string.IsNullOrEmpty(targetRepo);
}

// ── Entry point ──────────────────────────────────────────────────────────────

if (args.Length == 0)
{
    return PrintUsage();
}

var command = args[0];

if (command is "sync-git-gh-standard" or "check-git-gh-standard")
{
    var remaining = args.Skip(1).ToArray();

    if (!ParseSyncCheckArgs(remaining, out var targetRepo, out var sourceOverride, out var dryRun))
    {
        PrintUsage();
        return 1;
    }

    // Resolve source repo: --source flag > env var > auto-detect walking up from cwd.
    var sourceRepo =
        sourceOverride
        ?? Environment.GetEnvironmentVariable("SQUAD_STANDARD_SOURCE_REPO")
        ?? FindSourceRepoRoot(Environment.CurrentDirectory);

    if (sourceRepo is null)
    {
        Console.Error.WriteLine(
            "Unable to locate the standard-pack source repository. " +
            "Run this command from within the repository or provide --source <path>.");
        return 2;
    }

    sourceRepo = Path.GetFullPath(sourceRepo);
    targetRepo = Path.GetFullPath(targetRepo);

    if (command == "sync-git-gh-standard")
    {
        return SyncCommand.Run(new SyncOptions(sourceRepo, targetRepo, dryRun));
    }

    return CheckCommand.Run(sourceRepo, targetRepo);
}

if (command == "sync-mesh")
{
    // sync-mesh delegates to the bash/PS1 script — it is not implemented in C#.
    var gitRepoRoot = FindGitRepoRoot(Environment.CurrentDirectory);
    if (gitRepoRoot is null)
    {
        Console.Error.WriteLine("Unable to locate repository root (.git). Run this command from within the repository.");
        return 1;
    }

    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    var forwarded = args.Skip(1).ToArray();

    string fileName;
    var scriptArgs = new List<string>();

    if (isWindows)
    {
        var scriptPath = Path.Combine(gitRepoRoot, ".squad", "templates", "skills", "distributed-mesh", "sync-mesh.ps1");
        fileName = "pwsh";
        scriptArgs.AddRange(["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath]);
        foreach (var arg in forwarded)
        {
            scriptArgs.Add(arg.Equals("--init", StringComparison.OrdinalIgnoreCase) ? "-Init" : arg);
        }
    }
    else
    {
        fileName = "bash";
        scriptArgs.Add(Path.Combine(gitRepoRoot, ".squad", "templates", "skills", "distributed-mesh", "sync-mesh.sh"));
        scriptArgs.AddRange(forwarded);
    }

    var psi = new ProcessStartInfo { FileName = fileName, UseShellExecute = false };
    foreach (var a in scriptArgs)
    {
        psi.ArgumentList.Add(a);
    }

    using var meshProcess = Process.Start(psi);
    if (meshProcess is null)
    {
        Console.Error.WriteLine($"Failed to start process: {fileName}");
        return 1;
    }

    meshProcess.WaitForExit();
    return meshProcess.ExitCode;
}

return PrintUsage();
