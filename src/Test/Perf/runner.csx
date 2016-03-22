// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load ".\util\runner_util.csx"
#load ".\util\test_util.csx"
#load ".\util\TraceManager_util.csx"

using System.Collections.Generic;
using System.IO;
using System;

InitUtilities();

var testDirectory = Path.Combine(MyWorkingDirectory(), "Tests");

var allResults = new List<Tuple<string, List<Tuple<int, string, object>>>>();
var failed = false;

var traceManager = new TraceManager();

// Print message at startup
Log("Starting Performance Test Run");
Log("hash: " + StdoutFrom("git", "show --format=\"%h\" HEAD --").Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None)[0]);
Log("time: " + DateTime.Now.ToString());

traceManager.Setup();

for(int i = 0; i < traceManager.Iterations; ++ i)
{
    traceManager.Start();
    
    // Run all the scripts that we've found and populate allResults.
    foreach (var script in GetAllCsxRecursive(testDirectory)) 
    {
        var scriptName = Path.GetFileNameWithoutExtension(script);
        Log("\nRunning " + scriptName);
        try
        {
            traceManager.StartScenario(scriptName, "csc");
            traceManager.StartEvent();
            
            var state = await RunFile(script);
            
            traceManager.EndEvent();
            traceManager.EndScenario();
            
            var metrics = (List<Tuple<int, string, object>>) state.GetVariable("Metrics").Value;
            allResults.Add(Tuple.Create(scriptName, metrics));
        }
        catch (Exception e)
        {
            Log("Test Failed: " + scriptName);
            Log(e.ToString());
            return 1;
        }
    }
    
    traceManager.EndScenarios();
    traceManager.WriteScenariosFileToDisk();

    traceManager.Stop();
    
    traceManager.ResetScenarioGenerator();
}

traceManager.Cleanup();

Log("\nALL RESULTS");

// Write to separate csv files depending on the metric
// that they are recording
var compileTimeBuilder = new System.Text.StringBuilder();
var runTimeBuilder = new System.Text.StringBuilder();
var fileSizeBuilder = new System.Text.StringBuilder();
foreach (var testResult in allResults)
{
    var test = testResult.Item1;
    var cases = testResult.Item2;
    foreach (var testCase in cases)
    {
        var reportKind = testCase.Item1;
        var caseDescription = testCase.Item2;
        var caseValue = testCase.Item3;
        System.Text.StringBuilder builder = null;

        switch((ReportKind) reportKind) {
            case ReportKind.CompileTime:
                builder = compileTimeBuilder;
                break;
            case ReportKind.RunTime:
                builder = runTimeBuilder;
                break;
            case ReportKind.FileSize:
                builder = fileSizeBuilder;
                break;
            default:
                throw new Exception("test specified an invalid report kind");
        }

        if (builder.Length != 0) {
            builder.AppendLine();
        }
        builder.Append($"{test}, {caseDescription}, {caseValue}");
    }
}

Log("Compiler Time:");
var cts = compileTimeBuilder.ToString();
File.WriteAllText(Path.Combine(MyTempDirectory(), "compiler_time.csv"), cts);
Log("\t" + cts.Replace("\n", "\n\t"));

Log("Run Time:");
var rts = runTimeBuilder.ToString();
File.WriteAllText(Path.Combine(MyTempDirectory(), "run_time.csv"), rts);
Log("\t" + rts.Replace("\n", "\n\t"));

Log("File Size:");
var fss = fileSizeBuilder.ToString();
File.WriteAllText(Path.Combine(MyTempDirectory(), "file_size.csv"), fss);
Log("\t" + fss.Replace("\n", "\n\t"));
