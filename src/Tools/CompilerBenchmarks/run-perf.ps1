# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

[CmdletBinding(PositionalBinding=$false)]
param (
    # By default, the Roslyn dir is expected to be next to this dir
    [string]$roslynDir = "$PSScriptRoot/../../.."
)

Set-Variable -Name LastExitCode 0
Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

try {
    . (Join-Path $roslynDir "eng/build-utils.ps1")

    # Download dotnet if it isn't already available
    Ensure-DotnetSdk

    $reproPath = Join-Path $ArtifactsDir "CodeAnalysisRepro"

    if (-not (Test-Path $reproPath)) {
        $tmpFile = [System.IO.Path]::GetTempFileName()
        Invoke-WebRequest -Uri "https://roslyninfra.blob.core.windows.net/perf-artifacts/CodeAnalysisRepro.zip" -UseBasicParsing -OutFile $tmpFile
        Unzip $tmpFile $ArtifactsDir
    }

    Exec-Command "dotnet" "run -c Release $reproPath"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
