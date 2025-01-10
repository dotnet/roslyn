$vstsDropNames = & "$PSScriptRoot\VstsDropNames.ps1"
$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}

$BasePath = "$PSScriptRoot\..\..\bin\Packages\$BuildConfiguration\Vsix"

if (Test-Path $BasePath) {
    $vsmanFiles = @()
    Get-ChildItem $BasePath *.vsman -Recurse -File |% {
        $version = (Get-Content $_.FullName | ConvertFrom-Json).info.buildVersion
        $fn = $_.Name
        $vsmanFiles += "$fn{$version}=https://vsdrop.corp.microsoft.com/file/v1/$vstsDropNames;$fn"
    }

    [string]::join(',',$vsmanFiles)
}
