# Perf Testing

Welcome to the new performance testing system, this document is split up into
sections explaining common scenarios for developers that are interested in
improving Roslyn performance.

* [Project Structure](#project-structure)
* [Running Tests Locally](#running-tests-locally)
    * [Running a Single Test](#running-a-single-test)
    * [Running the Whole Suite](#running-the-whole-suite)
* [Adding a Test](#adding-a-test)
    * [Basic Features](#basic-features)
    * [Advanced Features](#advanced-features)
    * [More Advanced Features](#more-advanced-features)

## Project Structure

All performance tests are written as simple `.csx` C# scripting files.  These
tests perform the end-to-end duties of that one test, and when finished, can call
`Report` in order to record the metrics that they measure.

These `.csx` files can be placed anywhere inside of the Perf directory, but
each test should be in its own directory alongside any dependencies that it
requires to run.

## Running Tests Locally

The big push on this perf-system is to make it easy for contributors to test
their changes locally.

**Before you do anything else**, make sure that you run the `bootstrap.bat` file
in the Perf directory.  This will build csi.exe in release mode, and store the
results in "infra/bin" so that they can be used by the performance runner.

### Running a Single Test

All of the test .csx files are runnable directly.  This means that in order to
run the hello world test, simply run `csi.exe path/to/hello_world.csx`.

Running a script directly will print out the metrics that it measured.

### Running the Whole Suite

If you thought running the tests individually was easy, just you wait! Running
the whole suite of performance tests is as simple as `csi.exe runner.csx`.
This script will recursively find all test cases, run them, and collect their
performance metrics to present at the end.

The runner will also store performance records for a given run inside of
`Perf/temp/*.csv`.  You can save these elsewhere if - for example -  you want
to compare perf-numbers between commits.

## Adding a Test

Making your own test is incredibly easy!  A test is a single `.csx` file that is
executed and reports any metrics that it wants.  Let's make a basic performance
test:

### Basic Features

Here is an example of the most basic "perf test" that one could write.
It doesn't actually measure performance, but we'll get to that later.

**bad_test.csx**
```c#
// Load the test utilities, we'll be using InitUtilities and Report.
#load "../util/test_util.csx"
// Initialize the test utilities that we just imported.  This is required!
InitUtilities();
// Say that we just compiled something and lie about the amount of time it took.
Report(ReportKind.CompileTime, "compile duration (ms)", new System.Random().Next(0, 100));
```

That's it!  Try running it with `csi.exe bad_test.csx`.  The test will also be
found by the suite runner, so `csi.exe runner.csx` will pick it up and add it
to the list of tests to run.

### Advanced Features

For a project that aims to measure compile time, we'd better do some measuring
and compiling.  To do that, we'll make a new test and use some more features
from `test_util.csx`

Files required for this test:
* `hello_world.csx`: The test script
* `HelloWorld.cs`: The file that we are going to compile

Let's create both of these files and put them in a "helloworld" directory.

This directory (The directory that the test file inhabits) is called the
"working directory".  This directory should contain files that are vital
to the test - in this case, `HelloWorld.cs`.  Other special directories
are "artifacts" (used for storing test output) and "temp" (used for
storing intermediate files), both found inside the working directory.

Now on to the script itself.

**hello_world.csx**
```c#
#load "../util/test_util.csx"
using System.IO;

InitUtilities();

// MyWorkingDirectory() returns the path to the working directory.
var pathToHelloWorld = Path.Combine(MyWorkingDirectory(), "HelloWorld.cs");
// We want to place any build artifacts in the directory returned by MyArtifactsDirectory().
// This will ensure that they are archived.
var pathToOutput = Path.Combine(MyArtifactsDirectory(), "HelloWorld.exe");

// Record the amount of time that it takes to execute the release csc binary given
// a path to HelloWorld.cs
var msToCompile = WalltimeMs(() => ShellOutVital(ReleaseCscPath(), pathToHelloWorld + " /out:" + pathToOutput));
// Report the time that it took to compile.
Report(ReportKind.CompileTime, "compile duration (ms)", msToCompile);
```

Once again, you can run this test simply by typing `csi.exe hello_world.csx`.
In fact, this example is a part of our test suite already.  You can find it
in the `helloworld` directory.

### More Advanced Features

For more advanced features, please read the docs in the `util` folder.
