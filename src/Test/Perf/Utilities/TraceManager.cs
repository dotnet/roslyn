// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Performance.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

namespace Roslyn.Test.Performance.Utilities
{
    public class TraceManagerFactory
    {
        public static ITraceManager GetBestTraceManager()
        {
            var cpcFullPath = Path.Combine(TestUtilities.GetCPCDirectoryPath(), "CPC.exe");
            if (File.Exists(cpcFullPath))
            {
                Log("Found CPC, using it for trace collection");
                return new TraceManager(cpcFullPath);
            }
            else
            {
                Log($"WARNING: Could not find CPC at {cpcFullPath} (no traces will be collected)");
                return new WallClockTraceManager();
            }
        }

        public static ITraceManager NoOpTraceManager()
        {
            return new WallClockTraceManager();
        }
    }

    public class TraceManager : ITraceManager
    {
        private ScenarioGenerator _scenarioGenerator;
        private readonly string _cpcPath;

        private int _startEventAbsoluteInstance = 1;
        private int _stopEventAbsoluteInstance = 1;

        public TraceManager(string cpcPath) : base()
        {
            _cpcPath = cpcPath;
            _scenarioGenerator = new ScenarioGenerator();
        }

        public bool HasWarmUpIteration => true;

        public void Setup()
        {
            ShellOutVital(_cpcPath, "/Setup /SkipClean", workingDirectory: TestUtilities.GetCPCDirectoryPath());
        }

        public void Start()
        {
            ShellOutVital(_cpcPath, "/Start /SkipClean", workingDirectory: TestUtilities.GetCPCDirectoryPath());
        }

        public void Stop()
        {
            var scenariosXmlPath = Path.Combine(GetCPCDirectoryPath(), "scenarios.xml");
            var consumptionTempResultsPath = Path.Combine(GetCPCDirectoryPath(), "ConsumptionTempResults.xml");
            ShellOutVital(_cpcPath, $"/Stop /SkipClean /ScenarioPath=\"{scenariosXmlPath}\" /ConsumptionTempResultsPath=\"{consumptionTempResultsPath}\"", workingDirectory: TestUtilities.GetCPCDirectoryPath());
        }

        public void Cleanup()
        {
            ShellOutVital(_cpcPath, "/Cleanup /SkipClean", workingDirectory: TestUtilities.GetCPCDirectoryPath());
        }

        public void StartScenarios()
        {
            _scenarioGenerator.AddScenariosFileStart();
        }

        public void StartScenario(string scenarioName, string processName)
        {
            _scenarioGenerator.AddStartScenario(scenarioName, processName);
        }

        public void StartEvent()
        {
            _scenarioGenerator.AddStartEvent(_startEventAbsoluteInstance);
            _startEventAbsoluteInstance++;
        }

        public void EndEvent()
        {
            _scenarioGenerator.AddEndEvent();
            _stopEventAbsoluteInstance++;
        }

        public void EndScenario()
        {
            _scenarioGenerator.AddEndScenario();
        }

        public void EndScenarios()
        {
            _scenarioGenerator.AddScenariosFileEnd();
        }

        public void WriteScenarios(string[] scenarios)
        {
            foreach (var line in scenarios)
            {
                _scenarioGenerator.AddLine(line);
            }
        }

        public void WriteScenariosFileToDisk()
        {
            _scenarioGenerator.WriteToDisk();
        }

        public void ResetScenarioGenerator()
        {
            _scenarioGenerator = new ScenarioGenerator();
            _startEventAbsoluteInstance = 1;
            _stopEventAbsoluteInstance = 1;
        }
    }

    public class WallClockTraceManager : ITraceManager
    {
        private readonly List<long> _durations = [];
        private string _testName = "";

        private Stopwatch _stopwatch;

        public WallClockTraceManager()
        {
        }

        public bool HasWarmUpIteration => false;

        public void Initialize()
        {
        }

        // We have one WallClockTraceManager per test, so we don't
        // need to worry about other tests showing up
        public void Cleanup()
        {
            var totalDuration = _durations.Sum(v => v);
            var average = _durations.Count != 0 ? totalDuration / _durations.Count : 0;
            var allString = string.Join(",", _durations);

            Log($"Wallclock times for {_testName}");
            Log($"ALL: [{allString}]");
            Log($"AVERAGE: {average}");
        }

        public void EndEvent()
        {
        }

        public void EndScenario()
        {
            _stopwatch.Stop();
            _durations.Add(_stopwatch.ElapsedMilliseconds);
        }

        public void EndScenarios()
        {
        }

        public void ResetScenarioGenerator()
        {
        }

        public void Setup()
        {
        }

        public void Start()
        {
        }

        public void StartEvent()
        {
        }

        public void StartScenarios()
        {
        }

        public void StartScenario(string scenarioName, string processName)
        {
            _testName = scenarioName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Stop()
        {
        }

        public void WriteScenarios(string[] scenarios)
        {
        }

        public void WriteScenariosFileToDisk()
        {
        }
    }
}
