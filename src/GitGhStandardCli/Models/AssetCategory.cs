namespace GitGhStandardCli.Models;

/// <summary>
/// Represents one logical group of assets managed by the standard-pack.
/// Entries in the manifest are either file names or directory names depending on <see cref="EntriesAreDirectories"/>.
/// </summary>
internal sealed record AssetCategory(
    string Name,
    string SourceRoot,
    string TargetRoot,
    string ManifestFile,
    bool EntriesAreDirectories = false,
    bool MakeFilesExecutable = false)
{
    /// <summary>GitHub workflow files synced from .github/workflows → .github/workflows/</summary>
    public static readonly AssetCategory Workflows = new(
        Name: "Workflows",
        SourceRoot: Path.Combine(".github", "workflows"),
        TargetRoot: Path.Combine(".github", "workflows"),
        ManifestFile: Path.Combine("source", ".squad", "workflows", "workflow-baseline-manifest.txt"));

    /// <summary>Git hook files synced from .github/hooks → .github/hooks/ (made +x after copy)</summary>
    public static readonly AssetCategory Hooks = new(
        Name: "Hooks",
        SourceRoot: Path.Combine(".github", "hooks"),
        TargetRoot: Path.Combine(".github", "hooks"),
        ManifestFile: Path.Combine("source", ".squad", "workflows", "hook-baseline-manifest.txt"),
        MakeFilesExecutable: true);

    /// <summary>Copilot skill directories synced from .github/skills → .github/skills/</summary>
    public static readonly AssetCategory Skills = new(
        Name: "Skills",
        SourceRoot: Path.Combine(".github", "skills"),
        TargetRoot: Path.Combine(".github", "skills"),
        ManifestFile: Path.Combine("source", ".squad", "workflows", "skill-manifest.txt"),
        EntriesAreDirectories: true);

    /// <summary>Copilot instruction files synced from .github/instructions → .github/instructions/</summary>
    public static readonly AssetCategory Instructions = new(
        Name: "Instructions",
        SourceRoot: Path.Combine(".github", "instructions"),
        TargetRoot: Path.Combine(".github", "instructions"),
        ManifestFile: Path.Combine("source", ".squad", "workflows", "instruction-manifest.txt"));

    /// <summary>Copilot prompt files synced from .github/prompts → .github/prompts/</summary>
    public static readonly AssetCategory Prompts = new(
        Name: "Prompts",
        SourceRoot: Path.Combine(".github", "prompts"),
        TargetRoot: Path.Combine(".github", "prompts"),
        ManifestFile: Path.Combine("source", ".squad", "workflows", "prompt-manifest.txt"));

    /// <summary>Agent definition files synced from .github/agents → .github/agents/</summary>
    public static readonly AssetCategory Agents = new(
        Name: "Agents",
        SourceRoot: Path.Combine(".github", "agents"),
        TargetRoot: Path.Combine(".github", "agents"),
        ManifestFile: Path.Combine("source", ".squad", "workflows", "agent-manifest.txt"));

    /// <summary>Squad skill directories synced from source/.squad/skills → .squad/skills/</summary>
    public static readonly AssetCategory SquadSkills = new(
        Name: "Squad Skills",
        SourceRoot: Path.Combine("source", ".squad", "skills"),
        TargetRoot: Path.Combine(".squad", "skills"),
        ManifestFile: Path.Combine("source", ".squad", "workflows", "squad-skill-manifest.txt"),
        EntriesAreDirectories: true);

    /// <summary>All categories in sync order. Workflows and Hooks first (existing behavior), then augmentation categories.</summary>
    public static readonly IReadOnlyList<AssetCategory> All =
    [
        Workflows,
        Hooks,
        Skills,
        Instructions,
        Prompts,
        Agents,
        SquadSkills,
    ];
}
