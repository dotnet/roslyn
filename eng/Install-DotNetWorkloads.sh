#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

# TODO: Maybe this and dotnet come in as params like in ps1
global_json_file="$(dirname "${scriptroot}")/global.json"

function NewTemporaryFilePath
{
  _TempFilePath=$(mktemp)
}

function GetWorkloadName
{
  local shortName=$1
  _WorkloadName="microsoft.net.sdk.$shortName"
}

function GetInstalledWorkloadVersion
{
  # TODO
  local workloadName=$1
  _WorkloadVersion=
}

function InstallWorkload
{
  local shortName=$1
  local version=$2
  # Receiving sourcesArgs array by name
  local -n sourcesArgs=$3

  GetWorkloadName "$shortName"
  local workloadName=$_WorkloadName

  GetInstalledWorkloadVersion "$workloadName"
  local installedVersion=$_WorkloadVersion

  # if ($installedVersion) {
  #     Write-Host "Workload $workloadName ($shortName) $installedVersion already installed"

  #     if (-not $version -or $version -eq $installedVersion) {
  #         continue
  #     }
  # }

  echo "Preparing to install workload $workloadName ($shortName) $version..."

  # # Safest to uninstall the existing version first
  # if ($installedVersion) {
  #     Write-Host "  Uninstalling $shortName..."

  #     # NOTE: This requires elevated access if using a system dotnet install
  #     & $dotnetPath workload uninstall $shortName
  # }

  local rollbackArgs=()
  local rollbackFile=

  if [ "$version" != "" ]; then
    NewTemporaryFilePath
    rollbackFile=$_TempFilePath
    echo "{ \"$workloadName\": \"$version\" }" > $rollbackFile
    # rollbackArgs="--from-rollback-file $rollbackFile"
    rollbackArgs+=("--from-rollback-file")
    rollbackArgs+=("$rollbackFile")
  fi

  echo "  Installing $shortName..."

  # NOTE: This requires elevated access if using a system dotnet install
  dotnet workload install "$shortName" "${rollbackArgs[@]}" "${sourcesArgs[@]}"

  if [ -f "$rollbackFile" ]; then
    rm -rf "$rollbackFile"
  fi
}

# Modeled after ReadGlobalJsonNativeTools in init-tools-native.sh
while IFS= read -rd '' line; do
  workload_infos+=("$line")
done < <(jq -r '. |
  select(has("workloads")) |
  ."workloads" |
  keys[] as $k |
  @sh "WORKLOAD=\($k) VERSION=\(.[$k].version // "") SOURCES=(\(.[$k].sources // ""))\u0000"' \
  "$global_json_file")

if [[ ${#workload_infos[@]} -eq 0 ]]; then
  echo "No workloads defined in global.json"
  exit 0;
else
  for index in "${!workload_infos[@]}"; do
    eval "${workload_infos["$index"]}"
    sourcesArgs=()
    if [[ ${#SOURCES[@]} -ne 0 ]]; then
      for sourceIndex in "${!SOURCES[@]}"; do
        sourcesArgs+=("--source")
        sourcesArgs+=("${SOURCES["$sourceIndex"]}")
      done
    fi
    # Passing sourcesArgs array by name
    InstallWorkload "$WORKLOAD" "$VERSION" sourcesArgs
  done
fi