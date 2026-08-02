using FluentAssertions;

namespace Unit.Tests;

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
    public void PrePushHook_ShouldAllowHotfixBranchesForReleaseFlows()
    {
        using var target = new TemporaryTargetRepository();

        var hookSourcePath = Path.Combine(RepositoryPaths.Root, ".github", "hooks", "pre-push");
        var targetHookPath = Path.Combine(target.RootPath, ".github", "hooks", "pre-push");
        Directory.CreateDirectory(Path.Combine(target.RootPath, ".github", "hooks"));

        File.Copy(hookSourcePath, targetHookPath);
        ProcessRunner.Run("chmod", ["+x", targetHookPath]).ExitCode.Should().Be(0);

        var dotnetStubDir = Path.Combine(Path.GetTempPath(), $"hook-dotnet-stub-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dotnetStubDir);
        var dotnetStubPath = Path.Combine(dotnetStubDir, "dotnet");
        File.WriteAllText(dotnetStubPath, "#!/usr/bin/env bash\nexit 0\n");
        ProcessRunner.Run("chmod", ["+x", dotnetStubPath]).ExitCode.Should().Be(0);

        var originalPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", $"{dotnetStubDir}{Path.PathSeparator}{originalPath}");

        try
        {
            ProcessRunner.Run("git", ["checkout", "-b", "hotfix/preview-main-reconcile"], target.RootPath)
                .ExitCode.Should().Be(0);

            var result = ProcessRunner.Run(
                "bash",
                [targetHookPath],
                target.RootPath,
                "refs/heads/hotfix/preview-main-reconcile\n");

            result.ExitCode.Should().Be(0, result.CombinedOutput);
            result.CombinedOutput.Should().Contain("All gates passed");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
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

        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", "git-gh-process-standard.md")).Should()
            .BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "skills", "git-workflow-standard", "SKILL.md")).Should()
            .BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", ".git-gh-standard-version")).Should().BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", "workflow-baseline-manifest.txt")).Should()
            .BeTrue();
        File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", "hook-baseline-manifest.txt")).Should()
            .BeTrue();

        foreach (var workflowFile in workflowEntries)
        {
            var sourceWorkflow = Path.Combine(repoRoot, ".github", "workflows", workflowFile);
            var targetWorkflow = Path.Combine(target.RootPath, ".github", "workflows", workflowFile);

            File.Exists(targetWorkflow).Should().BeTrue($"workflow should be copied: {workflowFile}");
            File.ReadAllText(targetWorkflow).Should().Be(File.ReadAllText(sourceWorkflow));
        }

        foreach (var hookFile in hookEntries)
        {
            var sourceHook = Path.Combine(repoRoot, ".github", "hooks", hookFile);
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
    public void CheckScript_ShouldReportPromotionGuardFailures_WhenPreviewGuardIsMissing()
    {
        using var target = new TemporaryTargetRepository();

        var syncScript = RepositoryPaths.SyncScriptPath;
        var checkScript = RepositoryPaths.CheckScriptPath;
        var repoRoot = RepositoryPaths.Root;
        var canonicalVersion = RepositoryPaths.GetCanonicalVersion();
        var previewGuardPath = Path.Combine(target.RootPath, ".github", "workflows", "squad-preview-guard.yml");

        target.SeedRequiredAdapters(canonicalVersion);
        ProcessRunner.Run("bash", [syncScript, target.RootPath, "--source-repo", repoRoot]).ExitCode.Should().Be(0);
        File.Delete(previewGuardPath);

        var result = ProcessRunner.Run("bash", [checkScript, target.RootPath, "--source-repo", repoRoot]);

        result.ExitCode.Should().Be(4, result.CombinedOutput);
        result.StdOut.Should().Contain("promotion guard workflow");
        result.StdOut.Should().Contain("squad-preview-guard.yml");
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
        var sourceWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-paths-guard.yml");
        var generatedWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-paths-guard.yml");

        var sourceWorkflow = File.ReadAllText(sourceWorkflowPath);
        var generatedWorkflow = File.ReadAllText(generatedWorkflowPath);

        generatedWorkflow.Should().Be(sourceWorkflow);
        sourceWorkflow.Should().Contain("context.payload.pull_request.base.ref");
        sourceWorkflow.Should().Contain("const mainBranch = process.env.MAIN_BRANCH;");
        sourceWorkflow.Should().Contain("const devBranch = process.env.DEV_BRANCH;");
        sourceWorkflow.Should().Contain("baseBranch !== mainBranch && baseBranch !== devBranch");
        sourceWorkflow.Should().Contain("baseBranch === mainBranch");
        sourceWorkflow.Should().Contain("baseBranch === devBranch");
        sourceWorkflow.Should()
            .Contain("main: block new or modified .squad/ and team-docs/ paths, but allow removals.");
        sourceWorkflow.Should().Contain("dev: keep .squad/ retained by blocking removals only.");
        sourceWorkflow.Should().Contain("The following files must NOT be merged into \\`${mainBranch}\\`.");
        sourceWorkflow.Should()
            .Contain("The following \\`.squad/\\` files must NOT be removed from \\`${devBranch}\\`.");
    }

    [Fact]
    public void PreviewGuardWorkflow_ShouldUseLiteralPreviewBranchExpression()
    {
        var previewGuardPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-preview-guard.yml");
        var previewGuard = File.ReadAllText(previewGuardPath);

        previewGuard.Should().Contain("PREVIEW_BRANCH: preview");
        previewGuard.Should().Contain("github.event.pull_request.base.ref == 'preview'");
        previewGuard.Should().NotContain("vars.SQUAD_PREVIEW_BRANCH");
    }

    [Fact]
    public void BackmergeGuardWorkflow_ShouldTargetAutomationHeadBranchWithMainFallback()
    {
        var sourceWorkflowPath =
            Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-main-to-dev-backmerge-guard.yml");
        var generatedWorkflowPath =
            Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-main-to-dev-backmerge-guard.yml");

        var sourceWorkflow = File.ReadAllText(sourceWorkflowPath);
        var generatedWorkflow = File.ReadAllText(generatedWorkflowPath);

        generatedWorkflow.Should().Be(sourceWorkflow);
        sourceWorkflow.Should().Contain("github.event.pull_request.base.ref == 'dev'");
        sourceWorkflow.Should().Contain("startsWith(");
        sourceWorkflow.Should().Contain("github.event.pull_request.head.ref,");
        sourceWorkflow.Should().Contain("automation/backmerge-main-to-dev");
        sourceWorkflow.Should().Contain("'main'");
        sourceWorkflow.Should().Contain("Back-merge PR from main to dev must not modify .squad/.");
        sourceWorkflow.Should().NotContain("vars.SQUAD_DEV_BRANCH");
        sourceWorkflow.Should().NotContain("vars.SQUAD_MAIN_BRANCH");
    }

    [Fact]
    public void ProtectedDevBranchWorkflows_ShouldUsePullRequestsInsteadOfDirectPushes()
    {
        var sourceBlogWorkflowPath =
            Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-blog-readme-sync.yml");
        var generatedBlogWorkflowPath =
            Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-blog-readme-sync.yml");
        var sourceHotfixWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-hotfix-backport-reminder.yml");
        var generatedHotfixWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-hotfix-backport-reminder.yml");

        var sourceBlogWorkflow = File.ReadAllText(sourceBlogWorkflowPath);
        var generatedBlogWorkflow = File.ReadAllText(generatedBlogWorkflowPath);
        var sourceHotfixWorkflow = File.ReadAllText(sourceHotfixWorkflowPath);
        var generatedHotfixWorkflow = File.ReadAllText(generatedHotfixWorkflowPath);

        generatedBlogWorkflow.Should().Be(sourceBlogWorkflow);
        generatedHotfixWorkflow.Should().Be(sourceHotfixWorkflow);

        sourceBlogWorkflow.Should().Contain("pull-requests: write");
        sourceBlogWorkflow.Should().Contain("fetch-depth: 0");
        sourceBlogWorkflow.Should().Contain("id: update_readme_sync");
        sourceBlogWorkflow.Should().Contain("cp README.md \"$RUNNER_TEMP/README.blog-readme-sync.md\"");
        sourceBlogWorkflow.Should().Contain("git restore README.md");
        sourceBlogWorkflow.Should().Contain("git fetch origin dev --depth=1");
        sourceBlogWorkflow.Should().Contain("if: steps.update_readme_sync.outputs.readme_changed == 'true'");
        sourceBlogWorkflow.Should().Contain("git switch -C automation/blog-readme-sync origin/dev");
        sourceBlogWorkflow.Should().Contain("cp \"$RUNNER_TEMP/README.blog-readme-sync.md\" README.md");
        sourceBlogWorkflow.Should().Contain("echo \"committed=false\" >> \"$GITHUB_OUTPUT\"");
        sourceBlogWorkflow.Should().Contain("Create or reuse README sync PR to dev");
        sourceBlogWorkflow.Should().Contain("if: steps.commit_readme_sync.outputs.committed == 'true'");
        sourceBlogWorkflow.Should().Contain("base: 'dev'");
        sourceBlogWorkflow.Should().Contain("head: 'automation/blog-readme-sync'");
        sourceBlogWorkflow.Should().NotContain("git push origin HEAD:dev");
        sourceBlogWorkflow.Should().NotContain("git switch -C automation/blog-readme-sync\n");

        sourceHotfixWorkflow.Should().Contain("git checkout -b hotfix/backport-${pr.number}");
        sourceHotfixWorkflow.Should().Contain("git push -u origin hotfix/backport-${pr.number}");
        sourceHotfixWorkflow.Should().Contain("'gh pr create --base dev'");
        sourceHotfixWorkflow.Should().Contain("].join('");
        sourceHotfixWorkflow.Should().Contain("--head hotfix/backport-${pr.number}");
        sourceHotfixWorkflow.Should().Contain("Do not push directly to \\`dev\\`.");
        sourceHotfixWorkflow.Should().NotContain("git push origin dev");
        sourceHotfixWorkflow.Should().NotContain("'gh pr create --base dev \\");
    }

    [Fact]
    public void ProtectedMainReadmeSyncWorkflow_ShouldUsePullRequestInsteadOfDirectPush()
    {
        var sourceWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-sync-readme.yml");
        var generatedWorkflowPath =
            Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-sync-readme.yml");

        var sourceWorkflow = File.ReadAllText(sourceWorkflowPath);
        var generatedWorkflow = File.ReadAllText(generatedWorkflowPath);

        generatedWorkflow.Should().Be(sourceWorkflow);
        sourceWorkflow.Should().Contain("pull-requests: write");
        sourceWorkflow.Should().Contain("fetch-depth: 0");
        sourceWorkflow.Should().Contain("id: prepare_sync_readme");
        sourceWorkflow.Should().Contain("cp docs/README.md \"$RUNNER_TEMP/README.sync-readme.docs.md\"");
        sourceWorkflow.Should().Contain("git restore docs/README.md");
        sourceWorkflow.Should().Contain("git fetch origin \"${MAIN_BRANCH}\" --depth=1");
        sourceWorkflow.Should().Contain("git switch -C automation/sync-readme \"origin/${MAIN_BRANCH}\"");
        sourceWorkflow.Should().Contain("Create or reuse README sync PR to main");
        sourceWorkflow.Should().Contain("base: mainBranch");
        sourceWorkflow.Should().Contain("head: 'automation/sync-readme'");
        sourceWorkflow.Should().NotContain("git push\n");
        sourceWorkflow.Should().NotContain("git push origin main");
    }

    [Fact]
    public void TestWorkflow_ShouldSkipPreviewAndMainBranchesForPushAndPullRequest()
    {
        var sourceWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-test.yml");
        var generatedWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-test.yml");

        var sourceWorkflow = File.ReadAllText(sourceWorkflowPath);
        var generatedWorkflow = File.ReadAllText(generatedWorkflowPath);

        generatedWorkflow.Should().Be(sourceWorkflow);
        sourceWorkflow.Should().Contain("workflow_call:");
        sourceWorkflow.Should().Contain("pull_request:");
        sourceWorkflow.Should().Contain("branches-ignore:");
        sourceWorkflow.Should().Contain("- preview");
        sourceWorkflow.Should().Contain("- main");
        sourceWorkflow.Should().NotContain("push:");
    }

    [Fact]
    public void ProtectedPromotionWorkflows_ShouldUseProtectedPullRequestLegs()
    {
        var sourcePromoteWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-promote.yml");
        var generatedPromoteWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-promote.yml");
        var sourcePreviewGuardWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-preview-guard.yml");
        var generatedPreviewGuardWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-preview-guard.yml");
        var sourceMainGuardWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-main-guard.yml");
        var generatedMainGuardWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-main-guard.yml");

        var sourcePromoteWorkflow = File.ReadAllText(sourcePromoteWorkflowPath);
        var generatedPromoteWorkflow = File.ReadAllText(generatedPromoteWorkflowPath);
        var sourcePreviewGuardWorkflow = File.ReadAllText(sourcePreviewGuardWorkflowPath);
        var generatedPreviewGuardWorkflow = File.ReadAllText(generatedPreviewGuardWorkflowPath);
        var sourceMainGuardWorkflow = File.ReadAllText(sourceMainGuardWorkflowPath);
        var generatedMainGuardWorkflow = File.ReadAllText(generatedMainGuardWorkflowPath);

        generatedPromoteWorkflow.Should().Contain("Prepare dev → preview promotion PR");
        generatedPreviewGuardWorkflow.Should().Contain("guard-preview-source");
        generatedMainGuardWorkflow.Should().Contain("guard-main-source");

        sourcePromoteWorkflow.Should().Contain("pull-requests: write");
        sourcePromoteWorkflow.Should().Contain("PREVIEW_PROMOTION_BRANCH");
        sourcePromoteWorkflow.Should()
            .Contain("git switch -C \"${PREVIEW_PROMOTION_BRANCH}\" \"origin/${PREVIEW_BRANCH}\"");
        sourcePromoteWorkflow.Should().Contain("git push --force-with-lease origin \"${PREVIEW_PROMOTION_BRANCH}\"");
        sourcePromoteWorkflow.Should().Contain("Create or reuse sanitized dev → preview promotion PR");
        sourcePromoteWorkflow.Should().Contain("const previewBranch = process.env.PREVIEW_BRANCH;");
        sourcePromoteWorkflow.Should().Contain("basehead: `${previewBranch}...${promotionBranch}`");
        sourcePromoteWorkflow.Should().Contain("base: previewBranch");
        sourcePromoteWorkflow.Should().Contain("head: promotionBranch");
        sourcePromoteWorkflow.Should()
            .Contain("title: `chore: promote ${devBranch} → ${previewBranch} via sanitized PR`");
        sourcePromoteWorkflow.Should().Contain("`- Source branch: \\`${devBranch}\\``");
        sourcePromoteWorkflow.Should()
            .NotContain("title: `chore: promote ${DEV_BRANCH} → ${PREVIEW_BRANCH} via sanitized PR`");
        sourcePromoteWorkflow.Should().NotContain("`- Source branch: \\`${DEV_BRANCH}\\``");
        sourcePromoteWorkflow.Should().Contain("avoids direct pushes to protected `preview`");
        sourcePromoteWorkflow.Should().NotContain("git push origin \"${PREVIEW_BRANCH}\"");
        sourcePromoteWorkflow.Should().Contain("Create or reuse preview → main release PR");
        sourcePromoteWorkflow.Should().Contain("compareCommitsWithBasehead");
        sourcePromoteWorkflow.Should().Contain("basehead: `${mainBranch}...${previewBranch}`");
        sourcePromoteWorkflow.Should().Contain("base: mainBranch");
        sourcePromoteWorkflow.Should().Contain("head: previewBranch");
        sourcePromoteWorkflow.Should().NotContain("git push origin \"${MAIN_BRANCH}\"");

        sourcePreviewGuardWorkflow.Should().Contain("const previewBranch = process.env.PREVIEW_BRANCH;");
        sourcePreviewGuardWorkflow.Should().Contain("const promotionBranch = process.env.PREVIEW_PROMOTION_BRANCH;");
        sourcePreviewGuardWorkflow.Should().Contain("source !== promotionBranch");
        sourcePreviewGuardWorkflow.Should().Contain("must come from ${promotionBranch}");
        sourcePreviewGuardWorkflow.Should().NotContain("source === \"dev\"");

        sourceMainGuardWorkflow.Should().Contain("const mainBranch = \"main\";");
        sourceMainGuardWorkflow.Should().Contain("const previewBranch = \"preview\";");
        sourceMainGuardWorkflow.Should().Contain("source === previewBranch || source.startsWith(\"hotfix/\")");
        sourceMainGuardWorkflow.Should()
            .Contain("must come from ${previewBranch} or hotfix/* branches");
        sourceMainGuardWorkflow.Should().NotContain("source === \"dev\"");
    }

    [Fact]
    public void PreviewToDevBackmergeWorkflows_ShouldUseProtectedPullRequestLegs()
    {
        var sourceWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-preview-to-dev-backmerge.yml");
        var generatedWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-preview-to-dev-backmerge.yml");
        var sourceGuardWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-preview-to-dev-backmerge-guard.yml");
        var generatedGuardWorkflowPath = Path.Combine(RepositoryPaths.Root, ".github", "workflows",
            "squad-preview-to-dev-backmerge-guard.yml");

        var sourceWorkflow = File.ReadAllText(sourceWorkflowPath);
        var generatedWorkflow = File.ReadAllText(generatedWorkflowPath);
        var sourceGuardWorkflow = File.ReadAllText(sourceGuardWorkflowPath);
        var generatedGuardWorkflow = File.ReadAllText(generatedGuardWorkflowPath);

        generatedWorkflow.Should().Be(sourceWorkflow);
        generatedGuardWorkflow.Should().Be(sourceGuardWorkflow);

        sourceWorkflow.Should().Contain("PREVIEW_BRANCH: preview");
        sourceWorkflow.Should().Contain("BACKMERGE_BRANCH: automation/backmerge-preview-to-dev");
        sourceWorkflow.Should().Contain("git merge \"origin/${PREVIEW_BRANCH}\"");
        sourceWorkflow.Should().Contain("git checkout --ours -- .squad/ || true");
        sourceWorkflow.Should().Contain("title: 'chore: sync preview back into dev'");
        sourceWorkflow.Should().Contain("- Source branch: `automation/backmerge-preview-to-dev`");
        sourceWorkflow.Should().Contain("git push --force-with-lease origin \"${BACKMERGE_BRANCH}\"");

        sourceGuardWorkflow.Should().Contain("github.event.pull_request.base.ref == 'dev'");
        sourceGuardWorkflow.Should().Contain("automation/backmerge-preview-to-dev");
        sourceGuardWorkflow.Should().Contain("github.event.pull_request.head.ref");
        sourceGuardWorkflow.Should().Contain("startsWith(");
        sourceGuardWorkflow.Should().Contain("|| github.event.pull_request.head.ref == 'preview'");
    }

    [Fact]
    public void BranchCleanupWorkflow_ShouldBeSourceSyncedAndSupportDispatchAndSchedule()
    {
        var sourceWorkflowPath =
            Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-branch-worktree-cleanup.yml");
        var generatedWorkflowPath =
            Path.Combine(RepositoryPaths.Root, ".github", "workflows", "squad-branch-worktree-cleanup.yml");

        var sourceWorkflow = File.ReadAllText(sourceWorkflowPath);
        var generatedWorkflow = File.ReadAllText(generatedWorkflowPath);

        generatedWorkflow.Should().Be(sourceWorkflow);
        sourceWorkflow.Should().Contain("workflow_dispatch:");
        sourceWorkflow.Should().Contain("schedule:");
        sourceWorkflow.Should().Contain("scripts/squad/cleanup-squad-branches.sh");
        sourceWorkflow.Should().Contain("--apply");
        sourceWorkflow.Should().Contain("--delete-remote");
        sourceWorkflow.Should().Contain("Cleanup stale squad/sprint/hotfix branches and worktrees");
    }

    [Fact]
    public void CleanupScripts_ShouldIncludeHotfixBranchPatternSupport()
    {
        var bashScriptPath = Path.Combine(RepositoryPaths.Root, "scripts", "squad", "cleanup-squad-branches.sh");
        var psScriptPath = Path.Combine(RepositoryPaths.Root, "scripts", "squad", "cleanup-squad-branches.ps1");

        var bashScript = File.ReadAllText(bashScriptPath);
        var psScript = File.ReadAllText(psScriptPath);

        bashScript.Should().Contain("refs/heads/hotfix/*");
        bashScript.Should().Contain("refs/remotes/${REMOTE}/hotfix/*");
        bashScript.Should().Contain("No candidate squad/*, sprint/*, or hotfix/* branches found.");
        bashScript.Should().Contain("^(squad|sprint|hotfix)/([0-9]+)(-|$)");

        psScript.Should().Contain("refs/heads/hotfix/*");
        psScript.Should().Contain("refs/remotes/$remote/hotfix/*");
        psScript.Should().Contain("No candidate squad/*, sprint/*, or hotfix/* branches found.");
        psScript.Should().Contain("^(squad|sprint|hotfix)/(\\d+)(-|$)");
        psScript.Should().Contain("--json number,state,mergedAt,closedAt,url");
        psScript.Should().Contain("--json state,url,number");
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