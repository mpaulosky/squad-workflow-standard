using FluentAssertions;

namespace SquadWorkflowStandard.Tests;

public sealed class CliIntegrationTests
{
    [Fact]
    public void Cli_ShouldPrintUsageAndFail_WhenNoArgumentsAreProvided()
    {
        // Arrange
        var cliProject = RepositoryPaths.CliProjectPath;

        // Act
        var result = ProcessRunner.Run("dotnet", ["run", "--project", cliProject, "--"]);

        // Assert
        result.ExitCode.Should().Be(1, result.CombinedOutput);
        result.StdOut.Should().Contain("Usage:");
        result.StdOut.Should().Contain("git-gh-standard-cli");
    }

    [Fact]
    public void Cli_ShouldPrintUsageAndFail_WhenCommandIsUnsupported()
    {
        // Arrange
        var cliProject = RepositoryPaths.CliProjectPath;

        // Act
        var result = ProcessRunner.Run("dotnet", ["run", "--project", cliProject, "--", "invalid-command"]);

        // Assert
        result.ExitCode.Should().Be(1, result.CombinedOutput);
        result.StdOut.Should().Contain("Usage:");
    }

    [Fact]
    public void Cli_ShouldDelegateSyncAndCheckScripts_Successfully()
    {
        using var target = new TemporaryTargetRepository();

        // Arrange
        var cliProject = RepositoryPaths.CliProjectPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();

        target.SeedRequiredAdapters(canonicalVersion);

        // Act
        var syncResult = ProcessRunner.Run(
            "dotnet",
            [
                "run",
                "--project",
                cliProject,
                "--",
                "sync-git-gh-standard",
                target.RootPath,
                "--source-repo",
                repoRoot
            ]);

        var checkResult = ProcessRunner.Run(
            "dotnet",
            [
                "run",
                "--project",
                cliProject,
                "--",
                "check-git-gh-standard",
                target.RootPath,
                "--source-repo",
                repoRoot
            ]);

        // Assert
        syncResult.ExitCode.Should().Be(0, syncResult.CombinedOutput);
        checkResult.ExitCode.Should().Be(0, checkResult.CombinedOutput);
        checkResult.StdOut.Should().Contain("STATUS: OK");
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
