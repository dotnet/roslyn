// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#r "../../../Roslyn.Test.Performance.Utilities.dll"

using System.IO;
using System.Collections.Generic;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

class CSharpCompilerTest: PerfTest
{
    private string _rspFile;
    private ILogger _logger;

    public CSharpCompilerTest(string rspFile): base() {
        _rspFile = rspFile;
        _logger = new ConsoleAndFileLogger();
    }
    
    public override void Setup() 
    {
        DownloadProject("csharp", version: 1, logger: _logger);
    }
    
    public override void Test() 
    {
        string responseFile = "@" + Path.Combine(TempDirectory, "csharp", _rspFile);
        string keyfileLocation = Path.Combine(TempDirectory, "csharp", "keyfile", "35MSSharedLib1024.snk");
        string args = $"{responseFile} /keyfile:{keyfileLocation}";

        string workingDirectory = Path.Combine(TempDirectory, "csharp");

        ShellOutVital(Path.Combine(MyBinaries(), "csc.exe"), args, workingDirectory);
        _logger.Flush();
    }
    
    public override int Iterations => 1;
    public override string Name => "csharp " + _rspFile;
    public override string MeasuredProc => "csc";
    public override bool ProvidesScenarios => false;
    public override string[] GetScenarios()
    {
        throw new System.NotImplementedException();
    }
}

TestThisPlease(    
    new CSharpCompilerTest("CSharpCompiler.rsp"),
    new CSharpCompilerTest("CSharpCompilerNoAnalyzer.rsp"),
    new CSharpCompilerTest("CSharpCompilerNoAnalyzerNoDeterminism.rsp")
    );
