using System.Reflection;
using FluentAssertions;
using GitGhStandardCli.Commands;
using GitGhStandardCli.Models;
using GitGhStandardCli.Services;

namespace Unit.Tests;

public sealed class CommandCoverageTests
{
	[Fact]
	public void SyncCommand_ShouldSynchronizeFilesAndWriteVersionStamp()
	{
		using var target = new TemporaryTargetRepository();
		var options = new SyncOptions(RepositoryPaths.Root, target.RootPath, false);

		var exitCode = SyncCommand.Run(options);

		exitCode.Should().Be(0);
		File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", "git-gh-process-standard.md")).Should()
			.BeTrue();
		File.Exists(Path.Combine(target.RootPath, ".squad", "workflows", ".git-gh-standard-version")).Should().BeTrue();
		File.ReadAllText(Path.Combine(target.RootPath, ".squad", "workflows", ".git-gh-standard-version"))
			.Trim()
			.Should().Be(RepositoryPaths.GetCanonicalVersion());
	}

	[Fact]
	public void CheckCommand_ShouldReturnEnforcementFailure_WhenAdaptersAreMissing()
	{
		using var target = new TemporaryTargetRepository();

		var exitCode = CheckCommand.Run(RepositoryPaths.Root, target.RootPath);

		exitCode.Should().Be(3);
	}

	[Fact]
	public void CheckCommand_ShouldReturnCanonicalSourceMissing_WhenSourceRepoIsUnavailable()
	{
		using var target = new TemporaryTargetRepository();

		var exitCode = CheckCommand.Run(Path.Combine(target.RootPath, "missing-source"), target.RootPath);

		exitCode.Should().Be(2);
	}

	[Fact]
	public void Program_ShouldPrintUsage_WhenNoArgsArePassed()
	{
		InvokeProgram().Should().Be(1);
	}

	[Fact]
	public void Program_ShouldRouteSyncAndCheckCommands()
	{
		using var target = new TemporaryTargetRepository();

		InvokeProgram("sync-git-gh-standard", target.RootPath, "--source", RepositoryPaths.Root)
			.Should().Be(0);
		InvokeProgram("check-git-gh-standard", target.RootPath, "--source", RepositoryPaths.Root)
			.Should().Be(4);
	}

	[Fact]
	public void Program_ShouldRejectUnsupportedCommands()
	{
		InvokeProgram("not-a-real-command").Should().Be(1);
	}

	[Fact]
	public void ServiceHelpers_ShouldCreateDirectories_CopyFilesAndRecognizePreservedPaths()
	{
		var tempRoot = Path.Combine(Path.GetTempPath(), $"squad-service-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempRoot);

		try
		{
			var nestedFile = Path.Combine(tempRoot, "nested", "deep", "sample.txt");
			DirectoryEnsurer.EnsureParent(nestedFile);
			Directory.Exists(Path.GetDirectoryName(nestedFile)).Should().BeTrue();

			var sourceFile = Path.Combine(tempRoot, "source.txt");
			File.WriteAllText(sourceFile, "hello world");

			var targetFile = Path.Combine(tempRoot, "nested", "deep", "copied.txt");
			FileSync.CopyIfDistinct(sourceFile, targetFile).Should().BeTrue();
			FileSync.CopyIfDistinct(sourceFile, targetFile).Should().BeFalse();

			File.ReadAllText(targetFile).Should().Be("hello world");
			FileSync.EnsureExecutable(targetFile);

			var manifestPath = Path.Combine(tempRoot, "manifest.txt");
			File.WriteAllLines(manifestPath, ["# comment", "", "first", "second"]);
			var entries = ManifestReader.ReadEntries(manifestPath);
			entries.Should().Equal(["first", "second"]);

			PreservedPathGuard.IsPreserved(".squad/routing.md").Should().BeTrue();
			PreservedPathGuard.IsPreserved("src/Program.cs").Should().BeFalse();
		}
		finally
		{
			if (Directory.Exists(tempRoot))
			{
				Directory.Delete(tempRoot, recursive: true);
			}
		}
	}

	private static int InvokeProgram(params string[] args)
	{
		var programType = typeof(SyncCommand).Assembly.GetTypes()
			.FirstOrDefault(t =>
				t.Name == "Program" &&
				t.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static) is not null);
		programType.Should().NotBeNull();

		var mainMethod =
			programType!.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
		mainMethod.Should().NotBeNull();

		return (int)mainMethod!.Invoke(null, [args])!;
	}
}
