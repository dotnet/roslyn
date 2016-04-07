// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "../../../Roslyn.Test.Performance.Utilities.dll"
using System.IO;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using System.IO;

InitUtilities();
var logger = new ConsoleAndFileLogger("log.txt");
DownloadProject("csharp", version: 1, logger: logger);

var rspFile = "CSharpCompilerNoAnalyzer.rsp";
string responseFile = "@" + Path.Combine(MyTempDirectory(), "csharp", rspFile);
string keyfileLocation = Path.Combine(MyTempDirectory(), "csharp", "keyfile", "35MSSharedLib1024.snk");
string args = $"{responseFile} /keyfile:{keyfileLocation}";

string executeInDirectory = Path.Combine(MyTempDirectory(), "csharp");

var msToCompile = WalltimeMs(() => ShellOutVital(CscPath(), args, true, logger, executeInDirectory));
Report(ReportKind.CompileTime, $"{rspFile} compile duration (ms)", msToCompile, logger);
logger.Flush();