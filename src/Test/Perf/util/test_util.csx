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

//
// Arguments
//

/// Returns the path to log file if one exists.
/// Returns null otherwise.
string LogFile() {
    var key = "--log=";
    return(from arg in Args
    where arg.StartsWith(key)
    select arg.Substring(key.Length)).FirstOrDefault();
}

/// Returns true if --verbosity is passed on the command line
bool IsVerbose() {
    return Args.Contains("--verbose");
}

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

/// Returns the directory that you can put artifacts like
/// etl traces or compiled binaries
string MyArtifactsDirectory()
{
    var path = Path.Combine(MyWorkingDirectory(), "artifacts");
    Directory.CreateDirectory(path);
    return path;
}

string MyTempDirectory()
{
    var workingDir = MyWorkingDirectory();
    var path = Path.Combine(workingDir, "temp");
    Directory.CreateDirectory(path);
    return path;
}

string RoslynDirectory()
{
    var workingDir = MyWorkingDirectory();
    var srcTestPerf = Path.Combine("src", "Test", "Perf").ToString();
    return workingDir.Substring(0, workingDir.IndexOf(srcTestPerf));
}

string BinDirectory()
{
    return Path.Combine(RoslynDirectory(), "Binaries");
}

string PerfTestDirectory()
{
    return Path.Combine(RoslynDirectory(), "src", "Test", "Perf", "Tests");
}

string BinDebugDirectory()
{
    return Path.Combine(BinDirectory(), "Debug");
}

string BinReleaseDirectory()
{
    return Path.Combine(BinDirectory(), "Release");
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

string GetCPCDirectoryPath()
{
    return Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\CPC");
}

string GetCPCSourceBinaryLocation()
{
    return $@"\\mlangfs1\public\basoundr\CpcBinaries";
}

//
// Process spawning and error handling.
//

public class ProcessResult
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
void ShellOutVital(
        string file,
        string args,
        string workingDirectory = null,
        CancellationToken? cancelationToken = null) {
    var result = ShellOut(file, args, workingDirectory, cancelationToken);
    if (result.Failed) {
        LogProcessResult(result);
        throw new System.Exception("ShellOutVital Failed");
    }
}

ProcessResult ShellOut(
        string file,
        string args,
        string workingDirectory = null,
        CancellationToken? cancelationToken = null)
{
    if (workingDirectory == null) {
        workingDirectory = MyWorkingDirectory();
    }

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

    if (cancelationToken != null) {
        cancelationToken.Value.Register(() => process.Kill());
    }

    if (IsVerbose()) {
        Log($"running \"{file}\" with arguments \"{args}\" from directory {workingDirectory}");
    }
    
    process.Start();

    var output = new StringWriter();
    var error = new StringWriter();

    process.OutputDataReceived += (s, e) => {
        if (!String.IsNullOrEmpty(e.Data)) {
            output.WriteLine(e.Data);
        }
    };

    process.ErrorDataReceived += (s, e) => {
        if (!String.IsNullOrEmpty(e.Data)) {
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

ProcessResult ShellOutUsingShellExecute(
        string file,
        string args,
        string workingDirectory = null,
        CancellationToken? cancelationToken = null)
{
    var tcs = new TaskCompletionSource<ProcessResult>();
    var startInfo = new ProcessStartInfo(file, args);
    startInfo.UseShellExecute = true;
    var process = new Process
    {
        StartInfo = startInfo
    };

    if (cancelationToken != null) {
        cancelationToken.Value.Register(() => process.Kill());
    }
    
    if (IsVerbose()) {
        Log($"running \"{file}\" with arguments \"{args}\" from directory {workingDirectory}");
    }
    
    process.Start();

    process.WaitForExit();

    return new ProcessResult {
        ExecutablePath = file,
        Args = args,
        Code = 0,
        StdOut = "",
        StdErr = "",
    };
}

void CopyDirectory(string source, string destination)
{
    var result = ShellOutUsingShellExecute("Robocopy", $"/mir {source} {destination}");
    if (!result.Succeeded)
    {
        throw new IOException($"Failed to copy \"{source}\" to \"{destination}\".");
    }
}

string StdoutFrom(string program, string args = "") {
    var result = ShellOut(program, args);
    if (result.Failed) {
        LogProcessResult(result);
        throw new Exception("Shelling out failed");
    }
    return result.StdOut.Trim();
}

//
// Timing and Testing
//

long WalltimeMs(Action action)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    action();
    return stopwatch.ElapsedMilliseconds;
}

long WalltimeMs<R>(Func<R> action)
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
var Metrics = new List<Tuple<int, string, object>>();

/// Logs a message.
///
/// The actual implementation of this method may change depending on
/// if the script is being run standalone or through the test runner.
void Log(string info)
{
    System.Console.WriteLine(info);
    var log = LogFile();
    if (log != null) {
        File.AppendAllText(log, info + System.Environment.NewLine);
    }
}

/// Logs the result of a finished process
void LogProcessResult(ProcessResult result) {
    Log(String.Format("The process \"{0}\" {1} with code {2}",
        $"{result.ExecutablePath} {result.Args}",
        result.Failed ? "failed" : "succeeded",
        result.Code));
    Log($"Standard Out:\n{result.StdOut}");
    Log($"\nStandard Error:\n{result.StdErr}");
}

/// Reports a metric to be recorded in the performance monitor.
void Report(ReportKind reportKind, string description, object value)
{
    Metrics.Add(Tuple.Create((int) reportKind, description, value));
    Log(description + ": " + value.ToString());
}

/// Downloads a zip from azure store and extracts it into
/// the ./temp directory.
///
/// If this current version has already been downloaded
/// and extracted, do nothing.
void DownloadProject(string name, int version) {
    var zipFileName = $"{name}.{version}.zip";
    var zipPath = Path.Combine(MyTempDirectory(), zipFileName);
    // If we've already downloaded the zip, assume that it
    // has been downloaded *and* extracted.
    if (File.Exists(zipPath))
    {
        Log($"Didn't download and extract {zipFileName} because one already exists.");
        return;
    }

    // Remove all .zip files that were downloaded before.
    foreach (var path in Directory.EnumerateFiles(MyTempDirectory(), $"{name}.*.zip")) {
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
    Log($"Extracting {zipPath} to {MyTempDirectory()}");
    ZipFile.ExtractToDirectory(zipPath, MyTempDirectory());
    Log($"Done Extracting");
}
