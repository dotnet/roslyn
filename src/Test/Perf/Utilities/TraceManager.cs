// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Roslyn.Test.Performance.Utilities;
using System.Collections.Generic;
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

    struct WallClockRecord
    {
        public string name;
        public int duration;
    }

    public class WallClockTraceManager : ITraceManager
    {
        private List<WallClockRecord> records = new List<WallClockRecord>();

        private string currentName;
        private int currentStartTime;

        public WallClockTraceManager()
        {
        }

        public bool HasWarmUpIteration => false;

        public void Initialize()
        {
        }

        public void Cleanup()
        {
            string curName = null;
            var totalDuration = 0;
            var numRuns = 0;
            foreach (var record in records) {
                if (record.name == curName)
                {
                    totalDuration += record.duration;
                    numRuns += 1;
                }
                else
                {
                    if (curName != null)
                    {
                        Log($"{curName}: {totalDuration / numRuns}");
                    }
                    curName = record.name;
                    totalDuration = record.duration;
                    numRuns = 1;
                }
            }

            if (curName != null)
            {
                Log($"{curName}: {totalDuration / numRuns}");
            }
        }

        public void EndEvent()
        {
        }

        public void EndScenario()
        {
            records.Add(new WallClockRecord() {
                name = currentName,
                duration = System.Environment.TickCount - currentStartTime,
            });
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
            currentName = scenarioName;
            currentStartTime = System.Environment.TickCount;
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
