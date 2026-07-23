using FluentAssertions;

namespace SquadWorkflowStandard.Tests;

public sealed class ScriptIntegrationTests
{
    [Fact]
    public void SyncScript_ShouldPrintUsageAndFail_WhenTargetRepoIsMissing()
    {
        // Arrange
        var syncScript = RepositoryPaths.SyncScriptPath;

        // Act
        var result = ProcessRunner.Run("bash", [syncScript]);

        // Assert
        result.ExitCode.Should().Be(1);
        result.StdOut.Should().Contain("Usage:");
        result.StdOut.Should().Contain("sync-git-gh-standard.sh");
    }

    [Fact]
    public void CheckScript_ShouldPrintUsageAndFail_WhenTargetRepoIsMissing()
    {
        // Arrange
        var checkScript = RepositoryPaths.CheckScriptPath;

        // Act
        var result = ProcessRunner.Run("bash", [checkScript]);

        // Assert
        result.ExitCode.Should().Be(1);
        result.StdOut.Should().Contain("Usage:");
        result.StdOut.Should().Contain("check-git-gh-standard.sh");
    }

    [Fact]
    public void SyncScript_ShouldCopyCanonicalAssetsWorkflowsAndHooks()
    {
        using var target = new TemporaryTargetRepository();

        // Arrange
        var syncScript = RepositoryPaths.SyncScriptPath;
        var repoRoot = RepositoryPaths.Root;
        var workflowEntries = BaselineManifest.ReadEntries(RepositoryPaths.WorkflowManifestPath);
        var hookEntries = BaselineManifest.ReadEntries(RepositoryPaths.HookManifestPath);

        // Act
        var result = ProcessRunner.Run("bash", [syncScript, target.RootPath, "--source-repo", repoRoot]);

        // Assert
        result.ExitCode.Should().Be(0, result.CombinedOutput);

        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", "git-gh-process-standard.md")).Should().BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "skills", "git-workflow-standard", "SKILL.md")).Should().BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", ".git-gh-standard-version")).Should().BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", "workflow-baseline-manifest.txt")).Should().BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", "hook-baseline-manifest.txt")).Should().BeTrue();

        foreach (var workflowFile in workflowEntries)
        {
            var sourceWorkflow = Path.Combine(repoRoot, "source", "workflows", workflowFile);
            var targetWorkflow = Path.Combine(target.RootPath, ".github", "workflows", workflowFile);

            File.Exists(targetWorkflow).Should().BeTrue($"workflow should be copied: {workflowFile}");
            File.ReadAllText(targetWorkflow).Should().Be(File.ReadAllText(sourceWorkflow));
        }

        foreach (var hookFile in hookEntries)
        {
            var sourceHook = Path.Combine(repoRoot, "source", "hooks", hookFile);
            var targetHook = Path.Combine(target.RootPath, ".github", "hooks", hookFile);

            File.Exists(targetHook).Should().BeTrue($"hook should be copied: {hookFile}");
            File.ReadAllText(targetHook).Should().Be(File.ReadAllText(sourceHook));
        }
    }

    [Fact]
    public void CheckScript_ShouldReturnOk_WhenSyncedAndAdaptersArePresent()
    {
        using var target = new TemporaryTargetRepository();

        // Arrange
        var syncScript = RepositoryPaths.SyncScriptPath;
        var checkScript = RepositoryPaths.CheckScriptPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();

        target.SeedRequiredAdapters(canonicalVersion);
        ProcessRunner.Run("bash", [syncScript, target.RootPath, "--source-repo", repoRoot]).ExitCode.Should().Be(0);

        // Act
        var result = ProcessRunner.Run("bash", [checkScript, target.RootPath, "--source-repo", repoRoot]);

        // Assert
        result.ExitCode.Should().Be(0, result.CombinedOutput);
        result.StdOut.Should().Contain("STATUS: OK");
    }

    [Fact]
    public void CheckScript_ShouldReturnDriftExitCode_WhenLocalVersionDiffers()
    {
        using var target = new TemporaryTargetRepository();

        // Arrange
        var syncScript = RepositoryPaths.SyncScriptPath;
        var checkScript = RepositoryPaths.CheckScriptPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();

        target.SeedRequiredAdapters(canonicalVersion);
        ProcessRunner.Run("bash", [syncScript, target.RootPath, "--source-repo", repoRoot]).ExitCode.Should().Be(0);
        File.WriteAllText(Path.Combine(target.RootPath, ".squad", "workflows", ".git-gh-standard-version"), "0.0.0");

        // Act
        var result = ProcessRunner.Run("bash", [checkScript, target.RootPath, "--source-repo", repoRoot]);

        // Assert
        result.ExitCode.Should().Be(3, result.CombinedOutput);
        result.StdOut.Should().Contain("STATUS: DRIFT DETECTED");
    }

    [Fact]
    public void CheckScript_ShouldReturnAdapterFailureExitCode_WhenRequiredBindingIsMissing()
    {
        using var target = new TemporaryTargetRepository();

        // Arrange
        var syncScript = RepositoryPaths.SyncScriptPath;
        var checkScript = RepositoryPaths.CheckScriptPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();

        target.SeedRequiredAdapters(canonicalVersion);
        ProcessRunner.Run("bash", [syncScript, target.RootPath, "--source-repo", repoRoot]).ExitCode.Should().Be(0);
        File.WriteAllText(Path.Combine(target.RootPath, ".squad", "routing.md"), "invalid routing");

        // Act
        var result = ProcessRunner.Run("bash", [checkScript, target.RootPath, "--source-repo", repoRoot]);

        // Assert
        result.ExitCode.Should().Be(4, result.CombinedOutput);
        result.StdOut.Should().Contain("ADAPTER CHECK FAILED");
        result.StdOut.Should().Contain("STATUS: ENFORCEMENT INCOMPLETE");
    }
}
