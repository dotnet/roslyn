Analyzer Runner
-------------

AnalyzerRunner currently exists as part of the Roslyn source code. 

## Steps to run: 

1. Check out the latest main branch from dotnet/roslyn 
2. Run Restore.cmd to restore NuGet packages 
3. Build Roslyn.sln in the Release configuration, either within Visual Studio or by running Build.cmd -release 
4. Set AnalyzerRunner as the startup project 
5. Select the launch configuration (several are specified in launchProperties.json but you can create a new one or modify an existing one) 
6. Run AnalyzerRunner with Ctrl+F5 or on the command line 

  

 
