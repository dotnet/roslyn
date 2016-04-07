// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "../../../Roslyn.Test.Performance.Utilities.dll"
using System.IO;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
InitUtilities();

var logger = new ConsoleAndFileLogger("log.txt");
var pathToHelloWorld = Path.Combine(MyWorkingDirectory(), "HelloWorld.cs");
var pathToOutput = Path.Combine(MyArtifactsDirectory(), "HelloWorld.exe");

var msToCompile = WalltimeMs(() => ShellOutVital(CscPath(), pathToHelloWorld + " /out:" + pathToOutput, true, logger));
Report(ReportKind.CompileTime, "compile duration (ms)", msToCompile, logger);
logger.Flush();
