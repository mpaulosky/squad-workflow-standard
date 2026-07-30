using System.Runtime.InteropServices;

namespace GitGhStandardCli.Services;

/// <summary>
/// File copy with content-equality guard and Unix executable bit management.
/// </summary>
internal static class FileSync
{
    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="target"/> only when their contents differ.
    /// Returns true if the file was copied, false if skipped (identical).
    /// </summary>
    public static bool CopyIfDistinct(string source, string target)
    {
        if (File.Exists(target) && FilesAreIdentical(source, target))
        {
            return false;
        }

        File.Copy(source, target, overwrite: true);
        return true;
    }

    /// <summary>
    /// Sets the Unix executable bit (+x) on <paramref name="path"/>.
    /// No-op on Windows.
    /// </summary>
    public static void EnsureExecutable(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var current = File.GetUnixFileMode(path);
        var withExecute = current
            | UnixFileMode.UserExecute
            | UnixFileMode.GroupExecute
            | UnixFileMode.OtherExecute;

        if (current != withExecute)
        {
            File.SetUnixFileMode(path, withExecute);
        }
    }

    private static bool FilesAreIdentical(string a, string b)
    {
        var infoA = new FileInfo(a);
        var infoB = new FileInfo(b);

        if (infoA.Length != infoB.Length)
        {
            return false;
        }

        const int bufferSize = 4096;
        using var streamA = new FileStream(a, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
        using var streamB = new FileStream(b, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);

        Span<byte> bufA = stackalloc byte[bufferSize];
        Span<byte> bufB = stackalloc byte[bufferSize];

        int read;
        while ((read = streamA.Read(bufA)) > 0)
        {
            streamB.ReadExactly(bufB[..read]);
            if (!bufA[..read].SequenceEqual(bufB[..read]))
            {
                return false;
            }
        }

        return true;
    }
}
