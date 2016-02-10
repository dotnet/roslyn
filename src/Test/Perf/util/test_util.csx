using System.IO;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

//
// Directory Locating Functions
//

string _myWorkingFile = null;

void InitUtilities([CallerFilePath] string sourceFilePath = "")
{
    _myWorkingFile = sourceFilePath;
}

/// Returns the directory that houses the currenly executing script.
string MyWorkingDirectory()
{
    if (_myWorkingFile == null)
    {
        throw new Exception("Tests must call InitUtilities before doing any path-dependent operations.");
    }
    return Directory.GetParent(_myWorkingFile).FullName;
}

string MyArtifactsDirectory()
{
    var path = Path.Combine(MyWorkingDirectory(), "artifacts");
    Directory.CreateDirectory(path);
    return path;
}

string MyResultsDirectory()
{
    var path = Path.Combine(MyWorkingDirectory(), "results");
    Directory.CreateDirectory(path);
    return path;
}

string RoslynDirectory()
{
    var workingDir = MyWorkingDirectory();
    var srcTestPerf = Path.Combine("src", "Test", "Perf").ToString();
    return workingDir.Substring(0, workingDir.IndexOf(srcTestPerf));
}

string BinDebugDirectory()
{
    return Path.Combine(RoslynDirectory(), "Binaries", "Debug");
}

string BinReleaseDirectory()
{
    return Path.Combine(RoslynDirectory(), "Binaries", "Release");
}

string DebugCscPath()
{
    return Path.Combine(BinDebugDirectory(), "csc.exe");
}

string ReleaseCscPath()
{
    return Path.Combine(BinReleaseDirectory(), "csc.exe");
}

string DebugVbcPath()
{
    return Path.Combine(BinDebugDirectory(), "vbc.exe");
}

string ReleaseVbcPath()
{
    return Path.Combine(BinReleaseDirectory(), "vbc.exe");
}

//
// Process spawning and error handling.
//

class ProcessResult
{
    public string ExecutablePath {get; set;}
    public string Args {get; set;}
    public int Code {get; set;}
    public string StdOut {get; set;}
    public string StdErr {get; set;}

    public bool Failed => Code != 0;
    public bool Succeeded => !Failed;
}

ProcessResult ShellOut(
        string file,
        string args,
        CancellationToken? cancelationToken = null)
{
    var tcs = new TaskCompletionSource<ProcessResult>();
    var startInfo = new ProcessStartInfo(file, args);
    startInfo.RedirectStandardOutput = true;
    startInfo.RedirectStandardError = true;
    startInfo.UseShellExecute = false;
    var process = new Process
    {
        StartInfo = startInfo,
        EnableRaisingEvents = true,
    };

    if (cancelationToken != null) {
        cancelationToken.Value.Register(() => process.Kill());
    }

    process.Exited += (s, a) => {
        var result = new ProcessResult {
            ExecutablePath = file,
            Args = args,
            Code = process.ExitCode,
            StdOut = process.StandardOutput.ReadToEnd(),
            StdErr = process.StandardError.ReadToEnd(),
        };
        tcs.SetResult(result);
        process.Dispose();
    };

    process.Start();

    return tcs.Task.GetAwaiter().GetResult();
}

//
// Timing and Testing
//

long WalltimeMs<R>(out R result, Func<R> action)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    result = action();
    return stopwatch.ElapsedMilliseconds;
}

//
// Reporting and logging
//

/// A list of 
var Metrics = new List<Tuple<string, object>>();

/// Logs a message.
///
/// The actual implementation of this method may change depending on
/// if the script is being run standalone or through the test runner.
void Log(string info)
{
    System.Console.WriteLine(info);
}

/// Logs the result of a finished process
void LogProcessResult(ProcessResult result) {
    Log(String.Format("The process \"{0}\" {1} with code {2}",
        $"{result.ExecutablePath} {result.Args}",
        result.Failed ? "failed" : "succeeded",
        result.Code));
    Log($"Standard Out:\n{result.StdOut}");
    Log($"Standard Error:\n{result.StdErr}");
}

/// Reports a metric to be recorded in the performance monitor.
void Report(string description, object value)
{
    Metrics.Add(Tuple.Create(description, value));
    Log(description + ": " + value.ToString());
}
