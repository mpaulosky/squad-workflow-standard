#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Show-Usage {
    @"
Usage:
  sync-git-gh-standard.ps1 /absolute/path/to/target-repo [--source-repo /absolute/path/to/canonical-repo]

Optional environment variable:
  SQUAD_STANDARD_SOURCE_REPO=/absolute/path/to/canonical-repo
"@
}

function Resolve-RepoRoot {
    $scriptDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptDir "../..")).Path
}

function Test-FilesEqual {
    param(
        [Parameter(Mandatory = $true)][string]$PathA,
        [Parameter(Mandatory = $true)][string]$PathB
    )

    if (-not (Test-Path -LiteralPath $PathA -PathType Leaf) -or -not (Test-Path -LiteralPath $PathB -PathType Leaf)) {
        return $false
    }

    $hashA = (Get-FileHash -LiteralPath $PathA -Algorithm SHA256).Hash
    $hashB = (Get-FileHash -LiteralPath $PathB -Algorithm SHA256).Hash
    return $hashA -eq $hashB
}

function Copy-IfDistinct {
    param(
        [Parameter(Mandatory = $true)][string]$SourceFile,
        [Parameter(Mandatory = $true)][string]$TargetFile
    )

    $targetDir = Split-Path -Parent $TargetFile
    if ($targetDir -and -not (Test-Path -LiteralPath $targetDir)) {
        New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    }

    if (Test-FilesEqual -PathA $SourceFile -PathB $TargetFile) {
        return
    }

    Copy-Item -LiteralPath $SourceFile -Destination $TargetFile -Force
}

$scriptRepo = Resolve-RepoRoot
$targetRepo = ""
$sourceRepoOverride = ""

for ($i = 0; $i -lt $args.Count; $i++) {
    $arg = $args[$i]
    switch ($arg) {
        "--source-repo" {
            $i++
            if ($i -ge $args.Count) {
                Write-Host "Missing value for --source-repo"
                Show-Usage
                exit 1
            }
            $sourceRepoOverride = $args[$i]
        }
        "-h" { Show-Usage; exit 0 }
        "--help" { Show-Usage; exit 0 }
        default {
            if ([string]::IsNullOrWhiteSpace($targetRepo)) {
                $targetRepo = $arg
            } else {
                Write-Host "Unexpected argument: $arg"
                Show-Usage
                exit 1
            }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($targetRepo)) {
    Show-Usage
    exit 1
}

$sourceRepo = if ($sourceRepoOverride) { $sourceRepoOverride } elseif ($env:SQUAD_STANDARD_SOURCE_REPO) { $env:SQUAD_STANDARD_SOURCE_REPO } else { $scriptRepo }

if (-not (Test-Path -LiteralPath (Join-Path $targetRepo ".git") -PathType Container)) {
    Write-Host "Target repo is not a git repository: $targetRepo"
    exit 1
}

if (-not (Test-Path -LiteralPath $sourceRepo -PathType Container)) {
    Write-Host "Canonical source repository path not found: $sourceRepo"
    exit 1
}

New-Item -ItemType Directory -Force -Path (Join-Path $targetRepo ".squad/workflows") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $targetRepo ".squad/skills/git-workflow-standard") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $targetRepo ".github/workflows") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $targetRepo ".github/hooks") | Out-Null

$workflowStandard = Join-Path $sourceRepo "source/.squad/workflows/git-gh-process-standard.md"
$workflowReadme = Join-Path $sourceRepo "source/.squad/workflows/README.md"
$workflowSkill = Join-Path $sourceRepo "source/.squad/skills/git-workflow-standard/SKILL.md"
$workflowBaselineManifest = Join-Path $sourceRepo "source/.squad/workflows/workflow-baseline-manifest.txt"
$hookBaselineManifest = Join-Path $sourceRepo "source/.squad/workflows/hook-baseline-manifest.txt"

foreach ($requiredFile in @($workflowStandard, $workflowReadme, $workflowSkill)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        Write-Host "Missing canonical source file: $requiredFile"
        exit 2
    }
}

Copy-IfDistinct -SourceFile $workflowStandard -TargetFile (Join-Path $targetRepo ".squad/workflows/git-gh-process-standard.md")
Copy-IfDistinct -SourceFile $workflowReadme -TargetFile (Join-Path $targetRepo ".squad/workflows/README.md")
Copy-IfDistinct -SourceFile $workflowSkill -TargetFile (Join-Path $targetRepo ".squad/skills/git-workflow-standard/SKILL.md")

$syncedWorkflowCount = 0
if (Test-Path -LiteralPath $workflowBaselineManifest -PathType Leaf) {
    Copy-IfDistinct -SourceFile $workflowBaselineManifest -TargetFile (Join-Path $targetRepo ".squad/workflows/workflow-baseline-manifest.txt")

    foreach ($line in Get-Content -LiteralPath $workflowBaselineManifest) {
        $workflowFile = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($workflowFile) -or $workflowFile.StartsWith("#")) {
            continue
        }

        $sourceWorkflow = Join-Path $sourceRepo ("source/workflows/{0}" -f $workflowFile)
        $targetWorkflow = Join-Path $targetRepo (".github/workflows/{0}" -f $workflowFile)
        if (-not (Test-Path -LiteralPath $sourceWorkflow -PathType Leaf)) {
            Write-Host "Missing canonical workflow in source repo: $sourceWorkflow"
            exit 2
        }

        Copy-IfDistinct -SourceFile $sourceWorkflow -TargetFile $targetWorkflow
        $syncedWorkflowCount++
    }
}

$syncedHookCount = 0
if (Test-Path -LiteralPath $hookBaselineManifest -PathType Leaf) {
    Copy-IfDistinct -SourceFile $hookBaselineManifest -TargetFile (Join-Path $targetRepo ".squad/workflows/hook-baseline-manifest.txt")

    foreach ($line in Get-Content -LiteralPath $hookBaselineManifest) {
        $hookFile = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($hookFile) -or $hookFile.StartsWith("#")) {
            continue
        }

        $sourceHook = Join-Path $sourceRepo ("source/hooks/{0}" -f $hookFile)
        $targetHook = Join-Path $targetRepo (".github/hooks/{0}" -f $hookFile)
        if (-not (Test-Path -LiteralPath $sourceHook -PathType Leaf)) {
            Write-Host "Missing canonical hook in source repo: $sourceHook"
            exit 2
        }

        Copy-IfDistinct -SourceFile $sourceHook -TargetFile $targetHook
        if (-not $IsWindows) {
            & chmod +x $targetHook
        }
        $syncedHookCount++
    }
}

# Enforce hooks activation in the target repo.
& git -C $targetRepo config core.hooksPath ".github/hooks"

$version = "unknown"
$versionLine = (Get-Content -LiteralPath $workflowStandard | Where-Object { $_ -match '^Standard-Version:' } | Select-Object -First 1)
if ($versionLine) {
    $version = ($versionLine -split '\s+', 2)[1].Trim()
}
Set-Content -LiteralPath (Join-Path $targetRepo ".squad/workflows/.git-gh-standard-version") -Value $version

@"
Synced git/gh process standard from:
  $sourceRepo

Into:
  $targetRepo/.squad/workflows/git-gh-process-standard.md
  $targetRepo/.squad/workflows/README.md
  $targetRepo/.squad/skills/git-workflow-standard/SKILL.md
  $targetRepo/.squad/workflows/.git-gh-standard-version
"@ | Write-Host

if (Test-Path -LiteralPath $workflowBaselineManifest -PathType Leaf) {
    @"
  $targetRepo/.squad/workflows/workflow-baseline-manifest.txt

Workflow baseline synced:
  $syncedWorkflowCount workflow file(s) copied to $targetRepo/.github/workflows
"@ | Write-Host
}

if (Test-Path -LiteralPath $hookBaselineManifest -PathType Leaf) {
    @"
  $targetRepo/.squad/workflows/hook-baseline-manifest.txt

Hook baseline synced:
  $syncedHookCount hook file(s) copied to $targetRepo/.github/hooks
"@ | Write-Host
}

Write-Host "Next step: run scripts/squad/check-git-gh-standard.sh $targetRepo"
