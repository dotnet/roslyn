Powershell Guidelines
==============

Powershell is primarily used in this repo as the backbone of our infrastructure.  As such 
Powershell scripts need to be portable, reliable and take advantage of stop on error 
approaches.  The guidelines below are meant to push scripts to this mindset of execution.

# Coding Style

1. Opening braces always go at the end of an expression / statement. 
1. Closing braces always occur on an otherwise empty line.
1. We use four space indentation.
1. Use Pascal casing for functions and where possible follow the Verb-Name convention.
1. Use Camel casing for all other identifier.
1. Use full command names instead of aliass.  For example `Get-Content` vs. `gc`.  Aliases can be 
overriden by the environment and hence are not considered portable. 

# General Guidelines

## Body 

All scripts shall include the following two statements after parameter declarations:

``` powershell
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"
```

This both forces Powershell into a more strict mode of interepretation and swaps out the 
"On Error Resume Next" approach for an "On Error Stop" model.  Both of these help with our
goals of reliability as it makes errors hard stops (usless specifically stated otherwise)

The body of a Powershell script shall be wrapped inside the following try / catch template:

``` powershell
try { 
    # Body of Powershell script goes here
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
```

This will force scripts to exit with an error code when an unhandled exception occurs. 

## Parameters

The parameter block shall occur at the top of the script.  Authors should consider disabling
positional binding:

``` powershell
[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$test64 = $false,
    [switch]$testDeterminism = $false)
```

This helps alert callers to casual typos, which would otherwise go unnoticed, by making it an 
error. 

If the script is complicated enough that it contains a usage / help display, then the following 
pattern can be used:

``` powershell
[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$build = $false, 
    [string]$msbuildDir = "",
    [parameter(ValueFromRemainingArguments=$true)] $badArgs)

try {
    if ($badArgs -ne $null) {
        Print-Usage 
        exit 1
    }
}

```

## Executing windows commands

Invoking windows commands should be done via the Exec function.  This adds automatic error detection 
to the invocation and removes the need for error prone if checking after every command.

``` powershell
# DO NOT
& msbuild /v:m /m Roslyn.sln
# DO 
Exec-Block { & msbuild /v:m /m Roslyn.sln }
```

Note this will not work for the rare Windows commands which use 0 as an exit code on failure.  For 
example robocopy and corflags.

In some cases windows commands need to have their argument list built up dynamically.  When that 
happens do not use Invoke-Expression to execute the command, instead use Exec-Command.  The former
does not fail when the windows command fails, can invoke powershell argument parsing and doesn't 
have a mechanism for echoing output to console.  The Exec-Command uses Process directly and can support
the major functionality needed.


``` powershell
$command = "C:\Program Files (x86)\Microsoft Visual Studio\Preview\Dogfood\MSBuild\15.0\Bin\MSBuild.exe"
$args = "/v:m Roslyn.sln"
if (...) { 
    $args += " /fl /flp:v=diag"
}
# DO NOT
Invoke-Expression "& $command $args"
# DO
Exec-Command $command $args
```

## Comarisons with null
Whenever comparing with `$null` always make sure to put `$null` on the left hand side of the 
operator. For non-collection types this doesn't really affect behavior. For collection types though
having a collection on the left hand side changes the meaning of `-ne` and `-eq`. Instead of checking
for `$null` it will instead compare collection contents.

``` powershell
# DO NOT
if ($e -ne $null) { ... }
# DO
if ($null -ne $e) { ... }
```

