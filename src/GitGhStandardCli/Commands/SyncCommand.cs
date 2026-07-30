using System.Diagnostics;
using GitGhStandardCli.Models;
using GitGhStandardCli.Services;

namespace GitGhStandardCli.Commands;

internal static class SyncCommand
{
    private static readonly string VersionStampRelativePath =
        Path.Combine(".squad", "workflows", ".git-gh-standard-version");

    private static readonly string WorkflowStandardRelativePath =
        Path.Combine("source", ".squad", "workflows", "git-gh-process-standard.md");

    private static readonly string WorkflowReadmeRelativePath =
        Path.Combine("source", ".squad", "workflows", "README.md");

    private static readonly string WorkflowSkillRelativePath =
        Path.Combine("source", ".squad", "skills", "git-workflow-standard", "SKILL.md");

    public static int Run(SyncOptions options)
    {
        Console.WriteLine($"Source: {options.SourceRepo}");
        Console.WriteLine($"Target: {options.TargetRepo}");
        if (options.DryRun)
        {
            Console.WriteLine("[DRY RUN] No files will be written.");
        }

        Console.WriteLine();

        if (!Directory.Exists(options.SourceRepo))
        {
            Console.Error.WriteLine($"Canonical source repository not found: {options.SourceRepo}");
            return 2;
        }

        var workflowStandard = Path.Combine(options.SourceRepo, WorkflowStandardRelativePath);
        if (!File.Exists(workflowStandard))
        {
            Console.Error.WriteLine($"Missing canonical file: {workflowStandard}");
            return 2;
        }

        // Ensure required target directories exist.
        if (!options.DryRun)
        {
            Directory.CreateDirectory(Path.Combine(options.TargetRepo, ".squad", "workflows"));
            Directory.CreateDirectory(Path.Combine(options.TargetRepo, ".squad", "skills", "git-workflow-standard"));
            Directory.CreateDirectory(Path.Combine(options.TargetRepo, ".github", "workflows"));
            Directory.CreateDirectory(Path.Combine(options.TargetRepo, ".github", "hooks"));
        }

        var synced = new List<string>();
        var skipped = new List<string>();

        // Sync the core process standard files first (legacy non-manifest items).
        SyncCoreFile(workflowStandard, Path.Combine(options.TargetRepo, ".squad", "workflows", "git-gh-process-standard.md"), options, synced, skipped);

        var workflowReadme = Path.Combine(options.SourceRepo, WorkflowReadmeRelativePath);
        if (File.Exists(workflowReadme))
        {
            SyncCoreFile(workflowReadme, Path.Combine(options.TargetRepo, ".squad", "workflows", "README.md"), options, synced, skipped);
        }

        var workflowSkill = Path.Combine(options.SourceRepo, WorkflowSkillRelativePath);
        if (File.Exists(workflowSkill))
        {
            SyncCoreFile(workflowSkill, Path.Combine(options.TargetRepo, ".squad", "skills", "git-workflow-standard", "SKILL.md"), options, synced, skipped);
        }

        // Sync all asset categories via their manifests.
        foreach (var category in AssetCategory.All)
        {
            SyncCategory(category, options, synced, skipped);
        }

        // Configure git hooks path in target repo.
        if (!options.DryRun)
        {
            SetGitHooksPath(options.TargetRepo);
        }

        // Write version stamp.
        var version = ReadCanonicalVersion(workflowStandard);
        var versionStamp = Path.Combine(options.TargetRepo, VersionStampRelativePath);
        if (!options.DryRun)
        {
            File.WriteAllText(versionStamp, version);
        }

        Console.WriteLine($"Synced {synced.Count} file(s), skipped {skipped.Count} identical.");
        Console.WriteLine($"Version stamp: {version}");
        Console.WriteLine($"Next step: git-gh-standard-cli check-git-gh-standard {options.TargetRepo} --source {options.SourceRepo}");

        return 0;
    }

    private static void SyncCoreFile(string source, string target, SyncOptions options, List<string> synced, List<string> skipped)
    {
        if (!File.Exists(source))
        {
            return;
        }

        if (options.DryRun)
        {
            Console.WriteLine($"  [DRY RUN] sync {target}");
            return;
        }

        DirectoryEnsurer.EnsureParent(target);
        if (FileSync.CopyIfDistinct(source, target))
        {
            synced.Add(target);
        }
        else
        {
            skipped.Add(target);
        }
    }

    private static void SyncCategory(AssetCategory category, SyncOptions options, List<string> synced, List<string> skipped)
    {
        var manifestPath = Path.Combine(options.SourceRepo, category.ManifestFile);
        if (!File.Exists(manifestPath))
        {
            return;
        }

        // Sync the manifest file itself.
        var targetManifestPath = Path.Combine(options.TargetRepo, category.ManifestFile);
        SyncCoreFile(manifestPath, targetManifestPath, options, synced, skipped);

        var entries = ManifestReader.ReadEntries(manifestPath);
        var sourceRoot = Path.Combine(options.SourceRepo, category.SourceRoot);
        var targetRoot = Path.Combine(options.TargetRepo, category.TargetRoot);

        foreach (var entry in entries)
        {
            if (category.EntriesAreDirectories)
            {
                SyncDirectory(
                    Path.Combine(sourceRoot, entry),
                    Path.Combine(targetRoot, entry),
                    category.MakeFilesExecutable,
                    options, synced, skipped);
            }
            else
            {
                SyncFile(
                    Path.Combine(sourceRoot, entry),
                    Path.Combine(targetRoot, entry),
                    Path.Combine(category.TargetRoot, entry),
                    category.MakeFilesExecutable,
                    options, synced, skipped);
            }
        }
    }

    private static void SyncFile(
        string source,
        string target,
        string targetRelative,
        bool makeExecutable,
        SyncOptions options,
        List<string> synced,
        List<string> skipped)
    {
        if (PreservedPathGuard.IsPreserved(targetRelative))
        {
            Console.WriteLine($"  SKIP (preserved): {targetRelative}");
            return;
        }

        if (!File.Exists(source))
        {
            Console.Error.WriteLine($"  WARN: Missing source file: {source}");
            return;
        }

        if (options.DryRun)
        {
            Console.WriteLine($"  [DRY RUN] sync {targetRelative}");
            return;
        }

        DirectoryEnsurer.EnsureParent(target);
        if (FileSync.CopyIfDistinct(source, target))
        {
            synced.Add(targetRelative);
        }
        else
        {
            skipped.Add(targetRelative);
        }

        if (makeExecutable)
        {
            FileSync.EnsureExecutable(target);
        }
    }

    private static void SyncDirectory(
        string sourceDir,
        string targetDir,
        bool makeExecutable,
        SyncOptions options,
        List<string> synced,
        List<string> skipped)
    {
        if (!Directory.Exists(sourceDir))
        {
            Console.Error.WriteLine($"  WARN: Missing source directory: {sourceDir}");
            return;
        }

        if (!options.DryRun)
        {
            Directory.CreateDirectory(targetDir);
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
            var targetFile = Path.Combine(targetDir, relativePath);

            if (options.DryRun)
            {
                Console.WriteLine($"  [DRY RUN] sync {targetFile}");
                continue;
            }

            DirectoryEnsurer.EnsureParent(targetFile);
            if (FileSync.CopyIfDistinct(sourceFile, targetFile))
            {
                synced.Add(targetFile);
            }
            else
            {
                skipped.Add(targetFile);
            }

            if (makeExecutable)
            {
                FileSync.EnsureExecutable(targetFile);
            }
        }
    }

    private static void SetGitHooksPath(string targetRepo)
    {
        try
        {
            var psi = new ProcessStartInfo("git")
            {
                UseShellExecute = false,
                WorkingDirectory = targetRepo
            };
            psi.ArgumentList.Add("-C");
            psi.ArgumentList.Add(targetRepo);
            psi.ArgumentList.Add("config");
            psi.ArgumentList.Add("core.hooksPath");
            psi.ArgumentList.Add(".github/hooks");

            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  WARN: Could not set git core.hooksPath: {ex.Message}");
        }
    }

    internal static string ReadCanonicalVersion(string workflowStandardPath)
    {
        var versionLine = File.ReadLines(workflowStandardPath)
            .FirstOrDefault(static l => l.StartsWith("Standard-Version:", StringComparison.Ordinal));

        if (versionLine is null)
        {
            return "unknown";
        }

        return versionLine.Split(':', 2)[1].Trim();
    }
}
