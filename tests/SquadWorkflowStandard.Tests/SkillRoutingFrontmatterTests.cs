using System.Text.RegularExpressions;
using FluentAssertions;

namespace SquadWorkflowStandard.Tests;

public sealed class SkillRoutingFrontmatterTests
{
	private static readonly string SkillsRoot = Path.Combine(RepositoryPaths.Root, "source", ".squad", "skills");

	[Fact]
	public void AllSquadSkills_ShouldHaveDescriptionWithWhenAndInvokes()
	{
		var skillFiles = Directory.GetFiles(SkillsRoot, "SKILL.md", SearchOption.AllDirectories);
		skillFiles.Should().NotBeEmpty();

		foreach (var skillFile in skillFiles)
		{
			var description = ReadFrontmatterDescription(skillFile);

			description.Should().NotBeNullOrWhiteSpace($"description is required in {skillFile}");
			description.Should().Contain("WHEN:", $"WHEN routing cue is required in {skillFile}");
			description.Should().Contain("INVOKES:", $"INVOKES routing cue is required in {skillFile}");
			description.Should().NotContain("DO NOT USE FOR:", $"anti-trigger clauses are disallowed in {skillFile}");

			// Keep trigger lists concise and machine-routable.
			CountWhenPhrases(description).Should()
				.BeGreaterThanOrEqualTo(3, $"at least 3 WHEN phrases are required in {skillFile}");
		}
	}

	[Theory]
	[InlineData("git-workflow-standard", "standard branch flow")]
	[InlineData("build-repair", "build is broken")]
	[InlineData("pre-push-test-gate", "before git push")]
	[InlineData("release-process", "prepare MyBlog release PR")]
	[InlineData("sprint-planning", "plan to sprint breakdown")]
	[InlineData("merged-pr-guard", "stale squad branch commit")]
	public void PrioritySkills_ShouldRetainExpectedTriggerPhrase(string skillName, string requiredPhrase)
	{
		var skillFile = Path.Combine(SkillsRoot, skillName, "SKILL.md");
		File.Exists(skillFile).Should().BeTrue($"skill file should exist for {skillName}");

		var description = ReadFrontmatterDescription(skillFile);
		description.Should().Contain(requiredPhrase, $"priority routing phrase should remain stable for {skillName}");
	}

	private static string ReadFrontmatterDescription(string skillFile)
	{
		var lines = File.ReadAllLines(skillFile);
		lines.Should().NotBeEmpty();
		lines[0].Should().Be("---", $"YAML frontmatter must start with --- in {skillFile}");

		var frontmatter = new List<string>();
		for (var i = 1; i < lines.Length; i++)
		{
			if (lines[i] == "---")
			{
				break;
			}

			frontmatter.Add(lines[i]);
		}

		var descriptionLine = frontmatter.FirstOrDefault(static line =>
			line.TrimStart().StartsWith("description:", StringComparison.Ordinal));
		descriptionLine.Should().NotBeNull($"frontmatter description key is required in {skillFile}");
		if (descriptionLine is null)
		{
			throw new InvalidOperationException($"frontmatter description key is required in {skillFile}");
		}

		var value = descriptionLine.Split(':', 2)[1].Trim();
		if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
		{
			value = value[1..^1];
		}

		return value;
	}

	private static int CountWhenPhrases(string description)
	{
		var whenStart = description.IndexOf("WHEN:", StringComparison.Ordinal);
		var invokesStart = description.IndexOf("INVOKES:", StringComparison.Ordinal);
		if (whenStart < 0 || invokesStart < 0 || invokesStart <= whenStart)
		{
			return 0;
		}

		var whenSegment = description[whenStart..invokesStart];
		return Regex.Matches(whenSegment, "\"([^\"]+)\"").Count;
	}
}