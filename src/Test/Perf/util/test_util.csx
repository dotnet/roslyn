// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "System.IO.Compression.FileSystem"

using System.IO;
using System.IO.Compression;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Net;
using System.Globalization;

class RelativeDirectory 
{
    string _workingDir;
    
    public RelativeDirectory([CallerFilePath] string workingFile = "") 
    {
        _workingDir = Directory.GetParent(workingFile).FullName;
    }
    
    public string MyWorkingDirectory => _workingDir;
    

    /// Returns the directory that you can put artifacts like
    /// etl traces or compiled binaries
    public string MyArtifactsDirectory 
    { 
        get 
        {
            var path = Path.Combine(MyWorkingDirectory, "artifacts");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public string MyTempDirectory
    {
        get {
            var workingDir = MyWorkingDirectory;
            var path = Path.Combine(workingDir, "temp");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public string RoslynDirectory
    {
        get {
            // In Windows, our path could be reported as "src/Test/Perf" (as it should),
            // or "src/TeSt/PeRf" which is completely insane.
            var workingDir = MyWorkingDirectory;
            var srcTestPerf = Path.Combine("src", "Test", "Perf").ToString();
            CompareInfo inv = CultureInfo.InvariantCulture.CompareInfo;
            var idx = inv.IndexOf(workingDir, srcTestPerf, CompareOptions.IgnoreCase);
            return workingDir.Substring(0, idx);
        }
    }

    public string PerfDirectory => Path.Combine(RoslynDirectory, "src", "Test", "Perf");

    public string BinDirectory => Path.Combine(RoslynDirectory, "Binaries");
    
    public string BinDebugDirectory => Path.Combine(BinDirectory, "Debug");

    public string BinReleaseDirectory => Path.Combine(BinDirectory, "Release");

    public string DebugCscPath => Path.Combine(BinDebugDirectory, "csc.exe");
    
    public string ReleaseCscPath => Path.Combine(BinReleaseDirectory, "csc.exe");

    public string DebugVbcPath => Path.Combine(BinDebugDirectory, "vbc.exe");

    public string ReleaseVbcPath => Path.Combine(BinReleaseDirectory, "vbc.exe");
    
    public string CPCDirectoryPath
    {
        get {
            return Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\CPC");
        }
    }

    public string GetViBenchToJsonExeFilePath => Path.Combine(CPCDirectoryPath, "ViBenchToJson.exe");
    
    
    
    /// Downloads a zip from azure store and extracts it into
    /// the ./temp directory.
    ///
    /// If this current version has already been downloaded
    /// and extracted, do nothing.
    public void DownloadProject(string name, int version)
    {
        var zipFileName = $"{name}.{version}.zip";
        var zipPath = Path.Combine(MyTempDirectory, zipFileName);
        // If we've already downloaded the zip, assume that it
        // has been downloaded *and* extracted.
        if (File.Exists(zipPath))
        {
            Log($"Didn't download and extract {zipFileName} because one already exists.");
            return;
        }

        // Remove all .zip files that were downloaded before.
        foreach (var path in Directory.EnumerateFiles(MyTempDirectory, $"{name}.*.zip"))
        {
            Log($"Removing old zip {path}");
            File.Delete(path);
        }

        // Download zip file to temp directory
        var downloadTarget = $"https://dotnetci.blob.core.windows.net/roslyn-perf/{zipFileName}";
        Log($"Downloading {downloadTarget}");
        var client = new WebClient();
        client.DownloadFile(downloadTarget, zipPath);
        Log($"Done Downloading");

        // Extract to temp directory
        Log($"Extracting {zipPath} to {MyTempDirectory}");
        ZipFile.ExtractToDirectory(zipPath, MyTempDirectory);
        Log($"Done Extracting");
    }
}

abstract class PerfTest: RelativeDirectory {
    private List<Tuple<int, string, object>> _metrics = new List<Tuple<int, string, object>>();
    
    public PerfTest([CallerFilePath] string workingFile = ""): base(workingFile) {}
    
    /// Reports a metric to be recorded in the performance monitor.
    protected void Report(ReportKind reportKind, string description, object value)
    {
        _metrics.Add(Tuple.Create((int) reportKind, description, value));
        Log(description + ": " + value.ToString());
    }
    
    public abstract void Setup();
    public abstract void Test();
    public abstract int Iterations { get; }
    public abstract string Name { get; }
    public abstract string MeasuredProc { get; }
}

// This is a workaround for not being able to return
// arbitrary objects from a csi script while not being
// run under the runner script.
static PerfTest[] resultTests = null;

static void TestThisPlease(params PerfTest[] tests)
{
    if (IsRunFromRunner()) 
    {
        resultTests = tests;
    }
    else 
    {
        foreach (var test in tests) 
        {
            test.Setup();
            for (int i = 0; i < test.Iterations; i++) 
            {
                test.Test();
            }
        }
    }
}

//
// Arguments
//

// This is due to a design decision in csi that has Args non-static.
// Non-static variables are impossible to read inide of nested classes.
static IEnumerable<string> StaticArgs = null;
StaticArgs = Args;

/// Returns the path to log file if one exists.
/// Returns null otherwise.
static string LogFile()
{
    var key = "--log=";
    return (from arg in StaticArgs where arg.StartsWith(key) select arg.Substring(key.Length)).FirstOrDefault();
}

/// Returns true if --verbosity is passed on the command line
static bool IsVerbose()
{
    return StaticArgs.Contains("--verbose");
}

static bool IsRunFromRunner() 
{
    return StaticArgs.Contains("--from-runner");
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

/// Shells out, and if the process fails, log the error
/// and quit the script.
static void ShellOutVital(
        string file,
        string args,
        string workingDirectory = null,
        CancellationToken? cancelationToken = null)
{
    var result = ShellOut(file, args, workingDirectory, cancelationToken);
    if (result.Failed)
    {
        LogProcessResult(result);
        throw new System.Exception("ShellOutVital Failed");
    }
}

static ProcessResult ShellOut(
        string file,
        string args,
        string workingDirectory,
        CancellationToken? cancelationToken = null)
{
    var tcs = new TaskCompletionSource<ProcessResult>();
    var startInfo = new ProcessStartInfo(file, args);
    startInfo.RedirectStandardOutput = true;
    startInfo.RedirectStandardError = true;
    startInfo.UseShellExecute = false;
    startInfo.WorkingDirectory = workingDirectory;
    
    
    var process = new Process
    {
        StartInfo = startInfo,
        EnableRaisingEvents = true,
    };

    if (cancelationToken != null)
    {
        cancelationToken.Value.Register(() => process.Kill());
    }

    if (IsVerbose())
    {
        Log($"running \"{file}\" with arguments \"{args}\" from directory {workingDirectory}");
    }

    process.Start();

    var output = new StringWriter();
    var error = new StringWriter();

    process.OutputDataReceived += (s, e) => {
        if (!String.IsNullOrEmpty(e.Data))
        {
            output.WriteLine(e.Data);
        }
    };

    process.ErrorDataReceived += (s, e) => {
        if (!String.IsNullOrEmpty(e.Data))
        {
            error.WriteLine(e.Data);
        }
    };

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    return new ProcessResult {
        ExecutablePath = file,
        Args = args,
        Code = process.ExitCode,
        StdOut = output.ToString(),
        StdErr = error.ToString(),
    };
}

string StdoutFrom(string program, string args = "", string workingDirectory = null)
{
    if (workingDirectory == null) {
        var directoryInfo = new RelativeDirectory();
        workingDirectory = directoryInfo.MyTempDirectory;
    }
    
    var result = ShellOut(program, args, workingDirectory);
    if (result.Failed)
    {
        LogProcessResult(result);
        throw new Exception("Shelling out failed");
    }
    return result.StdOut.Trim();
}

//
// Timing and Testing
//

static long WalltimeMs(Action action)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    action();
    return stopwatch.ElapsedMilliseconds;
}

static long WalltimeMs<R>(Func<R> action)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    action();
    return stopwatch.ElapsedMilliseconds;
}

//
// Reporting and logging
//

enum ReportKind: int {
    CompileTime,
    RunTime,
    FileSize,
}

/// A list of
static var Metrics = new List<Tuple<int, string, object>>();

/// Logs a message.
///
/// The actual implementation of this method may change depending on
/// if the script is being run standalone or through the test runner.
static void Log(string info)
{
    System.Console.WriteLine(info);
    var log = LogFile();
    if (log != null)
    {
        File.AppendAllText(log, info + System.Environment.NewLine);
    }
}

/// Logs the result of a finished process
static void LogProcessResult(ProcessResult result)
{
    Log(String.Format("The process \"{0}\" {1} with code {2}",
        $"{result.ExecutablePath} {result.Args}",
        result.Failed ? "failed" : "succeeded",
        result.Code));
    Log($"Standard Out:\n{result.StdOut}");
    Log($"\nStandard Error:\n{result.StdErr}");
}
