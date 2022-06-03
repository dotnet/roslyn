# Performance Testing 
The speed of compilers and their tools is an incredibly important usability feature.
Although there are performance gates further down the release pipeline (namely in Visual Studio),
we would like to be warned of performance regressions earlier in order to have more time to respond.

Similarly, it would be handy for developers to directly measure performance on their own computers 
when working on perf-related features.

# Table of Contents 
* [Structure of Performance Testing](#structure-of-performance-testing)
* [How to Run Tests](#how-to-run-tests)
* [How to Write Tests](#how-to-write-tests)

# Structure of Performance Testing
The projects that make up the performance testing group are
* Perf.Runner
* Perf.Tests
* Perf.Utilities

## Perf.Runner
The Runner project produces a binary that runs tests, collects perf traces, and reports collected metrics.
For more info, check the section on [How to Run Tests](#how-to-run-tests).

## Perf.Tests
The Perf.Tests project is where all of the actual tests live.  These tests are simple `.csx` files that 
describe how to setup and run performances tests.  They also relay information back to the runner such as 
"what processes should you be collecting metrics for", or "this test needs to be run 3 times in order to 
reduce noise".

## Perf.Utilities
Perf.Utilities produces a `.dll` that is imported by the tests in Perf.Tests and Perf.Runner.   

# How to Run Tests
The binary produced by the runner is the main interface to the performance testing system.  Running the 
produced binary (`Roslyn.Test.Performance.Runner.exe`) will run all of the perf tests, collect traces, 
and report those metrics.

In addition, you may run any `.csx` file with csi in order to just run that one test.  `csi hello_world.csx` 
will just run the hello world test.

# How to Write Tests
Since tests are just `.csx` files, writing a new one is as easy as creating a new csharp script, and putting 
it in the Perf.Tests project.  

Here I'll go over writing the "hello world" compiler perf test, which you can find in 
`Perf.Tests/helloworld/hello_world.csx`.  

To start out, we have our copyright notice, and a `#r` that loads the `Roslyn.Test.Performance.Utilities.dll` 
that the `Perf.Test` project produces.  Then we import some functions from that dll.

```cs
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#r "../../../Roslyn.Test.Performance.Utilities.dll"

using System.IO;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
```

Next, we'll create a class that subclasses the `PerfTest` abstract class.  I'll skip over the constructor and
field declarations and go right to `Setup` and `Test`.  The `Setup` method is called once before the testing starts,
and `Test` is called once per iteration.  You'll notice that our test method is simply shelling out to `csc`,
compiling a hello world `.cs` file.

```cs
class HelloWorldTest : PerfTest 
{
    public override void Setup() 
    {
        _pathToHelloWorld = Path.Combine(MyWorkingDirectory, "HelloWorld.cs");
        _pathToOutput = Path.Combine(TempDirectory, "HelloWorld.exe");
    }
    
    public override void Test() 
    {
        ShellOutVital(Path.Combine(MyBinaries(), "csc.exe"), _pathToHelloWorld + " /out:" + _pathToOutput, MyWorkingDirectory);
        _logger.Flush();
    }

```

Next are some properties that the runner can use while executing the test.
* Iterations: The number of times that the test should be run by default.
* Name: The human-readable name of the test.
* MeasuredProc: The process that is going to be measured by the runner.
* ProvidedScenarios: `true` if this test manually provides scenarios (only used for TAO tests) 
* GetScenarios: TODO

```cs
    public override int Iterations => 1;
    public override string Name => "hello world";
    public override string MeasuredProc => "csc";
    public override bool ProvidesScenarios => false;
    public override string[] GetScenarios()
    {
        throw new System.NotImplementedException();
    }
}
```
