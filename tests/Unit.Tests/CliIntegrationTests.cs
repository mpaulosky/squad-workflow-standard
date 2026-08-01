using FluentAssertions;

namespace Unit.Tests;

public sealed class CliIntegrationTests
{
    [Fact]
    public void Cli_ShouldPrintUsageAndFail_WhenNoArgumentsAreProvided()
    {
        var cliProject = RepositoryPaths.CliProjectPath;

        var result = ProcessRunner.Run("dotnet", ["run", "--project", cliProject, "--"]);

        result.ExitCode.Should().Be(1, result.CombinedOutput);
        result.StdOut.Should().Contain("Usage:");
        result.StdOut.Should().Contain("git-gh-standard-cli");
    }

    [Fact]
    public void Cli_ShouldPrintUsageAndFail_WhenCommandIsUnsupported()
    {
        var cliProject = RepositoryPaths.CliProjectPath;

        var result = ProcessRunner.Run("dotnet", ["run", "--project", cliProject, "--", "invalid-command"]);

        result.ExitCode.Should().Be(1, result.CombinedOutput);
        result.StdOut.Should().Contain("Usage:");
    }

    [Fact]
    public void Cli_SyncAndCheck_ShouldSucceed_AndReportStatusOk()
    {
        using var target = new TemporaryTargetRepository();

        var cliProject = RepositoryPaths.CliProjectPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();

        target.SeedRequiredAdapters(canonicalVersion);

        var syncResult = ProcessRunner.Run(
            "dotnet",
            ["run", "--project", cliProject, "--",
             "sync-git-gh-standard", target.RootPath, "--source", repoRoot]);

        var checkResult = ProcessRunner.Run(
            "dotnet",
            ["run", "--project", cliProject, "--",
             "check-git-gh-standard", target.RootPath, "--source", repoRoot]);

        syncResult.ExitCode.Should().Be(0, syncResult.CombinedOutput);
        checkResult.ExitCode.Should().Be(0, checkResult.CombinedOutput);
        checkResult.StdOut.Should().Contain("STATUS: OK");
    }

    [Fact]
    public void Cli_Sync_ShouldDistributeSkills()
    {
        using var target = new TemporaryTargetRepository();
        var cliProject = RepositoryPaths.CliProjectPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();
        target.SeedRequiredAdapters(canonicalVersion);

        ProcessRunner.Run("dotnet",
            ["run", "--project", cliProject, "--",
             "sync-git-gh-standard", target.RootPath, "--source", repoRoot]);

        var entries = BaselineManifest.ReadEntries(RepositoryPaths.SkillManifestPath);
        entries.Should().NotBeEmpty();
        foreach (var skill in entries)
        {
            var skillMd = Path.Combine(target.RootPath, ".github", "skills", skill, "SKILL.md");
            File.Exists(skillMd).Should().BeTrue($"skill '{skill}/SKILL.md' should be synced to target");
        }
    }

    [Fact]
    public void Cli_Sync_ShouldDistributeInstructions()
    {
        using var target = new TemporaryTargetRepository();
        var cliProject = RepositoryPaths.CliProjectPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();
        target.SeedRequiredAdapters(canonicalVersion);

        ProcessRunner.Run("dotnet",
            ["run", "--project", cliProject, "--",
             "sync-git-gh-standard", target.RootPath, "--source", repoRoot]);

        var entries = BaselineManifest.ReadEntries(RepositoryPaths.InstructionManifestPath);
        entries.Should().NotBeEmpty();
        foreach (var file in entries)
        {
            var target_file = Path.Combine(target.RootPath, ".github", "instructions", file);
            File.Exists(target_file).Should().BeTrue($"instruction '{file}' should be synced to target");
        }
    }

    [Fact]
    public void Cli_Sync_ShouldDistributePrompts()
    {
        using var target = new TemporaryTargetRepository();
        var cliProject = RepositoryPaths.CliProjectPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();
        target.SeedRequiredAdapters(canonicalVersion);

        ProcessRunner.Run("dotnet",
            ["run", "--project", cliProject, "--",
             "sync-git-gh-standard", target.RootPath, "--source", repoRoot]);

        var entries = BaselineManifest.ReadEntries(RepositoryPaths.PromptManifestPath);
        entries.Should().NotBeEmpty();
        foreach (var file in entries)
        {
            var targetFile = Path.Combine(target.RootPath, ".github", "prompts", file);
            File.Exists(targetFile).Should().BeTrue($"prompt '{file}' should be synced to target");
        }
    }

    [Fact]
    public void Cli_Sync_ShouldDistributeAgents()
    {
        using var target = new TemporaryTargetRepository();
        var cliProject = RepositoryPaths.CliProjectPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();
        target.SeedRequiredAdapters(canonicalVersion);

        ProcessRunner.Run("dotnet",
            ["run", "--project", cliProject, "--",
             "sync-git-gh-standard", target.RootPath, "--source", repoRoot]);

        var entries = BaselineManifest.ReadEntries(RepositoryPaths.AgentManifestPath);
        entries.Should().NotBeEmpty();
        foreach (var file in entries)
        {
            var targetFile = Path.Combine(target.RootPath, ".github", "agents", file);
            File.Exists(targetFile).Should().BeTrue($"agent '{file}' should be synced to target");
        }
    }

    [Fact]
    public void Cli_Sync_ShouldDistributeSquadSkills()
    {
        using var target = new TemporaryTargetRepository();
        var cliProject = RepositoryPaths.CliProjectPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();
        target.SeedRequiredAdapters(canonicalVersion);

        ProcessRunner.Run("dotnet",
            ["run", "--project", cliProject, "--",
             "sync-git-gh-standard", target.RootPath, "--source", repoRoot]);

        var entries = BaselineManifest.ReadEntries(RepositoryPaths.SquadSkillManifestPath);
        entries.Should().NotBeEmpty();
        foreach (var skill in entries)
        {
            var skillMd = Path.Combine(target.RootPath, ".squad", "skills", skill, "SKILL.md");
            File.Exists(skillMd).Should().BeTrue($"squad skill '{skill}/SKILL.md' should be synced to target");
        }
    }

    [Fact]
    public void Cli_Sync_ShouldNotOverwrite_PreservedFiles()
    {
        using var target = new TemporaryTargetRepository();
        var cliProject = RepositoryPaths.CliProjectPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();
        target.SeedRequiredAdapters(canonicalVersion);

        // Write a sentinel to a preserved file — it must survive sync.
        var routingPath = Path.Combine(target.RootPath, ".squad", "routing.md");
        var originalContent = File.ReadAllText(routingPath);
        var sentinel = "# USER-SENTINEL-DO-NOT-OVERWRITE";
        File.WriteAllText(routingPath, originalContent + Environment.NewLine + sentinel);

        ProcessRunner.Run("dotnet",
            ["run", "--project", cliProject, "--",
             "sync-git-gh-standard", target.RootPath, "--source", repoRoot]);

        File.ReadAllText(routingPath).Should().Contain(sentinel, "preserved file must not be overwritten by sync");
    }

    [Fact]
    public void Cli_ShouldLocateRepoRoot_WhenRunFromGitWorktree()
    {
        using var worktree = GitWorktreeScope.Create(RepositoryPaths.Root);

        var cliProject = RepositoryPaths.CliProjectPath;
        var result = ProcessRunner.Run(
            "dotnet",
            ["run", "--project", cliProject, "--", "check-git-gh-standard"],
            workingDirectory: worktree.WorktreePath);

        result.CombinedOutput.Should().NotContain("Unable to locate repository root (.git)");
        result.StdOut.Should().Contain("Usage:");
    }
}
