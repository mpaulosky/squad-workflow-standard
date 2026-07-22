#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  check-git-gh-standard.sh /absolute/path/to/target-repo [--source-repo /absolute/path/to/canonical-repo]

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
  exit 2
fi

WORKFLOW_STANDARD="$SOURCE_REPO/.squad/workflows/git-gh-process-standard.md"
WORKFLOW_BASELINE_MANIFEST="$SOURCE_REPO/.squad/workflows/workflow-baseline-manifest.txt"

if [[ ! -f "$WORKFLOW_STANDARD" ]]; then
  echo "ERROR: Canonical workflow standard not found: $WORKFLOW_STANDARD"
  exit 2
fi

CANONICAL_VERSION="$(grep -E '^Standard-Version:' "$WORKFLOW_STANDARD" | awk '{print $2}')"
LOCAL_VERSION_FILE="$TARGET_REPO/.squad/workflows/.git-gh-standard-version"
LOCAL_VERSION="missing"
HAS_FAILURE=0
HAS_DRIFT=0

if [[ -f "$LOCAL_VERSION_FILE" ]]; then
  LOCAL_VERSION="$(tr -d '[:space:]' < "$LOCAL_VERSION_FILE")"
fi

echo "Canonical version: ${CANONICAL_VERSION:-unknown}"
echo "Local version:     ${LOCAL_VERSION}"

if [[ -z "${CANONICAL_VERSION}" || "${CANONICAL_VERSION}" == "unknown" ]]; then
  echo "ERROR: Canonical version not found."
  exit 2
fi

if [[ "${LOCAL_VERSION}" != "${CANONICAL_VERSION}" ]]; then
  HAS_FAILURE=1
  HAS_DRIFT=1
  echo "STATUS: DRIFT DETECTED"
  echo "Policy: detect-and-prompt before gated issue work."
  echo "Choose one:"
  echo "  1) Update now: scripts/squad/sync-git-gh-standard.sh $TARGET_REPO"
  echo "  2) Defer: continue now, but rerun this check before next gated work"
  if [[ -f "$TARGET_REPO/.squad/workflows/git-gh-process-standard.md" ]]; then
    echo "  3) View diff: diff -u \\"
    echo "       $TARGET_REPO/.squad/workflows/git-gh-process-standard.md \\"
    echo "       $SOURCE_REPO/.squad/workflows/git-gh-process-standard.md"
  else
    echo "  3) View diff: local canonical file missing; sync first"
  fi
fi

assert_file_contains() {
  local file="$1"
  local expected="$2"
  local message="$3"

  if [[ ! -f "$file" ]]; then
    HAS_FAILURE=1
    echo "ADAPTER CHECK FAILED: missing file $file"
    return
  fi

  if ! grep -Fq "$expected" "$file"; then
    HAS_FAILURE=1
    echo "ADAPTER CHECK FAILED: $message"
  fi
}

assert_file_contains \
  "$TARGET_REPO/.squad/routing.md" \
  ".squad/workflows/git-gh-process-standard.md" \
  ".squad/routing.md must reference canonical workflow source"
assert_file_contains \
  "$TARGET_REPO/.squad/routing.md" \
  ".squad/templates/issue-lifecycle.md" \
  ".squad/routing.md must bind issue lifecycle template"
assert_file_contains \
  "$TARGET_REPO/.squad/routing.md" \
  "single issue uses standard branch flow; 2+" \
  ".squad/routing.md must enforce standard-vs-worktree flow selection"
assert_file_contains \
  "$TARGET_REPO/.squad/routing.md" \
  "never push directly to \`main\` or \`dev\`" \
  ".squad/routing.md must hard-gate direct main/dev pushes"

assert_file_contains \
  "$TARGET_REPO/.squad/ceremonies.md" \
  ".squad/workflows/git-gh-process-standard.md" \
  ".squad/ceremonies.md must reference canonical workflow source"

assert_file_contains \
  "$TARGET_REPO/.squad/templates/issue-lifecycle.md" \
  "Workflow Standard Binding" \
  ".squad/templates/issue-lifecycle.md must include workflow standard binding section"
assert_file_contains \
  "$TARGET_REPO/.squad/templates/issue-lifecycle.md" \
  "Standard version: \`${CANONICAL_VERSION}\`" \
  ".squad/templates/issue-lifecycle.md must bind to canonical standard version"
assert_file_contains \
  "$TARGET_REPO/.squad/templates/issue-lifecycle.md" \
  "Enforcement level: hard gate" \
  ".squad/templates/issue-lifecycle.md must explicitly declare hard gate enforcement"
assert_file_contains \
  "$TARGET_REPO/.squad/templates/issue-lifecycle.md" \
  "Default branch policy: branch from \`main\`, PR to \`main\`" \
  ".squad/templates/issue-lifecycle.md must enforce main-first branch + PR policy"

assert_file_contains \
  "$TARGET_REPO/.squad/skills/git-workflow-standard/SKILL.md" \
  "Standard version: \`${CANONICAL_VERSION}\`" \
  ".squad/skills/git-workflow-standard/SKILL.md must match canonical standard version"

if [[ -f "$WORKFLOW_BASELINE_MANIFEST" ]]; then
  TARGET_WORKFLOW_BASELINE_MANIFEST="$TARGET_REPO/.squad/workflows/workflow-baseline-manifest.txt"
  if [[ ! -f "$TARGET_WORKFLOW_BASELINE_MANIFEST" ]]; then
    HAS_FAILURE=1
    echo "ADAPTER CHECK FAILED: missing file $TARGET_WORKFLOW_BASELINE_MANIFEST"
  elif ! cmp -s "$WORKFLOW_BASELINE_MANIFEST" "$TARGET_WORKFLOW_BASELINE_MANIFEST"; then
    HAS_FAILURE=1
    echo "ADAPTER CHECK FAILED: workflow baseline manifest drift detected"
  fi

  while IFS= read -r workflow_file || [[ -n "$workflow_file" ]]; do
    workflow_file="$(printf '%s' "$workflow_file" | tr -d '\r')"

    if [[ -z "$workflow_file" || "${workflow_file:0:1}" == "#" ]]; then
      continue
    fi

    source_workflow="$SOURCE_REPO/.github/workflows/$workflow_file"
    target_workflow="$TARGET_REPO/.github/workflows/$workflow_file"

    if [[ ! -f "$source_workflow" ]]; then
      HAS_FAILURE=1
      echo "ADAPTER CHECK FAILED: missing canonical workflow $source_workflow"
      continue
    fi

    if [[ ! -f "$target_workflow" ]]; then
      HAS_FAILURE=1
      echo "ADAPTER CHECK FAILED: missing target workflow $target_workflow"
      continue
    fi

    if ! cmp -s "$source_workflow" "$target_workflow"; then
      HAS_FAILURE=1
      echo "ADAPTER CHECK FAILED: workflow drift detected for $workflow_file"
    fi
  done < "$WORKFLOW_BASELINE_MANIFEST"
fi

if [[ "$HAS_FAILURE" -eq 0 ]]; then
  echo "STATUS: OK (version and hard-gate adapters in sync)"
  exit 0
fi

echo "STATUS: ENFORCEMENT INCOMPLETE"
echo "Fix drift and adapter bindings, then rerun this check."
echo "Suggested action: scripts/squad/sync-git-gh-standard.sh $TARGET_REPO"
echo "Exit code map: 0=ok, 2=canonical missing, 3=drift, 4=adapter enforcement failure"
if [[ "$HAS_DRIFT" -eq 1 ]]; then
  exit 3
fi
exit 4
