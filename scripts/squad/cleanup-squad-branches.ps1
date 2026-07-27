#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Show-Usage {
	@"
Usage:
  cleanup-squad-branches.ps1 [options]

Options:
  --repo <owner/repo>       GitHub repository (default: inferred from gh repo view)
  --remote <name>           Git remote to inspect/delete (default: origin)
  --orphan-days <days>      Minimum age in days before deleting orphaned branches (default: 14)
  --delete-remote           Delete eligible remote branches
  --force-local             Use git branch -D for eligible local branches
  --force-worktree          Use git worktree remove --force for eligible worktrees
  --apply                   Apply changes (default is dry-run)
  -h, --help                Show this help

Behavior:
	- Targets branch patterns: squad/*, sprint/*, and hotfix/*
	- Never deletes protected branches: main/dev/preview/insiders/default branch
	- Deletes only when branch is eligible:
	  * linked PR is merged or closed (and no open PR exists), or
	  * linked issue is closed (and no open PR exists), or
	  * branch is orphaned (no PR + no issue), older than --orphan-days
"@
}

function Require-Command {
	param([Parameter(Mandatory = $true)][string]$Name)
	if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
		throw "Missing required command: $Name"
	}
}

$repo = ""
$remote = "origin"
$orphanDays = 14
$apply = $false
$deleteRemote = $false
$forceLocal = $false
$forceWorktree = $false
$defaultBranch = ""

for ($i = 0; $i -lt $args.Count; $i++) {
	$arg = $args[$i]
	switch ($arg) {
		"--repo" {
			$i++
			if ($i -ge $args.Count) { throw "Missing value for --repo" }
			$repo = $args[$i]
		}
		"--remote" {
			$i++
			if ($i -ge $args.Count) { throw "Missing value for --remote" }
			$remote = $args[$i]
		}
		"--orphan-days" {
			$i++
			if ($i -ge $args.Count) { throw "Missing value for --orphan-days" }
			$orphanDays = [int]$args[$i]
		}
		"--delete-remote" { $deleteRemote = $true }
		"--force-local" { $forceLocal = $true }
		"--force-worktree" { $forceWorktree = $true }
		"--apply" { $apply = $true }
		"-h" { Show-Usage; exit 0 }
		"--help" { Show-Usage; exit 0 }
		default {
			throw "Unexpected argument: $arg"
		}
	}
}

if ($orphanDays -lt 0) {
	throw "--orphan-days must be a non-negative integer"
}

& git rev-parse --is-inside-work-tree 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
	throw "Run this script inside a git repository."
}

Require-Command -Name git
Require-Command -Name gh

if ([string]::IsNullOrWhiteSpace($repo)) {
	$repo = (& gh repo view --json nameWithOwner --jq '.nameWithOwner').Trim()
}

$defaultBranch = (& gh repo view $repo --json defaultBranchRef --jq '.defaultBranchRef.name' 2>$null).Trim()
if ([string]::IsNullOrWhiteSpace($defaultBranch) -or $defaultBranch -eq "null") {
	$defaultBranch = "main"
}

function Is-ProtectedBranch {
	param([Parameter(Mandatory = $true)][string]$Branch)

	if ($Branch -in @("main", "dev", "preview", "insiders")) {
		return $true
	}

	return $Branch -eq $defaultBranch
}

$mainWorktree = (& git rev-parse --show-toplevel).Trim()
$currentBranch = (& git branch --show-current).Trim()
$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

& git fetch $remote --prune 2>$null | Out-Null

$candidates = [System.Collections.Generic.HashSet[string]]::new()
$localExists = @{}
$remoteExists = @{}
$worktreesByBranch = @{}

$localBranches = & git for-each-ref --format='%(refname:short)' 'refs/heads/squad/*' 'refs/heads/sprint/*' 'refs/heads/hotfix/*'
foreach ($branch in $localBranches) {
	if ([string]::IsNullOrWhiteSpace($branch)) { continue }
	[void]$candidates.Add($branch)
	$localExists[$branch] = $true
}

$remoteRefs = & git for-each-ref --format='%(refname:short)' "refs/remotes/$remote/squad/*" "refs/remotes/$remote/sprint/*" "refs/remotes/$remote/hotfix/*"
foreach ($remoteRef in $remoteRefs) {
	if ([string]::IsNullOrWhiteSpace($remoteRef)) { continue }
	$branch = $remoteRef.Substring($remote.Length + 1)
	[void]$candidates.Add($branch)
	$remoteExists[$branch] = $true
}

$wtPath = ""
$wtBranch = ""
$wtLines = & git worktree list --porcelain
foreach ($line in $wtLines + "") {
	if ([string]::IsNullOrWhiteSpace($line)) {
		if ($wtPath -and $wtBranch) {
			$branchName = $wtBranch -replace '^refs/heads/', ''
			if (-not $worktreesByBranch.ContainsKey($branchName)) {
				$worktreesByBranch[$branchName] = [System.Collections.Generic.List[string]]::new()
			}
			$worktreesByBranch[$branchName].Add($wtPath)
		}
		$wtPath = ""
		$wtBranch = ""
		continue
	}

	if ($line.StartsWith("worktree ")) {
		$wtPath = $line.Substring("worktree ".Length)
	}
	elseif ($line.StartsWith("branch refs/heads/")) {
		$wtBranch = $line.Substring("branch ".Length)
	}
}

function Get-BranchEpoch {
	param([Parameter(Mandatory = $true)][string]$Branch)

	$best = 0L

	if ($localExists.ContainsKey($Branch)) {
		$epoch = (& git log -1 --format=%ct "refs/heads/$Branch" 2>$null).Trim()
		if ($epoch -match '^\d+$' -and [long]$epoch -gt $best) {
			$best = [long]$epoch
		}
	}

	if ($remoteExists.ContainsKey($Branch)) {
		$epoch = (& git log -1 --format=%ct "refs/remotes/$remote/$Branch" 2>$null).Trim()
		if ($epoch -match '^\d+$' -and [long]$epoch -gt $best) {
			$best = [long]$epoch
		}
	}

	return $best
}

Write-Host "Repo: $repo"
Write-Host "Default branch: $defaultBranch"
Write-Host "Remote: $remote"
Write-Host "Apply: $apply"
Write-Host "Delete remote: $deleteRemote"
Write-Host "Force local delete: $forceLocal"
Write-Host "Force worktree remove: $forceWorktree"
Write-Host "Orphan threshold (days): $orphanDays"
Write-Host ""

if ($candidates.Count -eq 0) {
	Write-Host "No candidate squad/*, sprint/*, or hotfix/* branches found."
	exit 0
}

Write-Host "Branch evaluation summary:"
Write-Host "BRANCH | ELIGIBLE | REASON | PR_STATE | ISSUE_STATE | AGE_DAYS | LOCAL | REMOTE | WORKTREES"

$eligibleCount = 0
$localDeleteCount = 0
$remoteDeleteCount = 0
$worktreeRemoveCount = 0
$skipCount = 0

$reasonCounts = @{}
$planLocal = [System.Collections.Generic.List[string]]::new()
$planRemote = [System.Collections.Generic.List[string]]::new()
$planWorktree = [System.Collections.Generic.List[string]]::new()
$protectedSkips = [System.Collections.Generic.List[string]]::new()
$actionSkips = [System.Collections.Generic.List[string]]::new()
$deletedLocal = [System.Collections.Generic.List[string]]::new()
$deletedRemote = [System.Collections.Generic.List[string]]::new()
$removedWorktrees = [System.Collections.Generic.List[string]]::new()

function Add-ReasonCount {
	param([Parameter(Mandatory = $true)][string]$Reason)

	if ($reasonCounts.ContainsKey($Reason)) {
		$reasonCounts[$Reason] = [int]$reasonCounts[$Reason] + 1
	}
	else {
		$reasonCounts[$Reason] = 1
	}
}

function Write-List {
	param(
		[Parameter(Mandatory = $true)][string]$Title,
		[System.Collections.IEnumerable]$Items = @()
	)

	$itemsArray = @($Items)

	Write-Host $Title
	if ($itemsArray.Count -eq 0) {
		Write-Host "  - none"
		return
	}

	foreach ($item in $itemsArray) {
		Write-Host "  - $item"
	}
}

foreach ($branch in ($candidates | Sort-Object)) {
	$prState = "NONE"
	$issueState = "NONE"
	$reason = "NOT_ELIGIBLE"
	$eligible = $false
	$localFlag = if ($localExists.ContainsKey($branch)) { "yes" } else { "no" }
	$remoteFlag = if ($remoteExists.ContainsKey($branch)) { "yes" } else { "no" }
	$wtCount = if ($worktreesByBranch.ContainsKey($branch)) { $worktreesByBranch[$branch].Count } else { 0 }

	if (Is-ProtectedBranch -Branch $branch) {
		$reason = "PROTECTED_BRANCH"
		Add-ReasonCount -Reason $reason
		[void]$protectedSkips.Add($branch)
		Write-Host "$branch | FALSE | $reason | $prState | $issueState | n/a | $localFlag | $remoteFlag | $wtCount"
		continue
	}

	$prJsonRaw = & gh pr list --repo $repo --state all --head $branch --json number, state, mergedAt, closedAt, url 2>$null
	if (-not $prJsonRaw) {
		$prJsonRaw = "[]"
	}

	$prs = $prJsonRaw | ConvertFrom-Json
	if ($prs -isnot [System.Array]) {
		$prs = @($prs)
	}

	$hasOpen = @($prs | Where-Object { $_.state -eq "OPEN" })
	$hasMerged = @($prs | Where-Object { $null -ne $_.mergedAt })
	$hasClosed = @($prs | Where-Object { $_.state -eq "CLOSED" })

	if ($hasOpen.Count -gt 0) {
		$prState = "OPEN"
	}
	elseif ($hasMerged.Count -gt 0) {
		$prState = "MERGED"
	}
	elseif ($hasClosed.Count -gt 0) {
		$prState = "CLOSED"
	}

	$issueNumber = ""
	if ($branch -match '^(squad|sprint|hotfix)/(\d+)(-|$)') {
		$issueNumber = $Matches[2]
	}

	if ($issueNumber) {
		try {
			$issueJson = & gh issue view $issueNumber --repo $repo --json state, url, number 2>$null
			$issueState = (($issueJson | ConvertFrom-Json).state).ToUpperInvariant()
		}
		catch {
			$issueState = "MISSING"
		}
	}

	$branchEpoch = Get-BranchEpoch -Branch $branch
	$ageDays = if ($branchEpoch -gt 0) { [int](($now - $branchEpoch) / 86400) } else { -1 }

	if ($prState -eq "OPEN") {
		$eligible = $false
		$reason = "OPEN_PR"
	}
	elseif ($prState -eq "MERGED" -or $prState -eq "CLOSED") {
		$eligible = $true
		$reason = "PR_$prState"
	}
	elseif ($issueState -eq "CLOSED") {
		$eligible = $true
		$reason = "ISSUE_CLOSED"
	}
	elseif ($prState -eq "NONE" -and ($issueState -eq "NONE" -or $issueState -eq "MISSING")) {
		if ($ageDays -ge $orphanDays) {
			$eligible = $true
			$reason = "ORPHANED_AGE"
		}
		else {
			$eligible = $false
			$reason = "ORPHANED_TOO_RECENT"
		}
	}

	$ageText = if ($ageDays -ge 0) { "$ageDays" } else { "unknown" }

	Write-Host "$branch | $($eligible.ToString().ToUpperInvariant()) | $reason | $prState | $issueState | $ageText | $localFlag | $remoteFlag | $wtCount"
	Add-ReasonCount -Reason $reason

	if (-not $eligible) {
		continue
	}

	$eligibleCount++

	if ($worktreesByBranch.ContainsKey($branch)) {
		foreach ($wt in $worktreesByBranch[$branch]) {
			if ($wt -ne $mainWorktree) {
				[void]$planWorktree.Add("$branch ($reason) -> $wt")
			}
		}
	}

	if ($localExists.ContainsKey($branch) -and $branch -ne $currentBranch) {
		[void]$planLocal.Add("$branch ($reason)")
	}

	if ($deleteRemote -and $remoteExists.ContainsKey($branch)) {
		[void]$planRemote.Add("$branch ($reason)")
	}

	if (-not $apply) {
		continue
	}

	if ($worktreesByBranch.ContainsKey($branch)) {
		foreach ($wt in $worktreesByBranch[$branch]) {
			if ($wt -eq $mainWorktree) {
				continue
			}

			if ($forceWorktree) {
				& git worktree remove --force $wt
			}
			else {
				& git worktree remove $wt
			}

			if ($LASTEXITCODE -eq 0) {
				$worktreeRemoveCount++
				[void]$removedWorktrees.Add($wt)
			}
			else {
				$skipCount++
				Write-Host "Skip worktree remove (failed): $wt"
				[void]$actionSkips.Add("worktree:$wt:remove_failed")
			}
		}
	}

	if ($localExists.ContainsKey($branch)) {
		if ($branch -eq $currentBranch) {
			$skipCount++
			Write-Host "Skip local delete for current branch: $branch"
		}
		else {
			if ($forceLocal) {
				& git branch -D $branch
			}
			else {
				& git branch -d $branch
			}

			if ($LASTEXITCODE -eq 0) {
				$localDeleteCount++
				[void]$deletedLocal.Add($branch)
			}
			else {
				$skipCount++
				Write-Host "Skip local delete (failed): $branch"
				[void]$actionSkips.Add("branch:$branch:local_delete_failed")
			}
		}
	}

	if ($deleteRemote -and $remoteExists.ContainsKey($branch)) {
		$openNowRaw = & gh pr list --repo $repo --state open --head $branch --json number --jq 'length' 2>$null
		$openNow = 0
		if ($openNowRaw -and ($openNowRaw.ToString().Trim() -match '^\d+$')) {
			$openNow = [int]$openNowRaw.ToString().Trim()
		}

		if ($openNow -gt 0) {
			$skipCount++
			Write-Host "Skip remote delete (open PR exists): $branch"
			[void]$actionSkips.Add("branch:$branch:remote_blocked_open_pr")
		}
		else {
			& git push $remote --delete $branch
			if ($LASTEXITCODE -eq 0) {
				$remoteDeleteCount++
				[void]$deletedRemote.Add($branch)
			}
			else {
				$skipCount++
				Write-Host "Skip remote delete (failed): $branch"
				[void]$actionSkips.Add("branch:$branch:remote_delete_failed")
			}
		}
	}
}

if ($apply) {
	& git worktree prune
}

Write-Host ""
Write-Host "Classification by reason:"
if ($reasonCounts.Count -eq 0) {
	Write-Host "  - none"
}
else {
	foreach ($entry in ($reasonCounts.GetEnumerator() | Sort-Object Name)) {
		Write-Host "  - $($entry.Name): $($entry.Value)"
	}
}

Write-List -Title "Protected branches skipped:" -Items $protectedSkips

if ($apply) {
	Write-List -Title "Deleted local branches:" -Items $deletedLocal
	Write-List -Title "Deleted remote branches:" -Items $deletedRemote
	Write-List -Title "Removed worktrees:" -Items $removedWorktrees
	Write-List -Title "Skipped actions:" -Items $actionSkips
}
else {
	Write-List -Title "Dry-run local delete plan:" -Items $planLocal
	Write-List -Title "Dry-run remote delete plan:" -Items $planRemote
	Write-List -Title "Dry-run worktree remove plan:" -Items $planWorktree
}

Write-Host ""
Write-Host "Summary:"
Write-Host "Eligible branches: $eligibleCount"
Write-Host "Local branches deleted: $localDeleteCount"
Write-Host "Remote branches deleted: $remoteDeleteCount"
Write-Host "Worktrees removed: $worktreeRemoveCount"
Write-Host "Skipped actions: $skipCount"

if (-not $apply) {
	Write-Host "Dry-run only. Re-run with --apply to perform cleanup."
}
