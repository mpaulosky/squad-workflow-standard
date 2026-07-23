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

    [Fact]
    public void SyncScript_ShouldConfigureHooksPathToDistributedHooks()
    {
        using var target = new TemporaryTargetRepository();

        var syncScript = RepositoryPaths.SyncScriptPath;
        var repoRoot = RepositoryPaths.Root;

        var syncResult = ProcessRunner.Run("bash", [syncScript, target.RootPath, "--source-repo", repoRoot]);
        var hooksPathResult = ProcessRunner.Run("git", ["config", "--get", "core.hooksPath"], target.RootPath);

        syncResult.ExitCode.Should().Be(0, syncResult.CombinedOutput);
        hooksPathResult.ExitCode.Should().Be(0, hooksPathResult.CombinedOutput);
        hooksPathResult.StdOut.Trim().Should().Be(".github/hooks");
    }

    [Fact]
    public void CheckScript_ShouldReturnAdapterFailureExitCode_WhenHooksPathIsIncorrect()
    {
        using var target = new TemporaryTargetRepository();

        var syncScript = RepositoryPaths.SyncScriptPath;
        var checkScript = RepositoryPaths.CheckScriptPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();

        target.SeedRequiredAdapters(canonicalVersion);
        ProcessRunner.Run("bash", [syncScript, target.RootPath, "--source-repo", repoRoot]).ExitCode.Should().Be(0);
        ProcessRunner.Run("git", ["config", "core.hooksPath", ".git/hooks"], target.RootPath).ExitCode.Should().Be(0);

        var result = ProcessRunner.Run("bash", [checkScript, target.RootPath, "--source-repo", repoRoot]);

        result.ExitCode.Should().Be(4, result.CombinedOutput);
        result.StdOut.Should().Contain("ADAPTER CHECK FAILED: git core.hooksPath must be '.github/hooks'");
        result.StdOut.Should().Contain("STATUS: ENFORCEMENT INCOMPLETE");
    }

    [Fact]
    public void CheckScript_ShouldReturnAdapterFailureExitCode_WhenSyncedHookIsNotExecutable()
    {
        using var target = new TemporaryTargetRepository();

        var syncScript = RepositoryPaths.SyncScriptPath;
        var checkScript = RepositoryPaths.CheckScriptPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();
        var targetHook = Path.Combine(target.RootPath, ".github", "hooks", "pre-push");

        target.SeedRequiredAdapters(canonicalVersion);
        ProcessRunner.Run("bash", [syncScript, target.RootPath, "--source-repo", repoRoot]).ExitCode.Should().Be(0);
        ProcessRunner.Run("chmod", ["-x", targetHook]).ExitCode.Should().Be(0);

        var result = ProcessRunner.Run("bash", [checkScript, target.RootPath, "--source-repo", repoRoot]);

        result.ExitCode.Should().Be(4, result.CombinedOutput);
        result.StdOut.Should().Contain($"ADAPTER CHECK FAILED: hook is not executable {targetHook}");
        result.StdOut.Should().Contain("STATUS: ENFORCEMENT INCOMPLETE");
    }

    [Fact]
    public void SquadPathsGuardWorkflow_ShouldBeBranchAwareForMainProtectionAndDevRetention()
    {
        var sourceWorkflowPath = Path.Combine(RepositoryPaths.Root, "source", "workflows", "squad-paths-guard.yml");
        var generatedWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-paths-guard.yml");

        var sourceWorkflow = File.ReadAllText(sourceWorkflowPath);
        var generatedWorkflow = File.ReadAllText(generatedWorkflowPath);

        generatedWorkflow.Should().Be(sourceWorkflow);
        sourceWorkflow.Should().Contain("context.payload.pull_request.base.ref");
        sourceWorkflow.Should().Contain("baseBranch === 'main'");
        sourceWorkflow.Should().Contain("baseBranch === 'dev'");
        sourceWorkflow.Should().Contain("main: block new or modified .squad/ and team-docs/ paths, but allow removals.");
        sourceWorkflow.Should().Contain("dev: keep .squad/ retained by blocking removals only.");
        sourceWorkflow.Should().Contain("The following files must NOT be merged into `main`.");
        sourceWorkflow.Should().Contain("The following `.squad/` files must NOT be removed from `dev`.");
    }

    [Fact]
    public void SyncAndCheckScripts_ShouldRecognizeGitWorktreeTarget()
    {
        using var worktree = GitWorktreeScope.Create(RepositoryPaths.Root);

        var syncScript = RepositoryPaths.SyncScriptPath;
        var checkScript = RepositoryPaths.CheckScriptPath;
        var repoRoot = RepositoryPaths.Root;

        var syncResult = ProcessRunner.Run("bash", [syncScript, worktree.WorktreePath, "--source-repo", repoRoot]);
        var checkResult = ProcessRunner.Run("bash", [checkScript, worktree.WorktreePath, "--source-repo", repoRoot]);

        syncResult.ExitCode.Should().Be(0, syncResult.CombinedOutput);
        checkResult.CombinedOutput.Should().NotContain("Target repo is not a git repository");
    }
}
