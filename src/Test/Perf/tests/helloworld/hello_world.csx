// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#r "../../Perf.Utilities/Roslyn.Test.Performance.Utilities.dll"

using System.IO;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

class HelloWorldTest : PerfTest 
{
    private string _pathToHelloWorld;
    private string _pathToOutput;
    private ILogger _logger;
    
    public HelloWorldTest(): base() 
    {
        _logger = new ConsoleAndFileLogger();
    }
    
    
    public override void Setup()
    {
        _pathToHelloWorld = Path.Combine(MyWorkingDirectory, "HelloWorld.cs");
        _pathToOutput = Path.Combine(TempDirectory, "HelloWorld.exe");
    }
    
    public override void Test() 
    {
        ShellOutVital(Path.Combine(MyBinaries(), "Exes", "csc", "net46", @"csc.exe"), _pathToHelloWorld + " /out:" + _pathToOutput, MyWorkingDirectory);
        _logger.Flush();
    }
    
    public override int Iterations => 3;
    public override string Name => "hello world";
    public override string MeasuredProc => "csc";
    public override bool ProvidesScenarios => false;
    public override string[] GetScenarios()
    {
        throw new System.NotImplementedException();
    }
}

TestThisPlease(new HelloWorldTest());
