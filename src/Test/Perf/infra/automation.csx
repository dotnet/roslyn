// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#r "./../../Roslyn.Test.Performance.Utilities.dll"

// RunFile()
#load "../util/runner_util.csx"

using Roslyn.Test.Performance.Utilities;

var directoryUtil = new RelativeDirectory();
TestUtilities.InitUtilitiesFromCsx();

// Install the Vsixes to RoslynPerf hive
await RunFile(Path.Combine(directoryUtil.MyWorkingDirectory, "install_vsixes.csx"));

// Run run_and_report.csx
await RunFile(Path.Combine(directoryUtil.MyWorkingDirectory, "run_and_report.csx"));
