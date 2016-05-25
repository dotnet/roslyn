// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#r "./../Roslyn.Test.Performance.Utilities.dll"

// RunFile()
// GetAllCsxRecursive()
#load "./util/runner_util.csx"
// Log()
// StdoutFrom()
// IsVerbose()
#load "./util/tools_util.csx"

using System.Collections.Generic;
using System.IO;
using System;
using Roslyn.Test.Performance.Utilities;

var directoryInfo = new RelativeDirectory();
var testDirectory = Path.Combine(directoryInfo.MyWorkingDirectory, "tests");

// Print message at startup
Log("Starting Performance Test Run");
Log("hash: " + StdoutFrom("git", "show --format=\"%h\" HEAD --").Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None)[0]);
Log("time: " + DateTime.Now.ToString());

var testInstances = new List<PerfTest>();

// Find all the tests from inside of the csx files.
foreach (var script in GetAllCsxRecursive(testDirectory))
{
    var scriptName = Path.GetFileNameWithoutExtension(script);
    Log($"Collecting tests from {scriptName}");
    var state = await RunFile(script);
    var tests = (PerfTest[]) state.GetVariable("resultTests").Value;
    testInstances.AddRange(tests);
}

var traceManager = TraceManagerFactory.GetTraceManager(IsVerbose());

traceManager.Initialize();
foreach (var test in testInstances)
{
    test.Setup();
    traceManager.Setup();
    
    var iterations = traceManager.HasWarmUpIteration ? 
                     test.Iterations + 1 :
                     test.Iterations;
                     
    for (int i = 0; i < iterations; i++)
    {
        traceManager.Start();
        traceManager.StartScenarios();
        
        if (test.ProvidesScenarios)
        {
            traceManager.WriteScenarios(test.GetScenarios());
            test.Test();
        }
        else
        {
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
}
