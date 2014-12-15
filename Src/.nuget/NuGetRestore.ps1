$NuGetExe = "$PSScriptRoot\NuGet.exe"

& $NuGetExe restore "$PSScriptRoot\packages.config" -PackagesDirectory "$PSScriptRoot\..\..\packages"
& $NuGetExe restore "$PSScriptRoot\..\Roslyn.sln"
& $NuGetExe restore "$PSScriptRoot\..\Samples\Samples.sln"