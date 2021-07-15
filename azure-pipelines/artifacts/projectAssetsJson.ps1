$ObjRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\obj")

if (!(Test-Path $ObjRoot)) { return }

@{
    "$ObjRoot" = (
        (Get-ChildItem "$ObjRoot\project.assets.json" -Recurse)
    );
}
