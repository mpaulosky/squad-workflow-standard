#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Show-Usage {
    @"
Usage:
  check-git-gh-standard.ps1 /absolute/path/to/target-repo [--source-repo /absolute/path/to/canonical-repo]

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

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string]$File,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not (Test-Path -LiteralPath $File -PathType Leaf)) {
        $script:hasFailure = $true
        Write-Host "ADAPTER CHECK FAILED: missing file $File"
        return
    }

    $content = Get-Content -LiteralPath $File -Raw
    if (-not $content.Contains($Expected)) {
        $script:hasFailure = $true
        Write-Host "ADAPTER CHECK FAILED: $Message"
    }
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

& git -C $targetRepo rev-parse --is-inside-work-tree 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Target repo is not a git repository: $targetRepo"
    exit 1
}

if (-not (Test-Path -LiteralPath $sourceRepo -PathType Container)) {
    Write-Host "Canonical source repository path not found: $sourceRepo"
    exit 2
}

$workflowStandard = Join-Path $sourceRepo "source/.squad/workflows/git-gh-process-standard.md"
$workflowBaselineManifest = Join-Path $sourceRepo "source/.squad/workflows/workflow-baseline-manifest.txt"
$hookBaselineManifest = Join-Path $sourceRepo "source/.squad/workflows/hook-baseline-manifest.txt"

if (-not (Test-Path -LiteralPath $workflowStandard -PathType Leaf)) {
    Write-Host "ERROR: Canonical workflow standard not found: $workflowStandard"
    exit 2
}

$canonicalVersion = ""
$versionLine = (Get-Content -LiteralPath $workflowStandard | Where-Object { $_ -match '^Standard-Version:' } | Select-Object -First 1)
if ($versionLine) {
    $canonicalVersion = ($versionLine -split '\s+', 2)[1].Trim()
}

$localVersionFile = Join-Path $targetRepo ".squad/workflows/.git-gh-standard-version"
$localVersion = "missing"
$script:hasFailure = $false
$hasDrift = $false

if (Test-Path -LiteralPath $localVersionFile -PathType Leaf) {
    $localVersion = (Get-Content -LiteralPath $localVersionFile -Raw).Trim()
}

Write-Host "Canonical version: $(if ($canonicalVersion) { $canonicalVersion } else { "unknown" })"
Write-Host "Local version:     $localVersion"

if ([string]::IsNullOrWhiteSpace($canonicalVersion)) {
    Write-Host "ERROR: Canonical version not found."
    exit 2
}

if ($localVersion -ne $canonicalVersion) {
    $script:hasFailure = $true
    $hasDrift = $true
    Write-Host "STATUS: DRIFT DETECTED"
    Write-Host "Policy: detect-and-prompt before gated issue work."
    Write-Host "Choose one:"
    Write-Host "  1) Update now: scripts/squad/sync-git-gh-standard.sh $targetRepo"
    Write-Host "  2) Defer: continue now, but rerun this check before next gated work"
    $localCanonicalFile = Join-Path $targetRepo ".squad/workflows/git-gh-process-standard.md"
    if (Test-Path -LiteralPath $localCanonicalFile -PathType Leaf) {
        Write-Host "  3) View diff: diff -u \"
        Write-Host "       $localCanonicalFile \"
        Write-Host "       $workflowStandard"
    } else {
        Write-Host "  3) View diff: local canonical file missing; sync first"
    }
}

Assert-FileContains -File (Join-Path $targetRepo ".squad/routing.md") -Expected ".squad/workflows/git-gh-process-standard.md" -Message ".squad/routing.md must reference canonical workflow source"
Assert-FileContains -File (Join-Path $targetRepo ".squad/routing.md") -Expected ".squad/templates/issue-lifecycle.md" -Message ".squad/routing.md must bind issue lifecycle template"
Assert-FileContains -File (Join-Path $targetRepo ".squad/routing.md") -Expected "single issue uses standard branch flow; 2+" -Message ".squad/routing.md must enforce standard-vs-worktree flow selection"
Assert-FileContains -File (Join-Path $targetRepo ".squad/routing.md") -Expected 'never push directly to `main` or `dev`' -Message ".squad/routing.md must hard-gate direct main/dev pushes"
Assert-FileContains -File (Join-Path $targetRepo ".squad/ceremonies.md") -Expected ".squad/workflows/git-gh-process-standard.md" -Message ".squad/ceremonies.md must reference canonical workflow source"
Assert-FileContains -File (Join-Path $targetRepo ".squad/templates/issue-lifecycle.md") -Expected "Workflow Standard Binding" -Message ".squad/templates/issue-lifecycle.md must include workflow standard binding section"
Assert-FileContains -File (Join-Path $targetRepo ".squad/templates/issue-lifecycle.md") -Expected ('Standard version: `{0}`' -f $canonicalVersion) -Message ".squad/templates/issue-lifecycle.md must bind to canonical standard version"
Assert-FileContains -File (Join-Path $targetRepo ".squad/templates/issue-lifecycle.md") -Expected "Enforcement level: hard gate" -Message ".squad/templates/issue-lifecycle.md must explicitly declare hard gate enforcement"
Assert-FileContains -File (Join-Path $targetRepo ".squad/templates/issue-lifecycle.md") -Expected 'Default branch policy: branch from `main`, PR to `main`' -Message ".squad/templates/issue-lifecycle.md must enforce main-first branch + PR policy"
Assert-FileContains -File (Join-Path $targetRepo ".squad/skills/git-workflow-standard/SKILL.md") -Expected ('Standard version: `{0}`' -f $canonicalVersion) -Message ".squad/skills/git-workflow-standard/SKILL.md must match canonical standard version"

$configuredHooksPath = (& git -C $targetRepo config --get core.hooksPath 2>$null)
$normalizedHooksPath = $configuredHooksPath
if ($normalizedHooksPath) {
    $normalizedHooksPath = $normalizedHooksPath.Trim()
    if ($normalizedHooksPath.StartsWith("./")) {
        $normalizedHooksPath = $normalizedHooksPath.Substring(2)
    }
}

if ([string]::IsNullOrWhiteSpace($configuredHooksPath)) {
    $script:hasFailure = $true
    Write-Host "ADAPTER CHECK FAILED: git core.hooksPath is not configured"
} elseif ($normalizedHooksPath -ne ".github/hooks") {
    $script:hasFailure = $true
    Write-Host "ADAPTER CHECK FAILED: git core.hooksPath must be '.github/hooks' (found: $configuredHooksPath)"
}

if (Test-Path -LiteralPath $workflowBaselineManifest -PathType Leaf) {
    $targetWorkflowBaselineManifest = Join-Path $targetRepo ".squad/workflows/workflow-baseline-manifest.txt"
    if (-not (Test-Path -LiteralPath $targetWorkflowBaselineManifest -PathType Leaf)) {
        $script:hasFailure = $true
        Write-Host "ADAPTER CHECK FAILED: missing file $targetWorkflowBaselineManifest"
    } elseif (-not (Test-FilesEqual -PathA $workflowBaselineManifest -PathB $targetWorkflowBaselineManifest)) {
        $script:hasFailure = $true
        Write-Host "ADAPTER CHECK FAILED: workflow baseline manifest drift detected"
    }

    foreach ($line in Get-Content -LiteralPath $workflowBaselineManifest) {
        $workflowFile = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($workflowFile) -or $workflowFile.StartsWith("#")) {
            continue
        }

        $sourceWorkflow = Join-Path $sourceRepo ("source/workflows/{0}" -f $workflowFile)
        $targetWorkflow = Join-Path $targetRepo (".github/workflows/{0}" -f $workflowFile)

        if (-not (Test-Path -LiteralPath $sourceWorkflow -PathType Leaf)) {
            $script:hasFailure = $true
            Write-Host "ADAPTER CHECK FAILED: missing canonical workflow $sourceWorkflow"
            continue
        }

        if (-not (Test-Path -LiteralPath $targetWorkflow -PathType Leaf)) {
            $script:hasFailure = $true
            Write-Host "ADAPTER CHECK FAILED: missing target workflow $targetWorkflow"
            continue
        }

        if (-not (Test-FilesEqual -PathA $sourceWorkflow -PathB $targetWorkflow)) {
            $script:hasFailure = $true
            Write-Host "ADAPTER CHECK FAILED: workflow drift detected for $workflowFile"
        }
    }
}

if (Test-Path -LiteralPath $hookBaselineManifest -PathType Leaf) {
    $targetHookBaselineManifest = Join-Path $targetRepo ".squad/workflows/hook-baseline-manifest.txt"
    if (-not (Test-Path -LiteralPath $targetHookBaselineManifest -PathType Leaf)) {
        $script:hasFailure = $true
        Write-Host "ADAPTER CHECK FAILED: missing file $targetHookBaselineManifest"
    } elseif (-not (Test-FilesEqual -PathA $hookBaselineManifest -PathB $targetHookBaselineManifest)) {
        $script:hasFailure = $true
        Write-Host "ADAPTER CHECK FAILED: hook baseline manifest drift detected"
    }

    foreach ($line in Get-Content -LiteralPath $hookBaselineManifest) {
        $hookFile = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($hookFile) -or $hookFile.StartsWith("#")) {
            continue
        }

        $sourceHook = Join-Path $sourceRepo ("source/hooks/{0}" -f $hookFile)
        $targetHook = Join-Path $targetRepo (".github/hooks/{0}" -f $hookFile)

        if (-not (Test-Path -LiteralPath $sourceHook -PathType Leaf)) {
            $script:hasFailure = $true
            Write-Host "ADAPTER CHECK FAILED: missing canonical hook $sourceHook"
            continue
        }

        if (-not (Test-Path -LiteralPath $targetHook -PathType Leaf)) {
            $script:hasFailure = $true
            Write-Host "ADAPTER CHECK FAILED: missing target hook $targetHook"
            continue
        }

        if (-not (Test-FilesEqual -PathA $sourceHook -PathB $targetHook)) {
            $script:hasFailure = $true
            Write-Host "ADAPTER CHECK FAILED: hook drift detected for $hookFile"
        }

        if (-not $IsWindows) {
            $mode = (Get-Item -LiteralPath $targetHook).Mode
            if (-not $mode.Contains("x")) {
                $script:hasFailure = $true
                Write-Host "ADAPTER CHECK FAILED: hook is not executable $targetHook"
            }
        }
    }
}

if (-not $script:hasFailure) {
    Write-Host "STATUS: OK (version and hard-gate adapters in sync)"
    exit 0
}

Write-Host "STATUS: ENFORCEMENT INCOMPLETE"
Write-Host "Fix drift and adapter bindings, then rerun this check."
Write-Host "Suggested action: scripts/squad/sync-git-gh-standard.sh $targetRepo"
Write-Host "Exit code map: 0=ok, 2=canonical missing, 3=drift, 4=adapter enforcement failure"
if ($hasDrift) {
    exit 3
}
exit 4
