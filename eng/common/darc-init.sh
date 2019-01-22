#!/usr/bin/env bash

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
verbosity=m

. "$scriptroot/tools.sh"

function InstallDarcCli {
  local darc_cli_package_name="microsoft.dotnet.darc"

  InitializeDotNetCli
  local dotnet_root=$_InitializeDotNetCli

  local uninstall_command=`$dotnet_root/dotnet tool uninstall $darc_cli_package_name -g`
  local tool_list=$($dotnet_root/dotnet tool list -g)
  if [[ $tool_list = *$darc_cli_package_name* ]]; then
    echo $($dotnet_root/dotnet tool uninstall $darc_cli_package_name -g)
  fi

  ReadGlobalVersion "Microsoft.DotNet.Arcade.Sdk"
  local toolset_version=$_ReadGlobalVersion

  echo "Installing Darc CLI version $toolset_version..."
  echo "You may need to restart your command shell if this is the first dotnet tool you have installed."
  echo $($dotnet_root/dotnet tool install $darc_cli_package_name --version $toolset_version -v $verbosity -g)
}

InstallDarcCli
