Param ( 

    # List of packages to be restored, or empty to restore all.    
    [string[]] $Packages = @(),

    [string] $NuGetUrl = "https://nuget.postsharp.net/nuget/caravela/" )

# Stop after first error.
$ErrorActionPreference = "Stop"

trap
{
    Write-Error $PSItem.ToString()
    exit 1
}

# Check that we are in the root of a GIT repository.
If ( -Not ( Test-Path -Path ".\.git" ) ) {
    throw "This script has to run in a GIT repository root!"
}

function Restore( $Package, $Version ) {
    if ( $Packages.Count -eq 0 -or $Packages -Contains $Package  ) {

        # 'dotnet tool install' fails when the package is already installed, so we need to check manually.
        if ( -Not ( Test-Path tools\.store\$Package\$Version ) ) {
             Write-Host "Installing $Package."
            & dotnet tool install --tool-path tools $Package --version $Version --add-source $NuGetUrl
            if ($LASTEXITCODE -ne 0 ) { throw "Tools installation failed." }
        } else {
            Write-Host "$Package was already installed."
        }
    }
}

Restore PostSharp.Engineering.BuildTools 1.0.2
Restore SignClient 1.3.155
Restore JetBrains.Resharper.GlobalTools 2021.2.1

