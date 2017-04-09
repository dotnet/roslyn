<#
.SYNOPSIS
Performs any post-test actions needed on the Roslyn build systems.

.PARAMETER UploadNuGets
If true, upload NuGets to MyGet.

.PARAMETER UploadVsixes
If true, upload Vsixes to MyGet.

.PARAMETER binariesPath
The root directory where the build outputs are written.

.PARAMETER branchName
The name of the branch that is being built.

.PARAMETER test
Whether or not to just test this script vs. actually publish

#>
Param(
    [string]$binariesPath = $null,
    [string]$branchName = $null,
    [string]$apiKey = $null,
    [switch]$test

)
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    Write-Host "Starting PostTest script..."

    # We need to remove 'refs/heads/' from the beginning of the string
    $branchName = $branchName -Replace "^refs/heads/"
    
    switch ($branchName)
    {
        "dev15.0.x" { } 
        "dev15.1.x" { } 
        "dev15.2.x" { } 
        "master" { } 
        default
        {
            if (-not $test)
            {
                Write-Host "Branch $branchName is not supported for publishing"
                exit 1
            }
        }
    }

    # MAIN BODY
    Stop-Process -Name "vbcscompiler" -Force -ErrorAction SilentlyContinue

    # Load in the NuGet.exe information
    . "$PSScriptRoot\..\..\..\build\scripts\LoadNuGetInfo.ps1"
    write-host "NuGet.exe path is $nugetexe"

    $exitCode = 0

    Write-Host "Uploading CoreXT packages..."

    try
    {
        $packagesDropDir = (Join-Path $binariesPath "DevDivPackages")
        $coreXTRoot = "\\cpvsbuild\drops\dd\NuGet"

        if (-not $test) 
        {
            <# Do not overwrite existing packages. #>
            robocopy /xo /xn /xc (Join-Path $packagesDropDir "Roslyn") $coreXTRoot "*.nupkg"

            <# TODO: Once all dependencies are available on NuGet we can merge the following two commands. #>
            robocopy /xo /xn /xc (Join-Path $packagesDropDir "ManagedDependencies") $coreXTRoot "VS.ExternalAPIs.*.nupkg"
            robocopy /xo /xn /xc (Join-Path $packagesDropDir "ManagedDependencies") (Join-Path $coreXTRoot "nugetorg") "Microsoft.*.nupkg" "System.*.nupkg" "ManagedEsent.*.nupkg"
            robocopy /xo /xn /xc (Join-Path $packagesDropDir "NativeDependencies") (Join-Path $coreXTRoot "nugetorg") "Microsoft.*.nupkg" "System.*.nupkg" "ManagedEsent.*.nupkg"
        }
    }
    catch [exception]
    {
        write-host $_.Exception
        $exitCode = 5
    }

    Write-Host "Uploading NuGet packages..."

    $nugetPath = Join-Path $binariesPath "NuGet\PerBuildPreRelease"

    [xml]$packages = Get-Content "$nugetPath\myget_org-packages.config"

    $sourceUrl = "https://dotnet.myget.org/F/roslyn/api/v2/package"
    
    pushd $nugetPath
    foreach ($package in $packages.packages.package)
    {
        $nupkg = $package.id + "." + $package.version + ".nupkg"
        Write-Host "  Uploading '$nupkg' to '$sourceUrl'"
        if (-not (test-path $nupkg))
        {
            Write-Error "NuGet $nupkg does not exist"
            $exitCode = 6
        }

        if (-not $test) 
        {
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
    }
    popd

    Write-Host "Uploading VSIX extensions..."

    $vsixPath = $binariesPath

    [xml]$extensions = Get-Content "$vsixPath\myget_org-extensions.config"

    pushd $vsixPath
    foreach ($extension in $extensions.extensions.extension)
    {
        $vsix = join-path $extension.path ($extension.id + ".vsix")
        if (-not (test-path $vsix)) 
        {
            Write-Error "VSIX $vsix does not exist"
            $exitCode = 6
        }

        $requestUrl = "https://dotnet.myget.org/F/roslyn/vsix/upload"
        
        Write-Host "  Uploading '$vsix' to '$requestUrl'"

        if (-not $test)
        { 
            $response = Invoke-WebRequest -Uri $requestUrl -Headers @{"X-NuGet-ApiKey"=$apiKey} -ContentType 'multipart/form-data' -InFile $vsix -Method Post -UseBasicParsing
            if ($response.StatusCode -ne 201)
            {
                Write-Error "Failed to upload VSIX extension: $vsix. Upload failed with Status code: $response.StatusCode"
                $exitCode = 4
            }
        }
    }
    popd

    Write-Host "Completed PostTest script with an exit code of '$exitCode'"

    exit $exitCode
}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
