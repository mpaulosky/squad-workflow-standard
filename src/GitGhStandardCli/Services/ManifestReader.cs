namespace GitGhStandardCli.Services;

/// <summary>
/// Reads a manifest file, returning non-empty, non-comment entries.
/// </summary>
internal static class ManifestReader
{
    public static IReadOnlyList<string> ReadEntries(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return [];
        }

        return File.ReadAllLines(manifestPath)
            .Select(static line => line.Trim().TrimEnd('\r'))
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Where(static line => !line.StartsWith('#'))
            .ToList();
    }
}
