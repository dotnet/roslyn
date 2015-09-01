$NuGetExe = "$PSScriptRoot\NuGet.exe"
$NuGetV3Exe = "$PSScriptRoot\V3\NuGet.exe"

& $NuGetExe restore "$PSScriptRoot\packages.config" -PackagesDirectory "$PSScriptRoot\..\packages" -ConfigFile "$PSScriptRoot\NuGet.config"
& $NuGetV3Exe restore "$PSScriptRoot\V3\packages.config" -PackagesDirectory "$PSScriptRoot\..\packages" -ConfigFile "$PSScriptRoot\NuGet.config"
& $NuGetV3Exe restore "$PSScriptRoot\..\Roslyn.sln" -ConfigFile "$PSScriptRoot\NuGet.config"
& $NuGetExe restore "$PSScriptRoot\..\src\Samples\Samples.sln" -ConfigFile "$PSScriptRoot\NuGet.config"
& $NuGetExe restore "$PSScriptRoot\..\src\Dependencies\Dependencies.sln" -ConfigFile "$PSScriptRoot\NuGet.config"

