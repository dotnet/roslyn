$NuGetExe = "$PSScriptRoot\NuGet.exe"

& $NuGetExe restore "$PSScriptRoot\..\Roslyn.sln"
& $NuGetExe restore "$PSScriptRoot\..\Samples\Samples.sln"