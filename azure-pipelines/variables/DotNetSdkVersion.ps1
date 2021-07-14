$globalJson = Get-Content -Path "$PSScriptRoot\..\..\global.json" | ConvertFrom-Json
$globalJson.sdk.version
