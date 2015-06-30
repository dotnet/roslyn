$NuGetExe = "$PSScriptRoot\NuGet.exe"

& $NuGetExe restore "$PSScriptRoot\packages.config" -PackagesDirectory "$PSScriptRoot\..\packages"
& $NuGetExe restore "$PSScriptRoot\..\Roslyn.sln"
& $NuGetExe restore "$PSScriptRoot\..\src\Samples\Samples.sln"
& $NuGetExe restore "$PSScriptRoot\..\src\Dependencies\Dependencies.sln"
