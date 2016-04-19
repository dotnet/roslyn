// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "./util/runner_util.csx"
#load "./util/test_util.csx"
#load "./util/trace_manager_util.csx"

using System.Collections.Generic;
using System.IO;
using System;

var directoryInfo = new RelativeDirectory();
var testDirectory = Path.Combine(directoryInfo.MyWorkingDirectory, "tests");

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
    var state = await RunFile(script);
    object[] tests = (object[]) state.GetVariable("resultTests").Value;
    testInstances.AddRange(tests);
}

var traceManager = TraceManagerFactory.GetTraceManager();
traceManager.Initialize();
foreach (dynamic test in testInstances)
{
    test.Setup();
    traceManager.Setup();
    
    var iterations = traceManager.HasWarmUpIteration ? 
                     test.Iterations + 1 :
                     test.Iterations;
                     
    for (int i = 0; i < iterations; i++)
    {
        traceManager.StartScenarios();
        traceManager.Start();
        traceManager.StartScenario(test.Name + i, test.MeasuredProc);
        traceManager.StartEvent();
        test.Test();
        traceManager.EndEvent();
        traceManager.EndScenario();
        
        traceManager.EndScenarios();
        traceManager.WriteScenariosFileToDisk();
        traceManager.Stop();
        traceManager.ResetScenarioGenerator();
    }

    traceManager.Cleanup();
}
