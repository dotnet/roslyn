// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "../../../Roslyn.Test.Performance.Utilities.dll"
#load "../../util/test_util.csx"
using System.IO;
using System.Collections.Generic;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

class CSharpCompilerTest: PerfTest
{
    private string _rspFile;
    public CSharpCompilerTest(string rspFile): base(new ConsoleAndFileLogger("log.txt")) {
        _rspFile = rspFile;
    }
    
    public override void Setup() 
    {
        DownloadProject("csharp", version: 1, logger: _logger);
    }
    
    public override void Test() 
    {
        string responseFile = "@" + Path.Combine(MyTempDirectory, "csharp", _rspFile);
        string keyfileLocation = Path.Combine(MyTempDirectory, "csharp", "keyfile", "35MSSharedLib1024.snk");
        string args = $"{responseFile} /keyfile:{keyfileLocation}";

        string executeInDirectory = Path.Combine(MyTempDirectory, "csharp");

        ShellOutVital(Path.Combine(MyBinaries(), "csc.exe"), args, true, _logger, executeInDirectory);
        _logger.Flush();
    }
    
    public override int Iterations => 2;
    public override string Name => "csharp " + _rspFile;
    public override string MeasuredProc => "csc";
    public override bool ProvidesScenarios => false;
    public override string[] GetScenarios()
    {
        throw new System.NotImplementedException();
    }
}

TestThisPlease(    new CSharpCompilerTest("CSharpCompiler.rsp"),
    new CSharpCompilerTest("CSharpCompilerNoAnalyzer.rsp"),
    new CSharpCompilerTest("CSharpCompilerNoAnalyzerNoDeterminism.rsp")
    );
