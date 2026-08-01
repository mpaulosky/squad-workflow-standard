using GitGhStandardCli.Models;
using GitGhStandardCli.Services;

namespace GitGhStandardCli.Commands;

internal static class CheckCommand
{
    private static readonly string WorkflowStandardRelativePath =
        Path.Combine("source", ".squad", "workflows", "git-gh-process-standard.md");

    private static readonly string VersionStampRelativePath =
        Path.Combine(".squad", "workflows", ".git-gh-standard-version");

    public static int Run(string sourceRepo, string targetRepo)
    {
        var evidence = new SynchronizationValidationEvidence
        {
            CanonicalSourceExists = Directory.Exists(sourceRepo),
            CanonicalWorkflowExists = false,
            CanonicalVersionResolved = false,
            CanonicalVersion = string.Empty,
            LocalVersion = "missing",
            VersionDriftDetected = false,
            EnforcementFailuresDetected = false,
            FailureMessages = []
        };

        if (!evidence.CanonicalSourceExists)
        {
            Console.Error.WriteLine($"Canonical source repository not found: {sourceRepo}");
            var sourceDecision = SynchronizationValidationContract.Evaluate(evidence);
            return sourceDecision.ExitCode;
        }

        var workflowStandard = Path.Combine(sourceRepo, WorkflowStandardRelativePath);
        evidence.CanonicalWorkflowExists = File.Exists(workflowStandard);
        if (!evidence.CanonicalWorkflowExists)
        {
            Console.Error.WriteLine($"Missing canonical file: {workflowStandard}");
            var workflowDecision = SynchronizationValidationContract.Evaluate(evidence);
            return workflowDecision.ExitCode;
        }

        var canonicalVersion = SyncCommand.ReadCanonicalVersion(workflowStandard);
        evidence.CanonicalVersionResolved = canonicalVersion != "unknown" && !string.IsNullOrEmpty(canonicalVersion);
        evidence.CanonicalVersion = canonicalVersion;
        if (!evidence.CanonicalVersionResolved)
        {
            Console.Error.WriteLine("ERROR: Canonical version not found.");
            var versionDecision = SynchronizationValidationContract.Evaluate(evidence);
            return versionDecision.ExitCode;
        }

        var localVersionFile = Path.Combine(targetRepo, VersionStampRelativePath);
        var localVersion = File.Exists(localVersionFile)
            ? File.ReadAllText(localVersionFile).Trim()
            : "missing";
        evidence.LocalVersion = localVersion;

        Console.WriteLine($"Canonical version: {canonicalVersion}");
        Console.WriteLine($"Local version:     {localVersion}");

        var hasDrift = false;
        var hasFailure = false;

        // Version check.
        if (localVersion != canonicalVersion)
        {
            hasDrift = true;
            hasFailure = true;
            Console.WriteLine("STATUS: DRIFT DETECTED");
            Console.WriteLine("Policy: detect-and-prompt before gated issue work.");
            Console.WriteLine("Choose one:");
            Console.WriteLine(
                $"  1) Update now: git-gh-standard-cli sync-git-gh-standard {targetRepo} --source {sourceRepo}");
            Console.WriteLine("  2) Defer: continue now, but rerun this check before next gated work");
        }

        // File content drift check for all asset categories.
        CheckCoreFiles(sourceRepo, targetRepo, ref hasFailure);
        CheckCategories(sourceRepo, targetRepo, ref hasFailure);

        // Enforcement adapter checks.
        CheckAdapters(targetRepo, canonicalVersion, ref hasFailure);

        // Promotion guard workflow checks.
        CheckPromotionGuardWorkflows(targetRepo, ref hasFailure);

        // Git hooks path check.
        CheckHooksPath(targetRepo, ref hasFailure);

        evidence.VersionDriftDetected = hasDrift;
        evidence.EnforcementFailuresDetected = hasFailure;
        evidence.FailureMessages = hasFailure
            ? ["one or more validation checks failed"]
            : [];

        var validationDecision = SynchronizationValidationContract.Evaluate(evidence);

        if (validationDecision.Outcome == SynchronizationValidationOutcome.Ok)
        {
            Console.WriteLine("STATUS: OK (version and hard-gate adapters in sync)");
            return validationDecision.ExitCode;
        }

        Console.WriteLine("STATUS: ENFORCEMENT INCOMPLETE");
        Console.WriteLine("Fix drift and adapter bindings, then rerun this check.");
        Console.WriteLine(
            $"Suggested action: git-gh-standard-cli sync-git-gh-standard {targetRepo} --source {sourceRepo}");
        Console.WriteLine("Exit code map: 0=ok, 2=canonical missing, 3=drift, 4=adapter enforcement failure");

        return validationDecision.ExitCode;
    }

    private static void CheckCoreFiles(string sourceRepo, string targetRepo, ref bool hasFailure)
    {
        var corePairs = new[]
        {
            (Src: Path.Combine("source", ".squad", "workflows", "git-gh-process-standard.md"),
                Tgt: Path.Combine(".squad", "workflows", "git-gh-process-standard.md")),
            (Src: Path.Combine("source", ".squad", "workflows", "README.md"),
                Tgt: Path.Combine(".squad", "workflows", "README.md")),
            (Src: Path.Combine("source", ".squad", "skills", "git-workflow-standard", "SKILL.md"),
                Tgt: Path.Combine(".squad", "skills", "git-workflow-standard", "SKILL.md")),
        };

        foreach (var (srcRel, tgtRel) in corePairs)
        {
            var source = Path.Combine(sourceRepo, srcRel);
            var target = Path.Combine(targetRepo, tgtRel);

            if (!File.Exists(source))
            {
                continue;
            }

            if (!File.Exists(target))
            {
                hasFailure = true;
                Console.WriteLine($"ADAPTER CHECK FAILED: missing file {tgtRel}");
                continue;
            }

            using var srcStream = File.OpenRead(source);
            using var tgtStream = File.OpenRead(target);
            if (!StreamsAreIdentical(srcStream, tgtStream))
            {
                hasFailure = true;
                Console.WriteLine($"ADAPTER CHECK FAILED: content drift in {tgtRel}");
            }
        }
    }

    private static void CheckCategories(string sourceRepo, string targetRepo, ref bool hasFailure)
    {
        foreach (var category in AssetCategory.All)
        {
            var manifestPath = Path.Combine(sourceRepo, category.ManifestFile);
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            // Check the manifest file itself.
            var targetManifest = Path.Combine(targetRepo, category.ManifestFile);
            if (!File.Exists(targetManifest))
            {
                hasFailure = true;
                Console.WriteLine($"ADAPTER CHECK FAILED: missing manifest {category.ManifestFile}");
            }
            else
            {
                using var s = File.OpenRead(manifestPath);
                using var t = File.OpenRead(targetManifest);
                if (!StreamsAreIdentical(s, t))
                {
                    hasFailure = true;
                    Console.WriteLine($"ADAPTER CHECK FAILED: manifest drift in {category.ManifestFile}");
                }
            }

            var entries = ManifestReader.ReadEntries(manifestPath);
            var sourceRoot = Path.Combine(sourceRepo, category.SourceRoot);
            var targetRoot = Path.Combine(targetRepo, category.TargetRoot);

            foreach (var entry in entries)
            {
                if (category.EntriesAreDirectories)
                {
                    CheckDirectory(
                        Path.Combine(sourceRoot, entry),
                        Path.Combine(targetRoot, entry),
                        category.MakeFilesExecutable,
                        ref hasFailure);
                }
                else
                {
                    CheckFile(
                        Path.Combine(sourceRoot, entry),
                        Path.Combine(targetRoot, entry),
                        Path.Combine(category.TargetRoot, entry),
                        category.MakeFilesExecutable,
                        ref hasFailure);
                }
            }
        }
    }

    private static void CheckFile(string source, string target, string label, bool checkExecutable, ref bool hasFailure)
    {
        if (!File.Exists(source))
        {
            return;
        }

        if (!File.Exists(target))
        {
            hasFailure = true;
            Console.WriteLine($"ADAPTER CHECK FAILED: missing target file {label}");
            return;
        }

        using var srcStream = File.OpenRead(source);
        using var tgtStream = File.OpenRead(target);
        if (!StreamsAreIdentical(srcStream, tgtStream))
        {
            hasFailure = true;
            Console.WriteLine($"ADAPTER CHECK FAILED: content drift in {label}");
        }

        if (checkExecutable && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
        {
            var mode = File.GetUnixFileMode(target);
            if (!mode.HasFlag(UnixFileMode.UserExecute))
            {
                hasFailure = true;
                Console.WriteLine($"ADAPTER CHECK FAILED: hook is not executable {label}");
            }
        }
    }

    private static void CheckDirectory(string sourceDir, string targetDir, bool checkExecutable, ref bool hasFailure)
    {
        if (!Directory.Exists(sourceDir))
        {
            return;
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
            CheckFile(sourceFile, Path.Combine(targetDir, relativePath), relativePath, checkExecutable, ref hasFailure);
        }
    }

    private static void CheckAdapters(string targetRepo, string canonicalVersion, ref bool hasFailure)
    {
        var adapters = GetAdapterChecks(canonicalVersion);
        foreach (var check in adapters)
        {
            AssertFileContains(
                Path.Combine(targetRepo, check.RelativePath),
                check.Required,
                check.FailMessage,
                ref hasFailure);
        }
    }

    private static void CheckPromotionGuardWorkflows(string targetRepo, ref bool hasFailure)
    {
        var requiredWorkflows = new[]
        {
            (Name: "preview promotion guard",
                RelativePath: Path.Combine(".github", "workflows", "squad-preview-guard.yml")),
            (Name: "main release guard", RelativePath: Path.Combine(".github", "workflows", "squad-main-guard.yml")),
            (Name: "back-merge guard",
                RelativePath: Path.Combine(".github", "workflows", "squad-main-to-dev-backmerge-guard.yml"))
        };

        foreach (var workflow in requiredWorkflows)
        {
            var targetPath = Path.Combine(targetRepo, workflow.RelativePath);
            if (!File.Exists(targetPath))
            {
                hasFailure = true;
                Console.WriteLine($"ADAPTER CHECK FAILED: missing {workflow.Name} workflow {workflow.RelativePath}");
                Console.WriteLine(
                    $"ADAPTER CHECK FAILED: promotion guard workflow {workflow.RelativePath} must be present to enforce preview/main source policy");
                Console.WriteLine(
                    $"ADAPTER CHECK FAILED: promotion guard workflow {workflow.RelativePath} is required for preview/main source policy enforcement");
            }
        }
    }

    private static void CheckHooksPath(string targetRepo, ref bool hasFailure)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = targetRepo
            };
            psi.ArgumentList.Add("-C");
            psi.ArgumentList.Add(targetRepo);
            psi.ArgumentList.Add("config");
            psi.ArgumentList.Add("--get");
            psi.ArgumentList.Add("core.hooksPath");

            using var proc = System.Diagnostics.Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;
            proc?.WaitForExit();

            var normalized = output.TrimStart('.', '/');
            if (string.IsNullOrEmpty(output))
            {
                hasFailure = true;
                Console.WriteLine("ADAPTER CHECK FAILED: git core.hooksPath is not configured");
            }
            else if (!normalized.Equals(".github/hooks", StringComparison.OrdinalIgnoreCase)
                     && !normalized.Equals("github/hooks", StringComparison.OrdinalIgnoreCase))
            {
                hasFailure = true;
                Console.WriteLine(
                    $"ADAPTER CHECK FAILED: git core.hooksPath must be '.github/hooks' (found: {output})");
            }
        }
        catch (Exception ex)
        {
            hasFailure = true;
            Console.WriteLine($"ADAPTER CHECK FAILED: could not read git core.hooksPath: {ex.Message}");
        }
    }

    private static void AssertFileContains(string filePath, string required, string message, ref bool hasFailure)
    {
        if (!File.Exists(filePath))
        {
            hasFailure = true;
            Console.WriteLine($"ADAPTER CHECK FAILED: missing file {filePath}");
            return;
        }

        if (!File.ReadAllText(filePath).Contains(required, StringComparison.Ordinal))
        {
            hasFailure = true;
            Console.WriteLine($"ADAPTER CHECK FAILED: {message}");
        }
    }

    private static IEnumerable<(string RelativePath, string Required, string FailMessage)> GetAdapterChecks(
        string canonicalVersion)
    {
        return
        [
            (
                ".squad/routing.md",
                ".squad/workflows/git-gh-process-standard.md",
                ".squad/routing.md must reference canonical workflow source"
            ),
            (
                ".squad/routing.md",
                ".squad/templates/issue-lifecycle.md",
                ".squad/routing.md must bind issue lifecycle template"
            ),
            (
                ".squad/routing.md",
                "single issue uses standard branch flow; 2+",
                ".squad/routing.md must enforce standard-vs-worktree flow selection"
            ),
            (
                ".squad/routing.md",
                "never push directly to `main`, `preview`, or `dev`",
                ".squad/routing.md must hard-gate direct main/preview/dev pushes"
            ),
            (
                ".squad/ceremonies.md",
                ".squad/workflows/git-gh-process-standard.md",
                ".squad/ceremonies.md must reference canonical workflow source"
            ),
            (
                ".squad/templates/issue-lifecycle.md",
                "Workflow Standard Binding",
                ".squad/templates/issue-lifecycle.md must include workflow standard binding section"
            ),
            (
                ".squad/templates/issue-lifecycle.md",
                $"Standard version: `{canonicalVersion}`",
                ".squad/templates/issue-lifecycle.md must bind to canonical standard version"
            ),
            (
                ".squad/templates/issue-lifecycle.md",
                "Enforcement level: hard gate",
                ".squad/templates/issue-lifecycle.md must explicitly declare hard gate enforcement"
            ),
            (
                ".squad/templates/issue-lifecycle.md",
                "Default branch policy: feature/work branches -> PR to `dev`; `dev` -> sanitized promotion branch -> PR to `preview`; `preview` -> PR to `main`",
                ".squad/templates/issue-lifecycle.md must enforce the protected dev-preview-main promotion flow"
            ),
            (
                ".squad/templates/issue-lifecycle.md",
                "Post-push requirement: after pushing a work branch, immediately open/update a PR to `dev`.",
                ".squad/templates/issue-lifecycle.md must require immediate work-branch PR creation to dev after push"
            ),
            (
                ".squad/templates/issue-lifecycle.md",
                "Promotion rule: do not auto-open `dev` -> `main` after routine work pushes; promotion flows through a sanitized PR branch into `preview`, then a separate `preview` -> `main` release PR.",
                ".squad/templates/issue-lifecycle.md must route promotion through protected preview before main"
            ),
            (
                ".squad/skills/git-workflow-standard/SKILL.md",
                $"Standard version: `{canonicalVersion}`",
                ".squad/skills/git-workflow-standard/SKILL.md must match canonical standard version"
            ),
        ];
    }

    private static bool StreamsAreIdentical(Stream a, Stream b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        const int bufferSize = 4096;
        Span<byte> bufA = stackalloc byte[bufferSize];
        Span<byte> bufB = stackalloc byte[bufferSize];

        int read;
        while ((read = a.Read(bufA)) > 0)
        {
            b.ReadExactly(bufB[..read]);
            if (!bufA[..read].SequenceEqual(bufB[..read]))
            {
                return false;
            }
        }

        return true;
    }
}
