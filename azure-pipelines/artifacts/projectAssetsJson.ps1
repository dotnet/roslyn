$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")

@{
    "$RepoRoot\obj" = (
        (Get-ChildItem "$RepoRoot\obj\project.assets.json" -Recurse)
    );
}
