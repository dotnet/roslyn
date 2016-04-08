// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "./util/runner_util.csx"
#load "./util/test_util.csx"
#load "./util/trace_manager_util.csx"

using System.Collections.Generic;
using System.IO;
using System;

var directoryInfo = new RelativeDirectory();
var testDirectory = Path.Combine(directoryInfo.MyWorkingDirectory, "Tests");

// Print message at startup
Log("Starting Performance Test Run");
Log("hash: " + StdoutFrom("git", "show --format=\"%h\" HEAD --").Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None)[0]);
Log("time: " + DateTime.Now.ToString());

var testInstances = new List<dynamic>();

// Find all the tests from inside of the csx files.
foreach (var script in GetAllCsxRecursive(testDirectory))
{
    var scriptName = Path.GetFileNameWithoutExtension(script);
    Log($"Collecting tests from {scriptName}");
    var tests = (object[]) (await RunFile(script)).ReturnValue;
    testInstances.AddRange(tests);
}

var traceManager = TraceManagerFactory.GetTraceManager();
traceManager.Setup();
for (int i = 0; i < traceManager.Iterations; i++)
{
    traceManager.Start();
    foreach (dynamic test in testInstances)
    {
        test.Setup();
        traceManager.StartScenario(test.Name, test.MeasuredProc);
        traceManager.StartEvent();
        test.Test();
        traceManager.EndEvent();
        traceManager.EndScenario();
    }
    traceManager.EndScenarios();
    traceManager.WriteScenariosFileToDisk();
    traceManager.Stop();
    traceManager.ResetScenarioGenerator();
}
traceManager.Cleanup();

/*
var traceManager = TraceManagerFactory.GetTraceManager();
traceManager.Setup();
// Run each of the tests
foreach (dynamic test in testInstances) 
{
    test.Setup();
    traceManager.Start();
    for (int i = 0; i < test.Iterations; i++) 
    {
        traceManager.StartScenario("temp" + i, "csc");
        traceManager.StartEvent();
        test.Test();
        traceManager.EndEvent();
        traceManager.EndScenario();
    }
    traceManager.EndScenarios();
    traceManager.WriteScenariosFileToDisk();
    traceManager.Stop();
    traceManager.Cleanup();
}
*/