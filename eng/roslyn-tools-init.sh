#!/usr/bin/env bash

set -euo pipefail

source="${BASH_SOURCE[0]}"
verbosity='minimal'

usage()
{
  echo "Usage: $0 [--verbosity <value>]"
}

while [[ $# -gt 0 ]]; do
  opt="$(echo "$1" | tr "[:upper:]" "[:lower:]")"
  case "$opt" in
    --verbosity)
      verbosity=$2
      shift
      ;;
    *)
      echo "Invalid argument: $1"
      usage
      exit 1
      ;;
  esac

  shift
done

# Resolve $source until the file is no longer a symlink.
while [[ -h "$source" ]]; do
  scriptroot="$(cd -P "$(dirname "$source")" && pwd)"
  source="$(readlink "$source")"
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$(cd -P "$(dirname "$source")" && pwd)"

. "$scriptroot/common/tools.sh"

function InstallRoslynTools {
  local package_name="Microsoft.RoslynTools"
  local package_name_lower="microsoft.roslyntools"
  local package_source="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json"

  InitializeDotNetCli true
  local dotnet_root=$_InitializeDotNetCli

  local tool_list
  tool_list=$("$dotnet_root/dotnet" tool list --global)
  if [[ $tool_list = *"$package_name_lower"* ]]; then
    "$dotnet_root/dotnet" tool uninstall "$package_name" --global
  fi

  echo "Installing the latest prerelease RoslynTools..."
  echo "You may need to restart your command shell if this is the first dotnet tool you have installed."
  "$dotnet_root/dotnet" tool install "$package_name" --prerelease --source "$package_source" --verbosity "$verbosity" --global
}

InstallRoslynTools
