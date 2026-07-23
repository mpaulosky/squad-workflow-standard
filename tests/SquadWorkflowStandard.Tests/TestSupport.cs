using System.Diagnostics;
using FluentAssertions;

namespace SquadWorkflowStandard.Tests;

internal sealed record CommandResult(int ExitCode, string StdOut, string StdErr)
{
    public string CombinedOutput => $"{StdOut}\n{StdErr}";
}

internal static class ProcessRunner
{
    public static CommandResult Run(string fileName, IEnumerable<string> args, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            psi.WorkingDirectory = workingDirectory;
        }

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi);
        process.Should().NotBeNull($"Process should start: {fileName}");

        var standardOutput = process!.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new CommandResult(process.ExitCode, standardOutput, standardError);
    }
}

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRepoRoot();
    public static string SyncScriptPath => Path.Combine(Root, "scripts", "squad", "sync-git-gh-standard.sh");
    public static string CheckScriptPath => Path.Combine(Root, "scripts", "squad", "check-git-gh-standard.sh");
    public static string CliProjectPath => Path.Combine(Root, "src", "GitGhStandardCli", "GitGhStandardCli.csproj");
    public static string CanonicalWorkflowPath =>
        Path.Combine(Root, "source", ".squad", "workflows", "git-gh-process-standard.md");
    public static string WorkflowManifestPath =>
        Path.Combine(Root, "source", ".squad", "workflows", "workflow-baseline-manifest.txt");
    public static string HookManifestPath =>
        Path.Combine(Root, "source", ".squad", "workflows", "hook-baseline-manifest.txt");

    public static string GetCanonicalVersion()
    {
        var versionLine = File.ReadLines(CanonicalWorkflowPath)
            .First(static line => line.StartsWith("Standard-Version:", StringComparison.Ordinal));
        return versionLine.Split(':', 2)[1].Trim();
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                File.Exists(Path.Combine(current.FullName, "README.md")) &&
                Directory.Exists(Path.Combine(current.FullName, "scripts")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root for tests.");
    }
}

internal sealed class TemporaryTargetRepository : IDisposable
{
    public TemporaryTargetRepository()
    {
        RootPath = Path.Combine(Path.GetTempPath(), $"squad-standard-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(RootPath);

        var initResult = ProcessRunner.Run("git", ["init"], RootPath);
        initResult.ExitCode.Should().Be(0, initResult.CombinedOutput);
    }

    public string RootPath { get; }

    public void SeedRequiredAdapters(string canonicalVersion)
    {
        Directory.CreateDirectory(Path.Combine(RootPath, ".squad", "skills", "git-workflow-standard"));
        Directory.CreateDirectory(Path.Combine(RootPath, ".squad", "templates"));
        Directory.CreateDirectory(Path.Combine(RootPath, ".squad"));

        File.WriteAllText(
            Path.Combine(RootPath, ".squad", "routing.md"),
            """
            Source: .squad/workflows/git-gh-process-standard.md
            Template: .squad/templates/issue-lifecycle.md
            Flow policy: single issue uses standard branch flow; 2+ issues require worktree flow
            Policy: never push directly to `main` or `dev`
            """);

        File.WriteAllText(
            Path.Combine(RootPath, ".squad", "ceremonies.md"),
            "Reference: .squad/workflows/git-gh-process-standard.md");

        File.WriteAllText(
            Path.Combine(RootPath, ".squad", "templates", "issue-lifecycle.md"),
            $"""
             ## Workflow Standard Binding
             - Standard version: `{canonicalVersion}`
             - Enforcement level: hard gate
             - Default branch policy: branch from `main`, PR to `main`
             """);

        File.WriteAllText(
            Path.Combine(RootPath, ".squad", "skills", "git-workflow-standard", "SKILL.md"),
            $"Standard version: `{canonicalVersion}`");
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}

internal static class BaselineManifest
{
    public static IReadOnlyList<string> ReadEntries(string path)
    {
        return File.ReadAllLines(path)
            .Select(static line => line.Trim().TrimEnd('\r'))
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Where(static line => !line.StartsWith('#'))
            .ToList();
    }
}
