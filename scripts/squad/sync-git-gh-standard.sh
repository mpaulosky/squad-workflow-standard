#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  sync-git-gh-standard.sh /absolute/path/to/target-repo [--source-repo /absolute/path/to/canonical-repo]

Optional environment variable:
  SQUAD_STANDARD_SOURCE_REPO=/absolute/path/to/canonical-repo
EOF
}

SCRIPT_REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TARGET_REPO=""
SOURCE_REPO_OVERRIDE=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --source-repo)
      shift
      if [[ $# -eq 0 ]]; then
        echo "Missing value for --source-repo"
        usage
        exit 1
      fi
      SOURCE_REPO_OVERRIDE="$1"
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      if [[ -z "$TARGET_REPO" ]]; then
        TARGET_REPO="$1"
      else
        echo "Unexpected argument: $1"
        usage
        exit 1
      fi
      ;;
  esac
  shift
done

if [[ -z "$TARGET_REPO" ]]; then
  usage
  exit 1
fi

SOURCE_REPO="${SOURCE_REPO_OVERRIDE:-${SQUAD_STANDARD_SOURCE_REPO:-$SCRIPT_REPO}}"

if [[ ! -d "$TARGET_REPO/.git" ]]; then
  echo "Target repo is not a git repository: $TARGET_REPO"
  exit 1
fi

if [[ ! -d "$SOURCE_REPO" ]]; then
  echo "Canonical source repository path not found: $SOURCE_REPO"
  exit 1
fi

mkdir -p "$TARGET_REPO/.squad/workflows"
mkdir -p "$TARGET_REPO/.squad/skills/git-workflow-standard"
mkdir -p "$TARGET_REPO/.github/workflows"

copy_if_distinct() {
  local source_file="$1"
  local target_file="$2"

  if [[ -f "$target_file" ]] && [[ "$(realpath "$source_file")" == "$(realpath "$target_file")" ]]; then
    return
  fi

  if [[ -f "$target_file" ]] && cmp -s "$source_file" "$target_file"; then
    return
  fi

  cp "$source_file" "$target_file"
}

WORKFLOW_STANDARD="$SOURCE_REPO/source/.squad/workflows/git-gh-process-standard.md"
WORKFLOW_README="$SOURCE_REPO/source/.squad/workflows/README.md"
WORKFLOW_SKILL="$SOURCE_REPO/source/.squad/skills/git-workflow-standard/SKILL.md"
WORKFLOW_BASELINE_MANIFEST="$SOURCE_REPO/source/.squad/workflows/workflow-baseline-manifest.txt"

for required_file in "$WORKFLOW_STANDARD" "$WORKFLOW_README" "$WORKFLOW_SKILL"; do
  if [[ ! -f "$required_file" ]]; then
    echo "Missing canonical source file: $required_file"
    exit 2
  fi
done

copy_if_distinct \
  "$WORKFLOW_STANDARD" \
  "$TARGET_REPO/.squad/workflows/git-gh-process-standard.md"

copy_if_distinct \
  "$WORKFLOW_README" \
  "$TARGET_REPO/.squad/workflows/README.md"

copy_if_distinct \
  "$WORKFLOW_SKILL" \
  "$TARGET_REPO/.squad/skills/git-workflow-standard/SKILL.md"

SYNCED_WORKFLOW_COUNT=0

if [[ -f "$WORKFLOW_BASELINE_MANIFEST" ]]; then
  copy_if_distinct \
    "$WORKFLOW_BASELINE_MANIFEST" \
    "$TARGET_REPO/.squad/workflows/workflow-baseline-manifest.txt"

  while IFS= read -r workflow_file || [[ -n "$workflow_file" ]]; do
    workflow_file="$(printf '%s' "$workflow_file" | tr -d '\r')"

    if [[ -z "$workflow_file" || "${workflow_file:0:1}" == "#" ]]; then
      continue
    fi

    source_workflow="$SOURCE_REPO/source/workflows/$workflow_file"
    target_workflow="$TARGET_REPO/.github/workflows/$workflow_file"

    if [[ ! -f "$source_workflow" ]]; then
      echo "Missing canonical workflow in source repo: $source_workflow"
      exit 2
    fi

    copy_if_distinct "$source_workflow" "$target_workflow"
    SYNCED_WORKFLOW_COUNT=$((SYNCED_WORKFLOW_COUNT + 1))
  done < "$WORKFLOW_BASELINE_MANIFEST"
fi

VERSION="$(grep -E '^Standard-Version:' "$WORKFLOW_STANDARD" | awk '{print $2}')"
echo "${VERSION:-unknown}" > "$TARGET_REPO/.squad/workflows/.git-gh-standard-version"

cat <<EOF
Synced git/gh process standard from:
  $SOURCE_REPO

Into:
  $TARGET_REPO/.squad/workflows/git-gh-process-standard.md
  $TARGET_REPO/.squad/workflows/README.md
  $TARGET_REPO/.squad/skills/git-workflow-standard/SKILL.md
  $TARGET_REPO/.squad/workflows/.git-gh-standard-version
EOF

if [[ -f "$WORKFLOW_BASELINE_MANIFEST" ]]; then
  cat <<EOF
  $TARGET_REPO/.squad/workflows/workflow-baseline-manifest.txt

Workflow baseline synced:
  $SYNCED_WORKFLOW_COUNT workflow file(s) copied to $TARGET_REPO/.github/workflows
EOF
fi

cat <<EOF
Next step: run scripts/squad/check-git-gh-standard.sh $TARGET_REPO
EOF
