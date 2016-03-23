// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "../../util/test_util.csx"
using System.IO;

InitUtilities();

DownloadProject("csharp", version: 1);

foreach (var rspFile in new string[] {"CSharpCompiler.rsp", "CSharpCompilerNoAnalyzer.rsp", "CSharpCompilerNoAnalyzerNoDeterminism.rsp"}){
    string responseFile = "@" + Path.Combine(MyTempDirectory(), "csharp", rspFile);
    string keyfileLocation = Path.Combine(MyTempDirectory(), "csharp", "keyfile", "35MSSharedLib1024.snk");
    string args = $"{responseFile} /keyfile:{keyfileLocation}";

    string executeInDirectory = Path.Combine(MyTempDirectory(), "csharp");

    var msToCompile = WalltimeMs(() => ShellOutVital(ReleaseCscPath(), args, executeInDirectory));
    Report(ReportKind.CompileTime, $"{rspFile} compile duration (ms)", msToCompile);  
}