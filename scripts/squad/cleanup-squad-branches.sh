#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  cleanup-squad-branches.sh [options]

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
  - Targets branch patterns: squad/* and sprint/*
  - Never deletes protected branches: main/dev/preview/insiders/default branch
  - Deletes only when branch is eligible:
    * linked PR is merged or closed (and no open PR exists), or
    * linked issue is closed (and no open PR exists), or
    * branch is orphaned (no PR + no issue), older than --orphan-days
EOF
}

require_command() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "Missing required command: $cmd"
    exit 1
  fi
}

to_upper() {
  printf '%s' "$1" | tr '[:lower:]' '[:upper:]'
}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO=""
REMOTE="origin"
ORPHAN_DAYS=14
APPLY=false
DELETE_REMOTE=false
FORCE_LOCAL=false
FORCE_WORKTREE=false
DEFAULT_BRANCH=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo)
      shift
      [[ $# -gt 0 ]] || { echo "Missing value for --repo"; usage; exit 1; }
      REPO="$1"
      ;;
    --remote)
      shift
      [[ $# -gt 0 ]] || { echo "Missing value for --remote"; usage; exit 1; }
      REMOTE="$1"
      ;;
    --orphan-days)
      shift
      [[ $# -gt 0 ]] || { echo "Missing value for --orphan-days"; usage; exit 1; }
      ORPHAN_DAYS="$1"
      ;;
    --delete-remote)
      DELETE_REMOTE=true
      ;;
    --force-local)
      FORCE_LOCAL=true
      ;;
    --force-worktree)
      FORCE_WORKTREE=true
      ;;
    --apply)
      APPLY=true
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unexpected argument: $1"
      usage
      exit 1
      ;;
  esac
  shift
done

if [[ ! "$ORPHAN_DAYS" =~ ^[0-9]+$ ]]; then
  echo "--orphan-days must be a non-negative integer"
  exit 1
fi

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "Run this script inside a git repository."
  exit 1
fi

require_command git
require_command gh
require_command jq

if [[ -z "$REPO" ]]; then
  REPO="$(gh repo view --json nameWithOwner --jq '.nameWithOwner')"
fi

DEFAULT_BRANCH="$(gh repo view "$REPO" --json defaultBranchRef --jq '.defaultBranchRef.name' 2>/dev/null || true)"
if [[ -z "$DEFAULT_BRANCH" || "$DEFAULT_BRANCH" == "null" ]]; then
  DEFAULT_BRANCH="main"
fi

is_protected_branch() {
  local branch="$1"
  case "$branch" in
    main|dev|preview|insiders)
      return 0
      ;;
  esac

  [[ "$branch" == "$DEFAULT_BRANCH" ]]
}

DEV_BRANCH="${SQUAD_DEV_BRANCH:-dev}"
MAIN_WORKTREE="$(git rev-parse --show-toplevel)"
CURRENT_BRANCH="$(git branch --show-current || true)"
NOW_EPOCH="$(date +%s)"

# Refresh remote refs before classifying branch state.
git fetch "$REMOTE" --prune >/dev/null 2>&1 || true

declare -A CANDIDATES=()
declare -A LOCAL_EXISTS=()
declare -A REMOTE_EXISTS=()
declare -A WORKTREE_BY_BRANCH=()

while IFS= read -r branch; do
  [[ -z "$branch" ]] && continue
  CANDIDATES["$branch"]=1
  LOCAL_EXISTS["$branch"]=1
done < <(git for-each-ref --format='%(refname:short)' 'refs/heads/squad/*' 'refs/heads/sprint/*')

while IFS= read -r remote_ref; do
  [[ -z "$remote_ref" ]] && continue
  branch="${remote_ref#${REMOTE}/}"
  CANDIDATES["$branch"]=1
  REMOTE_EXISTS["$branch"]=1
done < <(git for-each-ref --format='%(refname:short)' "refs/remotes/${REMOTE}/squad/*" "refs/remotes/${REMOTE}/sprint/*")

# Parse worktrees and map linked branches to paths.
wt_path=""
wt_branch=""
while IFS= read -r line || [[ -n "$line" ]]; do
  if [[ -z "$line" ]]; then
    if [[ -n "$wt_path" && -n "$wt_branch" ]]; then
      branch_name="${wt_branch#refs/heads/}"
      if [[ -n "${WORKTREE_BY_BRANCH[$branch_name]:-}" ]]; then
        WORKTREE_BY_BRANCH["$branch_name"]+=$'\n'"$wt_path"
      else
        WORKTREE_BY_BRANCH["$branch_name"]="$wt_path"
      fi
    fi
    wt_path=""
    wt_branch=""
    continue
  fi

  case "$line" in
    worktree\ *)
      wt_path="${line#worktree }"
      ;;
    branch\ refs/heads/*)
      wt_branch="${line#branch }"
      ;;
  esac
done < <(git worktree list --porcelain)

get_branch_epoch() {
  local branch="$1"
  local best=0

  if [[ -n "${LOCAL_EXISTS[$branch]:-}" ]]; then
    local_epoch="$(git log -1 --format=%ct "refs/heads/$branch" 2>/dev/null || echo 0)"
    if [[ "$local_epoch" =~ ^[0-9]+$ ]] && (( local_epoch > best )); then
      best=$local_epoch
    fi
  fi

  if [[ -n "${REMOTE_EXISTS[$branch]:-}" ]]; then
    remote_epoch="$(git log -1 --format=%ct "refs/remotes/$REMOTE/$branch" 2>/dev/null || echo 0)"
    if [[ "$remote_epoch" =~ ^[0-9]+$ ]] && (( remote_epoch > best )); then
      best=$remote_epoch
    fi
  fi

  echo "$best"
}

format_age_days() {
  local epoch="$1"
  if [[ "$epoch" =~ ^[0-9]+$ ]] && (( epoch > 0 )); then
    echo $(((NOW_EPOCH - epoch) / 86400))
  else
    echo "unknown"
  fi
}

echo "Repo: $REPO"
echo "Default branch: $DEFAULT_BRANCH"
echo "Remote: $REMOTE"
echo "Apply: $APPLY"
echo "Delete remote: $DELETE_REMOTE"
echo "Force local delete: $FORCE_LOCAL"
echo "Force worktree remove: $FORCE_WORKTREE"
echo "Orphan threshold (days): $ORPHAN_DAYS"
echo

if [[ ${#CANDIDATES[@]} -eq 0 ]]; then
  echo "No candidate squad/* or sprint/* branches found."
  exit 0
fi

echo "Branch evaluation summary:"
echo "BRANCH | ELIGIBLE | REASON | PR_STATE | ISSUE_STATE | AGE_DAYS | LOCAL | REMOTE | WORKTREES"

ELIGIBLE_COUNT=0
LOCAL_DELETE_COUNT=0
REMOTE_DELETE_COUNT=0
WORKTREE_REMOVE_COUNT=0
SKIP_COUNT=0

declare -A REASON_COUNTS=()
declare -a PLAN_LOCAL=()
declare -a PLAN_REMOTE=()
declare -a PLAN_WORKTREE=()
declare -a PROTECTED_SKIPS=()
declare -a ACTION_SKIPS=()
declare -a DELETED_LOCAL=()
declare -a DELETED_REMOTE=()
declare -a REMOVED_WORKTREES=()

add_reason_count() {
  local reason="$1"
  if [[ -n "${REASON_COUNTS[$reason]:-}" ]]; then
    REASON_COUNTS["$reason"]=$((REASON_COUNTS[$reason] + 1))
  else
    REASON_COUNTS["$reason"]=1
  fi
}

print_list() {
  local title="$1"
  shift
  echo "$title"
  if [[ $# -eq 0 ]]; then
    echo "  - none"
    return
  fi

  local item
  for item in "$@"; do
    echo "  - $item"
  done
}

while IFS= read -r branch; do
  pr_state="NONE"
  issue_state="NONE"
  reason="NOT_ELIGIBLE"
  eligible=false

  local_flag="no"
  remote_flag="no"
  worktree_count=0

  if [[ -n "${LOCAL_EXISTS[$branch]:-}" ]]; then
    local_flag="yes"
  fi

  if [[ -n "${REMOTE_EXISTS[$branch]:-}" ]]; then
    remote_flag="yes"
  fi

  wt_lines="${WORKTREE_BY_BRANCH[$branch]:-}"
  if [[ -n "$wt_lines" ]]; then
    worktree_count="$(printf '%s\n' "$wt_lines" | sed '/^$/d' | wc -l | tr -d ' ')"
  fi

  if is_protected_branch "$branch"; then
    reason="PROTECTED_BRANCH"
    add_reason_count "$reason"
    PROTECTED_SKIPS+=("$branch")
    echo "$branch | FALSE | $reason | $pr_state | $issue_state | n/a | $local_flag | $remote_flag | $worktree_count"
    continue
  fi

  pr_json="$(gh pr list --repo "$REPO" --state all --head "$branch" --json number,state,mergedAt,closedAt,url 2>/dev/null || echo '[]')"
  pr_has_merged="$(jq -r 'map(select(.mergedAt != null)) | length' <<<"$pr_json")"
  pr_has_open="$(jq -r 'map(select(.state == "OPEN")) | length' <<<"$pr_json")"
  pr_has_closed="$(jq -r 'map(select(.state == "CLOSED")) | length' <<<"$pr_json")"

  if [[ "$pr_has_open" =~ ^[0-9]+$ ]] && (( pr_has_open > 0 )); then
    pr_state="OPEN"
  elif [[ "$pr_has_merged" =~ ^[0-9]+$ ]] && (( pr_has_merged > 0 )); then
    pr_state="MERGED"
  elif [[ "$pr_has_closed" =~ ^[0-9]+$ ]] && (( pr_has_closed > 0 )); then
    pr_state="CLOSED"
  fi

  issue_number=""
  if [[ "$branch" =~ ^(squad|sprint)/([0-9]+)(-|$) ]]; then
    issue_number="${BASH_REMATCH[2]}"
  fi

  if [[ -n "$issue_number" ]]; then
    if issue_json="$(gh issue view "$issue_number" --repo "$REPO" --json state,url,number 2>/dev/null)"; then
      issue_state="$(jq -r '.state' <<<"$issue_json" | tr '[:lower:]' '[:upper:]')"
    else
      issue_state="MISSING"
    fi
  fi

  branch_epoch="$(get_branch_epoch "$branch")"
  age_days="$(format_age_days "$branch_epoch")"

  if [[ "$pr_state" == "OPEN" ]]; then
    eligible=false
    reason="OPEN_PR"
  elif [[ "$pr_state" == "MERGED" || "$pr_state" == "CLOSED" ]]; then
    eligible=true
    reason="PR_${pr_state}"
  elif [[ "$issue_state" == "CLOSED" ]]; then
    eligible=true
    reason="ISSUE_CLOSED"
  elif [[ "$pr_state" == "NONE" && ( "$issue_state" == "NONE" || "$issue_state" == "MISSING" ) ]]; then
    if [[ "$age_days" =~ ^[0-9]+$ ]] && (( age_days >= ORPHAN_DAYS )); then
      eligible=true
      reason="ORPHANED_AGE"
    else
      eligible=false
      reason="ORPHANED_TOO_RECENT"
    fi
  fi

  echo "$branch | $(to_upper "$eligible") | $reason | $pr_state | $issue_state | $age_days | $local_flag | $remote_flag | $worktree_count"
  add_reason_count "$reason"

  if [[ "$eligible" == true ]]; then
    ((ELIGIBLE_COUNT+=1))

    if [[ -n "$wt_lines" ]]; then
      while IFS= read -r wt; do
        [[ -z "$wt" ]] && continue
        if [[ "$wt" != "$MAIN_WORKTREE" ]]; then
          PLAN_WORKTREE+=("$branch ($reason) -> $wt")
        fi
      done <<<"$wt_lines"
    fi

    if [[ -n "${LOCAL_EXISTS[$branch]:-}" && "$branch" != "$CURRENT_BRANCH" ]]; then
      PLAN_LOCAL+=("$branch ($reason)")
    fi

    if [[ "$DELETE_REMOTE" == true && -n "${REMOTE_EXISTS[$branch]:-}" ]]; then
      PLAN_REMOTE+=("$branch ($reason)")
    fi

    if [[ "$APPLY" == true ]]; then
      if [[ -n "$wt_lines" ]]; then
        while IFS= read -r wt; do
          [[ -z "$wt" ]] && continue
          if [[ "$wt" == "$MAIN_WORKTREE" ]]; then
            continue
          fi

          if [[ "$FORCE_WORKTREE" == true ]]; then
            if git worktree remove --force "$wt"; then
              ((WORKTREE_REMOVE_COUNT+=1))
              REMOVED_WORKTREES+=("$wt")
            else
              ((SKIP_COUNT+=1))
              ACTION_SKIPS+=("worktree:$wt:remove_failed")
            fi
          else
            if git worktree remove "$wt"; then
              ((WORKTREE_REMOVE_COUNT+=1))
              REMOVED_WORKTREES+=("$wt")
            else
              ((SKIP_COUNT+=1))
              ACTION_SKIPS+=("worktree:$wt:remove_failed")
            fi
          fi
        done <<<"$wt_lines"
      fi

      if [[ -n "${LOCAL_EXISTS[$branch]:-}" ]]; then
        if [[ "$branch" == "$CURRENT_BRANCH" ]]; then
          ((SKIP_COUNT+=1))
          echo "Skip local delete for current branch: $branch"
        else
          if [[ "$FORCE_LOCAL" == true ]]; then
            if git branch -D "$branch"; then
              ((LOCAL_DELETE_COUNT+=1))
              DELETED_LOCAL+=("$branch")
            else
              ((SKIP_COUNT+=1))
              echo "Skip local delete (failed): $branch"
              ACTION_SKIPS+=("branch:$branch:local_delete_failed")
            fi
          else
            if git branch -d "$branch"; then
              ((LOCAL_DELETE_COUNT+=1))
              DELETED_LOCAL+=("$branch")
            else
              ((SKIP_COUNT+=1))
              echo "Skip local delete (not fully merged, use --force-local to override): $branch"
              ACTION_SKIPS+=("branch:$branch:local_not_merged")
            fi
          fi
        fi
      fi

      if [[ "$DELETE_REMOTE" == true && -n "${REMOTE_EXISTS[$branch]:-}" ]]; then
        open_pr_now="$(gh pr list --repo "$REPO" --state open --head "$branch" --json number --jq 'length' 2>/dev/null || echo 0)"
        if [[ "$open_pr_now" =~ ^[0-9]+$ ]] && (( open_pr_now > 0 )); then
          ((SKIP_COUNT+=1))
          echo "Skip remote delete (open PR exists): $branch"
          ACTION_SKIPS+=("branch:$branch:remote_blocked_open_pr")
        elif git push "$REMOTE" --delete "$branch"; then
          ((REMOTE_DELETE_COUNT+=1))
          DELETED_REMOTE+=("$branch")
        else
          ((SKIP_COUNT+=1))
          echo "Skip remote delete (failed): $branch"
          ACTION_SKIPS+=("branch:$branch:remote_delete_failed")
        fi
      fi
    fi
  fi
done < <(printf '%s\n' "${!CANDIDATES[@]}" | sort)

if [[ "$APPLY" == true ]]; then
  git worktree prune || true
fi

echo
echo "Classification by reason:"
if [[ ${#REASON_COUNTS[@]} -eq 0 ]]; then
  echo "  - none"
else
  while IFS= read -r reason; do
    echo "  - $reason: ${REASON_COUNTS[$reason]}"
  done < <(printf '%s\n' "${!REASON_COUNTS[@]}" | sort)
fi

print_list "Protected branches skipped:" "${PROTECTED_SKIPS[@]}"

if [[ "$APPLY" == true ]]; then
  print_list "Deleted local branches:" "${DELETED_LOCAL[@]}"
  print_list "Deleted remote branches:" "${DELETED_REMOTE[@]}"
  print_list "Removed worktrees:" "${REMOVED_WORKTREES[@]}"
  print_list "Skipped actions:" "${ACTION_SKIPS[@]}"
else
  print_list "Dry-run local delete plan:" "${PLAN_LOCAL[@]}"
  print_list "Dry-run remote delete plan:" "${PLAN_REMOTE[@]}"
  print_list "Dry-run worktree remove plan:" "${PLAN_WORKTREE[@]}"
fi

echo
echo "Summary:"
echo "Eligible branches: $ELIGIBLE_COUNT"
echo "Local branches deleted: $LOCAL_DELETE_COUNT"
echo "Remote branches deleted: $REMOTE_DELETE_COUNT"
echo "Worktrees removed: $WORKTREE_REMOVE_COUNT"
echo "Skipped actions: $SKIP_COUNT"

if [[ "$APPLY" == false ]]; then
  echo "Dry-run only. Re-run with --apply to perform cleanup."
fi
