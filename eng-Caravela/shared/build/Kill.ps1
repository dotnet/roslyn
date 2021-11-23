echo "Killing processes"
gcim win32_process | where { $_.Name -eq "dotnet.exe" } | where { $_.commandline -like "*VBCSCompiler.exe*" -or $_.commandline -like "*VBCSCompiler.dll*" -or $_.CommandLine -like '*MSBuild.dll*' } | foreach { Stop-Process -ID $_.ProcessId }
gcim win32_process | where { $_.Name -eq "VBCSCompiler.exe" } | foreach { Stop-Process -ID $_.ProcessId }