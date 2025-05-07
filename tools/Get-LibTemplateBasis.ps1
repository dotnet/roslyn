<#
.SYNOPSIS
    Returns the name of the well-known branch in the Library.Template repository upon which HEAD is based.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
Param(
    [switch]$ErrorIfNotRelated
)

# This list should be sorted in order of decreasing specificity.
$branchMarkers = @(
    @{ commit = 'fd0a7b25ccf030bbd16880cca6efe009d5b1fffc'; branch = 'microbuild' };
    @{ commit = '05f49ce799c1f9cc696d53eea89699d80f59f833'; branch = 'main' };
)

foreach ($entry in $branchMarkers) {
    if (git rev-list HEAD | Select-String -Pattern $entry.commit) {
        return $entry.branch
    }
}

if ($ErrorIfNotRelated) {
    Write-Error "Library.Template has not been previously merged with this repo. Please review https://github.com/AArnott/Library.Template/tree/main?tab=readme-ov-file#readme for instructions."
    exit 1
}
