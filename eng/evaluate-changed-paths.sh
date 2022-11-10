#!/usr/bin/env bash

# Disable globbing in this bash script since we iterate over path patterns
set -f

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if command returns non-zero exit code.
# Prevents hidden errors caused by missing error code propagation.
set -e

usage()
{
  echo "Script that evaluates changed paths and emits an azure devops variable if the changes contained in the current HEAD against the difftarget meet the includepahts/excludepaths filters:"
  echo "  --difftarget <value>       SHA or branch to diff against. (i.e: HEAD^1, origin/main, 0f4hd36, etc.)"
  echo "  --excludepaths <value>     Escaped list of paths to exclude from diff separated by '+'. (i.e: 'src/libraries/*+'src/installer/*')"
  echo "  --includepaths <value>     Escaped list of paths to include on diff separated by '+'. (i.e: 'src/libraries/System.Private.CoreLib/*')"
  echo "  --subset                   Subset name for which we're evaluating in order to include it in logs"
  echo "  --azurevariable            Name of azure devops variable to create if change meets filter criteria"
  echo ""

  echo "Arguments can also be passed in with a single hyphen."
}

source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done

scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
eng_root=`cd -P "$scriptroot" && pwd`

exclude_paths=()
include_paths=()
subset_name=''
azure_variable=''
diff_target=''

while [[ $# > 0 ]]; do
  opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
  case "$opt" in
    -help|-h)
      usage
      exit 0
      ;;
    -difftarget)
      diff_target=$2
      shift
      ;;
    -excludepaths)
      IFS='+' read -r -a tmp <<< $2
      exclude_paths+=(${tmp[@]})
      shift
      ;;
    -includepaths)
      IFS='+' read -r -a tmp <<< $2
      include_paths+=(${tmp[@]})
      shift
      ;;
    -subset)
      subset_name=$2
      shift
      ;;
    -azurevariable)
      azure_variable=$2
      shift
      ;;
  esac

  shift
done

ci=true # Needed in order to use pipeline-logging-functions.sh
. "$eng_root/common/pipeline-logging-functions.sh"

# -- expected args --
# $@: git diff arguments
customGitDiff() {
  (
    set -x
    git diff -M -C -b --ignore-cr-at-eol --ignore-space-at-eol "$@"
  )
}

# runs git diff with supplied filter.
# -- exit codes --
# 0: No match was found
# 1: At least 1 match was found
#
# -- expected args --
# $@: filter string
probePathsWithExitCode() {
  local _filter=$@
  echo ""
  customGitDiff --exit-code --quiet $diff_target -- $_filter
}

# -- expected args --
# $@: filter string
printMatchedPaths() {
  local _subset=$subset_name
  local _filter=$@
  echo ""
  echo "----- Matching files for $_subset -----"
  customGitDiff --name-only $diff_target -- $_filter
}

probePaths() {
  local _subset=$subset_name
  local _azure_devops_var_name=$azure_variable
  local exclude_path_string=""
  local include_path_string=""
  local found_applying_changes=false

  if [[ ${#exclude_paths[@]} -gt 0 ]]; then
    echo ""
    echo "******* Probing $_subset exclude paths *******";
    for _path in "${exclude_paths[@]}"; do
      echo "$_path"
      if [[ -z "$exclude_path_string" ]]; then
        exclude_path_string=":!$_path"
      else
        exclude_path_string="$exclude_path_string :!$_path"
      fi
    done

    if ! probePathsWithExitCode $exclude_path_string; then
      found_applying_changes=true
      printMatchedPaths $exclude_path_string
    fi
  fi

  if [[ $found_applying_changes != true && ${#include_paths[@]} -gt 0 ]]; then
    echo ""
    echo "******* Probing $_subset include paths *******";
    for _path in "${include_paths[@]}"; do
      echo "$_path"
      if [[ -z "$include_path_string" ]]; then
        include_path_string=":$_path"
      else
        include_path_string="$include_path_string :$_path"
      fi
    done

    if ! probePathsWithExitCode $include_path_string; then
      found_applying_changes=true
      printMatchedPaths $include_path_string
    fi
  fi

  if [[ $found_applying_changes == true ]]; then
    echo ""
    echo "Setting pipeline variable $_azure_devops_var_name=true"
    Write-PipelineSetVariable -name $_azure_devops_var_name -value true -is_multi_job_variable true
  else
    echo ""
    echo "No changed files for $_subset"
  fi
}

probePaths