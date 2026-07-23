using System.Diagnostics;
using System.Runtime.InteropServices;

static int PrintUsage()
{
    Console.WriteLine(
        """
        Usage:
          git-gh-standard-cli sync-git-gh-standard <target-repo> [--source-repo <source-repo>]
          git-gh-standard-cli check-git-gh-standard <target-repo> [--source-repo <source-repo>]
          git-gh-standard-cli sync-mesh [--init] [mesh.json]

        Notes:
          - This CLI coexists with shell scripts and delegates execution to the script equivalents.
          - On Windows it uses PowerShell scripts when available; on non-Windows it uses bash scripts.
        """);
    return 1;
}

static string? FindRepoRoot(string start)
{
    var current = new DirectoryInfo(Path.GetFullPath(start));
    while (current is not null)
    {
        var gitPath = Path.Combine(current.FullName, ".git");
        if (Directory.Exists(gitPath) || File.Exists(gitPath))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return null;
}

static (string FileName, List<string> Args) BuildScriptInvocation(string command, string repoRoot, string[] forwardedArgs)
{
    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    return command switch
    {
        "sync-git-gh-standard" => BuildInvocation(
            repoRoot,
            Path.Combine("scripts", "squad", "sync-git-gh-standard.sh"),
            Path.Combine("scripts", "squad", "sync-git-gh-standard.ps1"),
            forwardedArgs,
            isWindows),
        "check-git-gh-standard" => BuildInvocation(
            repoRoot,
            Path.Combine("scripts", "squad", "check-git-gh-standard.sh"),
            Path.Combine("scripts", "squad", "check-git-gh-standard.ps1"),
            forwardedArgs,
            isWindows),
        "sync-mesh" => BuildSyncMeshInvocation(repoRoot, forwardedArgs, isWindows),
        _ => throw new InvalidOperationException($"Unsupported command: {command}")
    };
}

static (string FileName, List<string> Args) BuildSyncMeshInvocation(string repoRoot, string[] forwardedArgs, bool isWindows)
{
    if (isWindows)
    {
        var scriptPath = Path.Combine(repoRoot, ".squad", "templates", "skills", "distributed-mesh", "sync-mesh.ps1");
        var args = new List<string> { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath };
        foreach (var arg in forwardedArgs)
        {
            args.Add(arg.Equals("--init", StringComparison.OrdinalIgnoreCase) ? "-Init" : arg);
        }

        return ("pwsh", args);
    }

    var bashScriptPath = Path.Combine(repoRoot, ".squad", "templates", "skills", "distributed-mesh", "sync-mesh.sh");
    var bashArgs = new List<string> { bashScriptPath };
    bashArgs.AddRange(forwardedArgs);
    return ("bash", bashArgs);
}

static (string FileName, List<string> Args) BuildInvocation(
    string repoRoot,
    string bashRelativePath,
    string pwshRelativePath,
    string[] forwardedArgs,
    bool isWindows)
{
    if (isWindows)
    {
        var scriptPath = Path.Combine(repoRoot, pwshRelativePath);
        return ("pwsh", new List<string> { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath }.Concat(forwardedArgs).ToList());
    }

    var bashScriptPath = Path.Combine(repoRoot, bashRelativePath);
    var bashArgs = new List<string> { bashScriptPath };
    bashArgs.AddRange(forwardedArgs);
    return ("bash", bashArgs);
}

static int RunScript(string fileName, List<string> args)
{
    var psi = new ProcessStartInfo
    {
        FileName = fileName,
        UseShellExecute = false,
        RedirectStandardOutput = false,
        RedirectStandardError = false
    };

    foreach (var arg in args)
    {
        psi.ArgumentList.Add(arg);
    }

    using var process = Process.Start(psi);
    if (process is null)
    {
        Console.Error.WriteLine($"Failed to start process: {fileName}");
        return 1;
    }

    process.WaitForExit();
    return process.ExitCode;
}

if (args.Length == 0)
{
    return PrintUsage();
}

var command = args[0];
if (command is not ("sync-git-gh-standard" or "check-git-gh-standard" or "sync-mesh"))
{
    return PrintUsage();
}

var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
if (repoRoot is null)
{
    Console.Error.WriteLine("Unable to locate repository root (.git). Run this command from within the repository.");
    return 1;
}

var forwarded = args.Skip(1).ToArray();
var invocation = BuildScriptInvocation(command, repoRoot, forwarded);
return RunScript(invocation.FileName, invocation.Args);
