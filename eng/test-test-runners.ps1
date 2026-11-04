# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

<#
.SYNOPSIS
  Offline regression checks for the already-built RunTests and RunHelix binaries.
.DESCRIPTION
  Build both runners first. This script creates a package-free reflection harness
  under artifacts, never submits a Helix job, and removes only its own fixture
  directory. It does not build repository projects or change the solution graph.
#>
[CmdletBinding()]
param(
  [ValidateSet('Debug', 'Release')]
  [string]$configuration = 'Debug',
  [string]$dotnetPath = 'dotnet'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path $repoRoot "artifacts\test-runner-regressions-$([guid]::NewGuid().ToString('N'))"
$pushed = $false

try {
  Push-Location $repoRoot
  $pushed = $true
  $dotnetPath = (Get-Command $dotnetPath -ErrorAction Stop).Source
  $version = & $dotnetPath --version
  if ($LASTEXITCODE -ne 0) { throw 'Cannot run the SDK pinned by global.json.' }
  $pinnedVersion = (Get-Content global.json -Raw | ConvertFrom-Json).sdk.version
  if ($version.Trim() -ne $pinnedVersion) {
    throw "Expected SDK $pinnedVersion from global.json, found $version. Pass -dotnetPath to the pinned installation."
  }

  $runnerPaths = @()
  foreach ($runner in @('RunTests', 'RunHelix')) {
    $configDir = Join-Path $repoRoot "artifacts\bin\$runner\$configuration"
    $configs = @(Get-ChildItem $configDir -Filter "$runner.runtimeconfig.json" -Recurse -ErrorAction SilentlyContinue)
    if ($configs.Count -ne 1) { throw "Build $runner for $configuration first (expected one runtimeconfig in $configDir)." }
    $runnerPaths += Join-Path $configs[0].DirectoryName "$runner.dll"
  }
  $tfm = (Get-Content ([IO.Path]::ChangeExtension($runnerPaths[0], 'runtimeconfig.json')) -Raw | ConvertFrom-Json).runtimeOptions.tfm
  New-Item -ItemType Directory $scratch | Out-Null
  # Stop repository-wide build imports at the generated project boundary.
  '<Project />' | Set-Content (Join-Path $scratch 'Directory.Build.props')
  '<Project />' | Set-Content (Join-Path $scratch 'Directory.Build.targets')
  '<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content (Join-Path $scratch 'NuGet.config')
  $harnessSource = [Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'test-runners\RegressionHarness.cs'))
  $minimizeSource = [Security.SecurityElement]::Escape((Join-Path $repoRoot 'src\Tools\PrepareTests\MinimizeUtil.cs'))
  @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$tfm</TargetFramework>
    <RollForward>LatestMajor</RollForward>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$harnessSource" />
    <Compile Include="$minimizeSource" />
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $scratch 'Harness.csproj')

  & $dotnetPath build (Join-Path $scratch 'Harness.csproj') --nologo --verbosity quiet
  if ($LASTEXITCODE -ne 0) { throw 'Regression harness build failed.' }
  & $dotnetPath (Join-Path $scratch "bin\Debug\$tfm\Harness.dll") $repoRoot $scratch $dotnetPath @runnerPaths
  if ($LASTEXITCODE -ne 0) { throw 'Runner regression checks failed.' }
}
finally {
  if ($pushed) { Pop-Location }
  if (Test-Path $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
}
