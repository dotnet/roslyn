$globalJson = Get-Content -LiteralPath "$PSScriptRoot\..\..\global.json" | ConvertFrom-Json
$globalJson.sdk.version
