# This script returns a hashtable of build variables that should be set
# at the start of a build or release definition's execution.

$vars = @{}

Get-ChildItem "$PSScriptRoot\*.ps1" -Exclude "_*" |% {
    $vars[$_.BaseName] = & $_
}

$vars
