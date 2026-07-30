namespace GitGhStandardCli.Services;

/// <summary>
/// Ensures parent directories exist before file operations.
/// </summary>
internal static class DirectoryEnsurer
{
    /// <summary>Creates the parent directory of <paramref name="filePath"/> if it does not exist.</summary>
    public static void EnsureParent(string filePath)
    {
        var parent = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }
}
