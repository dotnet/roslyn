// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "../util/test_util.csx"
#load "../util/runner_util.csx"
#load "../util/Download_util.csx"

var directoryUtil = new RelativeDirectory();

// Update the repository
string branch = StdoutFrom("git", "rev-parse --abbrev-ref HEAD");
ShellOutVital("git", $"pull origin {branch}");
ShellOutVital(Path.Combine(directoryUtil.RoslynDirectory, "Restore.cmd"), "", workingDirectory: directoryUtil.RoslynDirectory);

// Build Roslyn in Release Mode
ShellOutVital("msbuild", "./Roslyn.sln /p:Configuration=Release", workingDirectory: directoryUtil.RoslynDirectory);

// Run run_and_report.csx
await RunFile(Path.Combine(directoryUtil.MyWorkingDirectory, "run_and_report.csx"));
