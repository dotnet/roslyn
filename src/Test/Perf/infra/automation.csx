// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#r "./../../Roslyn.Test.Performance.Utilities.dll"

// IsVerbose()
#load "../util/test_util.csx"
// RunFile()
#load "../util/runner_util.csx"

using Roslyn.Test.Performance.Utilities;

var directoryUtil = new RelativeDirectory();
var logger = new ConsoleAndFileLogger();
TestUtilities.InitUtilities();

// Update the repository
string branch = TestUtilities.StdoutFrom("git", IsVerbose(), logger, "rev-parse --abbrev-ref HEAD");
TestUtilities.ShellOutVital("git", $"pull origin {branch}", IsVerbose(), logger);
TestUtilities.ShellOutVital(Path.Combine(directoryUtil.RoslynDirectory, "Restore.cmd"), "", IsVerbose(), logger, workingDirectory: directoryUtil.RoslynDirectory);

// Build Roslyn in Release Mode
TestUtilities.ShellOutVital("msbuild", "./Roslyn.sln /p:Configuration=Release", IsVerbose(), logger, workingDirectory: directoryUtil.RoslynDirectory);

// Install the Vsixes to RoslynPerf hive
await RunFile(Path.Combine(directoryUtil.MyWorkingDirectory, "install_vsixes.csx"));

// Run run_and_report.csx
await RunFile(Path.Combine(directoryUtil.MyWorkingDirectory, "run_and_report.csx"));
