<#
.SYNOPSIS
Performs any post-test actions needed on the Roslyn build systems.

.PARAMETER UploadNuGets
If true, upload NuGets to MyGet.

.PARAMETER UploadVsixes
If true, upload Vsixes to MyGet.

.PARAMETER BinariesDirectory
The root directory where the build outputs are written.

.PARAMETER BranchName
The name of the branch that is being built.

#>
Param(
    [boolean]$UploadNuGets=$true,
    [boolean]$UploadVsixes=$true,
    [boolean]$UploadCoreXTPackages=$true,
    [string]$BinariesDirectory,
    [string]$BranchName
)
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    Write-Host "Starting PostTest script..."

    # MAIN BODY
    Stop-Process -Name "vbcscompiler" -Force -ErrorAction SilentlyContinue

    # Load in the NuGet.exe information
    . "$PSScriptRoot\..\..\..\builds\scripts\LoadNuGetInfo.ps1"
    write-host "NuGet.exe path is $nugetexe"

    $exitCode = 0

    if ($UploadCoreXTPackages)
    {
        Write-Host "Uploading CoreXT packages..."

        try
        {
            $packagesDropDir = (Join-Path $BinariesDirectory "DevDivPackages")
            $coreXTRoot = "\\cpvsbuild\drops\dd\NuGet"

            <# Do not overwrite existing packages. #>
            robocopy /xo /xn /xc (Join-Path $packagesDropDir "Roslyn") $coreXTRoot "VS.ExternalAPIs.Roslyn.*.nupkg"

            <# TODO: Once all dependencies are available on NuGet we can merge the following two commands. #>
            robocopy /xo /xn /xc (Join-Path $packagesDropDir "Dependencies") $coreXTRoot "VS.ExternalAPIs.*.nupkg"
            robocopy /xo /xn /xc (Join-Path $packagesDropDir "Dependencies") (Join-Path $coreXTRoot "nugetorg") "Microsoft.*.nupkg" "System.*.nupkg" "ManagedEsent.*.nupkg"
        }
        catch [exception]
        {
            Write-Error -Exception $_.Exception
            $exitCode = 5
        }
    }

    # We need to remove 'refs/heads/' from the beginning of the string
    $BranchName = $BranchName -Replace "^refs/heads/"
    
    # We also need to replace all instances of '/' with '_'
    $BranchName = $BranchName.Replace("/", "_")

    if ($UploadNuGets)
    {
        Write-Host "Uploading NuGet packages..."

        $nugetPath = Join-Path $BinariesDirectory "NuGet\PerBuildPreRelease"

        [xml]$packages = Get-Content "$nugetPath\myget_org-packages.config"
        $apiKey = (Get-Content "\\mlangfs1\public\RoslynNuGetInfrastructure\mygetApiKey-dotnet.txt").Trim()

        $sourceUrl = ("https://dotnet.myget.org/F/roslyn-{0}-nightly/api/v2/package" -f $BranchName)
        
        pushd $nugetPath
        foreach ($package in $packages.packages.package)
        {
            $nupkg = $package.id + "." + $package.version + ".nupkg"
            Write-Host "  Uploading '$nupkg' to '$sourceUrl'"

            & "$NuGetExe" push "$nupkg" `
                -Source $sourceUrl `
                -ApiKey $apiKey `
                -NonInteractive `
                -Verbosity quiet
            if ($LastExitCode -ne 0)
            {
                Write-Error "Failed to upload NuGet package: $nupkg"
                $exitCode = 3
            }
        }
        popd
    }

    if ($UploadVsixes)
    {
        Write-Host "Uploading VSIX extensions..."

        $vsixPath = $BinariesDirectory

        [xml]$extensions = Get-Content "$vsixPath\myget_org-extensions.config"
        $apiKey = (Get-Content "\\mlangfs1\public\RoslynNuGetInfrastructure\mygetApiKey-dotnet.txt").Trim()

        pushd $vsixPath
        foreach ($extension in $extensions.extensions.extension)
        {
            $vsix = $extension.id + ".vsix"
            $requestUrl = ("https://dotnet.myget.org/F/roslyn-{0}-nightly/vsix/upload" -f $BranchName)
            
            Write-Host "  Uploading '$vsix' to '$requestUrl'"

            $response = Invoke-WebRequest -Uri $requestUrl -Headers @{"X-NuGet-ApiKey"=$apiKey} -ContentType 'multipart/form-data' -InFile $vsix -Method Post -UseBasicParsing
            if ($response.StatusCode -ne 201)
            {
                Write-Error "Failed to upload VSIX extension: $vsix. Upload failed with Status code: $response.StatusCode"
                $exitCode = 4
            }
        }
        popd
    }

    Write-Host "Completed PostTest script with an exit code of '$exitCode'"

    exit $exitCode
}
catch [exception]
{
    Write-Error -Exception $_.Exception
    exit -1
}
