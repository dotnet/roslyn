// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#r "System.IO.Compression.FileSystem"

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;

void CopyDirectory(string source, string destination, string argument = @"/mir")
{
    var result = ShellOut("Robocopy", $"{argument} {source} {destination}", "");

    // Robocopy has a success exit code from 0 - 7
    if (result.Code > 7)
    {
        throw new IOException($"Failed to copy \"{source}\" to \"{destination}\".");
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
    public string ExecutablePath { get; set; }
    public string Args { get; set; }
    public int Code { get; set; }
    public string StdOut { get; set; }
    public string StdErr { get; set; }

    public bool Failed => Code != 0;
    public bool Succeeded => !Failed;
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

    return new ProcessResult
    {
        ExecutablePath = file,
        Args = args,
        Code = process.ExitCode,
        StdOut = output.ToString(),
        StdErr = error.ToString(),
    };
}

string StdoutFrom(string program, string args = "", string workingDirectory = "")
{
    var result = ShellOut(program, args, workingDirectory);
    if (result.Failed)
    {
        LogProcessResult(result);
        throw new Exception("Shelling out failed");
    }
    return result.StdOut.Trim();
}

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
