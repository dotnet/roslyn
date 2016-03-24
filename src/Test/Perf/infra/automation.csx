// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "..\util\test_util.csx"
#load "..\util\runner_util.csx"
#load "..\util\Download_util.csx"

InitUtilities();

// Update the repository
string branch = StdoutFrom("git", "rev-parse --abbrev-ref HEAD");
ShellOutVital("git", $"pull origin {branch}");
ShellOutVital(Path.Combine(RoslynDirectory(), "Restore.cmd"), "", workingDirectory: RoslynDirectory());

// Build Roslyn in Release Mode
ShellOutVital("msbuild", "./Roslyn.sln /p:Configuration=Release", workingDirectory: RoslynDirectory());

// Run DownloadTools before using the TraceManager because TraceManager uses the downloaded CPC binaries 
DownloadTools();

// Run run_and_report.csx
await RunFile(Path.Combine(MyWorkingDirectory(), "run_and_report.csx"));
