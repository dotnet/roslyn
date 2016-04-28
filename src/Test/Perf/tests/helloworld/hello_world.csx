// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "../../../Roslyn.Test.Performance.Utilities.dll"
using System.IO;

class HelloWorldTest: PerfTest 
{
    private string _pathToHelloWorld;
    private string _pathToOutput;
    private ILogger _logger;
    
    public HelloWorldTest(): base() 
    {
        _logger = new ConsoleAndFileLogger("log.txt");
    }
    
    
    public override void Setup() 
    {
        _pathToHelloWorld = Path.Combine(MyWorkingDirectory, "HelloWorld.cs");
        _pathToOutput = Path.Combine(MyArtifactsDirectory, "HelloWorld.exe");
    }
    
    public override void Test() 
    {
        ShellOutVital(ReleaseCscPath, _pathToHelloWorld + " /out:" + _pathToOutput, _logger);
        logger.Flush();
    }
    
    public override int Iterations => 2;
    public override string Name => "hello world";
    public override string MeasuredProc => "csc";
    public override bool ProvidesScenarios => false;
    public override string[] GetScenarios()
    {
        throw new System.NotImplementedException();
    }
}

TestThisPlease(new HelloWorldTest());
