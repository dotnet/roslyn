# This script will link the `.editorconfig` file to the repository root and the `sln.DotSettings` next to each `.sln` solution file in the repository with a name corresponding to the solution name.

param (
[switch] $Remove = $false,

[switch] $Create = $false,

[switch] $Check = $false
)

# Stop after first error.
$ErrorActionPreference = "Stop"

trap
{
    Write-Error $PSItem.ToString()
    exit 1
}

# Check that we are in the root of a GIT repository.
If ( -Not ( Test-Path -Path ".\.git" ) ) {
    throw "This script has to run in a GIT repository root! Usage: Copy this file to the root of the repository and execute. The file deletes itself upon success."
}

# Update/initialize the engineering subtree.
$EngineeringDirectory = "eng\shared"

$EditorConfigFile = ".\.editorconfig"

if ( $Remove ) {
    if ( Test-Path -Path $EditorConfigFile ) {
        Remove-Item $EditorConfigFile
    }

    Get-ChildItem ".\" -Filter "*.sln.DotSettings" | 
    Foreach-Object {
        Remove-Item $_.FullName
    }
}

function CreateOrCheckSymbolicLink {
    param (
        [string] $Source,
        [string] $Target,
        [bool] $Check,
        [bool] $Create
    )

    if ( $Create ) {
        # If the creation of the symlinks fails, either the script needs to be executed with elevation, or the Windows Developer Mode needs to be enabled.
        # We have to use the mklink command instead of the New-Command cmd-let, because the New-Command cmd-let requires elevation even with the Windows Developer Mode enabled.
        # We have to execute the mklink command in a Windows Command Prompt process, because mklink is not a stand-alone executable, but a built-in command.
        & cmd /c mklink $Source $Target
    }

    if ( $Check ) {
        if ( !(Test-Path $Source ) ) {
            throw "'$Source' symbolic link is not created."
        }

        if ( (Get-ItemProperty $Source).LinkType -ne "SymbolicLink" ) {
            throw "'$Source' symbolic link failed to create."
        }
    
        if ( (Get-ItemProperty $Source).Target -ne $Target ) {
            throw "'$Source' symbolic link tragets a wrong file."
        }
    }
}

if ( $Create -or $Check ) {
    CreateOrCheckSymbolicLink -Source $EditorConfigFile -Target ".\$EngineeringDirectory\style\.editorconfig" -Check $Check -Create $Create

    Get-ChildItem ".\" -Filter "*.sln" | 
    Foreach-Object {
        CreateOrCheckSymbolicLink -Source "$($_.FullName).DotSettings" -Target ".\$EngineeringDirectory\style\sln.DotSettings" -Check $Check -Create $Create
    }
}

exit 0