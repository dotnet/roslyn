// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "../util/runner_util.csx"
#load "../util/test_util.csx"
using System.IO;

InitUtilities();

var pathToHelloWorld = Path.Combine(MyWorkingDirectory(), "HelloWorld.cs");
var pathToOutput = Path.Combine(MyArtifactsDirectory(), "HelloWorld.exe");

var msToCompile = WalltimeMs(() => ShellOutVital(ReleaseCscPath(), pathToHelloWorld + " /out:" + pathToOutput));
Report(ReportKind.CompileTime, "compile duration (ms)", msToCompile);
