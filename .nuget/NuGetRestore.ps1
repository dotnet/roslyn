$NuGetExe = "$PSScriptRoot\NuGet.exe"

& $NuGetExe restore "$PSScriptRoot\packages.config" -PackagesDirectory "$PSScriptRoot\..\packages" -ConfigFile "$PSScriptRoot\NuGet.config"
& $NuGetExe restore "$PSScriptRoot\..\Roslyn.sln" -ConfigFile "$PSScriptRoot\NuGet.config"
& $NuGetExe restore "$PSScriptRoot\..\src\Samples\Samples.sln" -ConfigFile "$PSScriptRoot\NuGet.config"
& $NuGetExe restore "$PSScriptRoot\..\src\Dependencies\Dependencies.sln" -ConfigFile "$PSScriptRoot\NuGet.config"

