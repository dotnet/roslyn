# Powershell Guidelines

Powershell is primarily used in this repo as the backbone of our infrastructure.  As such
Powershell scripts need to be portable, reliable and take advantage of stop on error
approaches.  The guidelines below are meant to push scripts to this mindset of execution.

## Coding Style

1. Opening braces always go at the end of an expression / statement. 
1. Closing braces always occur on an otherwise empty line.
1. We use two space indentation.
1. Use Pascal casing for functions and where possible follow the Verb-Name convention.
1. Use Camel casing for all other identifier.
1. Use full command names instead of aliases.  For example `Get-Content` vs. `gc`.  Aliases can be
overridden by the environment and hence are not considered portable.

## General Guidelines

### Script Template

All Powershell scripts that execute in CI need to conform to the following template.

```powershell
[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$ci = $false,
    [string]$configuration = "Debug")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try { 
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  $prepareMachine = $ci

  # Body of Powershell script goes here

  ExitWithExitCode 0
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
```

The rationale for these parts are:

- `Set-StrictMode`: forces Powershell into a stricter mode of interpretation and
swaps out the "On Error Resume Next" approach for an "On Error Stop" model. Both of these 
help with our goals of reliability as it makes errors hard stops (unless specifically stated otherwise)
- `$prepareMachine = $ci`: is necessary to ensure `ExitWithExitCode` properly exits all
build processes in CI environments (conforming to arcade guidelines).
- `ExitWithExitCode`: arcade standard for exiting a script that deals with process management
- `PositionalBinding=$false`: alerts callers when they have incorrect parameters invoking a
script

### Coding Guidelines

Use `Exec-*` functions to execute programs or `dotnet` commands. This adds automatic
error detection on invocation failure, incorrect parameters, etc ... 

```powershell
# DO NOT
& msbuild /v:m /m Roslyn.sln
& dotnet build Roslyn.sln

# DO
Exec-Command "msbuild" "/v:m /m Roslyn.sln"
Exec-DotNet "build Roslyn.sln"
```

Scripts that have many executions of `dotnet` commands can store the `dotnet` command in a variable
and use `Exec-Command` instead.

Call `Test-LastExitCode` after invoking a powershell script to make sure failure is not ignored.

```powershell
# DO NOT
& eng/make-bootstrap.ps1
Write-Host "Done with Bootstrap"

# DO
& eng/make-bootstrap.ps1
Test-LastExitCode
Write-Host "Done with Bootstrap"
```

Whenever comparing with `$null` always make sure to put `$null` on the left hand side of the 
operator. For non-collection types this doesn't really affect behavior. For collection types though
having a collection on the left hand side changes the meaning of `-ne` and `-eq`. Instead of checking for `$null` it will instead compare collection contents.

``` powershell
# DO NOT
if ($e -ne $null) { ... }
# DO
if ($null -ne $e) { ... }
```

### Powershell vs. pwsh

The Roslyn infrastructure should use `powershell` for execution not `pwsh`. The general .NET infra
still uses `Powershell` and calls into our scripts. Moving to `pwsh` in our scripts creates errors
in source and unified build. Until that moves to `pwsh` our scripts need to stay on `Powershell`.

The exception is that our VS Code helper scripts should use `pwsh`. That is not a part of our 
infrastructure and needs to run cross platform hence `pwsh` is appropriate.
