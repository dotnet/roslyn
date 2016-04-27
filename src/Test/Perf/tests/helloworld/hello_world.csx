// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "../../util/test_util.csx"
using System.IO;

class HelloWorldTest: PerfTest 
{
    private string _pathToHelloWorld;
    private string _pathToOutput;
    
    public HelloWorldTest(): base() {}
    
    public override void Setup() 
    {
        _pathToHelloWorld = Path.Combine(MyWorkingDirectory, "HelloWorld.cs");
        _pathToOutput = Path.Combine(MyArtifactsDirectory, "HelloWorld.exe");
    }
    
    public override void Test() 
    {
        ShellOutVital(ReleaseCscPath, _pathToHelloWorld + " /out:" + _pathToOutput);
    }
    
    public override int Iterations => 2;
    public override string Name => "hello world";
    public override string MeasuredProc => "csc";
}

TestThisPlease(new HelloWorldTest());
