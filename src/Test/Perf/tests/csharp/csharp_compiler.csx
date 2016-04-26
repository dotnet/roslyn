// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "../../util/test_util.csx"
using System.IO;
using System.Collections.Generic;

class CSharpCompilerTest: PerfTest 
{
    private string _rspFile; 
    public CSharpCompilerTest(string rspFile): base() {
        _rspFile = rspFile;
    }
    
    public override void Setup() 
    {
        DownloadProject("csharp", version: 1);
    }
    
    public override void Test() 
    {
        string responseFile = "@" + Path.Combine(MyTempDirectory, "csharp", _rspFile);
        string keyfileLocation = Path.Combine(MyTempDirectory, "csharp", "keyfile", "35MSSharedLib1024.snk");
        string args = $"{responseFile} /keyfile:{keyfileLocation}";

        string executeInDirectory = Path.Combine(MyTempDirectory, "csharp");

        ShellOutVital(ReleaseCscPath, args, executeInDirectory);
    }
    
    public override int Iterations => 2;
    public override string Name => "csharp " + _rspFile;
    public override string MeasuredProc => "csc";
}

TestThisPlease(
    new CSharpCompilerTest("CSharpCompiler.rsp"),
    new CSharpCompilerTest("CSharpCompilerNoAnalyzer.rsp"),
    new CSharpCompilerTest("CSharpCompilerNoAnalyzerNoDeterminism.rsp"));
